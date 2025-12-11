using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pathfinding : Singleton<Pathfinding>
{
    private const int MOVE_STRAIGHT_COST = 10;
    private const int MOVE_DIAGONAL_COST = 14;

    [Header("Config")]
    [SerializeField] private Transform gridDebugObjectrefab;

    private int width;
    private int height;
    private float cellSize;
    private GridSystem<PathNode> gridSystem;

    protected override void Awake()
    {
        base.Awake();
        gridSystem = new GridSystem<PathNode>(10, 10, 2f,
            (GridSystem<PathNode> g, GridPosition gridPosition) => new PathNode(gridPosition));
        gridSystem.CreateDebugObjects(gridDebugObjectrefab);
    }

    public List<GridPosition> FindPath(GridPosition startGridPos,  GridPosition endGridPos)
    {
        List<PathNode> openList = new List<PathNode>();
        List<PathNode> closeList = new List<PathNode>();

        PathNode startNode = gridSystem.GetGridObject(startGridPos);
        PathNode endNode = gridSystem.GetGridObject(endGridPos);
        openList.Add(startNode);

        // 初始化
        for (int x = 0; x < gridSystem.GetWidth(); x++)
        {
            for (int z = 0;  z < gridSystem.GetHeight(); z++)
            {
                GridPosition gridPos = new GridPosition(x, z);
                PathNode pathNode = gridSystem.GetGridObject(gridPos);

                pathNode.SetGCost(int.MaxValue);
                pathNode.SetHCost(0);
                pathNode.CalculateFCost();
                pathNode.ResetCameFromPathNode();
            }
        }

        startNode.SetGCost(0);
        startNode.SetHCost(CalculateDistance(startGridPos, endGridPos));
        startNode.CalculateFCost();

        while (openList.Count > 0)
        {
            PathNode curNode = GetLowestFCostPathNode(openList);

            // 到达终点
            if (curNode == endNode)
            {
                return CalculatePath(endNode);
            }

            openList.Remove(curNode);
            closeList.Add(curNode);

            foreach (PathNode neighbourNode in GetNeighbourList(curNode))
            {
                if (closeList.Contains(neighbourNode))
                    continue;
                int tentativeGCost = curNode.GetGCost() + CalculateDistance(curNode.GetGridPosition(), neighbourNode.GetGridPosition());

                if (tentativeGCost < neighbourNode.GetGCost()) 
                {
                    neighbourNode.SetCameFromPathNode(curNode);
                    neighbourNode.SetGCost(tentativeGCost);
                    neighbourNode.SetHCost(CalculateDistance(neighbourNode.GetGridPosition(), endGridPos));
                    neighbourNode.CalculateFCost();

                    if (!openList.Contains(neighbourNode))
                    {
                        openList.Add(neighbourNode);
                    }    
                }
            }
        }
        return null;
    }

    public int CalculateDistance(GridPosition gridPositionA, GridPosition gridPositionB)
    {
        GridPosition gridPositionDistance = gridPositionA - gridPositionB;
        int xDis = Mathf.Abs(gridPositionDistance.x);
        int zDis = Mathf.Abs(gridPositionDistance.z);
        int remaining = Mathf.Abs(xDis - zDis);
        return MOVE_DIAGONAL_COST * Mathf.Min(xDis, zDis) + MOVE_STRAIGHT_COST * remaining;
    }

    private PathNode GetLowestFCostPathNode(List<PathNode> pathNodeList)
    {
        PathNode lowestFCostPathNode = pathNodeList[0];
        for (int i = 1; i < pathNodeList.Count; i++)
        {
            if (pathNodeList[i].GetFCost() < lowestFCostPathNode.GetFCost())
            {
                lowestFCostPathNode = pathNodeList[i];
            }
        }
        return lowestFCostPathNode;
    }

    private PathNode GetNode(int x, int z)
    {
        return gridSystem.GetGridObject(new GridPosition(x, z));
    }

    private List<PathNode> GetNeighbourList(PathNode curNode)
    {
        List<PathNode> neighbourList = new List<PathNode>();

        GridPosition gridPosition = curNode.GetGridPosition();

        if (gridPosition.x - 1 >= 0) 
        {
            neighbourList.Add(GetNode(gridPosition.x - 1, gridPosition.z));
            if (gridPosition.z - 1 >= 0)
                neighbourList.Add(GetNode(gridPosition.x - 1, gridPosition.z - 1));
            if (gridPosition.z + 1 < gridSystem.GetHeight())
                neighbourList.Add(GetNode(gridPosition.x - 1, gridPosition.z + 1));
        }

        if (gridPosition.x + 1 < gridSystem.GetWidth()) 
        {
            neighbourList.Add(GetNode(gridPosition.x + 1, gridPosition.z));
            if (gridPosition.z - 1 >= 0)
                neighbourList.Add(GetNode(gridPosition.x + 1, gridPosition.z - 1));
            if (gridPosition.z + 1 < gridSystem.GetHeight())
                neighbourList.Add(GetNode(gridPosition.x + 1, gridPosition.z + 1));
        }

        if (gridPosition.z - 1 >= 0)
            neighbourList.Add(GetNode(gridPosition.x, gridPosition.z - 1));
        if (gridPosition.z + 1 < gridSystem.GetHeight())
            neighbourList.Add(GetNode(gridPosition.x, gridPosition .z + 1));
        return neighbourList;
    }

    private List<GridPosition> CalculatePath(PathNode endNode)
    {
        List<PathNode> pathNodeList = new List<PathNode>();
        pathNodeList.Add(endNode);
        PathNode curNode = endNode;
        while (curNode.GetCameFromPathNode() != null)
        {
            pathNodeList.Add(curNode.GetCameFromPathNode());
            curNode = curNode.GetCameFromPathNode();
        }
        pathNodeList.Reverse();
        List<GridPosition> gridPositionList = new List<GridPosition>();
        foreach (PathNode pathNode in pathNodeList)
        {
            gridPositionList.Add(pathNode.GetGridPosition());
        }
        return gridPositionList;
    }
}
