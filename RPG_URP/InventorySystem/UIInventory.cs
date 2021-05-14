using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIInventory : MonoBehaviour
{

    List<UIItem> uiItems = new List<UIItem>();
    public GameObject slotPrefab;
    public Transform slotPanel;
    public int numberOfSlots = 16;

    private void Awake()
    {

        for (int i = 0; i < numberOfSlots; i++) {

            GameObject instance = Instantiate(slotPrefab);
            instance.transform.SetParent(slotPanel);
            uiItems.Add(instance.GetComponentInChildren<UIItem>());
        }

    }

    public void updateSlot(int slot, Item item) {

        Debug.Log("slot: " + slot + ", item: " + item.id);

        if (slot < numberOfSlots && slot >= 0)
            uiItems[slot].updateItem(item);
        else
            Debug.Log($"Cannot insert into slot #{slot}, larger than capacity ({numberOfSlots})");
    }
    public void addNewItem(Item item) {

        //insert into first null index
        updateSlot(uiItems.FindIndex(i => i.item == null), item);
    }

    public void removeItem(Item item)
    {

        //insert into first null index
        updateSlot(uiItems.FindIndex(i => i.item == item), null);
    }

}
