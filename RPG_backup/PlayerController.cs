using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]

public class PlayerController : MonoBehaviour
{
    //Movement Variables
    private float walkSpeed = 16.0f;
    private float runSpeed = 24.0f;
    private float jumpSpeed = 18.0f;
    private float airDriftSpeed = 0.5f; //for left-right movement in air
    private float gravity = 30.0f;

    //Slide Variables
    private float slideMultiplier = 1.2f;
    private float timeForcedToSlide = 1.5f;
    private float timeSlideStart = 0;

    //Movement States
    private bool crouching = false;
    private bool sliding = false;
    private bool isRunning = false;
    private bool forcedToSlide = false;

    private bool airJumpUsed = false;
    private bool runningWhenJumped = false;
    private bool lastInAir = false;
    private bool jumpToAir = false; //bool to determine if we jumped to end up in air or walked off of a ledge

    //Movement Vectors
    private Vector3 lastPosition;
    private Vector3 positionOfLastJump;
    private Vector3 lastMomentum;
    private Vector3 slideMomentum;

    public GameObject playerObject;
    CameraController cameraController;
    CharacterController characterController;
    Vector3 moveDirection = Vector3.zero;

    [HideInInspector]
    public bool canMove = true;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        cameraController = GetComponent<CameraController>();

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        lastPosition = playerObject.transform.position;
        positionOfLastJump = playerObject.transform.position;
    }

    void Update()
    {


        lastPosition = playerObject.transform.position;


        //Debug.Log(characterController.isGrounded ? "GROUNDED" : "NOT GROUNDED");

        if (!crouching && Input.GetButton("Shift"))
        {
            isRunning = true;
        }

        else if (!sliding && characterController.isGrounded)
        {
            isRunning = false;
        }

        if (Input.GetButton("Crouch") || forcedToSlide)
        {
            //Debug.Log("Crouching");
            crouching = true;
        }

        else {
            crouching = false;
            sliding = false;
            Debug.Log("Not Sliding");
        }

        /*
        if (isRunning)
            cameraController.setSprintVignette();

        else
            cameraController.setWalkVignette();
        */

        if (characterController.isGrounded) {

            //if last frame was in air, and we just hit ground, bob camera based on fall speed
            if (lastInAir) {
                ///Debug.Log("y position of last jump: " + positionOfLastJump.y);
                cameraController.checkBobCamera(positionOfLastJump.y, jumpToAir);
            }

            airJumpUsed = false;
            lastInAir = false;
            positionOfLastJump = playerObject.transform.position; //set to player location for cases where character walks off cliff

            //can only slide if moving forward and currently running.
            if (isRunning && Input.GetButton("Crouch") && Input.GetAxis("Vertical") > 0 && !sliding)
            {
                Debug.Log("Begin Sliding");
                sliding = true;
                timeSlideStart = Time.time;
                StartCoroutine(forceSlideForTime(timeForcedToSlide));
                startSlide();
            }

            if (sliding)
            {
                Debug.Log("Sliding");
                //slideDecay();

                if (sliding && Input.GetButtonDown("Jump") && !forcedToSlide)
                {
                    Debug.Log("Jumping from slide");
                    groundJump();
                    sliding = false;
                }

            }

            //if not sliding
            else
            {

                //adds slight downward push to ensure isGrounded is detected properly
                moveDirection = new Vector3(Input.GetAxis("Horizontal"), -0.01f, Input.GetAxis("Vertical"));
                moveDirection = transform.TransformDirection(moveDirection);

                if (moveDirection.magnitude > 1)
                {
                    moveDirection.Normalize();
                }

                //if running, multiply moveDirection by runSpeed, else multiply by walk speed
                moveDirection *= (isRunning ? runSpeed : walkSpeed);
                runningWhenJumped = isRunning;

                if (Input.GetButtonDown("Jump"))
                {
                    groundJump();
                }

            }

        }

        else {
            lastInAir = true;

            //if jumping in air
            if (Input.GetButtonDown("Jump") && !airJumpUsed)
            {
                airJump();
            }

            //For drifting in air
            else {

                //To do: ?
            }

            //apply gravity if in air
            moveDirection.y -= gravity * Time.deltaTime;

        }

        lastMomentum = moveDirection;
        characterController.Move(moveDirection * Time.deltaTime);
    }

    private void startSlide() {

        moveDirection = new Vector3(Input.GetAxisRaw("Horizontal"), -0.01f, Input.GetAxisRaw("Vertical"));
        if (moveDirection.magnitude > 1)
        {
            moveDirection.Normalize();
        }
        moveDirection.x *= (runSpeed * slideMultiplier);
        moveDirection.z *= (runSpeed * slideMultiplier);

        slideMomentum = moveDirection; //BEFORE TRANSFORM DIRECTION

        moveDirection = transform.TransformDirection(moveDirection);
    }

    private void slideDecay() {

        float decayAmount = 0.1f;

        if (moveDirection.x > 0 && moveDirection.x < decayAmount)
            moveDirection.x = 0;

        else
            moveDirection.x -= decayAmount;

        if (moveDirection.z > 0 && moveDirection.z < decayAmount)
            moveDirection.z = 0;

        else
            moveDirection.z -= decayAmount;

        //moveDirection = transform.TransformDirection(moveDirection);

    }

    private void groundJump() {
        moveDirection.y = jumpSpeed;
        positionOfLastJump = playerObject.transform.position;
        jumpToAir = true;
    }

    private void airJump() {

        airJumpUsed = true;
        jumpToAir = true;
        positionOfLastJump = playerObject.transform.position;
        moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        moveDirection = transform.TransformDirection(moveDirection);

        if (moveDirection.magnitude > 1)
        {
            moveDirection.Normalize();
        }

        moveDirection *= (runningWhenJumped ? runSpeed : walkSpeed);
        moveDirection.y = jumpSpeed * 0.75f; //slightly lower second jump

        //reset bool for next jump
        runningWhenJumped = false;
    }

    private IEnumerator forceSlideForTime(float seconds) {

        forcedToSlide = true;
        Debug.Log("Forced to slide");

        yield return new WaitForSeconds(seconds);

        forcedToSlide = false;
        crouching = Input.GetButton("Crouch");

        Debug.Log("Not forced to slide");

        yield return null;
    }
}
