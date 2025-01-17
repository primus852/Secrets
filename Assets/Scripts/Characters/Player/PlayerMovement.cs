﻿using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Experimental.Rendering.Universal;

public class PlayerMovement : Character
{
    [SerializeField] private XPSystem levelSystem = default;
    [SerializeField] private float _speed = default;
    private float speed => (Input.GetButton("Run")) ? _speed * 2 : _speed;

    private Vector3 change;

    [SerializeField] private ConstrainedFloat _lumen = default;
    public ConstrainedFloat lumen => _lumen;
    [SerializeField] private ConstrainedFloat _mana = default;
    public ConstrainedFloat mana => _mana;
    [SerializeField] private ConstrainedFloat _health = default;
    public ConstrainedFloat healthMeter => _health;
    public override float health {
        get => _health.current;
        set {
            _health.current = value;
            if (_health.current <= 0) {
                StartCoroutine(DeathCo());
            }
        }
    }

    public VectorValue startingPosition;

    public Inventory myInventory;
    [SerializeField] private Item arrow = default;

    public SpriteRenderer receivedItemSprite;

    [Header("Hitboxes")]
    [SerializeField] private DamageOnTrigger[] directionalAttacks = default;
    [SerializeField] private DamageOnTrigger roundAttack = default;

    [Header("Projectiles")]
    [SerializeField] private float arrowSpeed = 1;
    public GameObject projectile; //arrows and so on

    [SerializeField] private AudioClip[] attackSounds = default;
    [SerializeField] private AudioClip levelUpSound = default;
    [Header("Lamp")]
    [SerializeField] private Light2D playerLamp;
    //############### LIFT-TEST      ##############
    //  public GameObject thing;
    public SpriteRenderer thingSprite;
    //############### LIFT-TEST-ENDE ##############

    private void OnEnable() => levelSystem.OnLevelChanged += LevelUpPlayer;
    private void OnDisable() => levelSystem.OnLevelChanged -= LevelUpPlayer;

    private void LevelUpPlayer()
    {
        _health.max = _health.max + 10;
        _mana.max = _mana.max + 10;
        _health.current = _health.max;
        _mana.current = _mana.max;
        currentState = State.idle;
        SoundManager.RequestSound(levelUpSound);
        if (effectAnimator)
        {
            effectAnimator.Play("LevelUp");
        }
    }



    private void Start()
    {
        SetAnimatorXY(Vector2.down);
        currentState = State.walk;
        transform.position = startingPosition.value;
        playerLamp = GameObject.Find("Lamp").GetComponent<Light2D>();
    }

    private AudioClip GetAttackSound() => attackSounds[Random.Range(0, attackSounds.Length)];
    private AudioClip GetLevelUpSound() => levelUpSound;


    private void Update()
    {
        // Is the player in an interaction?
        if (currentState == State.interact)
        {
            // Debug.Log("helpmeout");
            return;
        }

        change.x = Input.GetAxisRaw("Horizontal");
        change.y = Input.GetAxisRaw("Vertical");
        SetAnimatorXY(change);

        animator.SetBool("isRunning", Input.GetButton("Run"));

        var notStaggeredOrLifting = (currentState != State.stagger && currentState != State.lift);

        if (Input.GetButtonDown("Attack") && currentState != State.attack && notStaggeredOrLifting && myInventory.currentWeapon != null)
        {
            StartCoroutine(AttackCo());
        }
        //########################################################################### Round Attack if Mana > 0 ##################################################################################
        if (Input.GetButton("RoundAttack") && currentState != State.roundattack && notStaggeredOrLifting && myInventory.currentWeapon != null && mana.current > 0)  //Getbutton in GetButtonDown für die nicht dauerhafte Abfrage
        {
            StartCoroutine(RoundAttackCo());
        }
        //########################################################################### Bow Shooting with new Inventory ##################################################################################
        if (Input.GetButton("UseItem") && currentState != State.roundattack && notStaggeredOrLifting && currentState != State.attack)
        {
            if (arrow != null && myInventory.items[arrow] > 0 && myInventory.currentBow)
            {
                myInventory.items[arrow]--;
                StartCoroutine(SecondAttackCo());
            }
        }
        //############################################################################### Spell Cast ###############################################################################
        if (Input.GetButton("SpellCast") && myInventory.currentSpellbook && mana.current > 0 && notStaggeredOrLifting && currentState != State.attack)
        {
            StartCoroutine(SpellAttackCo());
        }

        if (Input.GetButtonDown("Lamp") && myInventory.currentLamp && lumen.current > 0)
        {
            toggleLamp();
        }
        //##############################################################################################################################################################

        if (Input.GetButtonUp("UseItem"))
        {
            animator.SetBool("isShooting", false);
        }

        animator.SetBool("isHurt", (currentState == State.stagger));
        animator.SetBool("Moving", (change != Vector3.zero));

        // ################################# Trying to drop things ################################################################
        // if (Input.GetButtonDown("Lift") && currentState == State.lift)
        // {
        //     LiftItem();
        //     Debug.Log("Item Dropped!");

        // }
        // ################################# Trying to drop things END ############################################################
    }

    private void FixedUpdate()
    {
        if (currentState == State.walk || currentState == State.idle || currentState == State.lift)
        {
            rigidbody.MovePosition(transform.position + change.normalized * speed * Time.deltaTime);
        }

        if (currentState != State.stagger)
        {
            rigidbody.velocity = Vector2.zero;
        }
    }

    public bool IsCriticalHit() => (myInventory.totalCritChance > 0 && Random.Range(0, 99) <= myInventory.totalCritChance);

    // #################################### Casual Attack ####################################
    private IEnumerator AttackCo()
    {
        var isCritical = IsCriticalHit();
        for (int i = 0; i < directionalAttacks.Length; i++)
        {
            directionalAttacks[i].damage = Random.Range(myInventory.currentWeapon.minDamage, myInventory.currentWeapon.maxDamage +1 );
            directionalAttacks[i].isCritical = isCritical;
        }

        SoundManager.RequestSound(GetAttackSound());
        animator.SetBool("Attacking", true);
        currentState = State.attack;
        yield return null;
        animator.SetBool("Attacking", false);

        yield return new WaitForSeconds(0.3f);

        if (currentState != State.interact)
        {
            currentState = State.walk;
        }
    }



    // ############################# Roundattack ################################################
    private IEnumerator RoundAttackCo()
    {
        roundAttack.damage = Random.Range(myInventory.currentWeapon.minDamage, myInventory.currentWeapon.maxDamage + 1);
        roundAttack.isCritical = IsCriticalHit();
        //! Is this missing a sound request?
        animator.SetBool("RoundAttacking", true);
        currentState = State.roundattack;
        yield return null;  //! This allows a round attack to be executed every other frame when the input is held, causing mana to drain very quickly
        animator.SetBool("RoundAttacking", false);
        currentState = State.walk;

        mana.current -= 1;
    }

    // ############################## Using the Item / Shooting the Bow #########################################
    private IEnumerator SecondAttackCo()
    {
        currentState = State.attack;
        animator.SetBool("isShooting", true);
        CreateProjectile(projectile, arrowSpeed, Random.Range(myInventory.currentBow.minDamage, myInventory.currentBow.maxDamage + 1));

        yield return new WaitForSeconds(0.3f);

        if (currentState != State.interact)
        {
            currentState = State.walk;
        }
    }

    private void CreateProjectile(GameObject projectilePrefab, float projectileSpeed, float projectileDamage)
    {
        var position = new Vector2(transform.position.x, transform.position.y + 0.5f); // Pfeil höher setzen
        var direction = new Vector2(animator.GetFloat("MoveX"), animator.GetFloat("MoveY"));
        var proj = Instantiate(projectilePrefab, position, Projectile.CalculateRotation(direction)).GetComponent<Projectile>();
        proj.rigidbody.velocity = direction.normalized * projectileSpeed; // This makes the object move
        var hitbox = proj.GetComponent<DamageOnTrigger>();
        hitbox.damage = projectileDamage;    //replace defaultvalue with the value given from the makespell()/playervalue
        hitbox.isCritical = IsCriticalHit();  // gets written into Derived class
    }

    // ############################## Using the SpellBook /Spellcasting #########################################
    private IEnumerator SpellAttackCo()
    {
        animator.SetBool("isCasting", true); // Set to cast Animation
        currentState = State.attack;
        MakeSpell();
        yield return new WaitForSeconds(0.3f);
        if (currentState != State.interact)
        {
            currentState = State.walk;
        }
        animator.SetBool("isCasting", false);

    }

    //################### instantiate spell when casted ###############################
    private void MakeSpell()
    {

        var prefab = myInventory.currentSpellbook.prefab;
        var speed = myInventory.currentSpellbook.speed;
        CreateProjectile(prefab, speed,Random.Range(myInventory.totalMinSpellDamage, myInventory.totalMaxSpellDamage + 1));
        mana.current -= myInventory.currentSpellbook.manaCosts;

    }

    //#################################### Item Found RAISE IT! #######################################

    public void RaiseItem()
    {
        if (myInventory.currentItem != null)
        {
            if (currentState != State.interact)
            {
                animator.SetBool("receiveItem", true);
                currentState = State.interact;
                receivedItemSprite.sprite = myInventory.currentItem.sprite;
            }
            else
            {
                animator.SetBool("receiveItem", false);
                currentState = State.idle;
                receivedItemSprite.sprite = null;
                myInventory.currentItem = null;
            }
        }
    }

    // #################################### LIFT Item ######################################
    /* 
       public void LiftItem()
       {
           if (playerInventory.currentItem != null)
           {
               if (currentState != State.lift)
               {             
                   animator.SetBool("isCarrying", true);
                   currentState = State.lift;
                   Debug.Log("State = Lift");
                   thingSprite.sprite = myInventory.currentItem.itemImage;
               }
               else
               {   
                   animator.SetBool("isCarrying", false);
                   currentState = State.idle;
                   Debug.Log("State = idle");
                   thingSprite.sprite = null;

                   myInventory.currentItem = null;
               }
           }
       }
    */

    public override void TakeDamage(float damage, bool isCritical)
    {
        if (!isInvulnerable)
        {
            myInventory.CalcDefense();
            var finalDamage = damage - myInventory.totalDefense;
            if (finalDamage > 0)
            {
                health -= finalDamage;
                DamagePopUpManager.RequestDamagePopUp(finalDamage, isCritical, transform);
                iframes?.TriggerInvulnerability();
            }
            else
            {
                DamagePopUpManager.RequestDamagePopUp(0, isCritical, transform);
            }
        //    Debug.Log(finalDamage + " damage after defense calculation.");
        }
    }

    // ########################### Getting hit and die ##############################################

    public override void Knockback(Vector2 knockback, float duration)
    {
        if (currentState != State.stagger && this.gameObject.activeInHierarchy)
        {
            StartCoroutine(KnockbackCo(knockback, duration));
        }
    }

    //##################### Death animation and screen ##############################

    private IEnumerator DeathCo()
    {
        currentState = State.dead;
        animator.SetBool("isDead", true);
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene("DeathMenu");
    }


    //Lamp
    private void toggleLamp()
    {
        var lamp = myInventory.currentLamp;
        if (playerLamp.intensity == 0)
        {
            InvokeRepeating("reduceLampLight", 0, 1);
            playerLamp.intensity = 1;
            playerLamp.color = lamp.color;
            playerLamp.pointLightOuterRadius = lamp.outerRadius;


        }
        else
        {
            playerLamp.intensity = 0;
            StopCoroutine("LampCo");
            CancelInvoke("reduceLampLight");
        }
    }

    private void reduceLampLight()
    {
        StartCoroutine(LampCo());
    }
    private IEnumerator LampCo()
    {   if(lumen.current > 0)
        {
            lumen.current -= 1;
            yield return new WaitForSeconds(1f);
        }

        if (lumen.current <= 0)
        {
            playerLamp.intensity = 0;
            CancelInvoke("reduceLampLight");
        }
    }

}
