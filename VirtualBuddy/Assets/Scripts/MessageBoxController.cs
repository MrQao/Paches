using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MessageBoxController : MonoBehaviour
{
    public TMP_Text messageBox;

    private void Update()
    {
        if (messageBox != null)
            messageBox.text = LogBuffer.GetFormattedLogs();
    }
}
