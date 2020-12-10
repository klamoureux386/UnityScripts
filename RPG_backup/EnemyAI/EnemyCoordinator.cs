using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyCoordinator : MonoBehaviour
{

    //GameObjects & Components
    public GameObject player;
    public CameraController playerCamController;

    //List of controllers on each enemy we want to control
    public EnemyController[] cautiousControllers;

    //List of targets varying by range to player

    //to do: this but better
    //Numbers go clockwise from north e.g.
    //0 = front, 1 = right, 2 = back, 3 = left


    //Mid-range
    /*private Vector3 midRangeFront;
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
    private Vector3 attackRangeBack;*/


    //Attack Ranges for Targetting Player
    //Player range = 0
    private float meleeRange = 8.0f;  //Default 8
    private float closeRange = 20.0f; //Default 20
    private float shortRange = 40.0f; //Default 40
    private float midRange = 60.0f;   //Default 60
    private float longRange = 80.0f;  //Default 80

    //https://www.youtube.com/watch?v=e4dlp2YnVz8


    void FixedUpdate()
    {

        setCautiousEnemies();

    }

    private void setCautiousEnemies() {

        foreach (EnemyController controller in cautiousControllers) {

            controller.setDistanceToPlayer(calculateDistanceBetween(controller.transform.position, player.transform.position));
            setAvoidancePriority(controller);
            //controller.setIfSeenByPlayer(playerCamController);  //See EnemyController.cs for choosing between these two
            controller.setIfInDirectionOfView(playerCamController);

            //If player is within aggro range
            if (controller.distanceToPlayer <= controller.lookRadius) {

                string currentAgentRange = whatRangeFromPlayer(controller.transform.position);
                string currentDestRange = whatRangeFromPlayer(controller.currentDestination);

                //if in melee or close range -- face player
                if (currentAgentRange == "melee" || currentAgentRange == "close") {
                    controller.agent.angularSpeed = 0;
                    controller.faceTarget(player.transform);
                }

                else {
                    controller.agent.angularSpeed = 240; //default
                }    

                //If unseen by the player
                if (!controller.seenByPlayer) {

                    if (currentAgentRange == "close") {
                        controller.currentDestination = player.transform.position;
                        currentDestRange = "melee";
                    }

                    //If unseen by player -- set random close range destination
                    else if (currentAgentRange != "close" && currentDestRange != "close") {
                        controller.currentDestination = randomTargetOnRange("close");
                        currentDestRange = "close";
                    }

                }

                //If seen by player & not melee range
                else {

                    string desiredDestRange;

                    if (currentAgentRange == "far")
                        desiredDestRange = "long";

                    else if (currentAgentRange == "long")
                        desiredDestRange = "mid";

                    else if (currentAgentRange == "mid")
                        desiredDestRange = "short";

                    else if (currentAgentRange == "short")
                        desiredDestRange = "close";

                    else if (currentAgentRange == "close")
                        desiredDestRange = "melee";

                    else
                        desiredDestRange = "melee";

                    Debug.Log("current " + controller.gameObject.name + " range: " + currentAgentRange + "\n" + "current Dest range: " + currentDestRange + "\n" + "desired Dest range: " + desiredDestRange);

                    //If in close range, move directly towards player
                    if (controller.distanceToPlayer <= closeRange) {
                        controller.currentDestination = player.transform.position;
                    }

                    //If not in close range & controller destination has moved out of range
                    else if (currentDestRange != desiredDestRange) {

                        if (desiredDestRange != currentDestRange) {

                            controller.currentDestination = randomTargetOnRange(desiredDestRange);

                            ///Keep this for later, try comparing to nav mesh bounds instead next
                            //https://docs.unity3d.com/ScriptReference/AI.NavMeshAgent.CalculatePath.html
                            /*
                            NavMeshPath path = new NavMeshPath();
                            //Debug.Log(controller.agent.CalculatePath(randomTarget, path));

                            string newRange = desiredDestRange;

                            //While path is partial, lower range by 1 and compute new path until we find a path, stops once we force path onto player position
                            //Note: this doesn't hit because somehow spots outside of the navmesh aren't being picked idek ig it has to do with calculatePath()
                            while (path.status == NavMeshPathStatus.PathPartial && newRange != "player") {
                                randomTarget = randomTargetOnRange(rangeBelow(newRange));
                                controller.agent.CalculatePath(randomTarget, path);
                                newRange = rangeBelow(newRange);
                                Debug.Log("Lowering range due to partial path, new range: " + newRange);
                            }

                            pathCalculated = true;
                            controller.currentDestination = randomTarget;
                            controller.agent.SetPath(path);*/

                        }

                    }

                }

            }

            //if currentDestination is different from set destination
            if (controller.agent.destination != controller.currentDestination)
                controller.agent.SetDestination(controller.currentDestination);


        }

    }

    private string rangeBelow(string range) {

        switch (range) {

            case "far": return "long";
            case "long": return "mid";
            case "mid": return "short";
            case "short": return "close";
            case "close": return "melee";
            case "melee": return "player";
            default: return "player";

        }

    }

    //returns random point on a circle whose radius is based on range
    private Vector3 randomTargetOnRange(string range) {

        float radius = 0;
        float distanceInsideRange = 5.0f; //should be at least radius of agent
        //note: larger numbers will make the enemies gravitate towards the player
        //faster. can be used to fine-tune the sporatic movement
        //should also be less than stopping distance i think

        if (range == "player") { return player.transform.position; }
        if (range == "melee") { radius = meleeRange; }
        if (range == "close") { radius = closeRange; }
        if (range == "short") { radius = shortRange; }
        if (range == "mid") { radius = midRange; }
        if (range == "long") { radius = longRange; }

        float randomRadian = Random.Range(0, 360) * Mathf.PI / 180;

        float randomX = Mathf.Cos(randomRadian);
        float randomZ = Mathf.Sin(randomRadian);

        Vector3 offset = new Vector3(randomX, 0, randomZ);

        offset *= (radius - distanceInsideRange);

        return offset + player.transform.position;
    }

    private void setAvoidancePriority(EnemyController controller) {

        if (controller.distanceToPlayer <= meleeRange)
            controller.agent.avoidancePriority = 5;

        else if (controller.distanceToPlayer <= closeRange)
            controller.agent.avoidancePriority = 10;

        else if (controller.distanceToPlayer <= shortRange)
            controller.agent.avoidancePriority = 20;

        else if (controller.distanceToPlayer <= midRange)
            controller.agent.avoidancePriority = 30;

        else
            controller.agent.avoidancePriority = 40;
    }

    private string whatRangeFromPlayer(Vector3 point) {

        float distanceToPlayer = calculateDistanceBetween(point, player.transform.position);

        if (distanceToPlayer <= meleeRange)
            return "melee";

        if (distanceToPlayer <= closeRange)
            return "close";

        if (distanceToPlayer <= shortRange)
            return "short";

        if (distanceToPlayer <= midRange)
            return "mid";

        if (distanceToPlayer <= longRange)
            return "long";

        return "far";
    }

    //returns true if range 2 is outside of range1
    private bool destinationOutsideExpectedRange(string range1, string range2) {

        //if range 2 > melee range, return true
        if (range1 == "melee")
            return (range2 == "close" || range2 == "short" || range2 == "mid" || range2 == "long" || range2 == "far");

        if (range1 == "close")
            return (range2 == "short" || range2 == "mid" || range2 == "long" || range2 == "far");

        if (range1 == "short")
            return (range2 == "mid" || range2 == "long" || range2 == "far");

        if (range1 == "mid")
            return (range2 == "long" || range2 == "far");

        if (range1 == "long")
            return (range2 == "far");

        return false;
    }

    //TO DO: change to sqrMagnitude calculation for efficiency
    private float calculateDistanceBetween(Vector3 source, Vector3 target) {

        return Vector3.Distance(source, target);
    }

    private void OnDrawGizmosSelected() {

        foreach (EnemyController controller in cautiousControllers) {
            //Agent Look Radius
            Gizmos.color = Color.grey;
            Gizmos.DrawWireSphere(controller.transform.position, controller.lookRadius);

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(controller.agent.destination, 0.25f);
        }

        //Attack Ranges (relative to player)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(player.transform.position, meleeRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(player.transform.position, closeRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(player.transform.position, shortRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(player.transform.position, midRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(player.transform.position, longRange);

        //Attack Range Target Destinations

    }

    //Notes:
    //Change distance call to sqrMagnitude (more efficient since we call it a lot)
    //https://docs.unity3d.com/ScriptReference/Vector3-sqrMagnitude.html
}
