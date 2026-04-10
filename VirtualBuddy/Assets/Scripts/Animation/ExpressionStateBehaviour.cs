using UnityEngine;

public class ExpressionStateBehaviour : StateMachineBehaviour
{
    [SerializeField] private Material expressionMaterial;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        var renderer = animator.GetComponentInChildren<SkinnedMeshRenderer>();
        if (renderer != null && expressionMaterial != null)
        {
            // 创建材质实例避免影响原始材质
            Material newMat = new Material(expressionMaterial);
            renderer.material = newMat;
        }
    }
}