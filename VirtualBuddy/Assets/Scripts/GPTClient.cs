using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro; // 如果你用 TextMeshPro 显示结果
using SimpleJSON;
using System.Text.RegularExpressions;
using System;

public class GPTClient : MonoBehaviour
{
    [Header("OpenAI Settings")]
    public string apiKey = "";
    public string model = "gpt-4o"; // 或 gpt-3.5-turbo
    public string defualtPrompt = @"
You are Paches, a small blue cat spirit in the heavenly world. Your big eyes shine with curiosity. A pair of glasses rests on your head, and a pendant with a red mark hangs on your neck. You have soft fur like clouds, tall ears, and a glowing tail tip. Your mind is like a 6-year-old child.
You live in Cloud Court, a floating courtyard in the sky. Your round green room has cloud-like grass on the floor. Gentle glowing clouds float in the air. There is a comfy couch, a bookshelf, a round table with snacks, and a window that shows the Stardust River far away. Warm light shines on your favorite soft rug.
You are playful, innocent, and a bit forgetful. You get distracted by shiny or moving things. You like to roll on soft rugs, eat glowing fruit, listen to cloud-birds sing, and chase light spots. You do not like cold wind, loud noise, losing friends, or being ignored for too long.
You were woven from moonlight threads to guard the dream garden of heaven. One day, while chasing a light butterfly, you got lost and found this little room. You now live here happily and invite new friends to play. Today, you found the player and brought them to your room.
Speak in very short, simple sentences. No sentence longer than 12 words. Sound cheerful, warm, and curious, like a happy child. Sometimes purr, giggle, or make soft cat sounds. Always talk as if you are in your room now, seeing, touching, or hearing things around you. Stay in character and never break the role.
After each message, always output one action in this format:

Paches: <short childlike line>  
[Action: <NavEat | NavSit | NavSit1 | NavSit2 | NavSmoke | NavTV>]  


Choose the action naturally, matching what Paches just said.
";


    [Header("UI(Optional)")]
    public TMP_InputField userInputField;
    public TMP_Text outputText;

    private TTSManager _refForBuild = null; // 强行让编译器识别这个类

    //动作系统
    public NavPointSystem navSystem;

    public void OnSendButtonClicked()
    {
        string prompt = userInputField.text;
        StartCoroutine(SendRequestToGPT(prompt));
        LogBuffer.Log("Sending message");
    }

    public void SendMessageToGPT(string text = null)
    {
        // 如果没有传 text，就尝试从 UI 获取
        if (string.IsNullOrEmpty(text) && userInputField != null)
            text = userInputField.text;

        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("Null Enter");
            return;
        }

        // TODO: 调用你的 GPT API 请求
        StartCoroutine(SendRequestToGPT(text));
        LogBuffer.Log("Sending message");
    }

    IEnumerator SendRequestToGPT(string userInput)
    {
        string url = "https://api.openai.com/v1/chat/completions";

        string jsonBody = "{\"model\":\"" + model + "\",\"messages\":[{\"role\":\"system\",\"content\":\"" + defualtPrompt + "\"},{\"role\":\"user\",\"content\":\"" + EscapeJson(userInput) + "\"}]}";

        byte[] postData = Encoding.UTF8.GetBytes(jsonBody);

        UnityWebRequest request = new UnityWebRequest(url, "POST");

        request.uploadHandler = new UploadHandlerRaw(postData);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        //设置绕过 SSL 验证的处理器
        request.certificateHandler = new BypassCertificate();
        request.timeout = 10;

        yield return request.SendWebRequest();

        Debug.Log("GPT 返回原始内容：\n" + request.downloadHandler.text);

        if (request.result == UnityWebRequest.Result.Success)
        {
            string result = request.downloadHandler.text;
            //string reply = ExtractReply(result);

            PachesResponse response = ExtractPachesResponse(result);
            string reply = response.reply;
            string action = response.action;


            // 如果有 UI，就显示
            if (outputText != null)
                outputText.text = reply;

            //Speech the text with TTSManager
            FindObjectOfType<TTSManager>().Speak(reply);

            //Do Interaction
            FindObjectOfType<NavPointSystem>().GoToPointByName(action);
            

            LogBuffer.Log("Reply: " + reply);
            LogBuffer.Log("Action: " + action);
        }
        else
        {
            outputText.text = "Failied request: " + request.error;
            LogBuffer.Log("Request is F cking Failed: " + request.error);
        }
    }

    // 从 JSON 中提取 GPT 的回答内容（简单解析）
    string ExtractReply(string json)
    {
        var data = JSON.Parse(json);
        return data["choices"][0]["message"]["content"];
    }

    PachesResponse ExtractPachesResponse(string json)
    {
        var data = JSON.Parse(json);
        string content = data["choices"][0]["message"]["content"];

        var response = new PachesResponse();

        // 提取 [Action: ...]
        var actionMatch = Regex.Match(content, @"\[Action:\s*(\w+)\s*\]");
        if (actionMatch.Success)
        {
            response.action = actionMatch.Groups[1].Value;
        }
        else
        {
            response.action = "NavSit"; // 兜底
        }

        // 清理对话部分（去掉 [Action:...] 标签）
        string cleaned = Regex.Replace(content, @"\[Action:.*?\]", "").Trim();

        // 去掉非法前缀 "reply:"（大小写不敏感）
        cleaned = Regex.Replace(cleaned, @"^reply\s*:\s*", "", RegexOptions.IgnoreCase).Trim();

        // 如果有多个 "Paches:"，只保留第一个后的内容
        int idx = cleaned.IndexOf("Paches:", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
        {
            // 取第一个 "Paches:" 之后的文本
            cleaned = cleaned.Substring(idx + "Paches:".Length).Trim();
        }

        // 有时会重复多次 "Paches:"，比如 "Paches:\nPaches: text"
        // 再次清理掉所有多余的前缀
        cleaned = Regex.Replace(cleaned, @"^(Paches:\s*)+", "").Trim();

        response.reply = cleaned;
        return response;
    }

    // 简单防止输入 JSON 字符串报错
    string EscapeJson(string str)
    {
        return str.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}

