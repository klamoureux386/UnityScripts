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

    //Sliding Speeds
    /*private float maxSlopeSpeed = 40f;
    private float steepSlopeSpeed = 35f;
    private float medSteepSlopeSpeed = 30f;
    private float medSlopeSpeed = 25f;
    private float slightMedSlopeSpeed = 20f;
    private float slightSlopeSpeed = 15f;
    private float verySlightSlopeSpeed = 10f;
    private float noSlopeSpeed = 0f;*/

    //Sliding Vars
    private float slideMultiplier = 1.2f;
    private float timeSlideStarted;
    private float timeForcedToSlide = 1.0f; //Time forced to slide in seconds
    public bool cancelSlide = false;

    private float sqrMagnitudeToMaintainSpeed = 0.03015259f; //sqrMagnitude of slopeNormal of 10 degree surface without Y

    //Rolling Vars
    public bool rolling = false;

    //Velocities
    [SerializeField] private Vector3 playerVelocity = Vector3.zero; //Primarily used for gravity, may also account for propulsion effects at some point
    [SerializeField] private Vector3 slideVelocity = Vector3.zero; //X and Z movement taken from input at time of slide, Rotated to match slope normal (will generate a Y value)

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

        playerVelocity.y += Mathf.Sqrt(jumpHeight * -3.0f * gravityValue);

    }

    private void startSlide() {

        sliding = true;
        timeSlideStarted = Time.time;

        //store input direction at time of slide (multiplied by sprint speed) in slideVelocity and scale it
        slideVelocity = (mainCameraTransform.forward * moveInput.x + mainCameraTransform.right * moveInput.y) * sprintSpeed;
        slideVelocity.y = 0; //ignore y direction of camera

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
    private Vector3 rotateDirectionOntoSlope(Vector3 currentDirection, Vector3 surfaceNormal) {

        Vector3 slopeMoveDirection = Vector3.ProjectOnPlane(currentDirection, surfaceNormal);

        //slopeMoveDirection = slopeMoveDirection.normalized;

        /*if (slopeMoveDirection.y > 0)
            slopeMoveDirection.y = 0;*/

        Debug.Log("surface normal: " + surfaceNormal);
        Debug.Log("new slope move direction: " + slopeMoveDirection);

        return slopeMoveDirection.normalized;
    }


    /* Slide Decay Multipliers
     * Flat Ground (between -3 & 3 degrees)
     * 
     */
    private void handleSlideDecay() {

        //float slopeAngle = groundChecker.groundSlopeAngle;

        Debug.Log("Matching slide to slope...");
        slideVelocity = determineIfRotateToSlope(slideVelocity, groundChecker.surfaceNormal);


        //TO MAKE THINGS GOOD: BE PICKY ABOUT WHEN TO CALL THIS AND HOW FAST TO ROTATE///////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //rotate towards slide normal after sliding for .25s
        if (Time.time - timeSlideStarted > 0.01f) {
            //slideVelocity = rotatePlayerTowardsSlopeNormal(slideVelocity, groundChecker.surfaceNormal);
            rotatePlayerTowardsSlopeNormal(slideVelocity, groundChecker.surfaceNormal);
        }


        slideVelocity *= 0.998f;
    
    }

    //https://answers.unity.com/questions/46770/rotate-a-vector3-direction.html

    //"Take the cross product of the two vectors and use the result vector as the axis to rotate around. Then perform an axis-angle rotation."
    private void rotatePlayerTowardsSlopeNormal(Vector3 slideDirection, Vector3 surfaceNormal) {

        Debug.Log("ROTATING towards slope normal?");

        Vector3 slopeNormalWithoutY = surfaceNormal;
        //But what about with y? NOTE: DON'T
        slopeNormalWithoutY.y = 0;


        //TO DO: DETERMINE ROTATION SPEED BASED ON SQR MAGNITUDE OF SURFACE NORMAL///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //SHARPER SLOPES ROTATE FASTER

        float rotSpeed = 1f;

        transform.rotation = Quaternion.LookRotation(Vector3.RotateTowards(transform.forward, slopeNormalWithoutY, rotSpeed * Time.deltaTime, 0.0f));
    }


    private Vector3 determineIfRotateToSlope(Vector3 currentDirection, Vector3 surfaceNormal) {

        //to do: come up with some kind of check that will allow us to slide off of sharp edge changes
        //without stopping our ability to stick to sharp slopes
        if (groundChecker.backHit && groundChecker.frontHit)
        {
            currentDirection = rotateDirectionOntoSlope(currentDirection, groundChecker.surfaceNormal);
        }

        return currentDirection.normalized;

    }



    private void startRoll() { 
        
        //Roll!
    }

    // Update is called once per frame
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

        //Attempt 2 - Same thing but moved down
        Vector3 moveDirection = mainCameraTransform.forward * moveInput.x + mainCameraTransform.right * moveInput.y;
        //getting rid of any extra y move direction
        moveDirection.y = 0;

        inputMovementDebug(moveDirection);

        if (groundChecker.surfaceNormal != Vector3.up)
        {
            moveDirection = determineIfRotateToSlope(moveDirection, groundChecker.surfaceNormal);
        }


        if (sliding)
        {
            handleSlideDecay();

            //Transform slide direction X and Z in direction player is facing on slope (keep Y speed)
            /*float oldSlideVelocityY = slideVelocity.y;
            Vector3 adjustedSlideVelocity = transform.TransformDirection(slideVelocity);
            adjustedSlideVelocity.y = oldSlideVelocityY;*/

            ccManager.Move(slideVelocity * sprintSpeed * Time.deltaTime);
        }
        else
        {
            ccManager.Move(moveDirection * moveSpeed * Time.deltaTime);
        }

        slopeMovementDebug(moveDirection, groundChecker.surfaceNormal);
        slideMovementDebug(slideVelocity);

        targetSlopeRotationMidwayDebug(slideVelocity, groundChecker.surfaceNormal);
        //targetSlopeRotationCrossDebug(slideVelocity, groundChecker.surfaceNormal);
        slopeInfluenceDebug(groundChecker.surfaceNormal);

        lastInAir = ccManager.customGrounded;

        updateAnimations();

    }

    private void updateAnimations() {

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

    /*private void targetSlopeRotationCrossDebug(Vector3 slideDirection, Vector3 slopeNormal) {

        Vector3 slopeNormalWithoutY = slopeNormal;
        slopeNormalWithoutY.y = 0;

        Vector3 crossVec = Vector3.Cross(slideDirection.normalized, slopeNormalWithoutY.normalized).normalized;
        crossVec.y = 0;

        Debug.DrawLine(transform.position, transform.position + crossVec, Color.magenta);
    }*/

    private void targetSlopeRotationMidwayDebug(Vector3 slideDirection, Vector3 slopeNormal) {

        Vector3 slopeNormalWithoutY = slopeNormal;
        slopeNormalWithoutY.y = 0;

        Vector3 midpointVector = (slideDirection.normalized + slopeNormalWithoutY).normalized;

        Debug.DrawLine(transform.position, transform.position + midpointVector, Color.magenta);
    
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
