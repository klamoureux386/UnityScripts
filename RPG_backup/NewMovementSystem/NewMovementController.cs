using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewMovementController : MonoBehaviour
{
    CharacterController charController;
    CameraController cameraController;
    GroundChecker groundChecker;

    //Input Variables
    private float inputHorizontal = 0;     //X movement
    private float inputVertical = 0;       //Z movement

    //Debugging Variables
    Vector3 surfaceNormal = Vector3.zero;
    Vector3 moveDirection = Vector3.zero;

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

        AerialStates.jumpingThisFrame = false;
        setGroundedMovementStates();
        inputHorizontal = Input.GetAxisRaw("Horizontal");
        inputVertical = Input.GetAxisRaw("Vertical");

        if (groundChecker.customIsGrounded && moveDirection.y <= 0 && groundChecker.groundSlopeAngle <= -3 && groundChecker.groundSlopeAngle < charController.slopeLimit) {
            //Debug.Log("locking to slope");
            moveDirection.y = MoveStats.forceToLockToSlope;
            GroundedStates.lockToSlope = true;
        }
        else {
            GroundedStates.lockToSlope = false;
        }

        if (GroundedStates.grounded)
            performGroundedMovement();
        else
            performAerialMovement();

    }

    private void performGroundedMovement() {

    }

    private void performAerialMovement() {

    }

    private void setGroundedMovementStates() {

        GroundedStates.grounded = charController.isGrounded;

        if (GroundedStates.forcedToSlide && Input.GetButton("Jump")) {
            GroundedStates.slideJumpBuffered = true;
        }

        if (!GroundedStates.crouching && Input.GetButton("Shift")) {
            GroundedStates.isRunning = true;
        }

        //set running to false unless last frame char was in air (last in air exception for cases where you want to slide on landing)
        else if (!GroundedStates.sliding && charController.isGrounded && !AerialStates.lastInAir) {
            GroundedStates.isRunning = false;
        }

        if (Input.GetButton("Crouch") || GroundedStates.forcedToSlide) {
            GroundedStates.crouching = true;
        }

        else {
            GroundedStates.crouching = false;
            GroundedStates.sliding = false;
        }
    }

}
