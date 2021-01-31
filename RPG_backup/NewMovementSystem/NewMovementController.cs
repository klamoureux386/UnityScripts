using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MethodExtensions;

public class NewMovementController : MonoBehaviour
{
    CharacterController charController;
    CameraController cameraController;
    GroundChecker groundChecker;

    //Input Variables
    private float inputHorizontal = 0;      //X movement
    private float inputVertical = 0;        //Z movement
    private bool inputJump = false;         //jump

    //Extra slide stuff
    private Vector3 slideSpeedStart = Vector3.zero;
    private bool lastSliding = false;
    private Vector3 originalSlideSpeed = Vector3.zero;

    //Extra aerial stuff
    private float timeGroundJumpStart = 0;

    //Debugging Variables
    [SerializeField] private Vector3 surfaceNormal = Vector3.zero;
    [SerializeField] private Vector3 moveDirection = Vector3.down;

    private void Update() {
        storePlayerInputs();
    }

    private void Awake() {

        charController = GetComponent<CharacterController>();
        cameraController = GetComponent<CameraController>();
        groundChecker = GetComponent<GroundChecker>();

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void performCharacterMovement() {

        surfaceNormal = groundChecker.surfaceNormal; //Debug
        lastSliding = GroundedStates.sliding;

        AerialStates.jumpingThisFrame = false;
        GroundedStates.setGroundedMovementStates(charController);

        if (groundChecker.customIsGrounded && moveDirection.y <= 0 && groundChecker.groundSlopeAngle <= -2 && groundChecker.groundSlopeAngle < charController.slopeLimit) {
            moveDirection.y = MoveStats.forceToLockToSlope;
            GroundedStates.lockToSlope = true;
        }
        else {
            GroundedStates.lockToSlope = false;
        }

        //if grounded
        if (GroundedStates.grounded) {
            performGroundedMovement();
        }
        else
            performAerialMovement();

        //Adjust Camera for current action
        //solution?: check if lastSliding == true in airJump, if so raise camera from slide
        if (lastSliding && !GroundedStates.sliding && !GroundedStates.forcedToSlide) {
            StartCoroutine(cameraController.raiseCameraFromSlide(MoveStats.timeToGetUpFromSlide, Time.time));
        }

        //Debug.Log(moveDirection);
        charController.Move(moveDirection * Time.deltaTime);
        
    }

    //[GROUNDED MOVEMENT FUNCTIONS]

    private void performGroundedMovement() {

        //Check if we need to bob camera if last in air before reseting aerial states

        AerialStates.reset(); //Reset Aerial States on Grounding

        checkIfNeedToLockToSlope();

        //to do: maybe make this the normalized input idk tho
        bool movingForward = inputVertical > 0 ? true : false; //if W, moving forward else not moving forward

        //Check if we need to start sliding
        if (GroundedStates.isRunning && GroundedStates.crouching && movingForward && !GroundedStates.sliding) {
            startSlide();
            StartCoroutine(forceSlideForTime(MoveStats.timeForcedToSlide));
            StartCoroutine(cameraController.dipCameraForSlide(MoveStats.timeForcedToSlide, Time.time));
        }

        //if sliding
        else if (GroundedStates.sliding) {
            adjustSlideMovement();
        }

        else {
            walkOrRunMovement();

            if (inputJump)
                groundJump();
        }

    }

    private void checkIfNeedToLockToSlope() {

        if (groundChecker.customIsGrounded && moveDirection.y <= 0 && groundChecker.groundSlopeAngle <= -3 && groundChecker.groundSlopeAngle < charController.slopeLimit) {
            moveDirection.y = MoveStats.forceToLockToSlope;
            GroundedStates.lockToSlope = true;
        }
        else
            GroundedStates.lockToSlope = false;
    }

    private void startSlide() {

        GroundedStates.sliding = true;
        MoveStats.timeSlideStart = Time.time;

        slideSpeedStart = alignMovementAndNormalize(new Vector3(inputHorizontal, MoveStats.groundedGravity, inputVertical));

        if (GroundedStates.lockToSlope)
            slideSpeedStart.y = MoveStats.forceToLockToSlope;

        slideSpeedStart.x *= (MoveStats.runSpeed * MoveStats.slideMultiplier);
        slideSpeedStart.z *= (MoveStats.runSpeed * MoveStats.slideMultiplier);

        originalSlideSpeed = slideSpeedStart;
        moveDirection = slideSpeedStart;

    }

    private void walkOrRunMovement() {

        Vector3 targetSpeed = alignMovementAndNormalize(new Vector3(inputHorizontal, MoveStats.groundedGravity, inputVertical));

        if (GroundedStates.lockToSlope)
            targetSpeed.y = MoveStats.forceToLockToSlope;

        //if running, multiply moveDirection by runSpeed, else multiply by walk speed
        targetSpeed.x *= (GroundedStates.isRunning ? MoveStats.runSpeed : MoveStats.walkSpeed);
        targetSpeed.z *= (GroundedStates.isRunning ? MoveStats.runSpeed : MoveStats.walkSpeed);

        moveDirection = targetSpeed;

    }

    //TO DO: DIVIDE SLIDE MOMENTUM INTO TWO PARTS
    //PART 1: UNDERLYING SLIDE SPEED THAT DECAYS OR INCREASES BASED ON AXIS NORMAL
    //PART 2: AMOUNT TO MULTIPLY THAT VALUE BY BASED ON SLOPE STEEPNESS
    private void adjustSlideMovement() {

        //Vector3 startSlideSpeed = originalSlideSpeed;

        //Vector3 rotatingSlideMovement = moveDirection;
        //Vector3 rotatingSlideMovement = originalSlideSpeed;

        if (groundChecker.groundSlopeAngle <= -5 && groundChecker.groundSlopeAngle >= -25) {
            originalSlideSpeed = rotateSlideTowardsNormal();
        }

        originalSlideSpeed.x = Mathf.Lerp(originalSlideSpeed.x, originalSlideSpeed.x, 0.02f);
        originalSlideSpeed.z = Mathf.Lerp(originalSlideSpeed.z, originalSlideSpeed.z, 0.02f);

        Vector3 slopeAcceleration = getSlopeAcceleration();

        moveDirection = originalSlideSpeed + slopeAcceleration;

    }

    private Vector3 getSlopeAcceleration() {

        Vector3 slopeAcceleration = Vector3.zero;

        float denominator = MoveStats.runSpeed * MoveStats.slideMultiplier; //33.6

        float xRatio = Mathf.Abs(MoveStats.moveSpeedOnSlideStart.x) / denominator;
        float zRatio = Mathf.Abs(MoveStats.moveSpeedOnSlideStart.z) / denominator;

        float decayAmountX = xRatio * 0.15f;
        float decayAmountZ = zRatio * 0.15f;
        bool xDecayed = false;
        bool zDecayed = false;

        //Downhill angles
        //don't decay slide if ground angle between -5 & -20
        if (groundChecker.groundSlopeAngle <= -5 && groundChecker.groundSlopeAngle >= -10) {
            return Vector3.zero;
        }

        //add slight speed
        if (groundChecker.groundSlopeAngle < -10 /*&& groundChecker.groundSlopeAngle >= -20*/) {
            //Debug.Log("adding speed");
            //To do: limit speed downhill to a certain maximum
            /*[TEMP]*/
            float speedDiv = 2.0f; //tune speed increase

            slopeAcceleration.x += (moveDirection.x > 0) ? decayAmountX / speedDiv : -decayAmountX / speedDiv;
            slopeAcceleration.z += (moveDirection.z > 0) ? decayAmountZ / speedDiv : -decayAmountZ / speedDiv;
        }

        return slopeAcceleration;

    }

    private Vector3 rotateSlideTowardsNormal() {

        Debug.LogFormat("X Sur: " + groundChecker.surfaceNormal.x + "\nZ Sur: " + groundChecker.surfaceNormal.z);

        float slideSpeed = MoveStats.runSpeed * MoveStats.slideMultiplier; //Default 42

        Vector3 surfaceNormalVector = new Vector3(groundChecker.surfaceNormal.x, 0, groundChecker.surfaceNormal.z);

        surfaceNormalVector = surfaceNormalVector.normalized;
        Debug.LogFormat("X SurNormal: " + surfaceNormalVector.x + "\nZ SurNormal: " + surfaceNormalVector.z);
        //surfaceNormalVector = surfaceNormalVector.abs();

        Debug.Log("Surface Norm Vector: " + surfaceNormalVector);

        float targetXSpeed = 0;
        float targetZSpeed = 0;

        float combinedNormals = surfaceNormalVector.x + surfaceNormalVector.z;
        if (surfaceNormalVector.x == 0 && surfaceNormalVector.z == 0)
            combinedNormals = 1;

        targetXSpeed = slideSpeed * surfaceNormalVector.x;
        targetZSpeed = slideSpeed * surfaceNormalVector.z;

        Debug.LogFormat("TARGET X SPEED: " + targetXSpeed + "\nTARGET Z SPEED: " + targetZSpeed);

        return new Vector3(targetXSpeed, 0, targetZSpeed);

    }

    /*
    //TO DO: DIVIDE SLIDE MOMENTUM INTO TWO PARTS
    //PART 1: UNDERLYING SLIDE SPEED THAT DECAYS OR INCREASES BASED ON AXIS NORMAL
    //PART 2: AMOUNT TO MULTIPLY THAT VALUE BY BASED ON SLOPE STEEPNESS
    private void adjustSlideMovement() {

        float slideSpeed = MoveStats.runSpeed * MoveStats.slideMultiplier; //Default 42

        Vector3 surfaceNormalVector = new Vector3(surfaceNormal.x, 0, surfaceNormal.z);
        surfaceNormalVector.Normalize();
        surfaceNormalVector = surfaceNormalVector.abs();

        float targetXSpeed = 0;
        float targetZSpeed = 0;

        float combinedNormals = surfaceNormal.x + surfaceNormal.z;
        if (surfaceNormal.x == 0 && surfaceNormal.z == 0)
            combinedNormals = 1;

        targetXSpeed = slideSpeed * (surfaceNormal.x / combinedNormals);
        targetZSpeed = slideSpeed * (surfaceNormal.z / combinedNormals);

        targetXSpeed /= surfaceNormalVector.x;
        targetZSpeed /= surfaceNormalVector.z;

        Debug.LogFormat("TARGET X SPEED: {0} \nTARGET Z SPEED: {1}", targetXSpeed, targetZSpeed);

        if (groundChecker.groundSlopeAngle <= -5 && groundChecker.groundSlopeAngle >= -25) {

            transform.TransformDirection(moveDirection);
            return;
        }
    }*/

    private IEnumerator forceSlideForTime(float seconds) {

        GroundedStates.forcedToSlide = true;

        yield return new WaitForSeconds(seconds);

        GroundedStates.forcedToSlide = false;
        GroundedStates.crouching = Input.GetButton("Crouch");

        yield return null;
    }

    //[AERIAL MOVEMENT FUNCTIONS]

    private void performAerialMovement() {

        //if first frame in the air
        if (!AerialStates.lastInAir) {

            //Handles cases where we slide off of a slope so gravity doesnt start at -50
            if (moveDirection.y <= MoveStats.forceToLockToSlope - 1)
                moveDirection.y = 0;

            AerialStates.airborneStartingSpeed = moveDirection;

        }

        AerialStates.lastInAir = true;

        //if jumping in air
        if (inputJump && !AerialStates.airJumpUsed && Time.time - timeGroundJumpStart >= MoveStats.timeBeforeAirJump) {
            airJump();
        }

        moveDirection.y -= MoveStats.gravity * Time.deltaTime;

    }

    private void groundJump() {

        timeGroundJumpStart = Time.time;

        //sacrifice a bit of height for forward momentum while sprinting
        if (GroundedStates.isRunning) {
            AerialStates.runningWhenJumped = true;
            moveDirection.y = MoveStats.jumpSpeed * 0.85f;
            moveDirection.x *= 1.05f;
            moveDirection.z *= 1.05f;
        }

        //more height, less momentum
        else {
            moveDirection.y = MoveStats.jumpSpeed;
        }

        //moveDirection.y = MoveStats.jumpSpeed;
        AerialStates.jumpingThisFrame = true;

    }

    private void airJump() {

        AerialStates.airJumpUsed = true;
        GroundedStates.sliding = false;

        Vector3 newDirection = alignMovementAndNormalize(new Vector3(inputHorizontal, 0, inputVertical));

        newDirection *= AerialStates.runningWhenJumped ? MoveStats.runSpeed : MoveStats.walkSpeed;
        newDirection.y = MoveStats.jumpSpeed * 0.9f; //slightly lower second jump

        moveDirection = newDirection;

    }

    //Helper Functions

    private Vector3 alignMovementAndNormalize(Vector3 moveDirection) {

        Vector3 newDirection = transform.TransformDirection(moveDirection);

        if (newDirection.magnitude > 1) {
            newDirection.Normalize();
        }

        return newDirection;

    }

    private void storePlayerInputs() {
        inputHorizontal = Input.GetAxisRaw("Horizontal");
        inputVertical = Input.GetAxisRaw("Vertical");
        inputJump = Input.GetButton("Jump");
    }

}
