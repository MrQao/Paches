using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ColliderTrigger : MonoBehaviour
{
    [Header("在外部可绑定的方法（类似按钮 OnClick）")]
    public UnityEvent onTriggered;

    [Header("是否只触发一次")]
    public bool triggerOnce = false;

    private bool hasTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (triggerOnce && hasTriggered) return;

        // 触发事件
        onTriggered?.Invoke();
        hasTriggered = true;

        Debug.Log($"{gameObject.name} 被 {other.name} 触发！");
    }
}
