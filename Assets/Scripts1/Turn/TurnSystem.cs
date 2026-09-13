using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TurnSystem : Singleton<TurnSystem>
{
    public event EventHandler OnTurnChanged;

    private int turnNumber = 1;

    private bool isPlayerTurn = true;
    public void NextTurn()
    {
        turnNumber++;
        isPlayerTurn = !isPlayerTurn;
        OnTurnChanged?.Invoke(this, EventArgs.Empty);
    }

    public int GetTurnNumber() => turnNumber;

    public bool IsPlayerTurn() => isPlayerTurn;
}
