using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DefenseAction : BaseAction
{
    private float totalSpinAmount;

    public override string GetName()
    {
        return "·ÀÓù";
    }

    private void Update()
    {
        if (!isActive) return;
        float spinAddAmount = 360f * Time.deltaTime;
        transform.eulerAngles += new Vector3(0, spinAddAmount, 0);
        totalSpinAmount += spinAddAmount;
        if(totalSpinAmount >= 360f)
        {
            ActionComplete();
        }
        
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        GridPosition roleGridPos = role.GetGridPosition();
        return new List<GridPosition> { roleGridPos };
    }

    public override void TakeAction(GridPosition gridPosition, Action _onActionComplete)
    {
        ActionStart(_onActionComplete);
        totalSpinAmount = 0f;
    }

    public override int GetActionPointsCost()
    {
        return 2;
    }
}
