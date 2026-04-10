// CharacterExpressionManager.cs
using UnityEngine;

public class CharacterExpressionManager : MonoBehaviour
{
    [System.Serializable]
    public class ExpressionMapping
    {
        public string stateName;
        public Material material;
    }

    public ExpressionMapping[] expressionMappings;
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        // 默认使用Idle表情
        SetExpression("idle");
    }

    public void SetExpression(string stateName)
    {
        foreach (var mapping in expressionMappings)
        {
            if (mapping.stateName == stateName && mapping.material != null)
            {
                var renderer = GetComponentInChildren<SkinnedMeshRenderer>();
                if (renderer != null)
                {
                    Material newMat = new Material(mapping.material);
                    renderer.material = newMat;
                }
                return;
            }
        }
        Debug.LogWarning($"Expression material for {stateName} not found!");
    }
}