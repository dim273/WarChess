using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoleManager : Singleton<RoleManager>
{
    private List<Role> roles;
    private List<Role> playerRoles;
    private List<Role> enemyRoles;

    protected override void Awake()
    {
        base.Awake();
        roles = new List<Role>();
        playerRoles = new List<Role>();
        enemyRoles = new List<Role>();
    }

    private void Start()
    {
        Role.OnAnyRoleSpawned += Role_OnAnyRoleSpawned;
        Role.OnAnyRoleDead += Role_OnAnyRoleDead;
    }

    private void Role_OnAnyRoleSpawned(object sender, EventArgs e)
    {
        Role role = sender as Role;
        roles.Add(role);
        if(role.IsEnemy()) 
            enemyRoles.Add(role);
        else 
            playerRoles.Add(role);
    }

    private void Role_OnAnyRoleDead(object sender, EventArgs e)
    {
        Role role = sender as Role;
        roles.Remove(role);
        if(role.IsEnemy())
            enemyRoles.Remove(role);
        else
            playerRoles.Remove(role);
    }

    public List<Role> GetAllRoles() => roles;
    public List<Role> GetAllPlayers() => playerRoles;
    public List<Role> GetAllEnemise() => enemyRoles;
}
