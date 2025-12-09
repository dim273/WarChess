using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridSystemVisual : Singleton<GridSystemVisual>
{
    [Header("Config")]
    [SerializeField] private Transform gridSystemSinglePrefab;

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
    }

    private void Update()
    {
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

    public void ShowGridPosition(List<GridPosition> gridPositions)
    {
        foreach (GridPosition gridPosition in gridPositions)
        {
            gridSystemVisualSingleArray[gridPosition.x, gridPosition.z].Show();
        }
    }

    private void UpdateGridSystemVisual()
    {
        HideAllGridPosition();
        BaseAction action = RoleActionSystem.instance.GetSelectedAction();
        ShowGridPosition(action.GetValidActionGridPositionList());
    }
   
}
