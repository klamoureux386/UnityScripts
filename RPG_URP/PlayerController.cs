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
    public SlideController slideController;
    public Animator cinemachineAnimator;
    public Animator playerAnimatior;
    GroundChecker groundChecker;

    //public GameObject characterMesh; //Disabled in hopes of doing something with IK

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
    //public bool cancelSlide = false;

    private float sqrMagnitudeToMaintainSpeed = 0.03015259f; //sqrMagnitude of slopeNormal of 10 degree surface without Y

    //Rolling Vars
    public bool rolling = false;

    //Velocities
    [SerializeField] private Vector3 moveVelocity = Vector3.zero;
    [SerializeField] private Vector3 playerVelocity = Vector3.zero; //Primarily used for gravity, may also account for propulsion effects at some point
    private Vector3 slideVelocity = Vector3.zero; //X and Z movement taken from input at time of slide, Rotated to match slope normal (will generate a Y value)
    private Vector3 slideVelocityAtStart = Vector3.zero;

    private Transform mainCameraTransform;

    private void Awake()
    {
        //charController = GetComponent<CharacterController>();
        groundChecker = GetComponent<GroundChecker>();
        slideController = GetComponent<SlideController>();
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

            if (crouching)
            {
                crouching = false;
                StartCoroutine(ccManager.growCharControllerFromCrouching());
            }

            else if (moveInput == Vector2.zero && !sliding && !crouching)
            {
                crouch();
            }

            //if sprinting on ground and not already sliding
            else if (sprinting && !sliding && ccManager.customGrounded)
            {
                slideController.startSlide(moveInput);
            }

            //if not srinting, not already rolling, and grounded: roll
            else if (!sprinting && !rolling && ccManager.customGrounded)
            {
                startRoll();
            }

        }

        else {

            if (slideController.sliding)
            {

                //If not forced to slide and flag active
                if (Time.time - timeSlideStarted > timeForcedToSlide && slideController.cancelSlide)
                {
                    //if time forced to slide is up
                    /*!sliding = false;
                    slideVelocity = Vector2.zero;

                    StartCoroutine(ccManager.growCharControllerFromSliding());*/

                    slideController.endSlide();

                }
                //else, set flag to cancel slide asap
                else
                {
                    slideController.cancelSlide = true;
                    //!cancelSlide = true;
                }

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

    private void startRoll() { 
        
        //Roll!
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

    //Adjusts move velocity to move parallel with slope
    private Vector3 rotateDirectionOntoSlopeMove(Vector3 moveDirection, Vector3 surfaceNormal)
    {

        Vector3 slopeMoveDirection = Vector3.ProjectOnPlane(moveDirection, surfaceNormal);

        return slopeMoveDirection.normalized;
    }

    private void FixedUpdate()
    {

        //Rotate character to face camera direction
        Vector3 inputDirection = new Vector3(moveInput.y, 0, moveInput.x);
        Vector3 camDirection = mainCameraTransform.rotation * inputDirection;
        Vector3 targetDirection = new Vector3(camDirection.x, 0, camDirection.z);

        //Turn character to cam forward if not sliding and not standing still
        if (moveInput != Vector2.zero && !slideController.sliding)
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

        slideController.checkIfCancelSlide();

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


        if (slideController.sliding)
        {
            slideController.manageSlide();

            //!THIS WORKS, DONT TOUCH LOL
            ccManager.Move(slideController.slideVelocity * Time.deltaTime);
        }
        else
        {
            moveVelocity = moveDirection.normalized * moveSpeed;
            ccManager.Move(moveDirection.normalized * moveSpeed * Time.deltaTime);
        }

        slopeMovementDebug(moveDirection, groundChecker.surfaceNormal);
        slideMovementDebug(slideController.slideVelocity);

        targetSlopeRotationMidwayDebug(slideController.slideVelocity, groundChecker.surfaceNormal);
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

        playerAnimatior.SetBool("isSliding", slideController.sliding);

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
