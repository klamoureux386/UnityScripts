using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIItem : MonoBehaviour
{
    public Item item;
    private Image spriteImage;

    private void Awake()
    {
        spriteImage = GetComponent<Image>();
        updateItem(null);
    }

    public void updateItem(Item itemToUpdate) {

        item = itemToUpdate;
        if (item != null)
        {
            spriteImage.color = Color.white;
            spriteImage.sprite = item.icon;
        }
        else {
            spriteImage.color = Color.clear;
        }
    }

}
