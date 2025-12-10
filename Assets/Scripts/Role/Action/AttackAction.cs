using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class AttackAction : BaseAction
{
    public event EventHandler<OnAttackEventArgs> onAttack;

    public class OnAttackEventArgs : EventArgs
    {
        public Role targetRole;
        public Role attackingRole;
    }

    private enum State
    {
        Aiming,
        Attacking,
        Cooloff
    }

    [Header("Config")]
    [SerializeField] private int maxAttackDistance;
    [SerializeField] private float aimingStateTime;
    [SerializeField] private float attackingStateTime;
    [SerializeField] private float cooloffStateTime;

    private float stateTimer;

    private State state;
    private Role targetRole;

    private bool canAttack;

    public override string GetName()
    {
        return "攻击";
    }

    private void Update()
    {
        if (!isActive) return;

        stateTimer -= Time.deltaTime;
        switch(state)
        {
            case State.Aiming:
                Vector3 aimDir = (targetRole.GetWorldPosition() - role.GetWorldPosition()).normalized;
                float rotateSpeed = 12f;
                transform.forward = Vector3.Lerp(transform.forward, aimDir, rotateSpeed * Time.deltaTime);
                break;
            case State.Attacking:
                if(canAttack)
                {
                    Attack();
                    canAttack = false;
                }
                break;
            case State.Cooloff:
                break;
        }

        if(stateTimer <= 0f)
            NextState();
    }

    private void NextState()
    {
        switch(state)
        {
            case State.Aiming:
                state = State.Attacking;
                stateTimer = attackingStateTime;
                break;
            case State.Attacking:
                state = State.Cooloff;
                stateTimer = cooloffStateTime;
                break;
            case State.Cooloff:
                ActionComplete();
                break;
        }
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        GridPosition roleGridPos = role.GetGridPosition();
        return GetValidActionGridPositionList(roleGridPos);
    }

    public List<GridPosition> GetValidActionGridPositionList(GridPosition roleGridPos)
    {
        // 获取可以移动的区域
        List<GridPosition> validGridPositionList = new List<GridPosition>();

        GridPosition roleGridPosition = role.GetGridPosition();
        for (int x = -maxAttackDistance; x <= maxAttackDistance; x++)
        {
            for (int z = -maxAttackDistance; z <= maxAttackDistance; z++)
            {
                GridPosition offsetGridPosition = new GridPosition(x, z);
                GridPosition testGridPosition = roleGridPosition + offsetGridPosition;

                // 检测不能移动的位置
                if (!LevelGrid.instance.IsValidGridPosition(testGridPosition))
                    continue;

                // 检测是圆形，要排除一些距离
                int testDistance = Mathf.Abs(x) + Mathf.Abs(z);
                if (testDistance > maxAttackDistance) continue;

                if (!LevelGrid.instance.HasAnyRoleOnGridPosition(testGridPosition))
                    continue;

                Role targetRole = LevelGrid.instance.GetRoleAtGridPosition(testGridPosition);
                if (targetRole == null || targetRole.IsEnemy() == role.IsEnemy()) continue;

                validGridPositionList.Add(testGridPosition);
            }
        }

        return validGridPositionList;
    }

    public override void TakeAction(GridPosition gridPosition, Action _onActionComplete)
    {
        targetRole = LevelGrid.instance.GetRoleAtGridPosition(gridPosition);

        state = State.Aiming;
        stateTimer = aimingStateTime;
        canAttack = true;

        ActionStart(_onActionComplete);
    }

    public override int GetActionPointsCost()
    {
        return 1;
    }

    private void Attack()
    {
        onAttack?.Invoke(this, new OnAttackEventArgs
        {
            targetRole = targetRole,
            attackingRole = role
        });
    }

    public int GetAttackRange()
    {
        return maxAttackDistance;
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPos)
    {
        Role targetRole = LevelGrid.instance.GetRoleAtGridPosition(gridPos);

        return new EnemyAIAction
        {
            gridPosition = gridPos,
            activeValue = 100 + Mathf.RoundToInt((1 - targetRole.GetHealthNormalized()) * 100f)
        };
    }

    public int GetTargetCountAtPosition(GridPosition gridPosition)
    {
        return GetValidActionGridPositionList(gridPosition).Count;
    }
}
