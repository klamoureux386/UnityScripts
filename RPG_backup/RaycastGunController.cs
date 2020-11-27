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

        impactEventAttribute.SetVector3("targetPosition", pointOfImpact);
        impactEventAttribute.SetVector3("angle", impactPointNormal);
        impactEffect.SendEvent("OnSpawn", impactEventAttribute);

        Debug.Log("position to spawn impact at: " + pointOfImpact);

    }
}
