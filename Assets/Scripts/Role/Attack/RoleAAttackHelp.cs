using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoleAAttackHelp : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private GameObject FX_Left;
    [SerializeField] private GameObject FX_Right;

    public void SetUp(bool _value)
    {
        FX_Left.SetActive(_value);
        FX_Right.SetActive(_value);
    }
}
