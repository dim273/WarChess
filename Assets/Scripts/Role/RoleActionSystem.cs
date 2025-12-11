using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class RoleActionSystem : Singleton<RoleActionSystem>
{
    public event EventHandler OnSelectedRoleChanged;
    public event EventHandler OnSelectedActionChanged;
    public event EventHandler<bool> OnBusyChanged;
    public event EventHandler OnActionStarted;

    [SerializeField] private Role selectedRole;
    [SerializeField] private LayerMask roleLayerMask;

    private BaseAction selectedAction;
    private bool isBusy;

    private void Start()
    {
        SetSelectedRole(selectedRole);
    }

    private void Update()
    {
        // 检测是否在执行某个任务
        if (isBusy) return;
        // 检测是否是敌人回合
        if (!TurnSystem.instance.IsPlayerTurn()) return;
        // 检测是否点击到了UI
        if (EventSystem.current.IsPointerOverGameObject()) return;

        if (TryHandleRoleSelection()) return;

        HandleSelectedAction();

    }

    private void HandleSelectedAction()
    {
        if(Input.GetMouseButtonDown(0))
        {
            GridPosition mouseGridPos = LevelGrid.instance.GetGridPosition(MouseWorld.instance.GetPosition());
            if (!selectedAction.IsValidActionGridPosition(mouseGridPos))
                return;
            if (!selectedRole.TrySpendActionPointsToTakeAction(selectedAction))
                return;

            SetBusy();
            selectedAction.TakeAction(mouseGridPos, ClearBusy);
            OnActionStarted?.Invoke(this, EventArgs.Empty);
        }
    }

    // 选择指定角色
    private bool TryHandleRoleSelection()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit raycastHit, float.MaxValue, roleLayerMask))
            {
                if (raycastHit.transform.TryGetComponent<Role>(out Role role))
                {
                    if (role == selectedRole) return false; // 若已经选中，则不会触发
                    if (role.IsEnemy()) return false;       // 若为敌人，也不会触发

                    SetSelectedRole(role);
                    return true;
                }
            }
        }
        return false;
    }

    private void SetSelectedRole(Role role)
    {
        selectedRole = role;
        SetSelectedAction(role.GetAction<MoveAction>());
        OnSelectedRoleChanged?.Invoke(this, EventArgs.Empty);
    }

    public Role GetSelectedRole() => selectedRole;

    public BaseAction GetSelectedAction() => selectedAction;

    public void SetSelectedAction(BaseAction baseAction)
    {
        selectedAction = baseAction;
        OnSelectedActionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SetBusy()
    {
        isBusy = true;
        OnBusyChanged?.Invoke(this, true);
    }
    private void ClearBusy()
    {
        isBusy = false;
        OnBusyChanged?.Invoke(this, false);
    }
}
