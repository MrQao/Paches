using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavAgentController : MonoBehaviour
{
    public UnityEngine.AI.NavMeshAgent agent;
    public Animator animator;

    private void Update()
    {
        if (animator) animator.SetFloat("speed", agent.velocity.magnitude);
    }
    public void MoveTo(Vector3 destination)
    {
        agent.SetDestination(destination);
    }

    public void MoveTo(NavPoint point)
    {
        MoveTo(point.transform.position);
    }

    
}
