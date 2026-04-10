using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ButtonDim : MonoBehaviour
{
    private Button myButton;
    public Color flashColor = Color.gray; // 一瞬间的颜色
    private Color originalColor;

    private void Awake()
    {
        myButton = GetComponent<Button>();
        originalColor = myButton.colors.normalColor;
        myButton.onClick.AddListener(() => {
            //Debug.Log("ButtonOnclicked");
            StartCoroutine(Flash());
            });
    }

    IEnumerator Flash()
    {
        // 临时换颜色
        var cb = myButton.colors;
        cb.normalColor = flashColor;
        myButton.colors = cb;

        // 下一帧恢复
        yield return null;

        cb.normalColor = originalColor;
        myButton.colors = cb;
    }
}
