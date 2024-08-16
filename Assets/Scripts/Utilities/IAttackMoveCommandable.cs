using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAttackMoveCommandable : IAttackCommandable, IMoveCommandable
{
    /// <summary>
    /// Move toward targetLocation. 
    /// If any valid enemy target is discovered, then perform Attack on it until it leaves range. 
    /// Do not chase the target. 
    /// Return to moving toward targetLocation and attacking discovered enemies until the targetLocation is reached.
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    bool AttackMove(Vector3 targetLocation);
}
