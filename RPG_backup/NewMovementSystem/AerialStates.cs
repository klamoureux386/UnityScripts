public static class AerialStates {

    //Aerial Movement States
    public static bool airJumpUsed { get; set; } = false;
    public static bool runningWhenJumped { get; set; } = false;
    public static bool lastInAir { get; set; } = false;
    public static bool jumpingThisFrame { get; set; } = false;

    public static void reset() {
        airJumpUsed = false;
        runningWhenJumped = false;
        lastInAir = false;
        jumpingThisFrame = false;
    }
}
