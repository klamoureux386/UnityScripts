using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GroundChecker : MonoBehaviour
{
    //Based on: http://thehiddensignal.com/unity-angle-of-sloped-ground-under-player/

    [Header("Results")]
    public float groundSlopeAngle = 0f;            // Angle of the slope in degrees
    public Vector3 groundSlopeDir = Vector3.zero;  // The calculated slope as a vector

    [Header("Settings")]
    public bool showDebug = false;                  // Show debug gizmos and lines
    public LayerMask castingMask;                   // Layer mask for casts. You'll want to ignore the player.
    private float startDistanceFromBottom = 0.2f;   // Should probably be higher than skin width
    private float sphereCastRadius = 1.0f;
    private float sphereCastDistance = 2.0f;        // How far spherecast moves down from origin point

    private float raycastLength = 1.5f;
    private float raycastLengthForward = 1.5f;

    public Transform rayOrigin1;                    //Front-most raycast position
    public Transform rayOrigin2;                    //Back-most raycast position
    public Transform rayOriginCenter;               //Center of character raycast position

    [Header("Sliding Info")]
    public bool locked = false;                     //Determines when to save/unsave origin locations
    private Quaternion rotationWhenLocked;
    public Transform rayLock1;
    public Transform rayLock2;
    public Transform rayLockCenter;


    //Component reference
    private CharacterController controller;

    /*To do: lock position of ray origins 1 & 2 on slide start for duration of slide [DONE]*/
    /*To do: UNLOCK transforms for one frame on jump (this allows the transforms to reposition on
      player jump assuming the player never releases the slide*/


    void Awake() 
    {
        // Get component on the same GameObject
        controller = GetComponent<CharacterController>();
        if (controller == null) { Debug.LogError("GroundChecker did not find a CharacterController component."); }
    }

    void FixedUpdate()
    {

        //Adjust raycast origin points based on direction character is moving relative to the way they're looking
        //note: front and back raycast location orientation based on rotation of center
        rotateRayCenterByInput();

        //Lock rotation of rayOrigins if entering slide
        //TO DO

        // Check ground, with an origin point defaulting to the bottom middle
        // of the char controller's collider. Plus a little higher
        if (controller && controller.isGrounded) {
            CheckGround(new Vector3(transform.position.x, transform.position.y - (controller.height / 2) + startDistanceFromBottom, transform.position.z));
        }
        else
            clearValues();

    }

    public void CheckGround(Vector3 origin) {

        // Out hit point from our cast(s)
        RaycastHit hit;

        // SPHERECAST
        // "Casts a sphere along a ray and returns detailed information on what was hit."
        if (Physics.SphereCast(origin, sphereCastRadius, Vector3.down, out hit, sphereCastDistance, castingMask)) {
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

        //RAYCASTS

        RaycastHit slopeHit1; //Front hit position
        RaycastHit slopeHit2; //Back hit position

        Vector3 frontPosition;
        Vector3 backPosition;

        if (!locked) {
            frontPosition = rayOrigin1.position;
            backPosition = rayOrigin2.position;
        }

        else {
            frontPosition = rayLock1.position;
            backPosition = rayLock2.position;
        }

        //First Raycast
        if (Physics.Raycast(frontPosition, Vector3.down, out slopeHit1, raycastLength)) {

            // Debug line to first hit point
            if (showDebug)
                Debug.DrawLine(frontPosition, slopeHit1.point, Color.red);

            // Get angle of slope on hit normal
            float angleOne = Vector3.Angle(slopeHit1.normal, Vector3.up);

            //Second Raycast
            if (Physics.Raycast(backPosition, Vector3.down, out slopeHit2, raycastLength)) {

                //If ground normal points directly up, surface is flat
                if (slopeHit1.normal == Vector3.up && slopeHit2.normal == Vector3.up) {
                    groundSlopeAngle = 0;
                    //Debug line to second hit point
                    if (showDebug) { Debug.DrawLine(backPosition, slopeHit2.point, Color.red); }
                }

                else {

                    Vector3 point1WithoutYDiff = new Vector3(slopeHit1.point.x, slopeHit2.point.y, slopeHit1.point.z);
                    groundSlopeAngle = returnAngleBetweenPoints(slopeHit1.point, slopeHit2.point, point1WithoutYDiff);

                    if (showDebug) {
                        //Blue & green debug lines to form angle triangle
                        Debug.DrawLine(slopeHit2.point, slopeHit1.point, Color.blue);
                        Debug.DrawLine(slopeHit2.point, point1WithoutYDiff, Color.green);
                    }

                }

            }

            else {
                // 2 collision points (sphere and first raycast): AVERAGE the two
                float average = (groundSlopeAngle + angleOne) / 2;
                groundSlopeAngle = average;
            }

            if (slopeHit1.point.y < slopeHit2.point.y)
                groundSlopeAngle *= -1;

        }

    }


    private void rotateRayCenterByInput() {

        Vector2 playerInput = new Vector2(Input.GetAxisRaw("Vertical"), Input.GetAxisRaw("Horizontal"));
        playerInput.Normalize();

        float angle = Mathf.Atan2(playerInput.y, playerInput.x);
        angle *= 180 / Mathf.PI; //convert to degrees

        Debug.Log("Angle to rotate by: " + angle);

        rayOriginCenter.transform.rotation = transform.rotation * Quaternion.AngleAxis(angle, Vector3.up);

        if (!locked)
            rayLockCenter.rotation = transform.rotation * Quaternion.AngleAxis(angle, Vector3.up);

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

    //Locks raycast rotation and sets it to it each frame
    public void lockRaycastLocations() {

        //if not locked, lock and record rotation
        if (!locked) {
            locked = true;
            Debug.Log("Raycast locations [Locked]");
            rotationWhenLocked = rayOriginCenter.rotation;
        }
        rayLockCenter.rotation = rotationWhenLocked;
    }

    public void unlockRaycastLocations() {

        //if locked, unlock and record rotation
        if (locked) {
            locked = false;
            Debug.Log("Raycast locations [Unlocked]");
            rayLockCenter.rotation = rayOriginCenter.rotation;
        }
    }

    private void clearValues() {
        groundSlopeAngle = 0f;
        groundSlopeDir = Vector3.zero;
    }

    void OnDrawGizmosSelected() {
        if (showDebug) {
            // Visualize SphereCast with two spheres and a line
            Vector3 startPoint = new Vector3(transform.position.x, transform.position.y - (controller.height / 2) + startDistanceFromBottom, transform.position.z);
            Vector3 endPoint = new Vector3(transform.position.x, transform.position.y - (controller.height / 2) + startDistanceFromBottom - sphereCastDistance, transform.position.z);

            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(startPoint, sphereCastRadius);

            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(endPoint, sphereCastRadius);

            Gizmos.DrawLine(startPoint, endPoint);

        }
    }

    //Resourcers:
    //https://stackoverflow.com/questions/19729831/angle-between-3-points-in-3d-space

}