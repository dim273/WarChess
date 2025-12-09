using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    private float timer;

    private void Start()
    {
        TurnSystem.instance.OnTurnChanged += TurnSystem_OnTurnChanged;
    }

    private void Update()
    {
        if (TurnSystem.instance.IsPlayerTurn()) return;

        timer -= Time.deltaTime;

        if (timer <= 0)
            TurnSystem.instance.NextTurn();

    }

    private void TurnSystem_OnTurnChanged(object sender, EventArgs e)
    {
        timer = 3f;
    }
}
