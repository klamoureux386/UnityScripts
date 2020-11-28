using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class RaycastGunController : MonoBehaviour
{

    //https://answers.unity.com/questions/22800/raycast-shooting.html
    //https://www.youtube.com/watch?v=THnivyG0Mvo

    public GameObject bulletExit;
    public GameObject playerCamera;
    public VisualEffect muzzleFlash;
    public VisualEffect impactEffect;

    private VFXEventAttribute impactEventAttribute;

    private void Start() {
        impactEventAttribute = impactEffect.CreateVFXEventAttribute();
    }

    void Update()
    {

        if (Input.GetButtonDown("Fire1")) {

            shoot();

        }

        if (impactEffect.culled) {
            Debug.Log("bullet impact effect culled");
        }

    }

    private void shoot() {

        RaycastHit hit;

        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit)) {

            //muzzleFlash.Play();

            Debug.Log("raycast hit");

            Debug.Log(hit.transform.name);

            spawnImpactEffect(hit.point, hit.normal);

            //Instantiate(impactEffectParticle, hit.point, Quaternion.LookRotation(hit.normal));
        }


    }

    //https://forum.unity.com/threads/single-particle-system-for-multiple-explosions-in-various-positions-and-colors-from-script.855634/#post-5920883
    //https://forum.unity.com/threads/multiple-event-burst-spawns-of-the-same-vfxgraph-possible.852049/
    //https://forum.unity.com/threads/multiple-event-burst-spawns-of-the-same-vfxgraph-possible.852049/
    private void spawnImpactEffect(Vector3 pointOfImpact, Vector3 impactPointNormal) {

        Vector3 velocityMins = new Vector3(-1.0f, -1.0f, -1.0f);
        Vector3 velocityMaxs = new Vector3(1.0f, 1.0f, 1.0f);

        //if normal value is negative, add it to the max (reduces max by abs)
        //if normal value is positive, add it to the min

        if (impactPointNormal.x <= 0)
            velocityMaxs.x += impactPointNormal.x;

        else
            velocityMins.x += impactPointNormal.x;

        if (impactPointNormal.y <= 0)
            velocityMaxs.y += impactPointNormal.y;

        else
            velocityMins.y += impactPointNormal.y;

        if (impactPointNormal.z <= 0)
            velocityMaxs.z += impactPointNormal.z;

        else
            velocityMins.z += impactPointNormal.z;

        impactEventAttribute.SetVector3("targetPosition", pointOfImpact);

        impactEventAttribute.SetVector3("direction", velocityMins); //min velocity = direction
        impactEventAttribute.SetVector3("velocity", velocityMaxs); //max velocity = velocity

        impactEffect.SendEvent("OnSpawn", impactEventAttribute);

        /*
        Debug.Log("position to spawn impact at: " + pointOfImpact);
        Debug.Log("impact point normal: " + impactPointNormal);
        Debug.Log("Velocity mins: " + velocityMins);
        Debug.Log("Velocity maxs: " + velocityMaxs);
        */

    }
}
