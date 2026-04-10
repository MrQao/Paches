using System.Collections;
using System.IO;
using System.Net.Http;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class TTSManager : MonoBehaviour
{
    public AudioSource audioSource;

    [Header("API Settings")]
    public string apiKey = "sk_9d2322286ea75c4384e2bdfa6b9842de7de3a565ca2ed545";
    public string voiceId = "vGQNBgLaiM3EdZtxIiuY"; // Find voiceID from ElevenLabs website

    public void Speak(string text)
    {
        StartCoroutine(SendTextToElevenLabs(text));
    }

    IEnumerator SendTextToElevenLabs(string text)
    {
        var url = $"https://api.elevenlabs.io/v1/text-to-speech/{voiceId}";

        var json = $"{{\"text\":\"{text}\",\"model_id\":\"eleven_monolingual_v1\",\"voice_settings\":{{\"stability\":0.5,\"similarity_boost\":0.75}}}}";
        var bodyRaw = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest www = UnityWebRequest.Put(url, bodyRaw))
        {
            www.method = UnityWebRequest.kHttpVerbPOST;
            www.SetRequestHeader("xi-api-key", apiKey);
            www.SetRequestHeader("Content-Type", "application/json");
            www.downloadHandler = new DownloadHandlerBuffer();

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("TTS Error: " + www.error);
            }
            else
            {
                byte[] mp3Data = www.downloadHandler.data;
                StartCoroutine(PlayMP3FromBytes(mp3Data));
            }
        }
    }

    IEnumerator PlayMP3FromBytes(byte[] mp3Data)
    {
        string tempPath = Path.Combine(Application.persistentDataPath, "tts.mp3");
        File.WriteAllBytes(tempPath, mp3Data);

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + tempPath, AudioType.MPEG))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                audioSource.clip = clip;
                audioSource.Play();
            }
            else
            {
                Debug.LogError("Audio loading failed: " + www.error);
            }
        }
    }
}
