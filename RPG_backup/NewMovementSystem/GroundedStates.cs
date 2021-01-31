using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class GroundedStates {

    //Grounded Movement States
    public static bool grounded { get; set; } = false;
    public static bool crouching { get; set; }  = false;
    public static bool sliding { get; set; } = false;
    public static bool isRunning { get; set; } = false;
    public static bool forcedToSlide { get; set; } = false;
    public static bool slideJumpBuffered { get; set; } = false;
    public static bool lockToSlope { get; set; } = false;

    public static void setGroundedMovementStates(CharacterController charController) {

        grounded = charController.isGrounded;

        if (forcedToSlide && Input.GetButton("Jump")) {
            slideJumpBuffered = true;
        }

        if (!crouching && Input.GetButton("Shift")) {
            isRunning = true;
        }

        //set running to false unless last frame char was in air (last in air exception for cases where you want to slide on landing)
        else if (!sliding && charController.isGrounded && !AerialStates.lastInAir) {
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

}
