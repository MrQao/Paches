using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodConsuming : MonoBehaviour
{
    private NavPointSystem navPointSystem;
    public NavPoint navEat;
    public NavPoint navPlay;
    private AudioSource charAudio;
    public AudioClip ChewingAudioClip;
    public AudioClip LaughingAudioClip;

    private void Awake()
    {
        navPointSystem = FindObjectOfType<NavPointSystem>();
        charAudio = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // 如果对方也是 Trigger，直接 return
        if (other.isTrigger) return;

        // 如果被角色碰到，就“吃掉”
        if (other.gameObject.CompareTag("Food"))
        {
            navPointSystem.GoToItem(navEat);
            charAudio.PlayOneShot(ChewingAudioClip);
            // TODO: 增加分数/回血/动画
            Destroy(other.gameObject);
        }

        if (other.gameObject.CompareTag("Toy"))
        {
            navPointSystem.GoToItem(navPlay);
            charAudio.PlayOneShot(LaughingAudioClip);

            // 找到并关闭所有 Trigger 类型的 Collider
            Collider[] colliders = other.GetComponents<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = false;
            }

            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;   // 关闭物理模拟
                rb.useGravity = false;   // 关闭重力
            }

            // 3 秒后销毁
            Destroy(other.gameObject, 3f);

            // TODO: 增加分数/回血/动画
        }
    }
}
