using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Meta.WitAi;
using Meta.WitAi.Events;
using Meta.WitAi.Requests;
using Meta.WitAi.Json;
using Oculus.Voice;
using TMPro;

public class WitSpeechHandler: MonoBehaviour
{
    [Header("Refs")]
    public AppVoiceExperience app;

    [Header("Rules")]
    public float perUtteranceSilenceSeconds = 2f;   // 一句话 2 秒静音切断
    public float sessionIdleTimeoutSeconds = 10f;  // 会话 10 秒无人说话切断
    public float watchdogTimeoutSeconds = 9f;      // 底层 10s 前主动重启

    [Header("Events")]
    public UnityEvent<string> OnFinalUtterance;
    public UnityEvent OnSessionEnded;

    // === 内部状态 ===
    private bool _sessionActive = false;   // 整个会话中
    private bool _isListening = false;     // 当前是否处于一句话输入中
    private float _lastSpeechTime = 0f;
    private string _lastPartial = null;

    // ==声音提示==
    public AudioSource CharAudio;
    public AudioClip EndSessionClip;

    // timers
    private Coroutine _utteranceSilenceCo, _sessionIdleCo, _watchdogCo;

    //===导航功能===
    public NavPoint navuser;
    public NavPointSystem navSystem;

    // ==== Unity lifecycle ====
    private void OnEnable()
    {
        if (!app) app = GetComponent<AppVoiceExperience>();

        app.VoiceEvents.OnPartialTranscription.AddListener(OnPartial);
        app.VoiceEvents.OnFullTranscription.AddListener(OnFull);
        app.VoiceEvents.OnError.AddListener(OnError);
    }

    private void OnDisable()
    {
        if (app != null)
        {
            app.VoiceEvents.OnPartialTranscription.RemoveListener(OnPartial);
            app.VoiceEvents.OnFullTranscription.RemoveListener(OnFull);
            app.VoiceEvents.OnError.RemoveListener(OnError);
        }
        HardStopAll();
    }

    void Awake()
    {
        OnFinalUtterance.AddListener((text) =>
        {
            LogBuffer.Log("Get Speech:" + text);
            FindObjectOfType<GPTClient>().SendMessageToGPT(text);
            //navSystem.GoToPointByNavPoint(navuser);

        });
        OnSessionEnded.AddListener(() =>
        {
            CharAudio.PlayOneShot(EndSessionClip);
        });
    }

    private void Start()
    {
        StartSession();
    }

    // ==== 外部接口 ====
    public void StartSession()
    {
        // 无论当前什么状态 → 全部中断重开
        HardStopAll();

        _sessionActive = true;
        _isListening = true;
        _lastSpeechTime = Time.time;

        app.Activate();
        StartUtteranceSilenceTimer();
        StartSessionIdleTimer();
        StartWatchdog();

        Debug.Log("[VS] Session started");
        LogBuffer.Log("[VS] Session started");
    }

    public void EndSession()
    {
        if (!_sessionActive) return;

        app.Deactivate();
        _sessionActive = false;
        _isListening = false;

        StopAllTimers();

        OnSessionEnded?.Invoke();
        Debug.Log("[VS] Session ended");
        LogBuffer.Log("[VS] Session ended");
    }

    // ==== 内部事件回调 ====
    private void OnPartial(string text)
    {
        if (!_sessionActive) return;
        if (string.IsNullOrWhiteSpace(text)) return;

        _lastSpeechTime = Time.time;
        _lastPartial = text;
    }

    private void OnFull(string text)
    {
        if (!_sessionActive) return;

        var final = string.IsNullOrEmpty(text) ? _lastPartial : text;
        if (!string.IsNullOrEmpty(final))
        {
            OnFinalUtterance?.Invoke(final);
            _lastPartial = null;
        }

        // 一句话结束 → 等待下一句
        _isListening = false;
        StartSessionIdleTimer();
    }

    private void OnError(string code, string message)
    {
        Debug.LogError($"[VS] Wit Error {code}: {message}");
        LogBuffer.Log($"[VS] Wit Error {code}: {message}");
        // 防止底层报错后状态没清
        EndSession();
    }

    // ==== 定时器逻辑 ====

    // ① 2 秒静音 → 结束当前 utterance
    private void StartUtteranceSilenceTimer()
    {
        StopUtteranceSilenceTimer();
        _utteranceSilenceCo = StartCoroutine(UtteranceSilenceTimerCo());
    }

    private void StopUtteranceSilenceTimer()
    {
        if (_utteranceSilenceCo != null) StopCoroutine(_utteranceSilenceCo);
        _utteranceSilenceCo = null;
    }

    private IEnumerator UtteranceSilenceTimerCo()
    {
        while (_sessionActive && _isListening)
        {
            if (Time.time - _lastSpeechTime >= perUtteranceSilenceSeconds)
            {
                // 逻辑上结束一句
                _isListening = false;
                StartSessionIdleTimer();
                yield break;
            }
            yield return null;
        }
    }

    // ② 10 秒无人说话 → 结束整个会话
    private void StartSessionIdleTimer()
    {
        StopSessionIdleTimer();
        _sessionIdleCo = StartCoroutine(SessionIdleTimerCo());
    }

    private void StopSessionIdleTimer()
    {
        if (_sessionIdleCo != null) StopCoroutine(_sessionIdleCo);
        _sessionIdleCo = null;
    }

    private IEnumerator SessionIdleTimerCo()
    {
        while (_sessionActive && !_isListening)
        {
            if (Time.time - _lastSpeechTime >= sessionIdleTimeoutSeconds)
            {
                EndSession();
                yield break;
            }
            yield return null;
        }
    }

    // ③ Watchdog: 底层超时保护
    private void StartWatchdog()
    {
        StopWatchdog();
        _watchdogCo = StartCoroutine(WatchdogCo());
    }

    private void StopWatchdog()
    {
        if (_watchdogCo != null) StopCoroutine(_watchdogCo);
        _watchdogCo = null;
    }

    private IEnumerator WatchdogCo()
    {
        while (_sessionActive)
        {
            yield return new WaitForSeconds(watchdogTimeoutSeconds);

            if (_sessionActive)
            {
                Debug.Log("[VS] Watchdog restart to avoid Wit timeout");
                //LogBuffer.Log("[VS] Watchdog restart to avoid Wit timeout");
                app.Deactivate();
                yield return new WaitForSeconds(0.1f);
                app.Activate();

                // 重启后续计时器
                _lastSpeechTime = Time.time;
                StartUtteranceSilenceTimer();
            }
        }
    }

    // ==== 收尾 ====
    private void HardStopAll()
    {
        StopAllTimers();
        if (app != null && app.Active) app.Deactivate();
        _sessionActive = false;
        _isListening = false;
        _lastPartial = null;
    }

    private void StopAllTimers()
    {
        StopUtteranceSilenceTimer();
        StopSessionIdleTimer();
        StopWatchdog();
    }
}
