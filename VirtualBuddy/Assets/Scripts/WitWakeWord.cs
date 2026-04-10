using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using Meta.WitAi.CallbackHandlers; // 仅用于拿到 WitResponseMatcher 类型
using Meta.WitAi;
using Oculus.Voice;

public class WitWakeWord : MonoBehaviour
{
    [Header("Refs")]
    public AppVoiceExperience app;            // 场景里的 AppVoiceExperience
    public WitResponseMatcher responseMatcher;// 用于“唤醒”的那个 ResponseMatcher（Intent= wake_word）
    public WitSpeechHandler speech;  // 你的会话脚本（2秒静音/10秒窗口）

    // 反射句柄
    private FieldInfo _multiValueField;       // 私有字段 onMultiValueEvent
    private MethodInfo _addListenerMI;
    private MethodInfo _removeListenerMI;
    private UnityAction<string[]> _wakeDelegate;

    //重置
    private Coroutine _restartCo;
    private bool _inSession = false;
    private bool _isStartingSession;

    //动作映射
    public NavPoint navuser;


    void Awake()
    {
        if (!app || !responseMatcher || !speech)
        {
            Debug.LogError("[WitWakeWordBridge] Missing refs.");
            LogBuffer.Log("[WitWakeWordBridge] Missing refs.");
            enabled = false; return;
        }

        // 准备反射：拿到 private MultiValueEvent onMultiValueEvent
        _multiValueField = typeof(WitResponseMatcher)
            .GetField("onMultiValueEvent", BindingFlags.NonPublic | BindingFlags.Instance);
        if (_multiValueField == null)
        {
            Debug.LogError("[WitWakeWordBridge] Field 'onMultiValueEvent' not found. SDK version may have changed");
            LogBuffer.Log("[WitWakeWordBridge] Field 'onMultiValueEvent' not found. SDK version may have changed");
            enabled = false; return;
        }

        var evObj = _multiValueField.GetValue(responseMatcher);
        if (evObj == null)
        {
            Debug.LogError("[WitWakeWordBridge] onMultiValueEvent is null.");
            LogBuffer.Log("[WitWakeWordBridge] onMultiValueEvent is null.");
            enabled = false; return;
        }

        // 通过反射拿 AddListener/RemoveListener 方法（签名是 UnityAction<string[]>）
        var evType = evObj.GetType();
        _addListenerMI = evType.GetMethod("AddListener");
        _removeListenerMI = evType.GetMethod("RemoveListener");
        _wakeDelegate = new UnityAction<string[]>(WakeWordDetected);

        // 订阅
        _addListenerMI.Invoke(evObj, new object[] { _wakeDelegate });

        // 会话结束时恢复唤醒 matcher
        speech.OnSessionEnded.AddListener(OnSessionEnded);
        if (!app.Active) app.Activate();
        RestartAfter(8f);
        //app.VoiceEvents.OnRequestCompleted.AddListener(() => { if (!app.Active) app.Activate(); });
    }

    void OnDestroy()
    {
        // 反订阅
        if (responseMatcher != null && _multiValueField != null && _removeListenerMI != null && _wakeDelegate != null)
        {
            var evObj = _multiValueField.GetValue(responseMatcher);
            if (evObj != null) _removeListenerMI.Invoke(evObj, new object[] { _wakeDelegate });
        }
        if (speech) speech.OnSessionEnded.RemoveListener(OnSessionEnded);
    }

    // === 被唤醒（ResponseMatcher 命中 wake_word） ===
    private void WakeWordDetected(string[] args)
    {
        Debug.Log("[WitWakeWordBridge] Wake word matched. Starting session...");
        //LogBuffer.Log("[WitWakeWordBridge] Wake word matched. Starting session...");
        // 入场：停掉唤醒用 matcher，避免会话内继续打到它
        if (_inSession) return; // 会话中直接忽略匹配，避免二次触发

        // 开始你的连续会话（占麦、2秒静音分句、10秒窗口）
        _inSession = true;
        Debug.Log("[WitWakeWord] Wake word matched. Starting session...");
        LogBuffer.Log("Wake up word successful");

        //if (navuser != null)
        //    FindObjectOfType<NavPointSystem>().GoToPointByNavPoint(navuser);
        //FindObjectOfType<TTSManager>().Speak("Mhm?");
        LogBuffer.Log("Starting Speech");
        StartSessionClean();
    }

    // === 会话结束 → 回到待机唤醒 ===
    private void OnSessionEnded()
    {
        Debug.Log("[WitWakeWordBridge] Session ended. Re-enable wake matcher.");
        LogBuffer.Log("[WitWakeWordBridge] Session ended. Re-enable wake matcher.");

        _inSession = false;        // 只要把闸门打开即可
        if (!app.Active) app.Activate();
        RestartAfter(8f);
    }
    public void RestartAfter(float seconds)
    {
        if (_restartCo != null) StopCoroutine(_restartCo);
        _restartCo = StartCoroutine(RestartAfterCo(seconds));
    }

    private IEnumerator RestartAfterCo(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!_inSession)
        {
            if (app.Active) app.Deactivate();
            // 给底层一点收尾时间
            yield return new WaitForSeconds(0.1f);
            app.Activate();
            RestartAfter(8f);
            _restartCo = null;
        }
    }

    private void StartSessionClean()
    {
        if (_isStartingSession) return;
        StartCoroutine(StartSessionCleanCo());
    }
    private IEnumerator StartSessionCleanCo()
    {
        _isStartingSession = true;

        // 1) 立刻中止当前请求并丢弃尾包（比 Deactivate 更狠）
        // - 如果你是 Oculus.Voice 的 AppVoiceExperience，也有 DeactivateAndAbort()
        // - 某些版本名为 DeactivateAndAbortRequest，按你 SDK 实际 API 选一个
        try
        {
            TryAbortIfAvailable();  // ← 关键：强制打断、丢弃 buffer
            app.Deactivate();
        }
        catch { /* 兼容不同版本 */ }

        // 2) 等待 Wit 确认“停止监听 + 请求完成”
        bool stopped = !app.MicActive;
        bool completed = !app.IsRequestActive;

        UnityAction onStopped = () => stopped = true;
        UnityAction onCompleted = () => completed = true;

        app.VoiceEvents.OnStoppedListening.AddListener(onStopped);
        app.VoiceEvents.OnRequestCompleted.AddListener(onCompleted);

        float timeout = 1.0f; // 一般几十毫秒就完成，这里给 1s 安全窗
        float t = 0f;
        while (!(stopped && completed) && t < timeout)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        app.VoiceEvents.OnStoppedListening.RemoveListener(onStopped);
        app.VoiceEvents.OnRequestCompleted.RemoveListener(onCompleted);

        // 3) 加一个最小帧延迟，确保底层管线切至 idle
        yield return null;

        // 4) 告诉你的会话层“丢弃唤醒词那一句”（防御尾包误触发）
        // 例如在 VoiceStarter 内部暴露一个 DropFirstUtteranceOnce 标志位
        // voice.DropFirstUtteranceOnce(); // 示例：按你自己的接口来

        // 5) 现在再开始会话
        speech.StartSession();

        _isStartingSession = false;
    }

    private void TryAbortIfAvailable()
    {
        var svcProp = app.GetType().GetProperty("WitService", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var svc = svcProp?.GetValue(app);
        if (svc == null) return;

        var abortMI = svc.GetType().GetMethod("DeactivateAndAbort", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (abortMI != null)
        {
            abortMI.Invoke(svc, null);
        }
    }
}
