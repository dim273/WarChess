using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoleSelectedVisual : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private Role role;
    private MeshRenderer meshRenderer;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    private void Start()
    {
        RoleActionSystem.instance.OnSelectedRoleChanged += RoleManager_OnSelectedRoleChanged;

        UpdateVisual();
    }

    private void RoleManager_OnSelectedRoleChanged(object sender, System.EventArgs e)
    {
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        // ¸üÐÂ×´Ì¬
        if (RoleActionSystem.instance.GetSelectedRole() == role)
            meshRenderer.enabled = true;
        else 
            meshRenderer.enabled = false;
    }

    private void OnDestroy()
    {
        RoleActionSystem.instance.OnSelectedRoleChanged -= RoleManager_OnSelectedRoleChanged;
    }
}
