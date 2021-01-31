using UnityEngine;

public static class AerialStates {

    //Aerial Movement States
    public static bool airJumpUsed { get; set; } = false;
    public static bool runningWhenJumped { get; set; } = false;
    public static bool lastInAir { get; set; } = false;
    public static bool jumpingThisFrame { get; set; } = false;
    public static Vector3 airborneStartingSpeed { get; set; } = Vector3.zero;

    public static void reset() {
        airJumpUsed = false;
        runningWhenJumped = false;
        lastInAir = false;
        jumpingThisFrame = false;
        airborneStartingSpeed = Vector3.zero;
    }
}
