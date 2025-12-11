using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    [SerializeField] private Role Role;

    private void Start()
    {
        
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            GridPosition mouseGridPos = LevelGrid.instance.GetGridPosition(MouseWorld.instance.GetPosition());
            GridPosition startGridPos = new GridPosition(0, 0);

            List<GridPosition> gridPositionList = Pathfinding.instance.FindPath(startGridPos, mouseGridPos);

            for (int i = 0; i < gridPositionList.Count - 1; i++)
            {
                Debug.DrawLine(
                    LevelGrid.instance.GetWorldPosition(gridPositionList[i]),
                    LevelGrid.instance.GetWorldPosition(gridPositionList[i + 1]),
                    Color.green,
                    10f
                );
            }
        }
    }
}
