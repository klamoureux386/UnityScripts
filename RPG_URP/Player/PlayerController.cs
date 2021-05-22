using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

//[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(GroundChecker))]
public class PlayerController : MonoBehaviour
{

    //!Note: Removing crouching for now

    //CharacterController charController;
    public CharacterControllerManager ccManager;
    public SlideController slideController;
    public AttackController attackController;
    public Animator cinemachineAnimator;
    public Animator playerAnimatior;
    GroundChecker groundChecker;

    //public GameObject characterMesh; //Disabled in hopes of doing something with IK

    public bool disableAnimation = false;

    //!private float crouchSpeed = 5.0f;
    private float runSpeed = 12.5f;
    private float sprintSpeed = 20.0f;
    private float rotationSpeed = 30.0f;
    private float jumpHeight = 4f;
    private float gravityValue = -30f; //Default: -20
    Vector2 moveInput = Vector2.zero;

    //State Vars
    public bool lockedOn = false;
    public bool sprinting = false;
    //!public bool crouching = false;
    //public bool sliding = false;

    private bool lastInAir = false;

    //Sliding Vars
    //private float slideMultiplier = 1.2f;
    //private float timeSlideStarted;
    //private float timeForcedToSlide = 1.0f; //Time forced to slide in seconds
    //public bool cancelSlide = false;

    //private float sqrMagnitudeToMaintainSpeed = 0.03015259f; //sqrMagnitude of slopeNormal of 10 degree surface without Y

    //Evade Vars
    public bool evading = false;
    private float backstepSpeed = 8.5f;
    private float rollDistance = 20.0f;
    private float evadeDuration = 1.0f; //Should be equal to evade animation length
    //future: backstep should be icicle/flame decoy backstep. Anything else should be a roll?

    //Velocities
    [SerializeField] private Vector3 moveVelocity = Vector3.zero;
    [SerializeField] private float moveVelocityMagnitude = 0f;
    [SerializeField] private Vector3 playerVelocity = Vector3.zero; //Primarily used for gravity, may also account for propulsion effects at some point
    private Vector3 slideVelocity = Vector3.zero; //X and Z movement taken from input at time of slide, Rotated to match slope normal (will generate a Y value)
    private Vector3 slideVelocityAtStart = Vector3.zero;
    [SerializeField] private Vector3 evadeVelocity = Vector3.zero;

    private Transform mainCameraTransform;

    private void Awake()
    {
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

        moveInput.x = vertical; //forward/backwards on thumbstick
        moveInput.y = horizontal; //sideways on thumbstick

    }

    public void OnLockOnInput(bool active)
    {
        
        lockedOn = !lockedOn;
        //Debug.Log("Toggling lock on: " + lockedOn);
        //TODO: MAKE A VAR AND SET LOCK ON TARGET HERE

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
            //Note: probably don't do this outside of for dev purposes, make state based; maybe its fine idk
            playerAnimatior.Play("Jump");
        }

    }

    public void OnHeavyAttackInput(bool active) {

        Debug.Log("Heavy attack input received: " + active);

        //!very simple for testing
        if (active)
        {
            playerAnimatior.Play("HeavyAttack");
            attackController.startHeavyAttack();
        }

    }

    public void OnRangedAttackInput(bool active) {

        if (lockedOn) {

            //TODO: Un hardcode target and use locked-on target var
            attackController.performRangedAttack(GameObject.Find("Enemy"));
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

    //Default input: B (Right button)
    public void OnEvadeInput(bool active) {

        if (active)
        {

            #region Crouching
            //If crouching and standing still
            //?Removing for now
            /*!if (crouching && moveInput == Vector2.zero)
            {
                crouching = false;
                StartCoroutine(ccManager.growCharControllerFromCrouching());
            }

            //If standing still and not crouching
            else if (moveInput == Vector2.zero && !sliding && !crouching)
            {
                crouch();
            }*/
            #endregion

            //if sprinting on ground and not already sliding
            if (sprinting && !slideController.sliding && ccManager.customGrounded)
            {
                slideController.startSlide(moveInput);
            }

            //note: here in case we decide to split backstep and roll into separate functions. 1 function works for now though
            //if not srinting, not already rolling, and not moving while grounded: evade (backstep)
            else if (!sprinting && !evading && ccManager.customGrounded && moveInput == Vector2.zero) {
                startEvade();
            }

            //if not srinting, not already rolling, and input moving while grounded: evade (roll)
            else if (!sprinting && !evading && ccManager.customGrounded && moveInput != Vector2.zero)
            {
                startEvade();

                //make second call for if moveInput == vector2.zero, evade directly back (backstep)
                //change crouch to if B is held while standing still
            }

        }

        else {

            if (slideController.sliding)
            {

                //If not forced to slide and flag active
                if (Time.time - slideController.timeSlideStarted > slideController.timeForcedToSlide && slideController.cancelSlide)
                {
                    slideController.endSlide();

                }
                //else, set flag to cancel slide asap
                else
                {
                    slideController.cancelSlide = true;
                }

            }

        }

    }

    /*!private void crouch() {

        //shrink to slide size for now
        crouching = true;
        StartCoroutine(ccManager.shrinkCharControllerCrouching());
    }*/

    private void jump() {

        playerVelocity.y = Mathf.Sqrt(jumpHeight * -3.0f * gravityValue);

    }

    private void startEvade() {

        evading = true;
        bool backstep = true;

        if (moveInput != Vector2.zero) {
            backstep = false;
        }

        Vector3 evadeDirection;

        if (backstep)
        {
            //Backstep directly back from player forward
            evadeDirection = -transform.forward;
            evadeDirection.y = 0;
            evadeDirection.Normalize();
            evadeVelocity = evadeDirection * backstepSpeed;
        }

        else
        {
            //Normalize horizontal move inputs and set vertical movement to 0
            evadeDirection = mainCameraTransform.forward * moveInput.x + mainCameraTransform.right * moveInput.y;
            evadeDirection.y = 0;
            evadeDirection.Normalize();
            evadeVelocity = evadeDirection * rollDistance;
        }

        if (backstep) {
            Debug.Log("Backstepping");
            playerAnimatior.Play("Backstep");
            //!fully loaded testing
            attackController.setFullyLoaded(true);
        }
        else { 
            Debug.Log("Rolling");
            playerAnimatior.Play("Roll");
            StartCoroutine(ccManager.shrinkCharControllerCrouching());
        }
        //Evade for fixed time, endEvade called afterward
        StartCoroutine(startEvadeTimer(backstep));
    }

    IEnumerator startEvadeTimer(bool backstep) {

        Vector3 evadeVelocityAtStart = evadeVelocity;
        float elapsedTime = 0f;

        float t;

        while (elapsedTime < evadeDuration) {

            //sinerp^2 duration https://chicounity3d.wordpress.com/2014/05/23/how-to-lerp-like-a-pro/
            t = elapsedTime / evadeDuration;
            t = Mathf.Sin(t * Mathf.PI * 0.5f);
            t = t * t;

            //?Adjust scalar value to change target roll velocity at end, vector3.zero feels too limiting on roll
            evadeVelocity = Vector3.Lerp(evadeVelocityAtStart, evadeVelocityAtStart * .2f/*Vector3.zero*/, t);

            elapsedTime += Time.deltaTime;

            yield return null;
        }

        endEvade(backstep);
    }

    private void endEvade(bool backstep) {

        evadeVelocity = Vector3.zero;
        evading = false;

        //Grow character controller if coming up from roll
        if (!backstep)
        {
            StartCoroutine(ccManager.growCharControllerFromCrouching());
        }

        Debug.Log("Evade ended");
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
        if (moveInput != Vector2.zero && !slideController.sliding && !evading)
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

        //!if (crouching) { moveSpeed = crouchSpeed; }

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


        if (evading)
        {
            ccManager.Move(evadeVelocity * Time.deltaTime);
        }
        else if (slideController.sliding)
        {
            slideController.manageSlide();

            //!THIS WORKS, DONT TOUCH LOL
            ccManager.Move(slideController.slideVelocity * Time.deltaTime);
        }
        //Move at half speed when shooting as FL
        else if (attackController.shootingFullyLoaded)
        {
            moveVelocity = moveDirection.normalized * runSpeed * 1.5f; //move faster than run but less than sprint
            moveVelocityMagnitude = moveVelocity.magnitude;
            ccManager.Move(moveVelocity * Time.deltaTime);
        }
        //else normal movement
        else {
            //TODO: un-normalize this at some point so player speed is based on how far they tilt the stick (keep sprint speed locked/normalized)
            moveVelocity = moveDirection.normalized * moveSpeed;
            moveVelocityMagnitude = moveVelocity.magnitude;
            ccManager.Move(/*moveDirection.normalized * moveSpeed*/ moveVelocity * Time.deltaTime);
        }

        //!All useful////////////////
        //!slopeMovementDebug(moveDirection, groundChecker.surfaceNormal);
        //!slideMovementDebug(slideController.slideVelocity);

        //!targetSlopeRotationMidwayDebug(slideController.slideVelocity, groundChecker.surfaceNormal);
        //targetSlopeRotationCrossDebug(slideVelocity, groundChecker.surfaceNormal);
        //!slopeInfluenceDebug(groundChecker.surfaceNormal);

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

        //!playerAnimatior.SetBool("isCrouching", crouching);

        playerAnimatior.SetBool("isGrounded", groundChecker.customIsGrounded);

        playerAnimatior.SetBool("heavyAttackBuffered", attackController.heavyAttackBuffered);

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
}
