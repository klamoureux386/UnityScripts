using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class GroundedStates {

    //Grounded Movement States
    public static bool grounded { get; set; } = false;
    public static bool crouching { get; set; }  = false;
    public static bool sliding { get; set; } = false;
    public static bool isRunning { get; set; } = false;
    public static bool forcedToSlide { get; set; } = false;
    public static bool slideJumpBuffered { get; set; } = false;
    public static bool lockToSlope { get; set; } = false;

}
