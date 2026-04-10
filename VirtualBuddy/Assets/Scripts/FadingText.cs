using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System.Xml;

public class FadeOutText : MonoBehaviour
{
    private TMP_Text tmpText;       // 拖你的 Text 进来
    private float duration = 2f;  // 持续时间（秒）

    void OnEnable()
    {
        tmpText = GetComponent<TMP_Text>();
        Color c = tmpText.color;
        c.a = 1f;                // 重置到完全不透明
        tmpText.color = c;
        StartCoroutine(FadeText());
    }

    IEnumerator FadeText()
    {
        Color c = tmpText.color;
        float startAlpha = c.a;

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float normalized = t / duration;
            c.a = Mathf.Lerp(startAlpha, 0, normalized);  // alpha 渐变
            tmpText.color = c;
            yield return null;
        }

        c.a = 0;
        tmpText.color = c;
        gameObject.SetActive(false);
    }
}
