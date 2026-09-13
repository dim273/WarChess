using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Role_B_Attack : RoleAttackBase
{
    [Header("Config")]
    [SerializeField] private TrailRenderer trail;
    [SerializeField] private Transform hitFX;
    [SerializeField] private float speed;

    private float posY;
    
 
    public override void SetUp(Vector3 _targetPos)
    {
        targetGridPos = LevelGrid.instance.GetGridPosition(_targetPos);
        posY = transform.position.y;
        targetPos = new Vector3(_targetPos.x, posY, _targetPos.z);
    }

    private void Update()
    {
        Vector3 moveDir = (targetPos - transform.position).normalized;
        float distanceBeforeMoving = Vector3.Distance(transform.position, targetPos);
        transform.position += moveDir * speed * Time.deltaTime;
        transform.position = new Vector3(transform.position.x, posY, transform.position.z); 
        float distanceAfterMoving = Vector3.Distance(transform.position, targetPos);

        if (distanceAfterMoving > distanceBeforeMoving) 
        {
            Role target = LevelGrid.instance.GetRoleAtGridPosition(targetGridPos);
            target.Damage(damage);
            transform.position = targetPos;
            Instantiate(hitFX, transform.position, Quaternion.identity);
            trail.transform.parent = null;
            Destroy(gameObject);
        }
    }
}
