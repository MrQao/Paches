using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ToggleEventForwarder : MonoBehaviour
{
    private Toggle toggle;

    [Header("勾选时触发")]
    public UnityEvent onToggleOn;

    [Header("取消勾选时触发")]
    public UnityEvent onToggleOff;

    void Awake()
    {
        toggle = GetComponent<Toggle>();

        // 注册监听
        toggle.onValueChanged.AddListener(HandleToggleChanged);
    }

    private void HandleToggleChanged(bool isOn)
    {
        if (isOn)
            onToggleOn?.Invoke();
        else
            onToggleOff?.Invoke();
    }
}
