using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class UIController : MonoBehaviour {

    public GameObject revolver6Cylinder;
    public List<Image> ammoImages;

    [SerializeField] Color activatedColor = new Color(255, 255, 255, 100.0f / 255.0f); //Default alpha of 100
    //[SerializeField] Color FLactivatedColor = new Color(0, 255, 50, 150.0f / 255.0f); //Default alpha of 150
    [SerializeField] Color32 FLactivatedColor = new Color(0, 191, 38, 150); //Default alpha of 150
    [SerializeField] Color deactivatedColor = new Color(255, 255, 255, 0);

    public int ammoMax = 6;
    public int FLammoRemaining = 0;

    //public bool fullyLoaded = false; //fully loaded = double capacity, purple flames turn to green, shooting consumes green flames clockwise then purple


    public void updateAmmoCounter(int remainingAmmo) {

        //float activatedAlpha = 100.0f / 255.0f; //Alpha of 100

        //Color deactivatedColor = new Color(255, 255, 255, 0);
        //Color activatedColor = new Color(255, 255, 255, activatedAlpha);

        int ammoToDeactivate = ammoMax - remainingAmmo /*activeAmmo*/;

        //Deactivate ammo icons up until ammo spent
        for (int i = 0; i < ammoToDeactivate; i++) {

            ammoImages.ElementAt(i).color = deactivatedColor;
        }

        //Make sure remaining ammo is visible
        for (int i = ammoToDeactivate; i < ammoMax; i++) {
            ammoImages.ElementAt(i).color = activatedColor;
        }

    }

    public void useOneFullyLoadedAmmo() {

        //Change FL ammo color back to normal flame
        ammoImages.ElementAt(ammoMax - FLammoRemaining).color = activatedColor;

        FLammoRemaining--;

        //just in case
        if (FLammoRemaining < 0)
            FLammoRemaining = 0;

    }

    /*public void updateAmmoCounterFullyLoaded(int remainingFullyLoadedAmmo) { 
    
    }*/

    public void setFullyLoaded(bool value) {

        if (value)
        {

            for (int i = 0; i < ammoMax; i++)
            {

                ammoImages.ElementAt(i).color = FLactivatedColor;

            }

            FLammoRemaining = ammoMax;

        }

        //Reset color of flames if false
        else
        {

            for (int i = 0; i < ammoMax; i++) {

                //if any FL flames left, change color
                if (ammoImages.ElementAt(i).color == FLactivatedColor)
                    ammoImages.ElementAt(i).color = activatedColor;

            }
            
        }

    }


}
