using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour {

    public float lookRadius = 200.0f;
    public NavMeshAgent agent;

    //Raycasting Objects
    public Transform visionPoint; //Eyeball, essentially
    public LayerMask castingMask; //Ignore other enemies when checking player vision

    //Agent Collider
    private Collider agentCollider;

    //Enemy States
    public bool seenByPlayer = false;
    public float distanceToPlayer;
    public Vector3 currentDestination;      //target destination


    void Start() {
        agent = GetComponent<NavMeshAgent>();
        agentCollider = GetComponent<CapsuleCollider>();
    }

    public void faceTarget(Transform target) {

        float timeMultiplier = 5.0f; //higher slerps faster

        Vector3 direction = (target.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * timeMultiplier);

    }

    public void setDistanceToPlayer(float distance) {

        //distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        distanceToPlayer = distance;
    }

    //Note: as of right now I'm using the top one so enemies the player is facing that go out
    // of view for a split-second won't recompute their path. conveniently it's also cheaper

    //Returns true if agent is in direction camera is facing, even if behind a wall
    public void setIfInDirectionOfView(CameraController camController) {

        Vector3 screenPoint = camController.playerCamera.WorldToViewportPoint(transform.position);
        if (screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1) {
            seenByPlayer = true;
        }
        else {
            seenByPlayer = false;
        }

    }

    //Returns true if agent is visible to camera and not obscured by an object
    public void setIfSeenByPlayer(CameraController camController) {

        if (GeometryUtility.TestPlanesAABB(camController.getPlanes(), agentCollider.bounds))
            seenByPlayer = ifRaycastHitsPlayer(visionPoint.position, camController);
        else
            seenByPlayer = false;
    }

    /*void setRangeTargets() {

        //Mid-Range Targets
        midRangeFront = player.transform.position + player.transform.forward * midRange;
        midRangeBack = player.transform.position + -player.transform.forward * midRange;
        midRangeRight = player.transform.position + player.transform.right * midRange;
        midRangeLeft = player.transform.position + -player.transform.right * midRange;

        //Close-Range Targets
        closeRangeFront = player.transform.position + player.transform.forward * closeRange;
        closeRangeBack = player.transform.position + -player.transform.forward * closeRange;
        closeRangeRight = player.transform.position + player.transform.right * closeRange;
        closeRangeLeft = player.transform.position + -player.transform.right * closeRange;

    }*/

    //Note: see resources
    private bool ifRaycastHitsPlayer(Vector3 source, CameraController camController) {

        RaycastHit hit;

        Vector3 playerPos = camController.transform.position;

        Vector3 direction = (playerPos - source).normalized;
        float distance = Vector3.Distance(visionPoint.position, playerPos);

        //Debug.DrawRay(source, direction*lookRadius, Color.red, 0.1f);
        if (Physics.Raycast(source, direction, out hit, distance, castingMask)) {

            //Debug.Log("raycast hit name: " + hit.transform.name);

            if (hit.transform.gameObject.tag == "Player")
                return true;

        }

        return false;

    }

    private void OnDrawGizmosSelected() {

        //Agent Look Radius
        Gizmos.color = Color.grey;
        Gizmos.DrawWireSphere(transform.position, lookRadius);

        /*
        //Attack Ranges (relative to player)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(player.transform.position, attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(player.transform.position, closeRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(player.transform.position, midRange);

        //Attack Range Target Destinations

        float sphereSize = 0.2f;
        //Mid Range
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(midRangeFront, sphereSize);
        Gizmos.DrawWireSphere(midRangeBack, sphereSize);
        Gizmos.DrawWireSphere(midRangeRight, sphereSize);
        Gizmos.DrawWireSphere(midRangeLeft, sphereSize);

        //Close Range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(closeRangeFront, sphereSize);
        Gizmos.DrawWireSphere(closeRangeBack, sphereSize);
        Gizmos.DrawWireSphere(closeRangeRight, sphereSize);
        Gizmos.DrawWireSphere(closeRangeLeft, sphereSize);*/

    }

    //Resources:
    //http://answers.unity.com/questions/8003/how-can-i-know-if-a-gameobject-is-seen-by-a-partic.html
    //https://answers.unity.com/questions/605155/raycast-get-which-layer-was-hit.html
}
