﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Liftable : Interactable
{

    public InventoryItem contents;
    public PlayerInventory playerInventory;
    public Signals LiftItem;
    private Animator anim;
    public GameObject thing;
    bool isCarrying;



    void Start()
    {
        anim = GetComponent<Animator>();
    }


    void LateUpdate()
    {



        if (Input.GetButtonDown("Lift") && playerInRange && isCarrying == false)
        {


            Debug.Log("HOCHHEBEN!");
            Lifting();
            isCarrying = true;
        }
        else if(Input.GetButtonDown("Lift") && isCarrying == true)
        {
            Debug.Log("ABSTELLEN!");
            Dropping();
        }

        



     }

    //############### TEST-Lift ###################################
    public void Lifting()
    {
        Debug.Log("LIFT OBJECT");
        if (playerInventory.myInventory.Contains(contents))
        {
            contents.numberHeld++;
        }
        if (!playerInventory.myInventory.Contains(contents))
        { 
            playerInventory.myInventory.Add(contents);
            playerInventory.currentItem = contents;
        }
        contextOff.Raise();
        thing.SetActive(false);
        LiftItem.Raise();
    }


    // ############################## Test-Drop ###################### WIESO WIRD DAS NIE AUFGERUFEN!?
    public void Dropping()
    {
        Debug.Log("Drop OBJECT");      
        thing.SetActive(true);
       
    }
    // ############################## Test-Drop ######################




    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && other.isTrigger)
        {
            contextOn.Raise();
            playerInRange = true;

        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && other.isTrigger)
        {
            contextOff.Raise();
            playerInRange = false;

        }
    }



}


