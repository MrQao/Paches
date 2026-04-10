using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPrefab : MonoBehaviour
{
    [Header("生成位置")]
    public Transform spawnPoint;

    /// <summary>
    /// 在 spawnPoint 生成传入的 prefab
    /// </summary>
    public void Spawn(GameObject prefab)
    {
        if (prefab == null || spawnPoint == null)
        {
            Debug.LogWarning("Prefab 或 SpawnPoint 未设置！");
            return;
        }

        Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
    }
}
