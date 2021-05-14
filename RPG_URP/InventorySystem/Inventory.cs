using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Inventory : MonoBehaviour
{
    public List<Item> characterItems = new List<Item>();
    public ItemDatabase itemDatabase;
    public UIInventory inventoryUI;

    private void Start()
    {
        giveItem(0);
        giveItem(1);
    }

    public void giveItem(int id) {
        Item itemToAdd = itemDatabase.getItem(id);
        Debug.Log($"{itemToAdd.title} added");
        characterItems.Add(itemToAdd);
        inventoryUI.addNewItem(itemToAdd);
        Debug.Log("Added item: " + itemToAdd.title);
    }

    public Item findItem(int id) {

        return characterItems.Find(item => item.id == id);
    }

    public void removeItem(int id) {

        Item item = findItem(id);

        if (item != null) {

            characterItems.Remove(item);
            inventoryUI.removeItem(item);
            Debug.Log($"{item.title}(#{item.id}) removed!");
        }

    }

}
