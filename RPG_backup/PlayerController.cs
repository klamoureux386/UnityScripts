using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    //Components
    public GameObject playerObject;
    //public Animator anim;
    CameraController cameraController;
    CharacterController charController;
    GroundChecker groundChecker;

    //Input Variables
    //[Header ("Player Inputs")]
    /*[SerializeField]*/private float inputHorizontal = 0;     //X movement
    /*[SerializeField]*/private float inputVertical = 0;       //Z movement

    //Movement Variables
    [Header ("Movement Variables (read-only)")]
    [SerializeField] private float walkSpeed = 16.0f;       //Default 16
    [SerializeField] private float runSpeed = 24.0f;        //Default 24
    [SerializeField] private float jumpSpeed = 18.0f;       //Default 18
    [SerializeField] private float gravity = 30.0f;          //Default 30
    [SerializeField] private float groundedGravity = 0.01f; //Default 0.01

    //Grounded Movement States
    [Header ("Grounded States")]
    [SerializeField] private bool crouching = false;
    [SerializeField] private bool sliding = false;
    [SerializeField] private bool isRunning = false;
    [SerializeField] private bool forcedToSlide = false;
    [SerializeField] private bool slideJumpBuffered = false;

    //Aerial Movement States
    [Header ("Aerial States")]
    [SerializeField] private bool airJumpUsed = false;
    [SerializeField] private bool runningWhenJumped = false;
    [SerializeField] private bool lastInAir = false;
    [SerializeField] private bool jumpingThisFrame = false;

    //Slide Variables
    private float slideMultiplier = 1.4f;       //Default 1.25-1.4
    private float timeSlideStart = 0;
        //Slide Animation Variables
    private float timeForcedToSlide = 0.25f;    //Should be same length as slide animation duration
    private float timeToGetUpFromSlide = 0.10f; //Should be same length as slide get-up animation

    //Last Movement Variables
    private Vector3 positionOfLastJump;
    private bool lastSliding = false;

    //Forces on Player
    [SerializeField] private Vector3 moveDirection = Vector3.zero;
    private Vector3 slopeOnForce = Vector3.zero;


    void Awake()
    {
        //GetComponents
        charController = GetComponent<CharacterController>();
        cameraController = GetComponent<CameraController>();
        groundChecker = GetComponent<GroundChecker>();

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        //Initialization
        positionOfLastJump = playerObject.transform.position;
    }


    void Update()
    {
        performCharacterMovement();  
    }

    public void performCharacterMovement() {

        lastSliding = sliding;
        setGroundedMovementStates();
        storePlayerInputs();

        //move vector calculated differently on ground vs. in-air
        if (charController.isGrounded)
            groundedMovement();
        else
            aerialMovement();

        //Ground Checker Sliding Lock
        if (sliding)
            groundChecker.lockRaycastLocations();
        else
            groundChecker.unlockRaycastLocations();

        //Adjust Camera for current action
        //solution?: check if lastSliding == true in airJump, if so raise camera from slide
        if (lastSliding && !sliding && !forcedToSlide) {
            StartCoroutine(cameraController.raiseCameraFromSlide(timeToGetUpFromSlide, Time.time));
        }

        charController.Move(moveDirection * Time.deltaTime);

        //updateAnimator();
    }

    //Movement Functions

    private void groundedMovement() {

        //if landing, check if we need to dip camera down
        if (lastInAir)
            cameraController.checkBobCamera(positionOfLastJump.y, false);

        resetAerialStates(); //Reset air states since we're grounded

        checkIfStartSlide(); //setsMoveDirection, do not touch for duration of slide as slideDecay modifies it

        if (sliding) {
            slideDecay();

            //if jump is buffered or key is pressed after slide lock
            if ((Input.GetButtonDown("Jump") || slideJumpBuffered) && !forcedToSlide) {
                slideJump();
                slideJumpBuffered = false;
            }

        }

        //if not sliding
        else {

            //adds slight downward push to ensure isGrounded is detected properly
            moveDirection = new Vector3(inputHorizontal, -0.01f, inputVertical);
            moveDirection = alignMovementAndNormalize(moveDirection);

            //if running, multiply moveDirection by runSpeed, else multiply by walk speed
            moveDirection *= (isRunning ? runSpeed : walkSpeed);

            if (Input.GetButtonDown("Jump")) {
                groundJump(); //sets jumpToAir and moveDirection Y speed
            }

        }

    }

    private void aerialMovement() {

        lastInAir = true;

        //if jumping in air
        if (Input.GetButtonDown("Jump") && !airJumpUsed) {
            airJump();
        }

        //For drifting in air
        else {

            //To do: ?
        }

        //apply gravity if in air
        moveDirection.y -= gravity * Time.deltaTime;
    }

    //Jump Functions

    private void groundJump() {
        moveDirection.y = jumpSpeed;
        positionOfLastJump = playerObject.transform.position;

        if (isRunning)
            runningWhenJumped = true;

    }

    private void airJump() {

        airJumpUsed = true;
        sliding = false;
        positionOfLastJump = playerObject.transform.position;
        moveDirection = new Vector3(inputHorizontal, 0, inputVertical);
        moveDirection = alignMovementAndNormalize(moveDirection);

        moveDirection *= (runningWhenJumped ? runSpeed : walkSpeed);
        moveDirection.y = jumpSpeed * 0.75f; //slightly lower second jump
    }

    private void slideJump() {

        positionOfLastJump = playerObject.transform.position;
        moveDirection = new Vector3(inputHorizontal, 0, inputVertical);
        moveDirection = alignMovementAndNormalize(moveDirection);

        moveDirection.x *= (runSpeed * slideMultiplier);
        moveDirection.z *= (runSpeed * slideMultiplier);
        moveDirection.y = jumpSpeed;

        runningWhenJumped = true;

    }

    //Sliding Functions

    private void checkIfStartSlide() {

        //can only slide if moving forward and currently running.
        if (isRunning && Input.GetButton("Crouch") && inputVertical > 0 && !sliding) {
            //Debug.Log("Begin Sliding");
            sliding = true;
            timeSlideStart = Time.time;
            StartCoroutine(forceSlideForTime(timeForcedToSlide));
            StartCoroutine(cameraController.dipCameraForSlide(timeForcedToSlide, Time.time));
            startSlide();
        }

    }

    private void startSlide() {

        moveDirection = new Vector3(inputHorizontal, -0.01f, inputVertical);
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
        if (groundChecker.groundSlopeAngle >= 10 && groundChecker.groundSlopeAngle <= 20) {
            forcedToSlide = false;
            decayAmount *= 2;
        }

        if (groundChecker.groundSlopeAngle > 20) {
            forcedToSlide = false;
            decayAmount *= 2;
        }

        //Vector3 slideDecayVector = new Vector3(decayAmount, 0, decayAmount);

        //X Decay
        if (moveDirection.x > 0) {
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
        if (moveDirection.z > 0) {
            if (moveDirection.z <= decayAmount)
                zDecayed = true;

            else
                moveDirection.z -= decayAmount;
        }

        else if (moveDirection.z < 0) {
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

    private IEnumerator forceSlideForTime(float seconds) {

        forcedToSlide = true;
        //Debug.Log("Forced to slide");

        yield return new WaitForSeconds(seconds);

        forcedToSlide = false;
        crouching = Input.GetButton("Crouch");

        //Debug.Log("Not forced to slide");

        yield return null;
    }

    //State Set Functions

    /* Sets the following based on player input:
     * isRunning
     * crouching
     * sliding (can only set as false. true setting done in slide function)
     * slideJumpBuffered
     */

    private void setGroundedMovementStates() {

        if (forcedToSlide && Input.GetButton("Jump")) {
            slideJumpBuffered = true;
        }

        if (!crouching && Input.GetButton("Shift")) {
            isRunning = true;
        }

        //set running to false unless last frame char was in air (last in air exception for cases where you want to slide on landing)
        else if (!sliding && charController.isGrounded && !lastInAir) {
            isRunning = false;
        }

        if (Input.GetButton("Crouch") || forcedToSlide) {
            crouching = true;
        }

        else {
            crouching = false;
            sliding = false;
        }
    }

    //State Reset Functions

    private void resetAerialStates() {

        airJumpUsed = false;
        runningWhenJumped = false;
        lastInAir = false;
        jumpingThisFrame = false;
        positionOfLastJump = playerObject.transform.position; //set to player location for cases where character walks off cliff
    }

    //Storage Functions

    private void storePlayerInputs() {

        inputHorizontal = Input.GetAxisRaw("Horizontal");
        inputVertical = Input.GetAxisRaw("Vertical");
    }

    //Helper Functions

    private Vector3 alignMovementAndNormalize(Vector3 moveDirection) {

        Vector3 newDirection = transform.TransformDirection(moveDirection);

        if (newDirection.magnitude > 1) {
            newDirection.Normalize();
        }

        return newDirection;

    }
}
