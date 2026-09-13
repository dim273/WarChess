using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoleWorldUI : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private TextMeshProUGUI actionPointsTMP;
    [SerializeField] private Role role;
    [SerializeField] private Image healthBarImage;
    [SerializeField] private HealthSystem healthSystem;

    private void Start()
    {
        Role.OnAnyActionPointsChanged += Role_OnAnyActionPointsChanged;
        healthSystem.onDamage += healthSystem_onDamage;

        UpdateActionPointsText();
        UpdateHealthBar();
    }

    private void UpdateActionPointsText()
    {
        actionPointsTMP.text = role.GetActionPoint().ToString();
    }

    private void UpdateHealthBar()
    {
        healthBarImage.fillAmount = healthSystem.GetHealthNormalized();
    }

    private void healthSystem_onDamage(object sender, EventArgs e)
    {
        UpdateHealthBar();
    }

    private void Role_OnAnyActionPointsChanged(object sender, EventArgs e)
    {
        UpdateActionPointsText();
    }
}
