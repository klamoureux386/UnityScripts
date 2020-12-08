using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour {

    //Camera Controller Component
    //CameraController camController;


    public float lookRadius = 100.0f;
    public GameObject player;
    public Camera playerCamera;
    public NavMeshAgent agent;

    //Raycasting Objects
    public Transform visionPoint; //Eyeball, essentially
    public LayerMask castingMask; //Ignore other enemies when checking player vision

    //Agent Collider
    private Collider agentCollider;

    //Points per circle range
    private Vector3 midRangeFront;
    private Vector3 midRangeRight;
    private Vector3 midRangeLeft;
    private Vector3 midRangeBack;

    private Vector3 closeRangeFront;
    private Vector3 closeRangeRight;
    private Vector3 closeRangeLeft;
    private Vector3 closeRangeBack;

    private Vector3 attackRangeFront;
    private Vector3 attackRangeRight;
    private Vector3 attackRangeLeft;
    private Vector3 attackRangeBack;

    //Attack Ranges for Targetting Player
    private float attackRange = 3.0f;
    private float closeRange = 9.0f;
    private float midRange = 18.0f;

    //Enemy States
    public bool seenByPlayer = false;


    void Start() {
        //camController = player.GetComponent<CameraController>();
        agent = GetComponent<NavMeshAgent>();
        agentCollider = GetComponent<CapsuleCollider>();
        //target = player.transform;
    }

    // Update is called once per frame
    void FixedUpdate() {

        /*if (seenByPlayer)
            Debug.Log("seen by player");

        setRangeTargets();*/

        /*float distance = Vector3.Distance(transform.position, player.transform.position);

        if (distance <= midRange + agent.stoppingDistance)
            faceTarget(player.transform);

        else
            agent.SetDestination(midRangeFront);*/

    }

    public void faceTarget(Transform target) {

        Vector3 direction = (target.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5.0f);

    }

    public void setIfSeenByPlayer(CameraController camController) {

        if (GeometryUtility.TestPlanesAABB(camController.getPlanes(), agentCollider.bounds))
            seenByPlayer = ifRaycastHitsPlayer(visionPoint.position);
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
    private bool ifRaycastHitsPlayer(Vector3 source) {

        RaycastHit hit;

        Vector3 playerPos = new Vector3(player.transform.position.x, playerCamera.transform.position.y, player.transform.position.z);

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
        Gizmos.DrawWireSphere(closeRangeLeft, sphereSize);

    }

    //Resources:
    //http://answers.unity.com/questions/8003/how-can-i-know-if-a-gameobject-is-seen-by-a-partic.html
    //https://answers.unity.com/questions/605155/raycast-get-which-layer-was-hit.html
}
