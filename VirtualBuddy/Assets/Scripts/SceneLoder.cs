using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoder : MonoBehaviour
{
    // Start is called before the first frame update
    public string sceneName;

    // 调用时传入延迟秒数
    public void NextScene(float delaySeconds = 0f)
    {
        Debug.Log("ChangingScene after " + delaySeconds + "s...");
        StartCoroutine(LoadSceneWithDelay(delaySeconds));
    }

    private IEnumerator LoadSceneWithDelay(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        SceneManager.LoadScene(sceneName);
    }
}
