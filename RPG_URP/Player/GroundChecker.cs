using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[RequireComponent(typeof(CharacterController))]
public class GroundChecker : MonoBehaviour
{
    //Based on: http://thehiddensignal.com/unity-angle-of-sloped-ground-under-player/

    [Header("Results")]
    public float groundSlopeAngle = 0f;            // Angle of the slope in degrees based on player orientation
    public float normalRelativeToUp = 0f;          // "True" angle of slope (abs value)
    public bool frontHit = false;
    public bool backHit = false;
    public Vector3 groundSlopeDir = Vector3.zero;  // The calculated slope as a vector
    public Vector3 surfaceNormal = Vector3.zero;
    public bool customIsGrounded = false;          // isGrounded based on spherecast
    //public bool raycastGrounded = false;

    [Header("Settings")]
    public bool showDebug = false;                  // Show debug gizmos and lines
    public LayerMask castingMask;                   // Layer mask for casts. You'll want to ignore the player.
    public float startDistanceFromBottom = 1.3f;    // Should probably be higher than skin width
    public float sphereCastRadius = 0.5f;
    public float sphereCastDistance = 1.0f;        // How far spherecast moves down from origin point

    private float raycastLengthFront = 1.2f;    //slightly longer for grabbing downhill slope info on slide
    private float raycastLengthBack = 0.8f;

    private float startingControllerHeight;

    public Transform rayOrigin1;                    //Front-most raycast position
    public Transform rayOrigin2;                    //Back-most raycast position
    public Transform rayOriginCenter;               //Center of character raycast position

    /*[Header("Sliding Info")]
    public bool locked = false;                     //Determines when to save/unsave origin locations
    public Quaternion rotationWhenLocked;
    public Transform rayLock1;
    public Transform rayLock2;
    public Transform rayLockCenter;*/

    //private float groundCheckRaycastDistance = 0.46f;

    //Component reference
    private CharacterController controller;


    void Awake() 
    {
        // Get component on the same GameObject
        controller = GetComponent<CharacterController>();
        if (controller == null) { Debug.LogError("GroundChecker did not find a CharacterController component."); }
        startingControllerHeight = controller.height;
    }

    void FixedUpdate()
    {

        Vector3 origin = new Vector3(transform.position.x, transform.position.y - (startingControllerHeight / 2) + startDistanceFromBottom*1.5f, transform.position.z);

        normalRelativeToUp = Mathf.Abs(Vector3.Angle(surfaceNormal, Vector3.up));

        //Lazy, fix later
        /*RaycastHit hit;
        if (Physics.Raycast(rayOriginCenter.transform.position, Vector3.down, out hit, groundCheckRaycastDistance, castingMask))
        {
            raycastGrounded = true;
        }
        else {
            raycastGrounded = false;
        }*/

        CheckGround(origin);

    }

    public void CheckGround(Vector3 origin) {

        //Debug.Log("Checking Ground");

        // Out hit point from our cast(s)
        RaycastHit hit;

        // SPHERECAST
        // "Casts a sphere along a ray and returns detailed information on what was hit."
        if (Physics.SphereCast(origin, sphereCastRadius, Vector3.down, out hit, sphereCastDistance, castingMask))
        {

            //Debug.Log("spherecast hit");

            surfaceNormal = hit.normal;

            customIsGrounded = true;
            // Angle of our slope (between these two vectors). 
            // A hit normal is at a 90 degree angle from the surface that is collided with (at the point of collision).
            // e.g. On a flat surface, both vectors are facing straight up, so the angle is 0.
            groundSlopeAngle = Vector3.Angle(hit.normal, Vector3.up);

            // Find the vector that represents our slope as well. 
            //  temp: basically, finds vector moving across hit surface 
            Vector3 temp = Vector3.Cross(hit.normal, Vector3.down);
            //  Now use this vector and the hit normal, to find the other vector moving up and down the hit surface
            groundSlopeDir = Vector3.Cross(temp, hit.normal);
        }

        else {
            clearValues();
            customIsGrounded = false;
        }

        //RAYCASTS

        RaycastHit slopeHit1; //Front hit position
        RaycastHit slopeHit2; //Back hit position

        Vector3 frontPosition;
        Vector3 backPosition;

        frontPosition = rayOrigin1.position;
        backPosition = rayOrigin2.position;

        //First Raycast
        if (Physics.Raycast(frontPosition, Vector3.down, out slopeHit1, raycastLengthFront)) {

            frontHit = true;

            // Debug line to first hit point
            if (showDebug)
                Debug.DrawLine(frontPosition, slopeHit1.point, Color.red);

            // Get angle of slope on hit normal
            float angleOne = Vector3.Angle(slopeHit1.normal, Vector3.up);

            //Second Raycast
            if (Physics.Raycast(backPosition, Vector3.down, out slopeHit2, raycastLengthBack)) {

                backHit = true;

                //If ground normal points directly up, surface is flat
                if (slopeHit1.normal == Vector3.up && slopeHit2.normal == Vector3.up)
                {
                    groundSlopeAngle = 0;
                }

                //Compute Ground angle
                else
                {

                    Vector3 point1WithoutYDiff = new Vector3(slopeHit1.point.x, slopeHit2.point.y, slopeHit1.point.z);
                    groundSlopeAngle = returnAngleBetweenPoints(slopeHit1.point, slopeHit2.point, point1WithoutYDiff);

                    if (showDebug)
                    {
                        //Debug line to second hit point
                        Debug.DrawLine(backPosition, slopeHit2.point, Color.red);
                        //Blue & green debug lines to form angle triangle
                        Debug.DrawLine(slopeHit2.point, slopeHit1.point, Color.blue);
                        Debug.DrawLine(slopeHit2.point, point1WithoutYDiff, Color.green);
                    }

                }

            }

            else
                backHit = false;

            /*else {
                // 2 collision points (sphere and first raycast): AVERAGE the two
                float average = (groundSlopeAngle + angleOne) / 2;
                groundSlopeAngle = average;
            }*/

            if (slopeHit1.point.y < slopeHit2.point.y)
                groundSlopeAngle *= -1;

        }

        else
            frontHit = false;

    }

    /*
        A
       /
      /
     /o
    B------C

    Returns angle o in degrees
    */
    private float returnAngleBetweenPoints(Vector3 A, Vector3 B, Vector3 C) {

        Vector3 v1 = new Vector3(A.x - B.x, A.y - B.y, A.z - B.z); //Vector BA
        Vector3 v2 = new Vector3(C.x - B.x, C.y - B.y, C.z - B.z); //Vector BC

        float v1mag = Mathf.Sqrt(v1.x * v1.x + v1.y * v1.y + v1.z * v1.z);
        Vector3 v1norm = new Vector3(v1.x / v1mag, v1.y / v1mag, v1.z / v1mag);

        float v2mag = Mathf.Sqrt(v2.x * v2.x + v2.y * v2.y + v2.z * v2.z);
        Vector3 v2norm = new Vector3(v2.x / v2mag, v2.y / v2mag, v2.z / v2mag);

        float res = v1norm.x * v2norm.x + v1norm.y * v2norm.y + v1norm.z * v2norm.z;

        float angle = Mathf.Acos(res);

        return angle * (180/Mathf.PI); //convert from radians to degrees
    }

    private void clearValues() {
        groundSlopeAngle = 0f;
        groundSlopeDir = Vector3.zero;
        surfaceNormal = Vector3.zero;
    }

    void OnDrawGizmosSelected() {
        if (showDebug) {

            //SPHERECAST DEBUG
            Vector3 startPoint = new Vector3(transform.position.x, transform.position.y - (startingControllerHeight / 2) + startDistanceFromBottom, transform.position.z);
            Vector3 endPoint = new Vector3(transform.position.x, transform.position.y - (startingControllerHeight / 2) + startDistanceFromBottom - sphereCastDistance, transform.position.z);

            Gizmos.color = Color.green;
            //Gizmos.DrawWireSphere(startPoint, sphereCastRadius);

            Gizmos.DrawWireSphere(startPoint, sphereCastRadius);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(endPoint, sphereCastRadius);

            Gizmos.DrawLine(startPoint, endPoint);

            //Debug.DrawRay(rayOriginCenter.transform.position, Vector3.down * groundCheckRaycastDistance, Color.cyan); //old raycast grounded
            /////////////////////////////////////////////////

            //RAYCAST DEBUG

            //Debug line to second hit point
            if (showDebug) {
                Debug.DrawRay(rayOrigin1.position, Vector3.down * raycastLengthFront, Color.red);
                Debug.DrawRay(rayOrigin2.position, Vector3.down * raycastLengthBack, Color.red);
                //Debug.DrawLine(rayOrigin2.position, slopeHit2.point, Color.red);
            }
        }
    }

    //Resourcers:
    //https://stackoverflow.com/questions/19729831/angle-between-3-points-in-3d-space

}