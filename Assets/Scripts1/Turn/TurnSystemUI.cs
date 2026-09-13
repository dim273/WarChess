using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class TurnSystemUI : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private Button endTurnBtn;
    [SerializeField] private TextMeshProUGUI turnInfoTMP;
    [SerializeField] private GameObject enemyTurnVisualUI;

    private void Start()
    {
        endTurnBtn.onClick.AddListener(() =>
        {
            TurnSystem.instance.NextTurn();
        });
        TurnSystem.instance.OnTurnChanged += TurnSystem_OnTurnChanged;

        UpdateTurnText();
        SetEnemyTurnVisualUI();
        SetNextTurnButton();
    }

    private void TurnSystem_OnTurnChanged(object sender, EventArgs e)
    {
        UpdateTurnText();
        SetEnemyTurnVisualUI();
        SetNextTurnButton();
    }

    private void UpdateTurnText()
    {
        turnInfoTMP.text = "当前回合：" + TurnSystem.instance.GetTurnNumber();
    }

    private void SetEnemyTurnVisualUI()
    {
        enemyTurnVisualUI.SetActive(!TurnSystem.instance.IsPlayerTurn());
    }

    private void SetNextTurnButton()
    {
        endTurnBtn.gameObject.SetActive(TurnSystem.instance.IsPlayerTurn());
    }
}
