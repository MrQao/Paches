using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WalkingDetector : MonoBehaviour
{
    public UnityEngine.AI.NavMeshAgent agent;
    public Animator animator;

    void Update()
    {
        // ��ȡ��ǰ�ٶ�
        float speed = agent.velocity.magnitude;

        // ��·״̬�ж�
        bool isWalking =
            !agent.pathPending &&                // ·���Ѽ���
            agent.remainingDistance > agent.stoppingDistance && // ���뻹û��
            speed > 0.05f;                        // �ٶȴ�����ֵ�����ⶶ����

        animator.SetFloat("speed", speed);
    }
}
