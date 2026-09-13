using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoleAnimator : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private Animator animator;

    [Header("Attack")]
    [SerializeField] private Transform attackPrefab;
    [SerializeField] private Transform attackTransform;
    [SerializeField] private float attackTime;

    private void Start()
    {
        if (TryGetComponent<MoveAction>(out MoveAction moveAction)) 
        {
            moveAction.OnMoveStart += moveAction_OnMoveStart;
            moveAction.OnMoveEnd += moveAction_OnMoveEnd;
        }

        if (TryGetComponent<AttackAction>(out AttackAction attackAction)) 
        {
            attackAction.onAttack += attackAction_onAttack;
        }
    }
    private void moveAction_OnMoveStart(object sender, EventArgs e)
    {
        animator.SetBool("Walking", true);
    }

    private void moveAction_OnMoveEnd(object sender, EventArgs e)
    {
        animator.SetBool("Walking", false);
    }

    private void attackAction_onAttack(object sender, AttackAction.OnAttackEventArgs e)
    {
        animator.SetTrigger("Attacking");

        StartCoroutine(CreateAttackFX(e.targetRole.GetWorldPosition()));
    }

    private IEnumerator CreateAttackFX(Vector3 targetPos)
    {
        yield return new WaitForSeconds(attackTime);
        Transform attackFXTransform = Instantiate(attackPrefab, attackTransform.position, Quaternion.identity);
        RoleAttackBase attack = attackFXTransform.GetComponent<RoleAttackBase>();
        attack.SetUp(targetPos);
    }
}
