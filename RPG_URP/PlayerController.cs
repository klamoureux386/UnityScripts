using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(GroundChecker))]
public class PlayerController : MonoBehaviour
{

    CharacterController charController;
    public Animator cinemachineAnimator;
    public Animator playerAnimatior;
    GroundChecker groundChecker;

    //CharacterController Settings:
    private float ccStandingHeight;
    private float ccStandingCenterY;
    private float ccCrouchHeight;
    private float ccCrouchCenterY;
    private float ccSlidingHeight;
    private float ccSlidingCenterY;

    private float walkSpeed = 10.0f;
    private float sprintSpeed = 20.0f;
    private float rotationSpeed = 30.0f;
    private float jumpHeight = 4f;
    private float gravityValue = -20f;
    Vector2 moveInput = Vector2.zero;
    public bool lockedOn = false;
    public bool jumping = false;
    public bool sprinting = false;

    //Sliding Vars
    public bool sliding = false;
    private float timeSlideStarted;
    private float timeForcedToSlide = 1.0f; //Time forced to slide in seconds
    public bool cancelSlide = false;

    //Rolling Vars
    public bool rolling = false;

    public bool regularGrounded = false;

    [SerializeField] private Vector3 playerVelocity = Vector3.zero;

    private Transform mainCameraTransform;

    private void Awake()
    {
        charController = GetComponent<CharacterController>();
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
        if (groundChecker.customIsGrounded)
        {
            jumping = true;
        }
        else {
            jumping = false;
        }
    }

    public void OnSprintInput(bool active)
    {
        //must be grounded to sprint
        if (groundChecker.customIsGrounded)
        {
            sprinting = active;
        }
        else
            sprinting = false;
    }

    public void OnEvadeInput(bool active) {

        if (active)
        {

            //if sprinting on ground and not already sliding
            if (sprinting && !sliding && groundChecker.customIsGrounded)
                startSlide();

            //if not srinting, not already rolling, and grounded: roll
            else if (!sprinting && !rolling && groundChecker.customIsGrounded) {
                startRoll();
            }

        }

        else {

            //If not forced to slide
            if (Time.time - timeSlideStarted > timeForcedToSlide && sliding)
            {
                //if time forced to slide is up
                sliding = false;

                StartCoroutine(growCharController());

            }
            //else, set flag to cancel slide asap
            else 
            {
                cancelSlide = true;
            }

        }

    }

    private void startSlide() {

        sliding = true;
        timeSlideStarted = Time.time;

        //HALF CONTROLLER HEIGHT WITH SLIDE, CAN LERP 
        StartCoroutine(shrinkCharController());

        Debug.Log("Slide started at: " + Time.time);
    }

    #region CharController Grow/Shrink
    //https://stackoverflow.com/questions/38473399/unity3d-using-time-deltatime-as-wait-time-for-a-coroutine
    //Take 0.5s to lerp to slide position
    private IEnumerator shrinkCharController() {

        //Debug.Log("shrinking char controller");

        float maxDuration = 0.5f;
        float duration = 0;

        while (duration < maxDuration)
        {
            Debug.Log("shrinking... Duration: " + duration);
            //change char controller from standing height (4) to sliding height (2) over 0.5s
            charController.height = Mathf.Lerp(ccStandingHeight, ccSlidingHeight, duration / maxDuration);
            //change char controller center from standing center ([0,1,0]) to sliding center ([0,0,0]) over 0.5s
            charController.center = new Vector3(0, Mathf.Lerp(ccStandingCenterY, ccSlidingCenterY, duration / maxDuration), 0);

            duration += Time.deltaTime;

            yield return null;
        }

        charController.height = 2;
        charController.center = new Vector3(0, 0, 0);

        //yield break;
    }

    //Take 0.5s to lerp to raised position
    //Note: do not stand up under a completely flat surface (rotation [0,0,0]). Anything else will push you out but 0,0,0 will not
    //To do: use raycast checks to determine if ok to crouch -> ok to stand all the way up
    private IEnumerator growCharController()
    {
        Debug.Log("growing char controller");

        float maxDuration = 0.5f;
        float duration = 0;

        while (duration < maxDuration)
        {

            //Debug.Log("growing... Duration: " + duration);

            //change char controller from sliding height (2) to standing height (4) over 0.5s
            charController.height = Mathf.Lerp(ccSlidingHeight, ccStandingHeight, duration / maxDuration);
            //change char controller center from sliding center ([0,0,0]) to standing center ([0,1,0]) over 0.5s
            charController.center = new Vector3(0, Mathf.Lerp(ccSlidingCenterY, ccStandingCenterY, duration / maxDuration), 0);

            duration += Time.deltaTime;

            yield return null;
        }

        charController.height = 4;
        charController.center = new Vector3(0, 1, 0);

        yield break;
    }
    #endregion

    private void startRoll() { 
        
        //Roll!
    }

    // Update is called once per frame
    void Update()
    {

        //if cancel slide flag set and not forced to slide
        if (cancelSlide && Time.time - timeSlideStarted > timeForcedToSlide && sliding) {
            sliding = false;
            cancelSlide = false;
            StartCoroutine(growCharController());
            Debug.Log("Slide cancelled by flag");
        }

        if (charController.isGrounded)
            regularGrounded = true;
        else
            regularGrounded = false;

        float moveSpeed = (sprinting) ? sprintSpeed : walkSpeed;

        if (charController.isGrounded && playerVelocity.y < 0) {
            playerVelocity.y = -0.01f;
        }

        Vector3 moveDirection = mainCameraTransform.forward * moveInput.x + mainCameraTransform.right * moveInput.y;
        charController.Move(moveDirection * moveSpeed * Time.deltaTime);

        //Apply jump force
        if (jumping && groundChecker.customIsGrounded) {

            playerVelocity.y += Mathf.Sqrt(jumpHeight * -3.0f * gravityValue);
            jumping = false;
        }

        //Apply gravity force to player
        playerVelocity.y += gravityValue * Time.deltaTime;
        charController.Move(playerVelocity * Time.deltaTime);

        //if moving, move in direction of camera
        if (moveInput != Vector2.zero) {
            float targetAngle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg + mainCameraTransform.eulerAngles.y;
            Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

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
    
    }

    private void saveCharacterControllerSettings() {

        //Standing Settings
        ccStandingHeight = charController.height;
        ccStandingCenterY = charController.center.y;
        //Crouching Settings
        ccCrouchHeight = charController.height * 0.75f;
        ccCrouchCenterY = charController.center.y * 0.75f;
        //Sliding Settings
        ccSlidingHeight = charController.height * 0.5f;
        ccSlidingCenterY = charController.center.y * 0.5f;
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
