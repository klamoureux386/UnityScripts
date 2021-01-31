using UnityEngine;

public static class MoveStats {

    //Movement Variables
    public static float walkSpeed { get; } = 20f;               //Default 16
    public static float runSpeed { get; } = 30f;                //Default 24
    public static float jumpSpeed { get; } = 22f;               //Default 18
    public static float gravity { get; } = 30f;                 //Default 30
    public static float groundedGravity { get; } = -0.01f;      //Default -0.01
    public static float slideMultiplier { get; } = 1.4f;        //Default 1.25-1.4
    public static float forceToLockToSlope { get; } = -40.0f;   //Default -40
    public static float maxSlopeToStickTo { get; } = 45.0f;     //Default 45 degrees

    //AirJump Varaible
    public static float timeBeforeAirJump = 0.5f;               //Default 0.2s
    //Slide Variables
    public static float timeSlideStart { get; set; } = 0;
    //Slide Animation Variables
    public static Vector3 moveSpeedOnSlideStart { get; set; } = Vector3.zero;    //X and Z movement
    public const float timeForcedToSlide = 0.25f;             //Should be same length as slide animation duration
    public const float timeToGetUpFromSlide = 0.10f;          //Should be same length as slide get-up animation

}
