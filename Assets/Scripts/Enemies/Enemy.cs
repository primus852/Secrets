﻿using System.Collections;
using UnityEngine;

public class Enemy : Character
{
    [Header("Enemy Stats")]
    [SerializeField] protected FloatValue maxHealth = default;
    private float _health;
    public float health {
        get => _health;
        set {
            if (value > maxHealth.value)
            {
                value = maxHealth.value;
            }
            else if (value < 0)
            {
                value = 0;
            }

            if (value < _health)
            {
                chaseRadius = originalChaseRadius * 10;
            }

            _health = value;

            if (_health <= 0)
            {
                Die();
            }
        }
    }

    [SerializeField] protected string enemyName = default;
    [SerializeField] protected int baseAttack = default;
    public float moveSpeed = default;
    [SerializeField] protected Vector2 homePosition = default;
    public float chaseRadius = default;
    [SerializeField] protected float attackRadius = default;
    public float originalChaseRadius = default;

    [Header("Death Effects")]
    [SerializeField] private GameObject deathEffect = default;
    [SerializeField] private float deathEffectDelay = 1;

    [Header("Death Signal")]
    [SerializeField] private Signals roomSignal = default;
    [SerializeField] private LootTable thisLoot = default;

    protected Transform target = default;

    protected virtual void OnEnable()
    {
        health = maxHealth.value;
        transform.position = homePosition;
        currentState = State.idle;
    }

    protected override void Awake()
    {
        base.Awake();

        homePosition = transform.position;
        health = maxHealth.value;
        originalChaseRadius = chaseRadius;

        target = GameObject.FindWithTag("Player").transform;
    }

    protected virtual void FixedUpdate()
    {
        var distance = Vector3.Distance(target.position, transform.position);
        if (distance <= chaseRadius && distance > attackRadius)
        {
            InsideChaseRadiusUpdate();
        }
        else if (distance > chaseRadius)
        {
            OutsideChaseRadiusUpdate();
        }
    }

    protected virtual void InsideChaseRadiusUpdate()
    {
        if (currentState == State.idle || currentState == State.walk && currentState != State.stagger)
        {
            Vector3 temp = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);

            SetAnimatorXYSingleAxis(temp - transform.position);
            rigidbody.MovePosition(temp);
            currentState = State.walk;
            animator.SetBool("WakeUp", true);
        }
    }

    protected virtual void OutsideChaseRadiusUpdate() {}

    private void Die()
    {
        Debug.Log("0 Leben");
        DeathEffect();
        MakeLoot();
        if (roomSignal != null)
        {
            roomSignal.Raise();
        }
        this.gameObject.SetActive(false);
    }

    private void DeathEffect()
    {
        if (deathEffect != null)
        {
            GameObject effect = Instantiate(deathEffect, transform.position, Quaternion.identity);
            Destroy(effect, deathEffectDelay);
        }
    }

    private void MakeLoot()
    {
        if (thisLoot != null)
        {
            ItemPickUp current = thisLoot.LootPowerUp();
            if (current != null)
            {
                Instantiate(current.gameObject, transform.position, Quaternion.identity);
            }
        }
    }

    public void Knock(float knockTime) {
        if (this.gameObject.activeInHierarchy)
        {
            StartCoroutine(KnockCo(knockTime));
        }
    }

    private IEnumerator KnockCo(float knockTime)
    {
        currentState = State.stagger;
        yield return new WaitForSeconds(knockTime);
        rigidbody.velocity = Vector2.zero;
        currentState = State.idle;
    }
}
