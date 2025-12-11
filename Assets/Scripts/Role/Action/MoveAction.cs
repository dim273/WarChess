using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MoveAction : BaseAction
{
    public event EventHandler OnMoveStart;
    public event EventHandler OnMoveEnd;

    [Header("Config")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float rotationSpeed;
    [SerializeField] private float stoppingDistance;
    [SerializeField] private int maxMoveDistance;

    private Vector3 targetPosition;

    protected override void Awake()
    {
        base.Awake();
        targetPosition = transform.position;
    }

    private void Update()
    {
        if (!isActive) return;

        if (Vector3.Distance(transform.position, targetPosition) > stoppingDistance)
        {
            Vector3 moveDirection = (targetPosition - transform.position).normalized;
            transform.position += moveDirection * moveSpeed * Time.deltaTime;

            transform.forward = Vector3.Lerp(transform.forward, moveDirection, Time.deltaTime * rotationSpeed);
        }
        else
        {
            OnMoveEnd?.Invoke(this, EventArgs.Empty);
            ActionComplete();
        }
    }

    public override void TakeAction(GridPosition gridPosition, Action _onActionComplete)
    {
        targetPosition = LevelGrid.instance.GetWorldPosition(gridPosition);
        OnMoveStart?.Invoke(this, EventArgs.Empty);
        ActionStart(_onActionComplete);
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        // 获取可以移动的区域
        List<GridPosition> validGridPositionList = new List<GridPosition>();

        GridPosition roleGridPosition = role.GetGridPosition();
        for (int x = -maxMoveDistance; x <= maxMoveDistance; x ++)
        {
            for (int z = -maxMoveDistance; z <= maxMoveDistance; z ++)
            {
                GridPosition offsetGridPosition = new GridPosition(x, z);
                GridPosition testGridPosition = roleGridPosition + offsetGridPosition;

                // 检测不能移动的位置
                if (!LevelGrid.instance.IsValidGridPosition(testGridPosition))
                    continue;
                if (roleGridPosition == testGridPosition)
                    continue;
                if (LevelGrid.instance.HasAnyRoleOnGridPosition(testGridPosition))
                    continue;

                validGridPositionList.Add(testGridPosition); 
            }
        }

        return validGridPositionList;
    }

    public override string GetName()
    {
        return "移动";
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPos)
    {
        int tagetCountAtGridPosition = 
            role.GetAction<AttackAction>().GetTargetCountAtPosition(gridPos);

        return new EnemyAIAction
        {
            gridPosition = gridPos,
            activeValue = 10 * tagetCountAtGridPosition,
        };
    }
}
