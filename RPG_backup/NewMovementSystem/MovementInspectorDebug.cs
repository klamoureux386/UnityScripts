using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class MovementInspectorDebug : MonoBehaviour {

    //Grounded Movement States
    [Header("Grounded States")]
    [SerializeField] private bool controllerGrounded = false;
    [SerializeField] private bool crouching = false;
    [SerializeField] private bool sliding = false;
    [SerializeField] private bool isRunning = false;
    [SerializeField] private bool forcedToSlide = false;
    [SerializeField] private bool slideJumpBuffered = false;
    [SerializeField] private bool lockToSlope = false;

    //Aerial Movement States
    [Header("Aerial States")]
    [SerializeField] private bool airJumpUsed = false;
    [SerializeField] private bool runningWhenJumped = false;
    [SerializeField] private bool lastInAir = false;
    [SerializeField] private bool jumpingThisFrame = false;

    private void FixedUpdate() {
        updateDebugStates();   
    }

    private void updateDebugStates() {
        
        //Grounded States
        controllerGrounded = GroundedStates.grounded;
        crouching = GroundedStates.crouching;
        sliding = GroundedStates.sliding;
        isRunning = GroundedStates.isRunning;
        forcedToSlide = GroundedStates.forcedToSlide;
        slideJumpBuffered = GroundedStates.slideJumpBuffered;
        lockToSlope = GroundedStates.lockToSlope;

        //Aerial States
        airJumpUsed = AerialStates.airJumpUsed;
        runningWhenJumped = AerialStates.runningWhenJumped;
        lastInAir = AerialStates.lastInAir;
        jumpingThisFrame = AerialStates.jumpingThisFrame;
    }
}
