using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Role : MonoBehaviour
{
    public static event EventHandler OnAnyActionPointsChanged;

    [Header("Config")]
    [SerializeField] private int maxActionPoints;
    [SerializeField] private bool isEnemy;

    private int actionPoints;

    private GridPosition gridPosition;

    private MoveAction moveAction;
    private DefenseAction defenseAction;
    private BaseAction[] baseActionArray;
    private void Awake()
    {
        moveAction = GetComponent<MoveAction>();
        defenseAction = GetComponent<DefenseAction>();
        baseActionArray = GetComponents<BaseAction>();
    }
    private void Start()
    {
        actionPoints = maxActionPoints;
        gridPosition = LevelGrid.instance.GetGridPosition(transform.position);
        LevelGrid.instance.AddRoleAtGridPosition(gridPosition, this);

        TurnSystem.instance.OnTurnChanged += TurnSystem_OnTurnChanged;
    }
    private void Update()
    {
        GridPosition newGridPosition = LevelGrid.instance.GetGridPosition(transform.position);
        if (newGridPosition != gridPosition)
        {
            LevelGrid.instance.RoleMovedGridPosition(this, gridPosition, newGridPosition);
            gridPosition = newGridPosition;
        }
    }

    public MoveAction GetMoveAction() => moveAction;

    public DefenseAction GetDefenseAction() => defenseAction;

    public int GetActionPoint() => actionPoints;

    public Vector3 GetWorldPosition() => transform.position;

    public bool IsEnemy() => isEnemy;

    public GridPosition GetGridPosition()
    {
        return gridPosition;
    }

    public BaseAction[] GetBaseActionArray() => baseActionArray;

    public bool TrySpendActionPointsToTakeAction(BaseAction action)
    {
        // 尝试消耗行动点做出指定行动
        if (CanSpendActionPointsToTakeAction(action))
        {
            SpendActionPoints(action.GetActionPointsCost());
            return true;
        }
        return false;
    }

    public bool CanSpendActionPointsToTakeAction(BaseAction action)
    {
        // 检查行动点是否足够
        if (actionPoints >= action.GetActionPointsCost())
        {
            return true;
        }
        return false;
    }

    public void SpendActionPoints(int amount)
    {
        // 消耗行动点
        actionPoints -= amount;
        OnAnyActionPointsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void TurnSystem_OnTurnChanged(object sender, EventArgs e)
    {
        if ((IsEnemy() && !TurnSystem.instance.IsPlayerTurn()) || (!IsEnemy() && TurnSystem.instance.IsPlayerTurn()))
        {
            // 下一回合开始时的操作
            actionPoints = maxActionPoints;
            OnAnyActionPointsChanged?.Invoke(this, EventArgs.Empty);
        }
    }  
    
    public void Damage()
    {
        Debug.Log("Damage");
    }
}
