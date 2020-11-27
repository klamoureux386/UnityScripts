using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicalGunController : MonoBehaviour
{

    public GameObject bulletExit;
    private ObjectPooler OP;
    int currentBullet = 0;

    private List<GameObject> pooledBullets;

    void Awake()
    {
        OP = ObjectPooler.SharedInstance;
        pooledBullets = OP.GetAllPooledObjects(0);
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetButtonDown("Fire1")) {

            Debug.Log("Spawning Bullet " + currentBullet);

            GameObject bullet = pooledBullets[currentBullet];

            //Get first pooled object
            //GameObject bullet = OP.GetPooledObject(0);

            bullet.SetActive(true);

            bullet.transform.position = bulletExit.transform.position;

            Rigidbody bulletRB = bullet.GetComponent<Rigidbody>();
            bulletRB.AddForce(bulletExit.transform.TransformDirection(Vector3.forward)*20, ForceMode.Impulse);

            currentBullet++;

            if (currentBullet >= OP.itemsToPool[0].amountToPool)
                clearAllBullets();

        }

    }


    //Just for prototyping, cleanup/delete later
    private void clearAllBullets() {

        for (int i = 0; i < pooledBullets.Count; i++) {

            pooledBullets[i].SetActive(false);
        }

        currentBullet = 0;
    }

}
