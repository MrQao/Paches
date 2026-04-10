using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavPoint : MonoBehaviour
{
    public string pointName;
    public string animTriggerName; // 动画Trigger名
    public float waitAfterArrival = 0f;
    public Transform lookAtTarget;   // 可选：到点后朝向谁
}
