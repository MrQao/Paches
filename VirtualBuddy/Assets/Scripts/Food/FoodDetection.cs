using Oculus.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class FoodDetection : MonoBehaviour
{
    private NavPointSystem navPointSystem;
    public NavPoint navItem;

    public float triggerCooldown = 1f; // 间隔时间（秒）
    private float lastTriggerTime = -999f;

    private void Awake()
    {
        navPointSystem = FindObjectOfType<NavPointSystem>();
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (Time.time - lastTriggerTime >= triggerCooldown)
            {
                // 让角色目标改为自己
                navPointSystem.GoToPointByNavPoint(navItem);

                // 记录触发时间
                lastTriggerTime = Time.time;
            }
        }
    }
}
