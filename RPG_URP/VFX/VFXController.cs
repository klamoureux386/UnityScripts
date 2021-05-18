using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class VFXController : MonoBehaviour
{

    public GameObject Player;
    public VisualEffect gunSmoke;
    public Transform primaryGunBarrelPos;
    public Transform secondaryGunBarrelPos;

    public void shootPrimaryGun() {

        gunSmoke.SetVector3("customPosition", primaryGunBarrelPos.position);
        gunSmoke.SetVector3("customDirection", Player.transform.forward);
        
        gunSmoke.Play();
        
        //set spawn position of effect to primary gun barrel
    }

    public void shootSecondaryGun() {

        //set spawn position of effect to secondary gun barrel
    }

}
