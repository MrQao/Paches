using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SafeGrib : MonoBehaviour
{
    private XRGrabInteractable grab;
    private Collider col;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        col = GetComponent<Collider>();

        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        // 抓取时禁用碰撞（避免撞飞玩家）
        col.enabled = false;
    }

    void OnRelease(SelectExitEventArgs args)
    {
        // 放下时恢复碰撞
        col.enabled = true;
    }
}
