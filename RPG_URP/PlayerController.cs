using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

//[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(GroundChecker))]
public class PlayerController : MonoBehaviour
{

    //CharacterController charController;
    public CharacterControllerManager ccManager;
    public Animator cinemachineAnimator;
    public Animator playerAnimatior;
    GroundChecker groundChecker;

    public bool disableAnimation = false;

    private float crouchSpeed = 5.0f;
    private float runSpeed = 10.0f;
    private float sprintSpeed = 20.0f;
    private float rotationSpeed = 30.0f;
    private float jumpHeight = 4f;
    private float gravityValue = -30f; //Default: -20
    Vector2 moveInput = Vector2.zero;

    //State Vars
    public bool lockedOn = false;
    public bool sprinting = false;
    public bool crouching = false;
    public bool sliding = false;

    private bool lastInAir = false;

    //Sliding Vars
    private float slideMultiplier = 1.2f;
    private float timeSlideStarted;
    private float timeForcedToSlide = 1.0f; //Time forced to slide in seconds
    public bool cancelSlide = false;

    private float sqrMagnitudeToMaintainSpeed = 0.03015259f; //sqrMagnitude of slopeNormal of 10 degree surface without Y

    //Rolling Vars
    public bool rolling = false;

    //Velocities
    [SerializeField] private Vector3 moveVelocity = Vector3.zero;
    [SerializeField] private Vector3 playerVelocity = Vector3.zero; //Primarily used for gravity, may also account for propulsion effects at some point
    [SerializeField] private Vector3 slideVelocity = Vector3.zero; //X and Z movement taken from input at time of slide, Rotated to match slope normal (will generate a Y value)
    [SerializeField] private Vector3 slideVelocityAtStart = Vector3.zero;

    private Transform mainCameraTransform;

    private void Awake()
    {
        //charController = GetComponent<CharacterController>();
        groundChecker = GetComponent<GroundChecker>();
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Start()
    {
        mainCameraTransform = Camera.main.transform;
    }

    public void OnMoveInput(float horizontal, float vertical) {

        moveInput.x = vertical; //forward on thumbstick
        moveInput.y = horizontal; //sideways on thumbstick
        //Debug.Log($"Player Controller Move Input: {vertical}, {horizontal}");

    }

    public void OnLockOnInput(bool active)
    {
        
        lockedOn = !lockedOn;
        Debug.Log("Toggling lock on: " + lockedOn);

        if (lockedOn)
            cinemachineAnimator.Play("LockedOnCamera");
        else
            cinemachineAnimator.Play("StandardCamera");

    }

    public void OnJumpInput(bool active)
    {
        //must be grounded to jump
        if (ccManager.customGrounded)
        {
            jump();
            //Note: don't do this outside of for dev purposes, make state based
            playerAnimatior.Play("Jump");
        }

    }

    //Toggle for dev
    public void OnSprintInput(bool active)
    {
        //must be grounded to sprint
        if (ccManager.customGrounded && !sprinting)
        {
            sprinting = active;
        }
        else if (sprinting)
            sprinting = false;
    }

    public void OnEvadeInput(bool active) {

        if (active)
        {

            if (crouching) {
                crouching = false;
                StartCoroutine(ccManager.growCharControllerFromCrouching());
            }

            else if (moveInput == Vector2.zero && !sliding && !crouching) {
                crouch();
            }

            //if sprinting on ground and not already sliding
            else if (sprinting && !sliding && ccManager.customGrounded)
                startSlide();

            //if not srinting, not already rolling, and grounded: roll
            else if (!sprinting && !rolling && ccManager.customGrounded) {
                startRoll();
            }

        }

        else {

            //If not forced to slide
            if (Time.time - timeSlideStarted > timeForcedToSlide && !cancelSlide)
            {
                //if time forced to slide is up
                sliding = false;
                slideVelocity = Vector2.zero;

                StartCoroutine(ccManager.growCharControllerFromSliding());

            }
            //else, set flag to cancel slide asap
            else 
            {
                cancelSlide = true;
            }

        }

    }

    private void crouch() {

        //shrink to slide size for now
        crouching = true;
        StartCoroutine(ccManager.shrinkCharControllerCrouching());
    }

    private void jump() {

        playerVelocity.y = Mathf.Sqrt(jumpHeight * -3.0f * gravityValue);

    }

    private void startSlide() {

        sliding = true;
        timeSlideStarted = Time.time;

        //store input direction at time of slide (multiplied by sprint speed) in slideVelocity and scale it
        slideVelocity = (mainCameraTransform.forward * moveInput.x + mainCameraTransform.right * moveInput.y).normalized;
        slideVelocity.y = 0; //ignore y direction of camera

        slideVelocityAtStart = slideVelocity * sprintSpeed;

        //slideVelocity = rotateDirectionOntoSlope(slideVelocity, groundChecker.surfaceNormal);

        //Take current moveDirection speed (x and z) and scale it
        //store that in slideVelocity
        //(mostly) ignore player moveInput while sliding
        //maintain, decay, or increase speed of playerVelocity (sliding speed) depending on surface angle
        //Wants: player to be able to release left stick and let slide dictate movement, allows player to use right stick

        //HALF CONTROLLER HEIGHT WITH SLIDE, CAN LERP 
        StartCoroutine(ccManager.shrinkCharControllerSliding());

        //Debug.Log("Slide started at: " + Time.time);
    }

    //Adjusts slide velocity to move parallel with slope
    //?Split into two to test non-normalization
    private Vector3 rotateDirectionOntoSlopeSlide(Vector3 slideDirection, Vector3 surfaceNormal) {

        //float originalMagnitude = slideDirection.magnitude;
        //Debug.Log("original magnitude: " + slideDirection);
        Vector3 slopeMoveDirection = Vector3.ProjectOnPlane(slideDirection, surfaceNormal);
        //Debug.Log("projected magnitude: " + slopeMoveDirection);

        return slopeMoveDirection.normalized;
        //return slopeMoveDirection.normalized;
    }

    //Adjusts move velocity to move parallel with slope
    private Vector3 rotateDirectionOntoSlopeMove(Vector3 moveDirection, Vector3 surfaceNormal)
    {

        Vector3 slopeMoveDirection = Vector3.ProjectOnPlane(moveDirection, surfaceNormal);

        return slopeMoveDirection.normalized;
    }


    /* Slide Decay Multipliers
     * Flat Ground (between -3 & 3 degrees)
     * 
     */
    private void handleSlideDecay() {

        //float slopeAngle = groundChecker.groundSlopeAngle;

        //Debug.Log("Matching slide to slope...");
        if (groundChecker.surfaceNormal != Vector3.up) {
            slideVelocity = determineIfRotateOntoSlopeSlide(slideVelocity, groundChecker.surfaceNormal);
        }


        //!TO MAKE THINGS GOOD: BE PICKY ABOUT WHEN TO CALL THIS AND HOW FAST TO ROTATE///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //rotate towards slide normal after sliding for .25s
        if (Time.time - timeSlideStarted > 0.25f) {
            //slideVelocity = rotatePlayerTowardsSlopeNormal(slideVelocity, groundChecker.surfaceNormal);
            slideVelocity = rotatePlayerTowardsSlopeNormal(slideVelocity, groundChecker.surfaceNormal);
        }


        /*slideVelocity *= 0.998f;*/
    
    }

    //https://answers.unity.com/questions/46770/rotate-a-vector3-direction.html

    //"Take the cross product of the two vectors and use the result vector as the axis to rotate around. Then perform an axis-angle rotation."
    private Vector3 rotatePlayerTowardsSlopeNormal(Vector3 slideDirection, Vector3 surfaceNormal) {

        Vector3 slopeNormalWithoutY = surfaceNormal;
        //But what about with y? NOTE: DON'T
        slopeNormalWithoutY.y = 0;


        //!TO DO: DETERMINE ROTATION SPEED BASED ON SQR MAGNITUDE OF SURFACE NORMAL///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //SHARPER SLOPES ROTATE FASTER

        float rotSpeed = 0.5f; //average 0.5

        //?Attempt 2: Try, if groundSlopeAngle is positive, turning speed is much higher. Messy but getting closer conceptually
        //TODO: Turn divisor values into uphill/downhill

        float rotDivisor = 30;

        //turn much harder on upward slopes
        if (groundChecker.groundSlopeAngle > 0) {
            rotDivisor = 10f;
        }

        float calculatedRotSpeed = Mathf.Abs(groundChecker.groundSlopeAngle) / rotDivisor; //slope of 45 will result in old average rot speed

        //minimum rot speed when transitioning from uphill to downhill
        if (calculatedRotSpeed < 1.0f && rotDivisor == 10) //slowest rotation of 0.1
            calculatedRotSpeed = 1.0f;

        //minimum rotspeed once we transition to downhill
        if (calculatedRotSpeed < 0.75f && rotDivisor == 30)
            calculatedRotSpeed = 0.75f;


        //To do: determine rotation speed based on angle sliding at, slope angle of surface

        //to do: if angle difference between current slideDirection and targetCorrectedSlideDirection is < epsilon, maintain slide direction
        //this epsilon will be used to fine tune the slight jiggle when sliding down uneven surfaces

        //to get started: try just doing it on downhill slopes
        //if (groundChecker.groundSlopeAngle < 0)
        //{

        //!IF ANGLE BETWEEN TRANSFORM FORWARD AND SLOPE N WITHOUT Y IS < EPSILON, ROTATE VERY SLOWLY? WILL REMOVE JITTER?
        //!If angle left ~< 7 degrees, rotate slow. Also if groundAngle < 10 degrees, dont rotate at all just lose speed

        float angleLeftToRotate = Vector3.Angle(transform.forward, slopeNormalWithoutY);

        //Debug.Log("ANGLE BETWEEN CURRENT AND TARGET ROTATION: " + angleLeftToRotate);


        //Values to adjust jitter on slight rotations
        if (angleLeftToRotate < 8f)
            calculatedRotSpeed = 0.1f;
        if (angleLeftToRotate < 4f)
            calculatedRotSpeed = 0.05f;
        if (angleLeftToRotate < 2f)
            calculatedRotSpeed = 0.025f;
        if (angleLeftToRotate < 1f)
            calculatedRotSpeed = 0.01f;

        if (groundChecker.normalRelativeToUp <= 12)
            return slideDirection;

        /*if (Mathf.Abs(groundChecker.groundSlopeAngle) < 2 && angleLeftToRotate > 70 && angleLeftToRotate < 85)
            calculatedRotSpeed = 0f;*/

        //dont rotate if aiming uphill 
        /*if (groundChecker.groundSlopeAngle < 10 && groundChecker.groundSlopeAngle > 0 && angleLeftToRotate > 90)
            calculatedRotSpeed = 0f;*/

        transform.rotation = Quaternion.LookRotation(Vector3.RotateTowards(transform.forward, slopeNormalWithoutY, calculatedRotSpeed * Time.deltaTime, 0.0f));

        //?Attempt to get rid of normalization in slideDirection for varying speeds
            //?return Vector3.RotateTowards(slideDirection.normalized, slopeNormalWithoutY, calculatedRotSpeed * Time.deltaTime, 0.0f);
        return Vector3.RotateTowards(slideDirection, slopeNormalWithoutY, calculatedRotSpeed * Time.deltaTime, 0.0f);
        //}

        //return slideDirection;

    }


    private Vector3 determineIfRotateOntoSlopeSlide(Vector3 slideDirection, Vector3 surfaceNormal) {

        //to do: come up with some kind of check that will allow us to slide off of sharp edge changes
        //without stopping our ability to stick to sharp slopes
        if (groundChecker.backHit && groundChecker.frontHit)
        {
            slideDirection = rotateDirectionOntoSlopeSlide(slideDirection, groundChecker.surfaceNormal);
        }

        return slideDirection;

    }

    private Vector3 determineIfRotateOntoSlopeMove(Vector3 moveDirection, Vector3 surfaceNormal)
    {

        //to do: come up with some kind of check that will allow us to slide off of sharp edge changes
        //without stopping our ability to stick to sharp slopes
        if (groundChecker.backHit && groundChecker.frontHit)
        {
            moveDirection = rotateDirectionOntoSlopeMove(moveDirection, groundChecker.surfaceNormal);
        }

        return moveDirection.normalized;

    }



    private void startRoll() { 
        
        //Roll!
    }

    private void FixedUpdate()
    {

        //Rotate character to face camera direction
        Vector3 inputDirection = new Vector3(moveInput.y, 0, moveInput.x);
        Vector3 camDirection = mainCameraTransform.rotation * inputDirection;
        Vector3 targetDirection = new Vector3(camDirection.x, 0, camDirection.z);

        //Turn character to cam forward if not sliding and not standing still
        if (moveInput != Vector2.zero && !sliding)
        { //turn the character to face the direction of travel when there is input
            transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(targetDirection),
            Time.deltaTime * rotationSpeed
            );
        }

        if (!disableAnimation)
        {
            updateAnimations();
        }
    }

    void Update()
    {

        //if cancel slide flag set and not forced to slide
        if (cancelSlide && Time.time - timeSlideStarted > timeForcedToSlide && sliding) {
            sliding = false;
            slideVelocity = Vector2.zero;
            cancelSlide = false;
            StartCoroutine(ccManager.growCharControllerFromSliding());
            Debug.Log("Slide cancelled by flag");
        }

        float moveSpeed = (sprinting) ? sprintSpeed : runSpeed;

        if (crouching) { moveSpeed = crouchSpeed; }

        //NEED TO REDO HOW WE STICK TO GROUND

        //if downward momentum while grounded, reset Y momentum
        if (ccManager.customGrounded && playerVelocity.y < 0) {
            playerVelocity.y = -1.0f;

        }

        //Apply gravity force to player
        if (!ccManager.customGrounded)
        {
            playerVelocity.y += gravityValue * Time.deltaTime;
        }

        ////////////////

        ccManager.Move(playerVelocity * Time.deltaTime);

        //Attempt 2 - Same thing but moved down
        Vector3 moveDirection = mainCameraTransform.forward * moveInput.x + mainCameraTransform.right * moveInput.y;
        //getting rid of any extra y move direction
        moveDirection.y = 0;

        inputMovementDebug(moveDirection);

        //Adjust move direction onto slope angle for walking/running
        if (groundChecker.surfaceNormal != Vector3.up)
        {
            moveDirection = determineIfRotateOntoSlopeMove(moveDirection, groundChecker.surfaceNormal);
        }


        if (sliding)
        {
            handleSlideDecay();

            //!THIS WORKS, DONT TOUCH LOL
            //ccManager.Move(slideVelocity.normalized * sprintSpeed * Time.deltaTime);
            ccManager.Move(slideVelocity/*.normalized*/ * sprintSpeed * Time.deltaTime);

            //?ccManager.Move(slideVelocity * sprintSpeed * Time.deltaTime);

            //?non normalized:
            //ccManager.Move(slideVelocity * Time.deltaTime);
        }
        else
        {
            moveVelocity = moveDirection.normalized * moveSpeed;
            ccManager.Move(moveDirection.normalized * moveSpeed * Time.deltaTime);
        }

        slopeMovementDebug(moveDirection, groundChecker.surfaceNormal);
        slideMovementDebug(slideVelocity);

        targetSlopeRotationMidwayDebug(slideVelocity, groundChecker.surfaceNormal);
        //targetSlopeRotationCrossDebug(slideVelocity, groundChecker.surfaceNormal);
        slopeInfluenceDebug(groundChecker.surfaceNormal);

        lastInAir = ccManager.customGrounded;

    }

    private void inputMovementDebug(Vector3 moveDirection) {

        Debug.DrawLine(transform.position, transform.position + moveDirection.normalized, Color.red);
    }

    private void slopeMovementDebug(Vector3 moveDirection, Vector3 slopeNormal)
    {
        Vector3 slopeMoveDirection = Vector3.ProjectOnPlane(moveDirection, slopeNormal);

        Debug.DrawLine(transform.position, transform.position + slopeMoveDirection.normalized, Color.green);
    }

    //Length of cyan line indicates the influence the slope normal has on slide direction, direction of line is the slide normal without Y
    private void slopeInfluenceDebug(Vector3 slopeNormal) {


        Vector3 slopeNormalWithoutY = slopeNormal;
        slopeNormalWithoutY.y = 0;

        //Vector3 slopeInfluenceDirection = Vector3.ProjectOnPlane(slopeNormal, Vector3.right);

        Debug.DrawLine(transform.position, transform.position + slopeNormalWithoutY, Color.cyan);
        //Debug.Log("Distance of cyan line: " + Vector3.Distance(transform.position, transform.position + slopeNormalWithoutY));
        //Debug.Log("Magnitude of cyan line: " + Vector3.Magnitude(transform.position - (transform.position + slopeNormalWithoutY)));
        //Debug.Log("SqrMagnitude of cyan line: " + Vector3.SqrMagnitude(transform.position - (transform.position + slopeNormalWithoutY)));
    
    }

    private void slideMovementDebug(Vector3 slideVelocity) {

        Debug.DrawLine(transform.position, transform.position + slideVelocity.normalized, Color.yellow);
    }

    private void targetSlopeRotationMidwayDebug(Vector3 slideDirection, Vector3 slopeNormal) {

        Vector3 slopeNormalWithoutY = slopeNormal;
        slopeNormalWithoutY.y = 0;

        Vector3 midpointVector = (slideDirection.normalized + slopeNormalWithoutY).normalized;

        Debug.DrawLine(transform.position, transform.position + midpointVector, Color.magenta);
    
    }

    private void updateAnimations()
    {

        if (moveInput != Vector2.zero)
        {
            playerAnimatior.SetBool("isRunning", true);
        }
        else
        {
            playerAnimatior.SetBool("isRunning", false);
        }

        playerAnimatior.SetBool("isSprinting", sprinting);

        playerAnimatior.SetBool("isSliding", sliding);

        playerAnimatior.SetBool("isCrouching", crouching);

        playerAnimatior.SetBool("isGrounded", groundChecker.customIsGrounded);

        /*if (!groundChecker.customIsGrounded)
            playerAnimatior.Play("InAir");*/

    }


    //Unused Functions

    #region HOLD LOCK-ON
    /*
    //https://gamedevbeginner.com/input-in-unity-made-easy-complete-guide-to-the-new-system/
    //https://forum.unity.com/threads/gui-toggle-scripting.274402/ <-- This one is the answer
    public void OnLockOnInput(bool active) {

        if (active)
        {
            lockedOn = tryToLockOnEnemy();
        }
        else {

            if (lockedOn)
            {
                cinemachineAnimator.Play("StandardCamera");
            }
            lockedOn = false;
        }

        if (lockedOn) { Debug.Log("Locked on)"); }

    }

    private bool tryToLockOnEnemy() {

        //Logic check, maybe return the enemy locked onto as well

        if (!lockedOn)
        {
            Debug.Log("Trying to lock on...");
            cinemachineAnimator.Play("LockedOnCamera");
        }

        return true;
    }*/
    #endregion

    //Useless Notes & Old Code

    #region Code Graveyard
    //https://forum.unity.com/threads/moving-character-relative-to-camera.383086/
    /* ATTEMPT 1 - Character popping up when looking up 
    //if moving, move in direction of camera
    if (moveInput != Vector2.zero) {
        float targetAngle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg + mainCameraTransform.eulerAngles.y;
        Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }*/
    #endregion
}
