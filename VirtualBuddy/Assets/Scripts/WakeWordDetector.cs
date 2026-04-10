using Pv.Unity;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;

public class WakeWordDetector : MonoBehaviour
{
    private PorcupineManager porcupineManager;
    public NavPoint navuser;
    [SerializeField] private VoiceStarter voice;  // 在 Inspector 里拖 VoiceManager 上的 VoiceStarter 进来

    void Start()
    {
        // 1. AccessKey：在 picovoice 控制台生成
        string accessKey;

        /*// 2. 模型路径：注意 StreamingAssets 内的真实路径
        string keywordPath = Path.Combine(Application.streamingAssetsPath, "keyword_files/android/Hi-paches.ppn");*/
        string keywordPath;

        // iphone上用的vrDXGFHKZxwlTKJsiSUhlvBsV/GoK9AO3eTA/OMetq7P45dOyH236g==
#if UNITY_ANDROID && !UNITY_EDITOR
    keywordPath = Path.Combine(Application.streamingAssetsPath, "keyword_files/android/Hi-paches_en_android_v3_0_0.ppn");
    accessKey = "hnZVTx1akGZLuTmrs8FZE95/I+LxYU+2g8U01O8ImSJiibeIX2nlHQ==";
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        keywordPath = Path.Combine(Application.streamingAssetsPath, "keyword_files/windows/americano_windows.ppn");
        accessKey = "JH9DYh8JwUIU/wUuPOaKPorf/GXpIHOmC2m/9Mhh65NdgN3v8gBuUg==";
#else
    throw new NotSupportedException("Unsupported platform for Porcupine keyword file.");
#endif
        string modelPath = Path.Combine(Application.streamingAssetsPath, "porcupine_params.pv");
        // 3. 创建 PorcupineManager 实例
        porcupineManager = PorcupineManager.FromKeywordPaths(
            accessKey: accessKey,
            keywordPaths: new string[] { keywordPath },
            modelPath: modelPath,
            sensitivities: new float[] { 0.7f },
            wakeWordCallback: OnWakeWordDetected, // 绑定回调函数
            processErrorCallback: (err) =>
            {
                Debug.LogError("Porcupine Failed initialization: " + err.ToString());
                LogBuffer.Log("Porcupine Failed initialization: " + err.ToString());
            }
        );
        porcupineManager.Start();
        LogBuffer.Log("WakeWordSystem On running");
    }

    // 4. 回调函数：唤醒词识别成功时触发
    private void OnWakeWordDetected(int keywordIndex)
    {
        Debug.Log("唤醒词识别成功！");
        LogBuffer.Log("Wake up word successful");

        if (navuser != null)
            FindObjectOfType<NavPointSystem>().GoToPointByNavPoint(navuser);
        FindObjectOfType<TTSManager>().Speak("Mhm?");
        //FindObjectOfType<SpeechHandler>().StartSpeechRecognition();
        
        DisableWakeWordDetection();
        voice.StartSession();
    }

    void OnDestroy()
    {
        porcupineManager?.Stop();
        porcupineManager?.Delete();
    }

    public void EnableWakeWordDetection()
    {
        if (porcupineManager != null)
        {
            porcupineManager.Start();
            Debug.Log("Wake word detection started.");
            LogBuffer.Log("Wake word detection started.");
        }
    }

    public void DisableWakeWordDetection()
    {
        if (porcupineManager != null)
        {
            porcupineManager.Stop();
            Debug.Log("Wake word detection stopped.");
        }
    }

}
