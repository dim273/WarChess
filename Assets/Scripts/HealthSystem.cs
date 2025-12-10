using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.EventSystems;

public class HealthSystem : MonoBehaviour
{
    public event EventHandler onRoleDie;
    public event EventHandler onDamage;

    [Header("Config")]
    [SerializeField] private int maxHealth;

    private int health;

    private void Awake()
    {
        health = maxHealth;
    }

    public void Damage(int damageAmount)
    {
        health -= damageAmount;
        if (health < 0) 
        {
            health = 0;
        }
        onDamage?.Invoke(this, EventArgs.Empty);
        if (health == 0)
        {
            Die();
        }
    }

    private void Die()
    {
        onRoleDie?.Invoke(this, EventArgs.Empty);
    }

    public float GetHealthNormalized()
    {
        return (float)health / maxHealth;
    }
}
