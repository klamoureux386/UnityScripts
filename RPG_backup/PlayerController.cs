using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]

public class PlayerController : MonoBehaviour
{
    //Movement Variables
    private float walkSpeed = 16.0f; //Default 16
    private float runSpeed = 24.0f; //Default 24
    private float jumpSpeed = 18.0f; //Default 18
    private float airDriftSpeed = 0.5f; //for left-right movement in air, default 0.5
    private float gravity = 30.0f; //Default 30

    //Slide Variables
    private float slideMultiplier = 1.4f; //Default 1.25-1.4
    private float timeSlideStart = 0;
    //Slide Animation Variables
    private float timeForcedToSlide = 0.25f; //Should be same length as slide animation duration
    private float timeToGetUpFromSlide = 0.10f; //Should be same length as slide get-up animation

    //Grounded Movement States
    private bool crouching = false;
    public bool sliding = false;
    private bool isRunning = false;
    private bool forcedToSlide = false;
    private bool slideJumpBuffered = false;

    //Aerial Movement States
    private bool airJumpUsed = false;
    private bool runningWhenJumped = false;
    private bool lastInAir = false;
    private bool jumpToAir = false; //bool to determine if we jumped to end up in air or walked off of a ledge

    //Last Movement Variables
    private Vector3 lastPosition;
    private Vector3 positionOfLastJump;
    private Vector3 lastMomentum;
    private bool lastSliding = false;

    //private Vector3 slideMomentum;

    //Components
    public GameObject playerObject;
    public Animator anim;
    CameraController cameraController;
    CharacterController characterController;
    GroundChecker groundChecker;

    Vector3 moveDirection = Vector3.zero;

    /*[HideInInspector]
    public bool canMove = true;*/

    //To do: move slide functions into a Slide Controller

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        cameraController = GetComponent<CameraController>();
        groundChecker = GetComponent<GroundChecker>();

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        //lastPosition = playerObject.transform.position;
        positionOfLastJump = playerObject.transform.position;
    }

    void Update()
    {

        lastPosition = playerObject.transform.position;
        lastSliding = sliding;

        setGroundedMovementStates();

        //Debug.Log(characterController.isGrounded ? "GROUNDED" : "NOT GROUNDED");

        /*
        if (isRunning)
            cameraController.setSprintVignette();

        else
            cameraController.setWalkVignette();
        */


        //If we were sliding last frame but no longer are, raise camera

        //TO DO: will not call if the player does the following:
        //Slides and holds slide
        //Jumps while still holding slide
        //Air Jumps while still holding slide
        //Releases slide button while in air jump before hitting ground
        //issue: camera gets stuck at sliding level and will not raise back up

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
                //Debug.Log("Begin Sliding");
                sliding = true;
                timeSlideStart = Time.time;
                StartCoroutine(forceSlideForTime(timeForcedToSlide));
                StartCoroutine(cameraController.dipCameraForSlide(timeForcedToSlide, Time.time));
                startSlide();
            }

            if (sliding)
            {
                //Debug.Log("Sliding");
                slideDecay();

                //if jump is buffered or key is pressed after slide lock
                if ( (Input.GetButtonDown("Jump") || slideJumpBuffered) && !forcedToSlide)
                {
                    //Debug.Log("Jumping from slide");
                    slideJump();
                    slideJumpBuffered = false;
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

        //Adjust Camera for current action
        //solution?: check if lastSliding == true in airJump, if so raise camera from slide
        if (lastSliding && !sliding && !forcedToSlide) {
            StartCoroutine(cameraController.raiseCameraFromSlide(timeToGetUpFromSlide, Time.time));
        }

        updateAnimator();
    }

    private void startSlide() {

        moveDirection = new Vector3(Input.GetAxisRaw("Horizontal"), -0.01f, Input.GetAxisRaw("Vertical"));
        moveDirection = transform.TransformDirection(moveDirection);

        if (moveDirection.magnitude > 1)
        {
            moveDirection.Normalize();
        }
        moveDirection.x *= (runSpeed * slideMultiplier);
        moveDirection.z *= (runSpeed * slideMultiplier);

        //slideMomentum = moveDirection; //BEFORE TRANSFORM DIRECTION
    }

    //TO DO: [fix] if move direction is less than the decay amount on start, it snaps the player to 0 on that axis, change how decay works
    //https://answers.unity.com/questions/1358491/character-controller-slide-down-slope.html
    //http://thehiddensignal.com/unity-angle-of-sloped-ground-under-player/
    private void slideDecay() {

        float decayAmount = 0.1f;
        bool xDecayed = false;
        bool zDecayed = false;

        if (Time.time - timeSlideStart < timeForcedToSlide) { //don't decay until not forced to slide
            decayAmount = 0;
        }

        //Downhill angles
        //don't decay slide if ground angle between -20 & -35
        if (groundChecker.groundSlopeAngle <= -20 && groundChecker.groundSlopeAngle >= -35)
            return;

        //add speed depending on angle <-35
        if (groundChecker.groundSlopeAngle < -35) {
            //TO DO
            return;
        }

        //Uphill angles
        if (groundChecker.groundSlopeAngle >= 20 && groundChecker.groundSlopeAngle <= 35)
        {
            forcedToSlide = false;
            decayAmount *= 5;
        }

        //Vector3 slideDecayVector = new Vector3(decayAmount, 0, decayAmount);

        //X Decay
        if (moveDirection.x > 0)
        {
            if (moveDirection.x <= decayAmount)
                xDecayed = true;

            else
                moveDirection.x -= decayAmount;
        }

        else if (moveDirection.x < 0) {
            if (moveDirection.x >= -decayAmount)
                xDecayed = true;

            else
                moveDirection.x += decayAmount;
        }

        //Z Decay
        if (moveDirection.z > 0)
        {
            if (moveDirection.z <= decayAmount)
                zDecayed = true;

            else
                moveDirection.z -= decayAmount;
        }

        else if (moveDirection.z < 0)
        {
            if (moveDirection.z >= -decayAmount)
                zDecayed = true;

            else
                moveDirection.z += decayAmount;
        }

        if (xDecayed && zDecayed) {
            moveDirection.x = 0;
            moveDirection.z = 0;
            sliding = false;
        }

    }

    private void groundJump() {
        moveDirection.y = jumpSpeed;
        positionOfLastJump = playerObject.transform.position;
        jumpToAir = true;
        //sliding = false;
    }

    private void slideJump() {

        jumpToAir = true;
        moveDirection.y = jumpSpeed;
        positionOfLastJump = playerObject.transform.position;
        moveDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        moveDirection = transform.TransformDirection(moveDirection); //makes jump go towards where character is facing

        if (moveDirection.magnitude > 1) {
            moveDirection.Normalize();
        }

        moveDirection.x *= (runSpeed * slideMultiplier);
        moveDirection.z *= (runSpeed * slideMultiplier);
        moveDirection.y = jumpSpeed;

        runningWhenJumped = true;

    }

    private void airJump() {

        airJumpUsed = true;
        jumpToAir = true;
        sliding = false;
        positionOfLastJump = playerObject.transform.position;
        moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        moveDirection = transform.TransformDirection(moveDirection);

        if (moveDirection.magnitude > 1) {
            moveDirection.Normalize();
        }

        moveDirection *= (runningWhenJumped ? runSpeed : walkSpeed);
        moveDirection.y = jumpSpeed * 0.75f; //slightly lower second jump

        //reset bool for next jump
        runningWhenJumped = false;
    }

    private IEnumerator forceSlideForTime(float seconds) {

        forcedToSlide = true;
        //Debug.Log("Forced to slide");

        yield return new WaitForSeconds(seconds);

        forcedToSlide = false;
        crouching = Input.GetButton("Crouch");

        //Debug.Log("Not forced to slide");

        yield return null;
    }

    private void updateAnimator() {

        anim.SetBool("isRunning", isRunning);
        anim.SetBool("sliding", sliding);
        anim.SetBool("forcedToSlide", forcedToSlide);

    }

    private void setGroundedMovementStates() {

        if (forcedToSlide && Input.GetButton("Jump")) {
            slideJumpBuffered = true;
        }

        if (!crouching && Input.GetButton("Shift")) {
            isRunning = true;
        }

        //set running to false unless last frame char was in air (last in air exception for cases where you want to slide on landing)
        else if (!sliding && characterController.isGrounded && !lastInAir) {
            isRunning = false;
        }

        if (Input.GetButton("Crouch") || forcedToSlide) {
            //Debug.Log("Crouching");
            crouching = true;
        }

        else {
            crouching = false;
            sliding = false;
        }
    }
}
