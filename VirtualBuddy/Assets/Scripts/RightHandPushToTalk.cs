using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RightHandPushToTalk : MonoBehaviour
{
    public VoiceStarter voice;  // 拖进来（VoiceManager 上的）
    private InputAction _press;

    private void OnEnable()
    {
        // 绑定右手扳机（如需改为 A/B 键，可换 "<XRController>{RightHand}/primaryButton"）
        _press = new InputAction(type: InputActionType.Button, binding: "<XRController>{RightHand}/primaryButton");
        _press.Enable();

        _press.performed += OnPressed;
        //_press.canceled += OnReleased;
    }

    private void OnDisable()
    {
        _press.performed -= OnPressed;
        //_press.canceled -= OnReleased;
        _press.Disable();
    }

    private void OnPressed(InputAction.CallbackContext ctx) => voice?.StartSession();
    //private void OnReleased(InputAction.CallbackContext ctx) => voice?.StopListening();
}
