using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Meta.WitAi;
using Meta.WitAi.Events;
using Meta.WitAi.Requests;
using Meta.WitAi.Json;
using Oculus.Voice;
using TMPro;

/// 极简会话版：StartSession() 开始 -> 2 秒静音产生一次最终文本 -> 10 秒无声结束会话
public class VoiceStarter: MonoBehaviour
{
    [Header("Refs")]
    public AppVoiceExperience app; // 拖 AppVoiceExperience

    [Header("Rules")]
    [Tooltip("一次输入的静音阈值（秒）。达到则判定本次输入完成。")]
    public float perUtteranceSilenceSeconds = 2f;
    [Tooltip("会话空窗（秒）。本次输入结束后，在此时间内没人再说话则整个会话结束。")]
    public float sessionIdleTimeoutSeconds = 5f;
    [Tooltip("探测“有人开口”的短监听窗（秒），会话内轻量轮询以便自动再起一轮识别。")]
    public float sniffListenWindow = 1.2f;
    [Tooltip("两次探测之间的间隔（秒）。")]
    public float sniffOffGap = 0.3f;

    [Header("Events")]
    public UnityEvent<string> OnFinalUtterance; // 每条输入的最终文本
    public UnityEvent OnSessionEnded;           // 整个会话结束

    // state
    private bool _sessionActive = false;
    public bool _isListening = false;
    private float _lastSpeechTime = 0f;
    private string _lastPartial = null;
    private bool _hadSpeechThisActivation = false;
    private bool _hasSpokenInThisUtterance = false; // 本次输入是否已出现过 Partial

    // timers
    private Coroutine _utteranceSilenceCo, _sessionIdleCo, _sniffCo;

    // text field
    public TMP_InputField inputField;

    //Audio Source
    public AudioSource charAudioSource;
    public AudioClip endSpeechClip;

    // IfAble
    UnityAction<VoiceServiceRequest> _onReqInit;
    UnityAction _onMicStart, _onMicStop;
    UnityAction<string> _onPartial, _onFull;
    UnityAction<WitResponseNode> _onResponse;
    UnityAction<string, string> _onError;

    // 新增：用于忽略激活瞬间的假阳性 partial
    [SerializeField] private float partialArmDelay = 0.2f; // 200ms
    private float _activatedAt = -999f;
    [SerializeField] private int minCharsToTrigger = 2;

    private void Awake()
    {
        if (!app) app = GetComponent<AppVoiceExperience>();
    }

    private void Start()
    {
        OnFinalUtterance.AddListener(text => {
            inputField.text = text;
            LogBuffer.Log("Catch speech:"+text);
            FindObjectOfType<GPTClient>().SendMessageToGPT(text);
            StartUtteranceSilenceTimer();
        });
        OnSessionEnded.AddListener(() => {
            LogBuffer.Log("Speech Session Ended");
            //FindObjectOfType<WakeWordDetector>().EnableWakeWordDetection();
        });

    }

    private void OnEnable()
    {
        if (!app)
        {
            Debug.LogError("[VoiceStarter] AppVoiceExperience missing.");
            LogBuffer.Log("[VoiceStarter] AppVoiceExperience missing.");
            return;
        }

        // 1) 定义回调（全部用命名委托，避免匿名lambda难以解绑）
        _onReqInit = (req) => { /* 可选：按需记录req */ };

        _onMicStart = () =>
        {
            // 可选：若需要，开始计时等
        };

        _onMicStop = () =>
        {
            StopUtteranceSilenceTimer();
        };

        _onPartial = (t) =>
        {
            // —— 过滤：空白/激活瞬间/太短文本 ——
            var s = t?.Trim();
            if (string.IsNullOrEmpty(s)) return;

            // 过滤：忽略激活瞬间的假阳性
            if (Time.time - _activatedAt < partialArmDelay) return;

            // 过滤：忽略过短文本（比如单个字母/噪声）
            //if (s.Length < minCharsToTrigger) return;

            _lastSpeechTime = Time.time;
            _lastPartial = t;
            _hadSpeechThisActivation = true;
            
            LogBuffer.Log("[VS] PASS Partial: " + t);
        };

        _onFull = (t) =>
        {
            if (!_sessionActive) return; // 非会话期一律忽略
            var final = string.IsNullOrEmpty(t) ? _lastPartial : t;
            if (!string.IsNullOrEmpty(final))
            {

                OnFinalUtterance?.Invoke(final);
                _lastPartial = null;
            }
        };

        _onResponse = (r) =>
        {
            if (!_sessionActive) return; // 非会话期忽略
                                         // 可选兜底解析：保持你的实现或留空
                                         // string text = r?["text"]?.Value ?? r?["transcription"]?.Value
                                         //              ?? r?["speech"]?["transcription"]?.Value;
                                         // if (!string.IsNullOrEmpty(text)) { /* 通常不在此触发，避免双发 */ }
        };

        _onError = (code, msg) =>
        {
            Debug.LogError($"[VoiceStarter] Wit Error {code}: {msg}");
            LogBuffer.Log($"[VoiceStarter] Wit Error {code}: {msg}");
            if (_isListening) StopOneUtterance();
            EnsureSessionIdleTimer();
        };

        // 2) 订阅（只订一次）
        var ve = app.VoiceEvents;
        ve.OnRequestInitialized.AddListener(_onReqInit);
        ve.OnMicStartedListening.AddListener(_onMicStart);
        ve.OnMicStoppedListening.AddListener(_onMicStop);
        ve.OnPartialTranscription.AddListener(_onPartial);
        ve.OnFullTranscription.AddListener(_onFull);
        ve.OnResponse.AddListener(_onResponse);
        ve.OnError.AddListener(_onError);
    }


    private void OnDisable()
    {
        if (app != null)
        {
            app.VoiceEvents.OnRequestInitialized.RemoveListener(_onReqInit);
            app.VoiceEvents.OnMicStartedListening.RemoveListener(_onMicStart);
            app.VoiceEvents.OnMicStoppedListening.RemoveListener(_onMicStop);
            app.VoiceEvents.OnPartialTranscription.RemoveListener(_onPartial);
            app.VoiceEvents.OnFullTranscription.RemoveListener(_onFull);
            app.VoiceEvents.OnResponse.RemoveListener(_onResponse);
            app.VoiceEvents.OnError.RemoveListener(_onError);
        }
        HardStopAll();
    }

    // ===== 外部只需要这两个方法 =====
    public void StartSession()
    {
        //MicCheck();
        EnsureSessionEnded();
        LogBuffer.Log("Speech Session Starting");
        if (_sessionActive) return;
        _sessionActive = true;

        StartOneUtterance();         // 立刻开始第一轮
        //EnsureSessionIdleTimer();    // 开启 10s 会话空窗计时
    }



    public void CancelSession()
    {
        EndSession();
    }

    // ===== 一次输入（utterance）开始/结束 =====
    private void StartOneUtterance()
    {
        if (app == null) return;

        // 若上一条还没收尾，先关掉（避免“开太久才报错后恢复”的现象）
        //if (app.Active || app.IsRequestActive || app.MicActive)
        //    app.Deactivate();

        if (!app.Active)
        {
            _hadSpeechThisActivation = false;
            app.Activate();
        }
        PromoteToFullListening();
        StartUtteranceSilenceTimer();
        //StartSniffIfNeeded();
    }

    private void PromoteToFullListening()
    {
        LogBuffer.Log("Speech Utterance Starting");
        if (_isListening) return;
        _isListening = true;
        _lastSpeechTime = Time.time;
        StartUtteranceSilenceTimer();
    }

    private void StopOneUtterance()
    {
        LogBuffer.Log("Speech Utterance Stopped");
        if (app != null && app.Active) app.Deactivate();
        _isListening = false;
        StopUtteranceSilenceTimer();
        LogBuffer.Log("Speech Idle Started");
        EnsureSessionIdleTimer(); // 停完一条后进入会话空窗（等待下一次说话）
        StartSniffIfNeeded();     // 会话窗口内启动轻量探测
    }

    // ===== 会话结束 =====
    public void EndSession()
    {
        

        _isListening = false;
        _sessionActive = false;

        // 停掉所有定时器
        LogBuffer.Log("Speech Idle Timer Stopped.");
        StopSessionIdleTimer();
        StopUtteranceSilenceTimer();

        // 停掉 sniff 模式
        StopSniff();

        OnSessionEnded?.Invoke();

        LogBuffer.Log("Speech session ended.");

        charAudioSource.PlayOneShot(endSpeechClip);//提示结束对话
    }

    public void EnsureSessionEnded()
    {

        _isListening = false;
        _sessionActive = false;

        // 停掉所有定时器
        LogBuffer.Log("Speech Idle Timer Stopped.");
        StopSessionIdleTimer();
        StopUtteranceSilenceTimer();

        // 停掉 sniff 模式
        StopSniff();

        LogBuffer.Log("Speech session ensured.");
    }

    private void HardStopAll()
    {
        StopSessionIdleTimer();
        StopSniff();
        StopUtteranceSilenceTimer();
        if (app != null && app.Active) app.Deactivate();
        _isListening = false;
        _sessionActive = false;
        _lastPartial = null;
    }

    // ===== 2 秒静音 -> 结束本次输入 =====
    private void StartUtteranceSilenceTimer()
    {
        StopUtteranceSilenceTimer();
        if (perUtteranceSilenceSeconds > 0f)
            _utteranceSilenceCo = StartCoroutine(UtteranceSilenceTimer());
    }
    private void ResetUtteranceSilenceTimer()
    {
        if (_utteranceSilenceCo != null) { StopCoroutine(_utteranceSilenceCo); _utteranceSilenceCo = null; }
        if (_isListening && perUtteranceSilenceSeconds > 0f)
            _utteranceSilenceCo = StartCoroutine(UtteranceSilenceTimer());
    }
    private void StopUtteranceSilenceTimer()
    {
        if (_utteranceSilenceCo != null) { StopCoroutine(_utteranceSilenceCo); _utteranceSilenceCo = null; }
    }
    private IEnumerator UtteranceSilenceTimer()
    {
        LogBuffer.Log("Speech Utterance Timer Counting");
        while (_isListening)
        {
            if (Time.time - _lastSpeechTime >= perUtteranceSilenceSeconds)
            {
                StopOneUtterance(); // 结束本次输入（最终文本会在 Full/Response 里触发）
                yield break;
            }
            yield return null;
        }
    }

    // ===== 10 秒会话空窗 -> 会话结束 =====
    private void EnsureSessionIdleTimer()
    {
        if (!_sessionActive) return;
        StopSessionIdleTimer();
        LogBuffer.Log("Speech Idle Timer Counting");
        _sessionIdleCo = StartCoroutine(SessionIdleTimer());
    }
    private void StopSessionIdleTimer()
    {
        if (_sessionIdleCo != null) { StopCoroutine(_sessionIdleCo); _sessionIdleCo = null; }
    }
    private IEnumerator SessionIdleTimer()
    {
        while (_sessionActive)
        {
            if (_isListening) yield break; // 一旦重新开始监听就退出
            if (Time.time - _lastSpeechTime >= sessionIdleTimeoutSeconds)
            {
                EndSession();               // ★ 只结束，不做别的
                yield break;
            }
            yield return null;
        }
    }

    // ===== 会话内自动再起：轻量探测（短开短关） =====
    private void StartSniffIfNeeded()
    {
        LogBuffer.Log("Speech Sniff Started");
        if (!_sessionActive || _sniffCo != null || _isListening) return;
        _sniffCo = StartCoroutine(SniffLoop());
    }
    private void StopSniff()
    {
        LogBuffer.Log("Speech Sniff Stopped");
        if (_sniffCo != null) { StopCoroutine(_sniffCo); _sniffCo = null; }
        app.Deactivate(); 
    }
    private IEnumerator SniffLoop()
    {
        while (_sessionActive && !_isListening)
        {
            // 保证状态干净
            _hadSpeechThisActivation = false;

            // 打开一次短监听
            if (app != null && !app.Active)
            {
                app.Activate();
                _activatedAt = Time.time;  // 用已有字段记录激活时间
            }

            float start = Time.time;
            bool gotSpeech = false;

            // 在 sniffListenWindow 窗口里等是否有人开口
            while (Time.time - start < sniffListenWindow)
            {
                if (_hadSpeechThisActivation)
                {
                    gotSpeech = true;
                    break;
                }
                yield return null;
            }

            if (gotSpeech)
            {
                // 成功探测到 → 转正为 full listening
                PromoteToFullListening();
                _sniffCo = null;
                yield break;
            }

            // 没有人说话 → 关掉麦克风，等待 sniffOffGap 再尝试
            if (app != null && app.Active)
                app.Deactivate();

            yield return new WaitForSeconds(sniffOffGap);
        }

        _sniffCo = null;
    }


    void MicCheck()
    {
        if (Microphone.devices.Length > 0)
        {
            bool micBusy = Microphone.IsRecording(Microphone.devices[0]);
            LogBuffer.Log("Microphone is Recording: " + micBusy);
        }
    }
}
