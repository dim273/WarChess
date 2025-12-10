using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Role : MonoBehaviour
{
    public static event EventHandler OnAnyActionPointsChanged;
    public static event EventHandler OnAnyRoleSpawned;
    public static event EventHandler OnAnyRoleDead;

    [Header("Config")]
    [SerializeField] private int maxActionPoints;
    [SerializeField] private bool isEnemy;

    private int actionPoints;

    private GridPosition gridPosition;
    private HealthSystem healthSystem;

    private MoveAction moveAction;
    private DefenseAction defenseAction;
    private AttackAction attackAction;

    private BaseAction[] baseActionArray;
    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        moveAction = GetComponent<MoveAction>();
        defenseAction = GetComponent<DefenseAction>();
        attackAction = GetComponent<AttackAction>();
        baseActionArray = GetComponents<BaseAction>();
        actionPoints = maxActionPoints;
        
    }
    private void Start()
    {
        gridPosition = LevelGrid.instance.GetGridPosition(transform.position);
        LevelGrid.instance.AddRoleAtGridPosition(gridPosition, this);

        TurnSystem.instance.OnTurnChanged += TurnSystem_OnTurnChanged;
        healthSystem.onRoleDie += healthSystem_onRoleDie;

        OnAnyRoleSpawned?.Invoke(this, EventArgs.Empty);
    }
    private void Update()
    {
        GridPosition newGridPosition = LevelGrid.instance.GetGridPosition(transform.position);
        if (newGridPosition != gridPosition)
        {
            GridPosition oldPos = gridPosition;
            gridPosition = newGridPosition;
            LevelGrid.instance.RoleMovedGridPosition(this, oldPos, newGridPosition);
        }
    }

    public MoveAction GetMoveAction() => moveAction;
    public DefenseAction GetDefenseAction() => defenseAction;
    public AttackAction GetAttackAction() => attackAction;

    public int GetActionPoint() => actionPoints;
    public Vector3 GetWorldPosition() => transform.position;
    public float GetHealthNormalized() => healthSystem.GetHealthNormalized();
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
    
    public void Damage(int damage)
    {
        healthSystem.Damage(damage);
    }

    private void healthSystem_onRoleDie(object sender, EventArgs e)
    {
        // 死亡
        LevelGrid.instance.RemoveRoleAtGridPosition(gridPosition, this);
        Destroy(gameObject);
        OnAnyRoleDead?.Invoke(this, EventArgs.Empty);
    }
}
