using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridObject
{
    private GridSystem gridSystem;
    private GridPosition gridPosition;
    private List<Role> roleList;

    public GridObject(GridSystem gridSystem, GridPosition gridPosition)
    {
        this.gridSystem = gridSystem;
        this.gridPosition = gridPosition;
        roleList = new List<Role>();
    }

    public override string ToString()
    {
        string roleString = "";
        foreach (Role role in roleList) 
        {
            roleString += role + "\n";
        }
        return gridPosition.ToString() + "\n" + roleString;
    }

    public void AddRole(Role role)
    {
        roleList.Add(role);
    }
    public void RemoveRole(Role role)
    {
        roleList.Remove(role);
    }
    public List<Role> GetRoleList()
    {
        return roleList;
    }

    public bool HasAnyRole()
    {
        return roleList.Count > 0;
    }

    public Role GetRole()
    {
        if(HasAnyRole())
            return roleList[0];
        else
            return null;
    }
}
