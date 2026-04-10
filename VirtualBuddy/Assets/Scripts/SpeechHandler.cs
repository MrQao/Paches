using System.Collections;
using System.Collections.Generic;
using UnityEngine.Android;
using UnityEngine;
using TMPro;

public class SpeechHandler : MonoBehaviour
{
    public TMP_InputField inputField;
    public TMP_Text outputText;

    void Start()
    {
        // 检查是否已授权麦克风权限
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            // 弹出系统权限请求
            Permission.RequestUserPermission(Permission.Microphone);
        }
        else
        {
            // 已拥有麦克风权限
            LogBuffer.Log("Microphone Permission Checked");
        }
    }

    public void StartSpeechRecognition()
    {
        LogBuffer.Log("Calling Speech Recognition");

        FindObjectOfType<WakeWordDetector>().DisableWakeWordDetection();

        AndroidJavaClass plugin = new AndroidJavaClass("com.example.speech_plugin.SpeechPlugin");
        plugin.CallStatic("startListening");
    }
    public void OnSpeechResult(string result)
    {

        if (result == "Error: 7")
        {
            //没说话的时候进入这个if，问题是他妈的这都直接不显示了草
            if (outputText != null)
                outputText.text = "Sorry? I can't hear you?";

            FindObjectOfType<TTSManager>().Speak("Sorry? I can't hear you?");
        }
        else
        {
            // 获得识别结果
            Debug.Log("Speech Recognized: " + result);

            if (inputField != null)
                inputField.text = result;
            LogBuffer.Log("Get result: " + result);

            FindObjectOfType<GPTClient>().SendMessageToGPT(result);
        }
        FindObjectOfType<WakeWordDetector>().EnableWakeWordDetection();
    }
}
