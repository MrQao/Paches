using UnityEngine;
using UnityEngine.Events;
using Oculus;  // 需要 Oculus Integration 包
using Oculus.Platform;
using Oculus.Platform.Models;

/// <summary>
/// 允许在 Inspector 中把 Quest 手柄按键映射到自定义方法
/// </summary>
public class MetaControllerButtonMapper : MonoBehaviour
{
    [Header("右手手柄按钮映射")]
    public UnityEvent onPressA; // OVRInput.Button.One
    public UnityEvent onPressB; // OVRInput.Button.Two

    [Header("左手手柄按钮映射")]
    public UnityEvent onPressX; // OVRInput.Button.Three
    public UnityEvent onPressY; // OVRInput.Button.Four

    [Header("摇杆按压映射")]
    public UnityEvent onPressRightStick; // OVRInput.Button.SecondaryThumbstick
    public UnityEvent onPressLeftStick;  // OVRInput.Button.PrimaryThumbstick


    private void Awake()
    {
        onPressA.AddListener(() =>
        {
            LogBuffer.Log("Button A Pressed");
        });
    }
    void Update()
    {
        // === 右手 ===
        if (OVRInput.GetDown(OVRInput.Button.One))
            onPressA?.Invoke();

        if (OVRInput.GetDown(OVRInput.Button.Two))
            onPressB?.Invoke();

        // === 左手 ===
        if (OVRInput.GetDown(OVRInput.Button.Three))
            onPressX?.Invoke();

        if (OVRInput.GetDown(OVRInput.Button.Four))
            onPressY?.Invoke();

        // === 摇杆点击 ===
        if (OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger))
            onPressRightStick?.Invoke();

        if (OVRInput.GetDown(OVRInput.RawButton.LIndexTrigger))
            onPressLeftStick?.Invoke();
    }
}
