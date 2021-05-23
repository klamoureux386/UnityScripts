using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class TestEnemyAI : MonoBehaviour
{

    private enum Position{ 
        Top,
        Mid,
        Bot
    }

    public GameObject player;
    private NavMeshAgent agent;

    public LayerMask jumpCheckCastingMask; //Layer mask to set what should should stop backwards jumps. IGNORE ENEMY.
    //Todo: Solution to make sure other enemies dont stand where you're landing is to make a mock cylinder at landing location and have other agents consider it an obstacle. Destroy mock on landing frame

    //Jump Path Debug vars
    public bool debug;
    private Vector3 startOfJumpPosDebug = Vector3.zero;
    private Vector3 heightOfJumpPosDebug = Vector3.zero;
    private Vector3 endOfJumpPosDebug = Vector3.zero;

    //Jump Path vars
    private Vector3 startOfJumpPos = Vector3.zero;
    private Vector3 heightOfJumpPos = Vector3.zero;
    private Vector3 endOfJumpPos = Vector3.zero;
    private bool topPathClear = false;
    private bool midPathClear = false;
    private bool botPathClear = false;

    //Path Debug
    private DrawNavmeshPathDebug pathDebug;

    //Test Destination
    public Transform testDestination;

    //State vars
    private bool jumping = false;

    //Test Agent Vars
    private bool jumpFinished = false; //For testing move to destination after jump
    private bool destinationSet = false;

    //Jump vars
    private float jumpBackDistance = 15.0f;
    private float jumpBackHeight = 2.0f;

    [SerializeField]private float customJumpBackDistance = 0f;
    [SerializeField]private float customJumpBackHeight = 0f;

    private float jumpDuration = 1.5f; //For a full distance jump

    public AnimationCurve jumpHeightCurve; //Peak reached at t=0.75
    public float jumpHeightPeakPercent = 0.5f; //Todo: change this if animation curve changes

    [SerializeField]private float moveSpeed = 10f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        pathDebug = GetComponent<DrawNavmeshPathDebug>();

        agent.speed = 7f;

    }

    // Update is called once per frame
    void Update()
    {

        //rotateToFacePlayer();

        if (debug)
            debugJumpClear();

        //Debug.Log(checkIfJumpClear());

        int check = 0;

        if (!jumping)
            check = checkIfJumpClear();

        
        if (check == 1 && !jumpFinished) {

            //In cases where 2nd raycast hits a position, we can use the distance to that point (without Y difference) to determine distance to jump back
            //This will allow us to set an adaptive jump distance based on where the raycast hits. We can make a bunch of different 2-part raycasts for a ton of jumps!
            if (customJumpBackDistance != 0 && customJumpBackHeight != 0)
            {
                StartCoroutine(jumpBackwards(customJumpBackDistance, jumpBackHeight, customJumpBackHeight));
            }
            else
            {
                StartCoroutine(jumpBackwards(jumpBackDistance, jumpBackHeight, 0));
            }
        }

        if (jumpFinished) {

            customJumpBackDistance = 0;
            customJumpBackHeight = 0;
            jumpFinished = false;
            jumping = false;
        }

        /*!
        //Jump first
        if (!jumping)
            StartCoroutine(jumpBackwards(jumpBackDistance, jumpBackHeight));

        
        //Navigate to destination
        if (jumpFinished && !destinationSet)
        {
            agent.SetDestination(testDestination.position);
            destinationSet = true;
        }

        //Draws path every frame
        if (debug && agent.path.status == NavMeshPathStatus.PathComplete && destinationSet) {
            pathDebug.drawPath(agent.path);
        }

        //!For Debug (Repeated jumps)
        if (jumpFinished) {
            //jumping = false;
            //jumpFinished = false;
            //agent.enabled = true;
        }

        if (jumpFinished && destinationSet && checkIfReachedDestination())
        {
            jumpFinished = false;
            destinationSet = false;
            StartCoroutine(jumpBackwards());
        }
        */

    }

    //https://www.loekvandenouweland.com/content/using-AnimationCurve-and-Vector3.Lerp-to-animate-an-object-in-unity.html

    //TODO: SPLIT LERP INTO 2 PARTS TO HANDLE CUSTOM JUMPS
    private IEnumerator jumpBackwards(float distance, float height, float endingJumpHeightDifference) {

        jumping = true;

        Vector3 forwardWithoutY = new Vector3(transform.forward.x, 0, transform.forward.z);

        Vector3 jumpDirection = -forwardWithoutY.normalized * distance;
        Vector3 jumpHeight = Vector3.up * height;

        Vector3 startingXZ = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 startingY = new Vector3(0, transform.position.y, 0);

        float elapsedTime = 0f;

        float t;

        //float t1; //time var for XZ lerp
        //float t2; //time var for Y lerp

        //TODO: PIECEWISE LERP https://www.alanzucconi.com/2021/01/24/piecewise-interpolation/

        //For 1st half of jump, keep t = jump duration
        //once we reach height of jump, set t = customDistance/TotalJumpDistance

        while (elapsedTime < jumpDuration)
        {

            //sinerp^2 duration https://chicounity3d.wordpress.com/2014/05/23/how-to-lerp-like-a-pro/
            t = elapsedTime / jumpDuration;

            //Curve for X lerp
            //t1 = Mathf.Sin(t * Mathf.PI * 0.5f);
            //t1 = t1 * t1;

            Vector3 totalMove = Vector3.Lerp(startingXZ, startingXZ + jumpDirection, t); //Z&X movement

            totalMove += Vector3.Lerp(startingY, startingY + jumpHeight, jumpHeightCurve.Evaluate(t)); //Y Movement

            //https://answers.unity.com/questions/1442044/navmesh-agent-not-placed-on-mesh.html
            //!Important read regarding agent warp
            //https://forum.unity.com/threads/nav-generating-inside-non-walkable-objects.445177/
            agent.Warp(totalMove); //!<---Clutch

            elapsedTime += Time.deltaTime;

            yield return null;
        }


        //Check position X feet back from transform

        //Determine jump height of Y feet from ground

        //Check height clearance (character height + y) to make sure jumping backwards is ok

        //Can use two separate lerp formulas, one for X position over time and one for Y position for varying animation

        //Should work on non-flat ground albeit with some issues maybe

        jumpFinished = true;

        //yield return null;

    }

    private void rotateToFacePlayer() {

        Vector3 playerTransformWithoutYDifference = new Vector3(player.transform.position.x, transform.position.y, player.transform.position.z);

        transform.rotation = Quaternion.LookRotation(playerTransformWithoutYDifference - transform.position, Vector3.up);
    }


    /* !RETURN VALUES
     * 0: Top, Middles, and Base jump paths all blocked -- Cannot jump! Consider jumping/dashing a different direction.
     * 1: All paths clear, good to jump
     * 2: Middle and Bottom paths clear, Top path blocked -- Consider dashing backwards instead, good for retreating uphills backwards. Use this: https://docs.unity3d.com/ScriptReference/AI.NavMesh.Raycast.html
     * 3: Middle and Top paths clear, Bottom path blocked -- Consider another jump height slightly higher than current one.
     * 4: Only Top path clear, Bottom & Middle path blocked -- Consider another jump height significantly higher than current one.
     * 5: Only Middle path clear or only Bottom path clear -- Return 1, too rare/specific to be useful.
     * */
    public int checkIfJumpClear() {

        //Vector3 normalizedCharForwardWithoutY = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;

        bool topClear;
        bool midClear;
        bool botClear;

        topClear = checkIfPathClearMidOrTop(Position.Top);
        midClear = checkIfPathClearMidOrTop(Position.Mid);

        //Checks if bottom hit is 
        NavMeshHit? botHit = checkIfPathClearBot();

        if (botHit == null)
            botClear = false;
        else
            botClear = true;

        //botClear = checkIfPathClear(Position.Bot);

        if (topClear && midClear && botClear)
            return 1;

        if (topClear && midClear && !botClear)
            return 3;

        return 0;

    }

    //Potentially return nullable raycast where null = true & raycast = probably false but can be evaluated?
    private bool checkIfPathClearMidOrTop(Position pos) {

        float startingY = 0;

        if (pos == Position.Top)
            startingY = agent.height;
        else if (pos == Position.Mid)
            startingY = agent.height / 2;
        else
            startingY = 0;


        Vector3 normalizedCharForwardWithoutY = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;

        startOfJumpPos = new Vector3(transform.position.x, transform.position.y + startingY, transform.position.z);

        heightOfJumpPos = new Vector3(
            transform.position.x - (normalizedCharForwardWithoutY.x * jumpHeightPeakPercent * jumpBackDistance), //3/4 of X distance backwards from character
            transform.position.y + startingY + jumpBackHeight,                                //Top of character plus height to jump off of ground
            transform.position.z - (normalizedCharForwardWithoutY.z * jumpHeightPeakPercent * jumpBackDistance) //3/4 of Z distance backwards from character
            );

        endOfJumpPos = startOfJumpPos - (normalizedCharForwardWithoutY * jumpBackDistance);

        RaycastHit firstHalfOfJump;
        RaycastHit secondHalfOfJump;

        //LineCast for first half of jump. If collision on first half of jump just return false -- collision on 2nd half is intended to determine where to land.
        if (Physics.Linecast(startOfJumpPos, heightOfJumpPos, out firstHalfOfJump, jumpCheckCastingMask))
            return false;

        //Second half of jump Linecast -- returns false if mid or top cast hit something
        if (Physics.Linecast(heightOfJumpPos, endOfJumpPos, out secondHalfOfJump, jumpCheckCastingMask) && pos != Position.Bot)
        {
            Debug.Log($"2nd half of {pos} ray collides with something!");
            return false; //THIS IS WHERE WE WOULD RETURN THE RESULTING HIT, CAN BASE LANDING LOCATION OFF OF IT
        }

        return true;

    }

    private NavMeshHit? checkIfPathClearBot() {

        float startingY = 0;

        Vector3 normalizedCharForwardWithoutY = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;

        startOfJumpPos = new Vector3(transform.position.x, transform.position.y + startingY, transform.position.z);

        heightOfJumpPos = new Vector3(
            transform.position.x - (normalizedCharForwardWithoutY.x * jumpHeightPeakPercent * jumpBackDistance), //3/4 of X distance backwards from character
            transform.position.y + startingY + jumpBackHeight,                                //Top of character plus height to jump off of ground
            transform.position.z - (normalizedCharForwardWithoutY.z * jumpHeightPeakPercent * jumpBackDistance) //3/4 of Z distance backwards from character
            );

        endOfJumpPos = startOfJumpPos - (normalizedCharForwardWithoutY * jumpBackDistance);

        RaycastHit firstHalfOfJump;
        RaycastHit secondHalfOfJump;

        //LineCast for first half of jump. If collision on first half of jump just return false -- collision on 2nd half is intended to determine where to land.
        if (Physics.Linecast(startOfJumpPos, heightOfJumpPos, out firstHalfOfJump, jumpCheckCastingMask))
        {
            Debug.Log("First half of bottom ray collides with something");
            return null;
        }

        Vector3 positionOfNavMeshCheck;
        bool customDistance = false;

        //Check if point of collision or end of path is within a certain range of a navmesh spot
        if (Physics.Linecast(heightOfJumpPos, endOfJumpPos, out secondHalfOfJump, jumpCheckCastingMask))
        {
            Debug.Log("Second half of bottom ray collides with something before end of jump back: " + secondHalfOfJump.point + ", setting custom jump distances");
            positionOfNavMeshCheck = secondHalfOfJump.point;

            Vector3 jumpPointWithoutYDifference = new Vector3(secondHalfOfJump.point.x, startOfJumpPos.y, secondHalfOfJump.point.z);

            customJumpBackDistance = Vector3.Distance(startOfJumpPos, jumpPointWithoutYDifference);
            customJumpBackHeight = secondHalfOfJump.point.y - startOfJumpPos.y; //Add default jump height to this when using it to calculate the maximum height

            Debug.Log($"Custom Jump Distances set: {customJumpBackDistance}, {customJumpBackHeight}");


        }
        else {

            Debug.Log("Second half of bottom ray reached its end without colliding with anything, checking if navmesh is within range.");
            positionOfNavMeshCheck = endOfJumpPos;
        }

        NavMeshHit result;

        //Distance to check for navmesh should be slightly greater than agent Offset (distance from surface) Default 0.01?
        float meshCheckDistance = 0.1f;

        if (NavMesh.SamplePosition(positionOfNavMeshCheck, out result, meshCheckDistance, NavMesh.AllAreas))
        {

            if (customDistance)

            //Draw wiresphere at navmesh location
            if (debug)
            {
                Debug.DrawLine(positionOfNavMeshCheck, positionOfNavMeshCheck + Vector3.up * meshCheckDistance, Color.red);
                Debug.DrawLine(positionOfNavMeshCheck, positionOfNavMeshCheck - Vector3.up * meshCheckDistance, Color.red);
            }
            Debug.Log("Navmesh point found at end of jump distance: " + result.position);
            return result;
        }

        else
        {
            Debug.Log("Cannot find navmesh point within search radius at end of jump distance.");
            return null;
        }
    }


    //!IMPORTANT: THIS ASSUMES THAT THE POSITION OF THE SCRIPT IS AT THE BASE OF THE MODEL AND NOT THEIR CENTER///////////////////////////////////////
    private void debugJumpClear()
    {

        Vector3 normalizedCharForwardWithoutY = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;

        float topStartingY = agent.height;
        float midStartingY = agent.height / 2;
        float botStartingY = 0;

        //Jump Clear From Base of Agent
        //Set positions only when not jumping (keeps route visible throughout jump)
        if (!jumping)
        {

            startOfJumpPosDebug = new Vector3(transform.position.x, transform.position.y, transform.position.z);

            heightOfJumpPosDebug = new Vector3(
                transform.position.x - (normalizedCharForwardWithoutY.x * jumpHeightPeakPercent * jumpBackDistance), //Jump Height peak time (at t = 3/4) of X distance backwards from character
                transform.position.y + jumpBackHeight,                                                            //Top of character plus height to jump off of ground
                transform.position.z - (normalizedCharForwardWithoutY.z * jumpHeightPeakPercent * jumpBackDistance)  //3/4 of Z distance backwards from character
                );

            endOfJumpPosDebug = startOfJumpPosDebug - (normalizedCharForwardWithoutY * jumpBackDistance);

        }

        //Bottom Line
        Debug.DrawLine(startOfJumpPosDebug + new Vector3(0, botStartingY, 0), heightOfJumpPosDebug + new Vector3(0, botStartingY, 0), Color.yellow);
        Debug.DrawLine(heightOfJumpPosDebug + new Vector3(0, botStartingY, 0), endOfJumpPosDebug + new Vector3(0, botStartingY, 0), Color.yellow);

        //MiddleLine
        Debug.DrawLine(startOfJumpPosDebug + new Vector3(0, midStartingY, 0), heightOfJumpPosDebug + new Vector3(0, midStartingY, 0), Color.yellow);
        Debug.DrawLine(heightOfJumpPosDebug + new Vector3(0, midStartingY, 0), endOfJumpPosDebug + new Vector3(0, midStartingY, 0), Color.yellow);

        //TopLine
        Debug.DrawLine(startOfJumpPosDebug + new Vector3(0, topStartingY, 0), heightOfJumpPosDebug + new Vector3(0, topStartingY, 0), Color.yellow);
        Debug.DrawLine(heightOfJumpPosDebug + new Vector3(0, topStartingY, 0), endOfJumpPosDebug + new Vector3(0, topStartingY, 0), Color.yellow);

        
        //!FUNCTIONING AS INTENDED
        //Debug for Custom Jumps
        if (customJumpBackDistance != 0 && customJumpBackHeight != 0) {

            Vector3 heightOfJumpPosDebugCustom = new Vector3(
                    startOfJumpPosDebug.x - (normalizedCharForwardWithoutY.x * jumpHeightPeakPercent * customJumpBackDistance), //Jump Height peak time (at t = 3/4) of X distance backwards from character
                    startOfJumpPosDebug.y + customJumpBackHeight + jumpBackHeight,                                              //Top of character + regular flat surface jump height +  jump height to land on raised surface
                    startOfJumpPosDebug.z - (normalizedCharForwardWithoutY.z * jumpHeightPeakPercent * customJumpBackDistance)  //3/4 of Z distance backwards from character
                    );
            Vector3 endOfJumpPosDebugCustom = startOfJumpPosDebug + (Vector3.up * customJumpBackHeight) - (normalizedCharForwardWithoutY * customJumpBackDistance);


            //Bottom Line Custom
            Debug.DrawLine(startOfJumpPosDebug + new Vector3(0, botStartingY, 0), heightOfJumpPosDebugCustom + new Vector3(0, botStartingY, 0), Color.red);
            Debug.DrawLine(heightOfJumpPosDebugCustom + new Vector3(0, botStartingY, 0), endOfJumpPosDebugCustom + new Vector3(0, botStartingY, 0), Color.red);

            //MiddleLine Custom
            Debug.DrawLine(startOfJumpPosDebug + new Vector3(0, midStartingY, 0), heightOfJumpPosDebugCustom + new Vector3(0, midStartingY, 0), Color.red);
            Debug.DrawLine(heightOfJumpPosDebugCustom + new Vector3(0, midStartingY, 0), endOfJumpPosDebugCustom + new Vector3(0, midStartingY, 0), Color.red);

            //TopLine Custom
            Debug.DrawLine(startOfJumpPosDebug + new Vector3(0, topStartingY, 0), heightOfJumpPosDebugCustom + new Vector3(0, topStartingY, 0), Color.red);
            Debug.DrawLine(heightOfJumpPosDebugCustom + new Vector3(0, topStartingY, 0), endOfJumpPosDebugCustom + new Vector3(0, topStartingY, 0), Color.red);

        }


    }

    public bool checkIfReachedDestination() {

        if (!agent.pathPending)
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                {
                    return true;
                }
            }
        }

        return false;

    }


}
