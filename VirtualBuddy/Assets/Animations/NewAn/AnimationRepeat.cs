using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationRepeat : StateMachineBehaviour
{
    public int loopTimes = 3;  // 循环次数
    private int counter;       // 当前计数
    private bool looping;      // 是否正在循环

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        counter = 0;
        looping = true;
        animator.Play(stateInfo.shortNameHash, layerIndex, 0f); // 播放一次
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (looping && stateInfo.normalizedTime >= 1f) // 动画播完一次
        {
            counter++;
            if (counter < loopTimes)
            {
                animator.Play(stateInfo.shortNameHash, layerIndex, 0f); // 重新开始
            }
            else
            {
                looping = false;
                // 退出到下一个状态，比如 Idle
                animator.SetTrigger("ExitLoop");
            }
        }
    }

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    //override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    //override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateMove is called right after Animator.OnAnimatorMove()
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that processes and affects root motion
    //}

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}
}
