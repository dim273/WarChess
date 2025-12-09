using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Role_B_Attack : RoleAttackBase
{
    [Header("Config")]
    [SerializeField] private TrailRenderer trail;
    [SerializeField] private GameObject hitFX;
    [SerializeField] private float speed;

    private float posY;
 
    public override void SetUp(Vector3 _targetPos)
    {
        targetPos = _targetPos;
        posY = transform.position.y;
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
            transform.position = targetPos;

            trail.transform.parent = null;
            Destroy(gameObject);
            Instantiate(hitFX, targetPos, Quaternion.identity);
        }
    }

}
