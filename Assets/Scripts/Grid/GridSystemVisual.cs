using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridSystemVisual : Singleton<GridSystemVisual>
{
    public enum GridVisualType
    {
        White,
        Blue,
        Red,
        Yellow,
        RedSoft
    }

    [Serializable]
    public struct GridVisualTypeMaterial
    {
        public GridVisualType gridVisualType;
        public Material material;
    }

    [Header("Config")]
    [SerializeField] private Transform gridSystemSinglePrefab;
    [SerializeField] private List<GridVisualTypeMaterial> gridVTMat;

    private GridSystemVisualSingle[,] gridSystemVisualSingleArray;

    private void Start()
    {
        gridSystemVisualSingleArray = new GridSystemVisualSingle[
            LevelGrid.instance.GetWidth(),
            LevelGrid.instance.GetHeight()
        ];

        for (int x = 0; x < LevelGrid.instance.GetWidth(); x++)
        {
            for (int z = 0; z < LevelGrid.instance.GetHeight(); z++)
            {
                GridPosition gridPosition = new GridPosition(x, z);
                Transform gridSVST = Instantiate(gridSystemSinglePrefab, LevelGrid.instance.GetWorldPosition(gridPosition), Quaternion.identity);
                gridSystemVisualSingleArray[x,z] = gridSVST.GetComponent<GridSystemVisualSingle>();
            }
        }

        LevelGrid.instance.onRoleMoveToNewGird += LevelGrid_onRoleMoveToNewGird;
        RoleActionSystem.instance.OnSelectedActionChanged += RoleActionSystem_OnSelectedActionChanged;

        UpdateGridSystemVisual();
    }

    public void HideAllGridPosition()
    {
        for (int x = 0; x < LevelGrid.instance.GetWidth(); x++) 
        {
            for (int z = 0; z < LevelGrid.instance.GetHeight(); z++) 
            {
                gridSystemVisualSingleArray[x, z].Hide();
            }
        }
    }

    public void ShowGridPosition(List<GridPosition> gridPositions, GridVisualType type)
    {
        foreach (GridPosition gridPosition in gridPositions)
        {
            gridSystemVisualSingleArray[gridPosition.x, gridPosition.z].Show(
                GetGridVisualTypeMaterial(type));
        }
    }

    private void ShowGridPositionRange(GridPosition gridPos, int range, GridVisualType type)
    {
        List<GridPosition> gridPosList = new List<GridPosition>();

        for(int x = -range;  x <= range; x++)
        {
            for (int z = -range; z <= range; z++)
            {
                GridPosition testGridPos = gridPos + new GridPosition(x, z);
                if (!LevelGrid.instance.IsValidGridPosition(testGridPos))
                    continue;
                int distance = Mathf.Abs(x) + Mathf.Abs(z);
                if (distance > range)
                    continue;
                gridPosList.Add(testGridPos);
            }
        }
        ShowGridPosition(gridPosList, type);
    }

    private void UpdateGridSystemVisual()
    {
        HideAllGridPosition();
        BaseAction action = RoleActionSystem.instance.GetSelectedAction();
        Role role = RoleActionSystem.instance.GetSelectedRole(); 

        GridVisualType type;
        switch (action)
        {
            default:
            case MoveAction moveAction:
                type = GridVisualType.White;
                break;
            case DefenseAction defenseAction:
                type = GridVisualType.Blue;
                break;
            case AttackAction attackAction:
                type = GridVisualType.Red;
                ShowGridPositionRange(role.GetGridPosition(), attackAction.GetAttackRange(), GridVisualType.RedSoft);
                break;
        }

        ShowGridPosition(action.GetValidActionGridPositionList(), type);
    }

    private void LevelGrid_onRoleMoveToNewGird(object sender, EventArgs e)
    {
        UpdateGridSystemVisual();
    }

    private void RoleActionSystem_OnSelectedActionChanged(object sender, EventArgs e)
    {
        UpdateGridSystemVisual();
    }

    private Material GetGridVisualTypeMaterial(GridVisualType type)
    {
        // 遍历材质数组，获取对应的材质
        foreach (GridVisualTypeMaterial mat in gridVTMat)
        {
            if (mat.gridVisualType == type)
            {
                return mat.material;
            }
        }
        return null;
    }
}
