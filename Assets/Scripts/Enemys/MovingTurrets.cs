﻿using UnityEngine;

public class MovingTurrets : TurretEnemy
{
    protected override void FixedUpdate()
    {
        var distance = Vector3.Distance(target.position, transform.position);
        if (distance <= chaseRadius)
        {
            if (currentState == EnemyState.idle || currentState == EnemyState.walk && currentState != EnemyState.stagger)
            {
                if (canFire)
                {
                    FireProjectile();
                }

                var newPosition = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);
                rigidbody.MovePosition(newPosition);
            }
        }
        else if (distance > chaseRadius)
        {
            OutsideChaseRadiusUpdate();
        }
    }
}
