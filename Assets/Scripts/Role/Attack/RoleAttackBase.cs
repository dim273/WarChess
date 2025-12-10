using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class RoleAttackBase : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] protected int damage;

    protected Vector3 targetPos;
    protected GridPosition targetGridPos;
    public abstract void SetUp(Vector3 _targetPos);
}
