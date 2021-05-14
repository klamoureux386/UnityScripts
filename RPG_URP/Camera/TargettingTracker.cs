using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cinemachine;

public class TargettingTracker : MonoBehaviour
{
    public PlayerController playerController;
    public List<GameObject> TargettableEnemies;
    public Image lockOnCursor;
    public Canvas screenCanvas;
    public GameObject Enemy;

    // Start is called before the first frame update
    void Start()
    {

        TargettableEnemies = new List<GameObject>();
        Debug.Log("Targettable enemy list instantiated.");

        TargettableEnemies.Add(Enemy);
    }

    // Update is called once per frame
    void LateUpdate()
    {

        if (TargettableEnemies.Count > 0) {

            lockOnCursor.transform.position = Camera.main.WorldToScreenPoint(TargettableEnemies[0].transform.position);

        }

        //check if this has a performance impact, it probably doesnt but who knows
        if (playerController.lockedOn)
            activateLockOn();
        else
            deactivateLockOn();

        
    }

    //Make sure to set these for camera activation and cut in Cinemachine Brain
    public void activateLockOn() {

        //Debug.Log("Activating Lock-On cursor");
        lockOnCursor.enabled = true;
    
    }
    public void deactivateLockOn() {

        //Debug.Log("Deactivating Lock-On cursor");
        lockOnCursor.enabled = false;
    }

    /*
    private void OnTriggerEnter(Collider other)
    {
        //if object entering trigger is an Enemy & is not contained in the list
        if (other.gameObject.tag == "Enemy" && !TargettableEnemies.Contains(other.gameObject))
        {

            TargettableEnemies.Add(other.gameObject);
            //targetGroup.AddMember(other.transform, 1, 2);
            Debug.Log($"{other.gameObject.name} added to targettable enemies (OnTriggerEnter)");
        }

        Debug.Log($"Targettable Enemies list size: {TargettableEnemies.Count}");
    }

    private void OnTriggerExit(Collider other)
    {

        //if object exiting trigger is an Enemy & is contained in the list
        if (other.gameObject.tag == "Enemy" && TargettableEnemies.Contains(other.gameObject))
        {

            TargettableEnemies.Remove(other.gameObject);
            //targetGroup.RemoveMember(other.transform);
            Debug.Log($"{other.gameObject.name} removed from targettable enemies (OnTriggerExit)");
        }

        Debug.Log($"Targettable Enemies list size: {TargettableEnemies.Count}");
        
    }*/

}
