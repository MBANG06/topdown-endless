using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interface contract for all damageable entities in the game.
/// </summary>
public interface IDamageable
{
    void TakeDamage(int damage);
    bool IsAlive { get; }
}
