using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Item
{

    public enum ItemType { 
        Weapon,
        Consumable
    }

    public int id;
    public string title;
    public string description;
    public ItemType type;
    public Sprite icon;

    public Item(int itemID, string itemTitle, string itemDescription, ItemType itemType) {

        id = itemID;
        title = itemTitle;
        description = itemDescription;
        type = itemType;
        icon = Resources.Load<Sprite>("Unlicensed/Sprites/" + title);
    }

}
