using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemDatabase : MonoBehaviour
{
    public List<Item> items;

    private void Awake()
    {
        buildDatabase();
    }

    public Item getItem(int id) {

        return items.Find(Item => Item.id == id);
    }

    private void buildDatabase() {

        items = new List<Item>() {
                new Item(0, "Chakrams", "Inflatable donuts for testing", Item.ItemType.Weapon),
                new Item(1, "Revolver", "pew pew", Item.ItemType.Weapon)
        };
    }

}
