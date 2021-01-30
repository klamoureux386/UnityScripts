public static class MoveStats {

    //Movement Variables
    public static float walkSpeed { get; } = 16f;             //Default 16
    public static float runSpeed { get; } = 24f;              //Default 24
    public static float jumpSpeed { get; } = 18f;             //Default 18
    public static float gravity { get; } = 30f;               //Default 30
    public static float groundedGravity { get; } = 0.01f;     //Default 0.01
    public static float slideMultiplier { get; } = 1.4f;      //Default 1.25-1.4
    public static float forceToLockToSlope { get; } = -40.0f; //Default -40
    public static float maxSlopeToStickTo { get; } = 45.0f;   //Default 45 degrees

}
