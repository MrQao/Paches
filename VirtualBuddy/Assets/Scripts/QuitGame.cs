using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class QuitGame : MonoBehaviour
{
    public void ExitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false; // 在编辑器里停止 Play
#else
        Application.Quit();                  // 在构建版本里退出游戏
#endif
    }
}
