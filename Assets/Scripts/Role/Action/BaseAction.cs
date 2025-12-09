using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseAction : MonoBehaviour
{
    protected Role role;
    protected bool isActive;
    protected Action onActionComplete;

    protected virtual void Awake()
    {
        role = GetComponent<Role>();
    }

    public abstract string GetName();

    public abstract void TakeAction(GridPosition gridPosition, Action _onActionComplete);

    public virtual bool IsValidActionGridPosition(GridPosition gridPos)
    {
        // 检测角色是否可以移动到当前位置
        List<GridPosition> validGridPosition = GetValidActionGridPositionList();
        return validGridPosition.Contains(gridPos);
    }

    public abstract List<GridPosition> GetValidActionGridPositionList();

    public virtual int GetActionPointsCost() => 1;

    protected void ActionStart(Action _onActionComplete)
    {
        isActive = true;
        onActionComplete = _onActionComplete;
    }

    protected void ActionComplete()
    {
        isActive = false;
        onActionComplete();
    }
}
