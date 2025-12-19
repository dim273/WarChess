using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Role_A_Attack : RoleAttackBase
{
    [Header("AttackFX")]
    [SerializeField] private float closeFXTime;
    RoleAAttackHelp helpA = null;


    public override void SetUp(Vector3 _targetPos)
    {
        targetGridPos = LevelGrid.instance.GetGridPosition(_targetPos);
        
        Role target = LevelGrid.instance.GetRoleAtGridPosition(targetGridPos);
        target.Damage(damage);

        Role roleA = RoleActionSystem.instance.GetSelectedRole();
        helpA = roleA.gameObject.GetComponent<RoleAAttackHelp>();
        if (helpA != null )
        {
            Debug.Log("Open");
            helpA.SetUp(true);
        }

        StartCoroutine(CloseAttackFX());
    }

    private IEnumerator CloseAttackFX()
    {
        yield return new WaitForSeconds(closeFXTime);
        Debug.Log("×¢ÏúÁË");
        if (helpA != null)
        {
            Debug.Log("Close");
            helpA.SetUp(false);
        }
        Destroy(gameObject);
    }
}
