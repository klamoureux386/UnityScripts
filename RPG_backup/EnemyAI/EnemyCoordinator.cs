using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyCoordinator : MonoBehaviour
{

    //GameObjects & Components
    public GameObject player;
    public CameraController playerCamController;

    //List of controllers on each enemy we want to control
    public EnemyController[] controllers;

    //List of targets varying by range to player

    //to do: this but better
    #region Ranges
    //Mid-range
    private Vector3 midRangeFront;
    private Vector3 midRangeRight;
    private Vector3 midRangeLeft;
    private Vector3 midRangeBack;

    //Close-range
    private Vector3 closeRangeFront;
    private Vector3 closeRangeRight;
    private Vector3 closeRangeLeft;
    private Vector3 closeRangeBack;

    //Attack-range
    private Vector3 attackRangeFront;
    private Vector3 attackRangeRight;
    private Vector3 attackRangeLeft;
    private Vector3 attackRangeBack;
    #endregion

    //Attack Ranges for Targetting Player
    private float attackRange = 3.0f;
    private float closeRange = 9.0f;
    private float midRange = 18.0f;

    void FixedUpdate()
    {
        setRangeTargets();

        foreach (EnemyController controller in controllers) {

            controller.setIfSeenByPlayer(playerCamController);

            float distanceToPlayer = Vector3.Distance(controller.transform.position, player.transform.position);

            if (distanceToPlayer <= midRange + controller.agent.stoppingDistance)
                controller.faceTarget(player.transform);

            else if (distanceToPlayer < controller.lookRadius)
                controller.agent.SetDestination(midRangeFront);

        }

    }

    private void setRangeTargets() {

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

    }

    private void OnDrawGizmosSelected() {

        foreach (EnemyController controller in controllers) {
            //Agent Look Radius
            Gizmos.color = Color.grey;
            Gizmos.DrawWireSphere(controller.transform.position, controller.lookRadius);
        }

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
}
