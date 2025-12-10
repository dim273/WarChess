using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ActionSystemUI : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private Transform actionButtonPrefab;
    [SerializeField] private Transform container;
    [SerializeField] private TextMeshProUGUI actionPointTMP;

    private List<ActionButtonUI> actionButtonList;

    private void Awake()
    {
        actionButtonList = new List<ActionButtonUI>();
    }

    private void Start()
    {
        RoleActionSystem.instance.OnSelectedActionChanged += ActionSystemUI_OnSelectedActionChanged;
        RoleActionSystem.instance.OnSelectedRoleChanged += ActionSystemUI_OnSelectedRoleChanged;
        RoleActionSystem.instance.OnActionStarted += ActionSystemUI_OnActionStarted;
        Role.OnAnyActionPointsChanged += Role_OnAnyActionPointsChanged;
        TurnSystem.instance.OnTurnChanged += TurnSystem_OnTurnChanged;

        CreateRoleActionButtons();
        UpdateSelectedVisual();
        UpdateActionPointsUI();
    }

    private void CreateRoleActionButtons()
    {
        foreach (Transform buttonTranform in container)
        {
            Destroy(buttonTranform.gameObject);
        }

        actionButtonList.Clear();

        Role selectedRole = RoleActionSystem.instance.GetSelectedRole();

        foreach (BaseAction baseAction in selectedRole.GetBaseActionArray())
        {
            Transform acButton = Instantiate(actionButtonPrefab, container);
            ActionButtonUI acButtonUI = acButton.GetComponent<ActionButtonUI>();
            acButtonUI.SetBaseAction(baseAction);
            actionButtonList.Add(acButtonUI);
        }
    }

    private void UpdateSelectedVisual()
    {
        // 更新图标的显示
        foreach (ActionButtonUI actionButton in actionButtonList)
        {
            actionButton.UpdateSelectedVisual();
        }
    }

    private void UpdateActionPointsUI()
    {
        // 更新任务点消耗
        Role role = RoleActionSystem.instance.GetSelectedRole();
        actionPointTMP.text = "行动点剩余：" + role.GetActionPoint();
    }

    private void ActionSystemUI_OnSelectedRoleChanged(object sender, EventArgs e)
    {
        CreateRoleActionButtons();
        UpdateSelectedVisual();
        UpdateActionPointsUI();
    }

    private void ActionSystemUI_OnSelectedActionChanged(object sender, EventArgs e)
    {
        UpdateSelectedVisual();
    }

    private void ActionSystemUI_OnActionStarted(object sender, EventArgs e)
    {
        UpdateActionPointsUI();
    }

    private void TurnSystem_OnTurnChanged(object sender, EventArgs e)
    {
        UpdateActionPointsUI();
    }

    private void Role_OnAnyActionPointsChanged(object sender, EventArgs e)
    {
        // 避免出现下一回开始，但是没有刷新行动点的错误
        UpdateActionPointsUI();
    }
}
