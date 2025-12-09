using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class RoleAttackBase : MonoBehaviour
{
    protected Vector3 targetPos;
    public abstract void SetUp(Vector3 _targetPos);
}
