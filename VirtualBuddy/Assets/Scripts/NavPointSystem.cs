using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TextCore.Text;
using UnityEngine.Events;

public class NavPointSystem : MonoBehaviour
{
    public NavAgentController agentCtrl;
    public UnityEngine.AI.NavMeshAgent agent;
    public List<NavPoint> navFurnitures;
    public List<NavPoint> navStrollings;
    public List<NavPoint> navInteractions;
    public Animator animator; // 角色 Animator
    public Transform charactertf;
    public int weightStrolling = 2;

    private bool isBusy = false;
    private bool isPerformingAction = false;
    private int currentIndex = 0;
    private NavPoint currentNavPoint;
    public string endTriggerName = "EndAction";

    private Coroutine _runningAction = null;

    void Start()
    {
        // 示例：启动就导航到第一个点
        if (navFurnitures.Count > 0)
        {
            currentNavPoint = navFurnitures[0];
            agentCtrl.MoveTo(currentNavPoint);
        }
    }
    private void Update()
    {
        if (!isPerformingAction && HasArrived())
        {
            LogBuffer.Log(currentNavPoint.name);
            _runningAction = StartCoroutine(DoPointAction(currentNavPoint));
        }
    }
    public void GoToNext()
    {
        if (Random.Range(0, 10) < weightStrolling)
        {
            currentIndex = Random.Range(0, navFurnitures.Count);
            currentNavPoint = navFurnitures[currentIndex];
        }
        else
        {
            currentIndex = Random.Range(0, navStrollings.Count);
            currentNavPoint = navStrollings[currentIndex];
        }

        
        agentCtrl.MoveTo(currentNavPoint);
    }

    public void GoToPointByName(string name)
    {
        if (isBusy)
            return;

        //终止上一个动作
        if (isPerformingAction)
        {
            StopCoroutine(_runningAction);
            animator.SetTrigger("EndAction");
        }
        isPerformingAction = false;

        var target = navInteractions.Find(p => p.pointName == name);
        if (target != null) {
            currentNavPoint = target;
            agentCtrl.MoveTo(target);
        }
    }

    public void GoToPointByNavPoint(NavPoint point)
    {
        if (isBusy)
            return;

        //终止上一个动作
        if (isPerformingAction)
        {
            StopCoroutine(_runningAction);
            animator.SetTrigger("EndAction");
        }
        isPerformingAction = false;

        currentNavPoint = point;
        agentCtrl.MoveTo(point);
    }

    public void GoToItem(NavPoint navItem)
    {
        if(isBusy)
            return;

        if (isPerformingAction)
        {
            StopCoroutine(_runningAction);
            animator.SetTrigger("EndAction");
        }
        isPerformingAction = false;

        isBusy = true;
        currentNavPoint = navItem;
        agentCtrl.MoveTo(navItem);
    }


    bool HasArrived()
    {
        return !agent.pathPending &&
               agent.remainingDistance <= agent.stoppingDistance &&
               (!agent.hasPath || agent.velocity.sqrMagnitude < 0.01f);
    }
    IEnumerator DoPointAction(NavPoint point)
    {
        isPerformingAction = true;
        animator.ResetTrigger("EndAction");

        if (point == null)
        {
            Debug.LogError("Null point");
            GoToNext();
            yield break;
        }

        // 1?? 朝向 lookAtTarget（如果有）
        if (point.lookAtTarget != null)
        {
            Vector3 lookDir = point.lookAtTarget.position - charactertf.position;
            lookDir.y = 0; // 保证只在水平面旋转
            if (lookDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                float rotSpeed = 5f;
                float elapsed = 0f;
                while (Quaternion.Angle(charactertf.rotation, targetRot) > 0.5f && elapsed < 1f)
                {
                    charactertf.rotation = Quaternion.Slerp(charactertf.rotation, targetRot, Time.deltaTime * rotSpeed);
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }
        }

        // 如果当前 NavPoint 有动画 Trigger
        if (!string.IsNullOrEmpty(point.animTriggerName))
        {
            animator.SetTrigger(point.animTriggerName);
        }

        // 停留一段时间（等动画播完）
        yield return new WaitForSeconds(point.waitAfterArrival);

        // 4?? 统一触发结束动作
        if (!string.IsNullOrEmpty(endTriggerName) && animator != null)
        {
            animator.SetTrigger(endTriggerName);
        }

        while (!IsInIdleState())
        {
            yield return null;
        }

        isBusy = false;
        isPerformingAction = false;

        GoToNext();
    }

    bool IsInIdleState()
    {
        if (animator == null) return true;
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName("idle"); // Idle 是你 Animator 里的 Idle 状态名
    }

    
}
