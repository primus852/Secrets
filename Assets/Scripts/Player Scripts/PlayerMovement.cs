﻿using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerMovement : Character
{
    [SerializeField] private float _speed = default;
    private float speed => (Input.GetButton("Run")) ? _speed * 2 : _speed;

    private Vector3 change;

    [SerializeField] private FloatMeter _mana = default;
    public FloatMeter mana => _mana;
    [SerializeField] private FloatMeter _health = default;
    public FloatMeter health => _health;

    public VectorValue startingPosition;

    public PlayerInventory myInventory;

    public SpriteRenderer receivedItemSprite;

    public Collider2D triggerCollider;

    [Header("Projectiles")]
    [SerializeField] private float arrowSpeed = 1;
    public GameObject projectile; //arrows and so on
    [SerializeField] private float fireballSpeed = 1;
    public GameObject fireball;
    [SerializeField] private float iceShardSpeed = 1;
    public GameObject iceShard;

    [Header("Invulnerability frames")]
    public Color FlashColor;
    public float flashDuration;
    public int numberOfFlasches;

    [Header("Sprites")]
    public Color regularPlayerColor;
    public Color regularHairColor;
    public Color regularArmorColor;
    public SpriteRenderer playerSprite;
    public SpriteRenderer armorSprite;
    public SpriteRenderer hairSprite;

    //############### LIFT-TEST      ##############
    //  public GameObject thing;
    public SpriteRenderer thingSprite;
    //############### LIFT-TEST-ENDE ##############

    private void Start()
    {
        SetAnimatorXY(Vector2.down);
        currentState = State.walk;

        transform.position = startingPosition.value;

        regularPlayerColor = playerSprite.color;
        regularHairColor = hairSprite.color;
        regularArmorColor = armorSprite.color;
    }

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
            // Debug.Log("Attack");
            StartCoroutine(AttackCo());
        }
        //########################################################################### Round Attack if Mana > 0 ##################################################################################
        if (Input.GetButton("RoundAttack") && currentState != State.roundattack && notStaggeredOrLifting && myInventory.currentWeapon != null && mana.current > 0)  //Getbutton in GetButtonDown für die nicht dauerhafte Abfrage
        {
            // Debug.Log("RoundAttack");
            StartCoroutine(RoundAttackCo());
        }
        //########################################################################### Bow Shooting with new Inventory ##################################################################################
        if (Input.GetButton("UseItem") && currentState != State.roundattack && notStaggeredOrLifting && currentState != State.attack)
        {
            var arrows = myInventory.myInventory.Find(x => x.itemName.Contains("Arrow"));
            if (arrows && arrows.numberHeld > 0 && myInventory.currentBow)
            {
                arrows.numberHeld--;
                StartCoroutine(SecondAttackCo());
            }
        }
        //############################################################################### Spell Cast ###############################################################################
        if (Input.GetButton("SpellCast") && myInventory.currentSpellbook && mana.current > 0 && notStaggeredOrLifting && currentState != State.attack)
        {
            StartCoroutine(SpellAttackCo());
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

    // #################################### Casual Attack ####################################
    private IEnumerator AttackCo()
    {
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
        animator.SetBool("RoundAttacking", true);
        currentState = State.roundattack;
        yield return null;
        animator.SetBool("RoundAttacking", false);
        currentState = State.walk;

        mana.current -= 1;
    }

    // ############################## Using the Item / Shooting the Bow #########################################
    private IEnumerator SecondAttackCo()
    {
        currentState = State.attack;
        MakeArrow();
        yield return new WaitForSeconds(0.3f);
        if (currentState != State.interact)
        {
            currentState = State.walk;
        }
    }

    private void CreateProjectile(GameObject projectilePrefab, float projectileSpeed)
    {
        var position = new Vector2(transform.position.x, transform.position.y + 0.5f); // Pfeil höher setzen
        var direction = new Vector2(animator.GetFloat("MoveX"), animator.GetFloat("MoveY"));
        var proj = Instantiate(projectilePrefab, position, Projectile.CalculateRotation(direction)).GetComponent<Projectile>();
        proj.rigidbody.velocity = direction * projectileSpeed;
    }

    //################### instantiate arrow when shot ###############################
    private void MakeArrow()
    {
        animator.SetBool("isShooting", true);
        CreateProjectile(projectile, arrowSpeed);
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
        if (myInventory.currentSpellbook.itemName == "Spellbook of Fire")
        {
            CreateProjectile(fireball, fireballSpeed);
            mana.current -= myInventory.currentSpellbook.manaCosts;
        }
        else if (myInventory.currentSpellbook.itemName == "Spellbook of Ice")
        {
            CreateProjectile(iceShard, iceShardSpeed);
            mana.current -= myInventory.currentSpellbook.manaCosts;
        }
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
                receivedItemSprite.sprite = myInventory.currentItem.itemImage;
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

    // ########################### Getting hit and die ##############################################

    public void Knock(float knockTime, float damage)
    {
        health.current -= damage;
        if (health.current > 0)
        {

            StartCoroutine(KnockCo(knockTime));
        }
        else
        {
            StartCoroutine(DeathCo());
        }
    }

    //############################# Knockback ################################################

    private IEnumerator KnockCo(float knockTime)
    {
        StartCoroutine(FlashCo());
        yield return new WaitForSeconds(knockTime);
        currentState = State.idle;
        rigidbody.velocity = Vector2.zero;
    }

    //##################### Death animation and screen ##############################

    private IEnumerator DeathCo()
    {
        currentState = State.dead;
        animator.SetBool("isDead", true);
        yield return new WaitForSeconds(3f);
        SceneManager.LoadScene("DeathMenu");
    }

    //##################### Invulnerability frame ##############################

    private IEnumerator FlashCo()
    {
        triggerCollider.enabled = false;

        for (int i = 0; i < numberOfFlasches; i++)
        {
            playerSprite.color = FlashColor;
            armorSprite.color = FlashColor;
            hairSprite.color = FlashColor;
            yield return new WaitForSeconds(flashDuration);
            playerSprite.color = regularPlayerColor;
            armorSprite.color = regularArmorColor;
            hairSprite.color = regularHairColor;
            yield return new WaitForSeconds(flashDuration);
        }

        triggerCollider.enabled = true;
    }
}
