﻿using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Legs")]
public class InventoryLegs : EquippableItem
{
    public int armorDefense;

    public override string fullDescription
        => description + ("\n\n ARMOR: ") + armorDefense;
}
