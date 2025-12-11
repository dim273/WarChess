using System.Collections;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class LevelGrid : Singleton<LevelGrid>
{
    public event EventHandler onRoleMoveToNewGird;

    [SerializeField] private Transform gridDebugPrefab;

    private GridSystem<GridObject> gridSystem;

    protected override void Awake()
    {
        base.Awake();
        gridSystem = new GridSystem<GridObject>(10, 10, 2f, 
            (GridSystem<GridObject> g, GridPosition gridPosition) => new GridObject(g, gridPosition));
        // gridSystem.CreateDebugObjects(gridDebugPrefab);
    }

    public void AddRoleAtGridPosition(GridPosition gridPosition, Role role)
    {
        GridObject gridObject = gridSystem.GetGridObject(gridPosition);
        gridObject.AddRole(role);
    }

    public List<Role> GetRoleListAtGridPosition(GridPosition gridPosition)
    {
        GridObject gridObject = gridSystem.GetGridObject(gridPosition);
        return gridObject.GetRoleList();
    }

    public void RemoveRoleAtGridPosition(GridPosition gridPosition, Role role)
    {
        GridObject gridObject = gridSystem.GetGridObject(gridPosition);
        gridObject.RemoveRole(role);
    }

    public void RoleMovedGridPosition(Role role, GridPosition fromPos, GridPosition toPos)
    {
        RemoveRoleAtGridPosition(fromPos, role);
        AddRoleAtGridPosition(toPos, role);
        onRoleMoveToNewGird?.Invoke(this, EventArgs.Empty);
    }

    public int GetWidth() => gridSystem.GetWidth();
    public int GetHeight() => gridSystem.GetHeight();

    public GridPosition GetGridPosition(Vector3 worldPos) => gridSystem.GetGridPosition(worldPos);
    public Vector3 GetWorldPosition(GridPosition gridPos) => gridSystem.GetWorldPosition(gridPos);
    public bool IsValidGridPosition(GridPosition gridPosition) => gridSystem.IsValidGridPosition(gridPosition);

    public bool HasAnyRoleOnGridPosition(GridPosition gridPos)
    {
        // 检测是否已经存在角色
        GridObject gridObject = gridSystem.GetGridObject(gridPos);
        return gridObject.HasAnyRole();
    }

     public Role GetRoleAtGridPosition(GridPosition gridPos)
    {
        // 获取当前地块上的角色
        GridObject gridObject = gridSystem.GetGridObject(gridPos);
        return gridObject.GetRole();
    }

}
