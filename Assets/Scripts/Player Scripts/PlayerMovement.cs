﻿using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum PlayerState
{
    walk,
    attack,
    interact,
    roundattack,
    stagger,
    idle,
    lift,
    dead
}

public class PlayerMovement : MonoBehaviour
{
    public PlayerState currentState;

    [SerializeField] private float _speed = default;
    private float speed => (Input.GetButton("Run")) ? _speed * 2 : _speed;

    private Rigidbody2D myRigidbody;
    private Vector3 change;
    private Animator animator;

    [SerializeField] private FloatMeter _mana = default;
    public FloatMeter mana => _mana;
    [SerializeField] private FloatMeter _health = default;
    public FloatMeter health => _health;

    public VectorValue startingPosition;

    public PlayerInventory myInventory; // New Inventory

    public SpriteRenderer receivedItemSprite;

    //Spells and Projectiles
    public GameObject projectile; //arrows and so on
    public GameObject fireball;
    public GameObject iceShard;

    public InventoryItem Bow;
    public InventoryWeapon Sword;

    public Color FlashColor;
    public float flashDuration;
    public int numberOfFlasches;
    public Collider2D triggerCollider;
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
        regularPlayerColor = playerSprite.color;
        regularHairColor = hairSprite.color;
        regularArmorColor = armorSprite.color;
        currentState = PlayerState.walk;
        animator = GetComponent<Animator>();
        myRigidbody = GetComponent<Rigidbody2D>();
        SetAnimatorXY(Vector2.down);
        transform.position = startingPosition.value;
    }

    private void Update()
    {
        // Is the player in an interaction?
        if (currentState == PlayerState.interact)
        {
            // Debug.Log("helpmeout");
            return;
        }

        change.x = Input.GetAxisRaw("Horizontal");
        change.y = Input.GetAxisRaw("Vertical");
        SetAnimatorXY(change);

        animator.SetBool("isRunning", Input.GetButton("Run"));

        var notStaggeredOrLifting = (currentState != PlayerState.stagger && currentState != PlayerState.lift);

        if (Input.GetButtonDown("Attack") && currentState != PlayerState.attack && notStaggeredOrLifting && myInventory.currentWeapon != null)
        {
            // Debug.Log("Attack");
            StartCoroutine(AttackCo());
        }
        //########################################################################### Round Attack if Mana > 0 ##################################################################################
        if (Input.GetButton("RoundAttack") && currentState != PlayerState.roundattack && notStaggeredOrLifting && myInventory.currentWeapon != null && mana.current > 0)  //Getbutton in GetButtonDown für die nicht dauerhafte Abfrage
        {
            // Debug.Log("RoundAttack");
            StartCoroutine(RoundAttackCo());
        }
        //########################################################################### Bow Shooting with new Inventory ##################################################################################
        if (Input.GetButton("UseItem") && currentState != PlayerState.roundattack && notStaggeredOrLifting && currentState != PlayerState.attack)
        {
            var arrows = myInventory.myInventory.Find(x => x.itemName.Contains("Arrow"));
            if (arrows.numberHeld > 0 && myInventory.currentBow)
            {
                arrows.numberHeld--;
                StartCoroutine(SecondAttackCo());
            }
        }
        //############################################################################### Spell Cast ###############################################################################
        if (Input.GetButton("SpellCast") && myInventory.currentSpellbook && mana.current > 0 && notStaggeredOrLifting && currentState != PlayerState.attack)
        {
            StartCoroutine(SpellAttackCo());
        }
        //##############################################################################################################################################################




        if (Input.GetButtonUp("UseItem"))
        {
            animator.SetBool("isShooting", false);
        }

        animator.SetBool("isHurt", (currentState == PlayerState.stagger));
        animator.SetBool("Moving", (change != Vector3.zero));

        // ################################# Trying to drop things ################################################################
        // if (Input.GetButtonDown("Lift") && currentState == PlayerState.lift)
        // {
        //     LiftItem();
        //     Debug.Log("Item Dropped!");

        // }
        // ################################# Trying to drop things END ############################################################
    }

    private void FixedUpdate()
    {
        if (currentState == PlayerState.walk || currentState == PlayerState.idle || currentState == PlayerState.lift)
        {
            myRigidbody.MovePosition(transform.position + change.normalized * speed * Time.deltaTime);
        }

        if (currentState != PlayerState.stagger)
        {
            myRigidbody.velocity = Vector2.zero;
        }
    }

    private void SetAnimatorXY(Vector2 direction)
    {
        direction.Normalize();
        if (direction != Vector2.zero)
        {
            direction.x = Mathf.Round(change.x);
            direction.y = Mathf.Round(change.y);

            animator.SetFloat("MoveX", direction.x);
            animator.SetFloat("MoveY", direction.y);
        }
    }

    // #################################### Casual Attack ####################################
    private IEnumerator AttackCo()
    {
        animator.SetBool("Attacking", true);
        currentState = PlayerState.attack;
        yield return null;
        animator.SetBool("Attacking", false);
        yield return new WaitForSeconds(0.3f);
        if (currentState != PlayerState.interact)
        {
            currentState = PlayerState.walk;
        }
    }

    // ############################# Roundattack ################################################
    private IEnumerator RoundAttackCo()
    {
        animator.SetBool("RoundAttacking", true);
        currentState = PlayerState.roundattack;
        yield return null;
        animator.SetBool("RoundAttacking", false);
        currentState = PlayerState.walk;

        mana.current -= 1;
    }

    // ############################## Using the Item / Shooting the Bow #########################################
    private IEnumerator SecondAttackCo()
    {
        currentState = PlayerState.attack;
        MakeArrow();
        yield return new WaitForSeconds(0.3f);
        if (currentState != PlayerState.interact)
        {
            currentState = PlayerState.walk;
        }
    }

    //################### instantiate arrow when shot ###############################
    private void MakeArrow()
    {
        animator.SetBool("isShooting", true);
        var arrowHeight = new Vector2(transform.position.x, transform.position.y + 0.5f); // Pfeil höher setzen
        var direction = new Vector2(animator.GetFloat("MoveX"), animator.GetFloat("MoveY"));
        var rotation = (Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg) * Vector3.forward;
        var arrow = Instantiate(projectile, arrowHeight, Quaternion.identity).GetComponent<arrow>();
        arrow.Setup(direction, rotation);
    }


    // ############################## Using the SpellBook /Spellcasting #########################################
    private IEnumerator SpellAttackCo()
    {
        currentState = PlayerState.attack;
        MakeSpell();
        yield return new WaitForSeconds(0.3f);
        if (currentState != PlayerState.interact)
        {
            currentState = PlayerState.walk;
        }

    }

    //################### instantiate spell when casted ###############################
    private void MakeSpell()
    {
        if (myInventory.currentSpellbook.itemName == "Spellbook of Fire")
        {
            animator.SetBool("isShooting", true); // Set to cast Animation
            var spellHeight = new Vector2(transform.position.x, transform.position.y + 0.5f); // Change Spellheight
            var direction = new Vector2(animator.GetFloat("MoveX"), animator.GetFloat("MoveY"));
            var rotation = (Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg) * Vector3.forward;
            var spellFireball = Instantiate(fireball, spellHeight, Quaternion.identity).GetComponent<SpellFireball>();
            spellFireball.Setup(direction, rotation);
            mana.current -= myInventory.currentSpellbook.manaCosts;
        }
        if (myInventory.currentSpellbook.itemName == "Spellbook of Ice")
        {
            animator.SetBool("isShooting", true); // Set to cast Animation
            var spellHeight = new Vector2(transform.position.x, transform.position.y + 0.5f); // Change Spellheight
            var direction = new Vector2(animator.GetFloat("MoveX"), animator.GetFloat("MoveY"));
            var rotation = (Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg) * Vector3.forward;
            var spellIceShard = Instantiate(iceShard, spellHeight, Quaternion.identity).GetComponent<SpellIceShard>();
            spellIceShard.Setup(direction, rotation);
            mana.current -= myInventory.currentSpellbook.manaCosts;
        }

    }

    //#################################### Item Found RAISE IT! #######################################

    public void RaiseItem()
    {
        if (myInventory.currentItem != null)
        {
            if (currentState != PlayerState.interact)
            {
                animator.SetBool("receiveItem", true);
                currentState = PlayerState.interact;
                receivedItemSprite.sprite = myInventory.currentItem.itemImage;
            }
            else
            {
                animator.SetBool("receiveItem", false);
                currentState = PlayerState.idle;
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
               if (currentState != PlayerState.lift)
               {             
                   animator.SetBool("isCarrying", true);
                   currentState = PlayerState.lift;
                   Debug.Log("PlayerState = Lift");
                   thingSprite.sprite = myInventory.currentItem.itemImage;
               }
               else
               {   
                   animator.SetBool("isCarrying", false);
                   currentState = PlayerState.idle;
                   Debug.Log("Playerstate = idle");
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
        if (myRigidbody != null)
        {
            StartCoroutine(FlashCo());
            yield return new WaitForSeconds(knockTime);
            currentState = PlayerState.idle;
            myRigidbody.velocity = Vector2.zero;
        }
    }

    //##################### Death animation and screen ##############################

    private IEnumerator DeathCo()
    {
        currentState = PlayerState.dead;
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
