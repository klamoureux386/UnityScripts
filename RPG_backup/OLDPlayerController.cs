using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]

public class OLDPlayerController : MonoBehaviour
{
    //Movement Variables
    private float walkSpeed = 16.0f; //Default 16
    private float runSpeed = 24.0f; //Default 24
    private float jumpSpeed = 18.0f; //Default 18
    //private float airDriftSpeed = 0.5f; //for left-right movement in air, default 0.5
    private float gravity = 3.0f; //Default 30
    private float groundedGravity = 0.01f;

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
    private Vector3 positionOfLastJump;
    private bool lastSliding = false;

    //Forces on Player
    private Vector3 moveDirection = Vector3.zero;
    private Vector3 slopeOnForce = Vector3.zero;

    //Components
    public GameObject playerObject;
    public Animator anim;
    CameraController cameraController;
    CharacterController characterController;
    GroundChecker groundChecker;

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
        positionOfLastJump = playerObject.transform.position;
    }

    void Update()
    {

        lastSliding = sliding;
        setGroundedMovementStates();
        slopeOnForce = Vector3.zero;

        //if (characterController.isGrounded) Debug.Log("Grounded");
        //Debug.Log(characterController.isGrounded ? "GROUNDED" : "NOT GROUNDED");

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
            jumpToAir = false;
            positionOfLastJump = playerObject.transform.position; //set to player location for cases where character walks off cliff

            checkIfStartSlide(); //setsMoveDirection, do not touch for duration of slide as slideDecay modifies it

            if (sliding) {
                slideDecay(); 

                //if jump is buffered or key is pressed after slide lock
                if ( (Input.GetButtonDown("Jump") || slideJumpBuffered) && !forcedToSlide) {
                    slideJump();
                    slideJumpBuffered = false;
                }

            }

            //if not sliding
            else
            {

                //adds slight downward push to ensure isGrounded is detected properly
                moveDirection = new Vector3(Input.GetAxis("Horizontal"), -0.01f, Input.GetAxis("Vertical"));
                moveDirection = alignMovementAndNormalize(moveDirection);

                //if running, multiply moveDirection by runSpeed, else multiply by walk speed
                moveDirection *= (isRunning ? runSpeed : walkSpeed);
                runningWhenJumped = isRunning;

                if (Input.GetButtonDown("Jump")) {
                    groundJump(); //sets jumpToAir and moveDirection Y speed
                }

            }

            if (!jumpToAir)
                moveDirection += groundChecker.applyForceIfGroundedOnSlope();
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
            moveDirection.y -= gravity/* * Time.deltaTime*/;

        }


        //https://forum.unity.com/threads/align-to-normal-and-look-down-slope.459499/

        Vector3 gravityVector = Vector3.down;
        //if not grounded and not 
        gravityVector *= !characterController.isGrounded && !jumpToAir ? gravity : groundedGravity;
        //gravityVector *= Time.deltaTime;

        Vector3 finalMovementVector = moveDirection/* + gravityVector*/;

        characterController.Move(finalMovementVector * Time.deltaTime);

        //jumpToAir = false;

        // GROUND CHECKING LOCK CALL
        if (sliding)
            groundChecker.lockRaycastLocations();
        else
            groundChecker.unlockRaycastLocations();
        //GROUND CHECKING LOCK CALL END

        //Adjust Camera for current action
        //solution?: check if lastSliding == true in airJump, if so raise camera from slide
        if (lastSliding && !sliding && !forcedToSlide) {
            StartCoroutine(cameraController.raiseCameraFromSlide(timeToGetUpFromSlide, Time.time));
        }

        updateAnimator();

    }

    private void checkIfStartSlide() {

        //can only slide if moving forward and currently running.
        if (isRunning && Input.GetButton("Crouch") && Input.GetAxisRaw("Vertical") > 0 && !sliding) {
            //Debug.Log("Begin Sliding");
            sliding = true;
            timeSlideStart = Time.time;
            StartCoroutine(forceSlideForTime(timeForcedToSlide));
            StartCoroutine(cameraController.dipCameraForSlide(timeForcedToSlide, Time.time));
            startSlide();
        }

    }

    private void startSlide() {

        moveDirection = new Vector3(Input.GetAxisRaw("Horizontal"), -0.01f, Input.GetAxisRaw("Vertical"));
        moveDirection = alignMovementAndNormalize(moveDirection);

        moveDirection.x *= (runSpeed * slideMultiplier);
        moveDirection.z *= (runSpeed * slideMultiplier);
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
        //don't decay slide if ground angle between -5 & -20
        if (groundChecker.groundSlopeAngle <= -5 && groundChecker.groundSlopeAngle >= -20)
            return;

        //add speed depending on angle <-20
        if (groundChecker.groundSlopeAngle < -20) {
            //TO DO
            return;
        }

        //Uphill angles
        //decay if slope is >= 10 & <= 20
        if (groundChecker.groundSlopeAngle >= 10 && groundChecker.groundSlopeAngle <= 20)
        {
            forcedToSlide = false;
            decayAmount *= 2;
        }

        if (groundChecker.groundSlopeAngle > 20) {
            forcedToSlide = false;
            decayAmount *= 2;
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
        //moveDirection.y = jumpSpeed;
        positionOfLastJump = playerObject.transform.position;
        moveDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        moveDirection = alignMovementAndNormalize(moveDirection);

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
        moveDirection = alignMovementAndNormalize(moveDirection);

        moveDirection *= (runningWhenJumped ? runSpeed : walkSpeed);
        moveDirection.y = jumpSpeed * 0.75f; //slightly lower second jump

        //reset bool for next jump
        runningWhenJumped = false;
    }

    private Vector3 alignMovementAndNormalize(Vector3 moveDirection) {

        Vector3 newDirection = transform.TransformDirection(moveDirection);

        if (newDirection.magnitude > 1) {
            newDirection.Normalize();
        }

        return newDirection;

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

    private void adjustVignette() {

        if (isRunning)
            cameraController.setSprintVignette();

        else
            cameraController.setWalkVignette();
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
