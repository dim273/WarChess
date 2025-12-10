using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyRagdollSpawner : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private Transform ragdollPrefab;
    [SerializeField] private Transform originalRootBone;

    private HealthSystem healthSystem;

    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        healthSystem.onRoleDie += healthSystem_onRoleDie;
    }

    private void healthSystem_onRoleDie(object sender, EventArgs e)
    {
        Transform ragdoll = Instantiate(ragdollPrefab, transform.position, transform.rotation);
        ragdoll.GetComponent<RagdollEnemy>().SetUp(originalRootBone);
    }
}
