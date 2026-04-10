using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Meta.WitAi;
using Meta.WitAi.Events;
using Oculus.Voice;
using Meta.WitAi.Data.Configuration;

public class WitSimpleSpeechHandler : MonoBehaviour
{
    [Header("Refs")]
    public AppVoiceExperience app;

    [Header("Events")]
    public UnityEvent<string> OnFinalUtterance;
    public UnityEvent OnSessionEnded;

    // ==声音提示==
    public AudioSource CharAudio;
    public AudioClip EndSessionClip;

    // === 内部状态 ===
    private bool _sessionActive = false;
    private string _lastPartial = null;

    // Watchdog
    private float watchdogTimeoutSeconds = 9f;
    private Coroutine _watchdogCo;

    // ==== Unity lifecycle ====
    private void OnEnable()
    {
        if (!app) app = GetComponent<AppVoiceExperience>();

        app.VoiceEvents.OnPartialTranscription.AddListener(OnPartial);
        app.VoiceEvents.OnFullTranscription.AddListener(OnFull);
        app.VoiceEvents.OnError.AddListener(OnError);

        WitConfiguration configuration = app.RuntimeConfiguration.witConfiguration;
        configuration.RequestTimeoutMs = 30000; // 默认 10000ms, 改成 30s
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
            Debug.Log("[VS] Get Speech:" + text);
            FindObjectOfType<GPTClient>().SendMessageToGPT(text);
        });
        OnSessionEnded.AddListener(() =>
        {
            if (CharAudio && EndSessionClip)
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
        HardStopAll();

        _sessionActive = true;
        app.Activate();

        StartWatchdog();

        Debug.Log("[VS] Session started");
    }

    public void EndSession()
    {
        if (!_sessionActive) return;

        app.Deactivate();
        _sessionActive = false;

        StopWatchdog();

        OnSessionEnded?.Invoke();
        Debug.Log("[VS] Session ended");
    }

    // ==== 内部事件回调 ====
    private void OnPartial(string text)
    {
        if (!_sessionActive) return;
        if (string.IsNullOrWhiteSpace(text)) return;

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
    }

    private void OnError(string code, string message)
    {
        Debug.LogError($"[VS] Wit Error {code}: {message}");
        StartSession();
    }

    // ==== Watchdog: 底层超时保护 ====
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
                app.Deactivate();
                yield return new WaitForSeconds(0.1f);
                app.Activate();
            }
        }
    }

    private IEnumerator ShortRestart()
    {
        Debug.Log("[VS] Restart to avoid Wit timeout");
        app.Deactivate();
        yield return new WaitForSeconds(1f);
        app.Activate();
    }
    // ==== 收尾 ====
    private void HardStopAll()
    {
        StopWatchdog();
        if (app != null && app.Active) app.Deactivate();
        _sessionActive = false;
        _lastPartial = null;
    }
}
