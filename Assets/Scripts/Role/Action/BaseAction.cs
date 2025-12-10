using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseAction : MonoBehaviour
{
    public event EventHandler onActionStarted;
    public event EventHandler onActionFinished;

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
        onActionStarted?.Invoke(this, EventArgs.Empty);
    }

    protected void ActionComplete()
    {
        isActive = false;
        onActionComplete();
        onActionFinished?.Invoke(this, EventArgs.Empty);
    }

    public Role GetRole() { return role; }

    // 敌人专属代码
    /**************************************************************************/
    public EnemyAIAction GetBestEnemyAIAction()
    {
        List<EnemyAIAction> enemyAIActionList = new List<EnemyAIAction>();
        List<GridPosition> vaildActionGridPositionList = GetValidActionGridPositionList();
        foreach (GridPosition gridPosition in vaildActionGridPositionList)
        {
            EnemyAIAction enemyAIAction = GetEnemyAIAction(gridPosition);
            enemyAIActionList.Add(enemyAIAction);
        }

        if (enemyAIActionList.Count > 0)
        {
            enemyAIActionList.Sort((EnemyAIAction a, EnemyAIAction b) => b.activeValue - a.activeValue);
            return enemyAIActionList[0];
        }
        else
        {
            return null;
        }
    }

    public abstract EnemyAIAction GetEnemyAIAction(GridPosition gridPos);
}
