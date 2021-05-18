using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GroundChecker))]
public class SlideController : MonoBehaviour
{

    public GroundChecker groundChecker;
    public CharacterControllerManager ccManager;

    private float sprintSpeed = 20.0f;

    private float timeForcedToSlide = 1.0f;

    private float timeOfLastAdjustment = 0;
    private bool timeOfAdjustmentSet = false;
    private Vector3 slideVelocityAtStartOfLerp = Vector3.zero;

    private float boostAtSlideStart = 1.1f; //10% boost

    public bool sliding = false;

    public bool cancelSlide = false;

    private float timeSlideStarted;

    private Transform mainCameraTransform;


    //TARGET MAGNITUDES BY SLOPE ANGLE OVER TIME?
    private float flatSlopeSpeed = 0f;


    [Header("Tracking")]
    public Vector3 slideVelocity = Vector3.zero; //X and Z movement taken from input at time of slide, Rotated to match slope normal (will generate a Y value)
    public float slideVelocityMagnitude = 0;
    public   Vector3 slideVelocityAtStart = Vector3.zero;

    private void Awake()
    {
        groundChecker = GetComponent<GroundChecker>();
        ccManager = GetComponent<CharacterControllerManager>();
        mainCameraTransform = Camera.main.transform;
    }

    private void FixedUpdate()
    {
        slideVelocityMagnitude = slideVelocity.magnitude;
    }


    public void startSlide(Vector2 moveInput) {

        sliding = true;
        timeSlideStarted = Time.time;

        //store input direction at time of slide (multiplied by sprint speed) in slideVelocity and scale it
        slideVelocity = (mainCameraTransform.forward * moveInput.x + mainCameraTransform.right * moveInput.y);
        slideVelocity.y = 0; //ignore y direction of camera, that's determined by the slope/ground we're on
        slideVelocity = slideVelocity.normalized * sprintSpeed * boostAtSlideStart; //!<--added boost

        slideVelocityAtStart = slideVelocity; //add slight boost

        StartCoroutine(ccManager.shrinkCharControllerSliding());

    }

    public void manageSlide() {

        //? Mediocre fix to stop occasional popping up when starting slide on a sharp upward angle
        if (!groundChecker.backHit && slideVelocity.y > 0)
            slideVelocity.y = 0;


        //Adjust slide movement to prevent bouncing down slopes
        if (groundChecker.backHit && groundChecker.frontHit && groundChecker.surfaceNormal != Vector3.up)
        {
            alignMovementToSlopeAngle();
        }

        //!Magnitude is still the same through here, speed unaffected only angle of movement depending on slope

        //Rotate slide movement and player rotation based on slope normal, (surface normal check prevents rotation reset in air)
        if (Time.time - timeSlideStarted > 0.25f && groundChecker.surfaceNormal != Vector3.zero)
        {
            rotatePlayerTowardsSlopeNormal();
        }

        //!Magnitude remains effectively the same through here, slight changes in far decimals

        //TODO: Adjust speed (magnitude) based on slope
        //TODO: the code was so clean up until adjusting speed D:

        if (groundChecker.backHit && groundChecker.frontHit)
        {
            adjustSlideSpeed2();
        }

    }

    //!This actually works perfectly fine, the built in unity terrain system just isn't very smooth
    private void alignMovementToSlopeAngle() {

        float magnitudeBeforeProjection = slideVelocity.magnitude;

        //Projecting onto plane chops the vector on a vertical rather than laying it down, losing a slight bit of magnitude

        slideVelocity = Vector3.ProjectOnPlane(slideVelocity, groundChecker.surfaceNormal);

        //to correct this, we store the magnitude of the vector before the projection, then normalize the resulting projection by the length of the original vector
        slideVelocity = slideVelocity.normalized * magnitudeBeforeProjection;
        
    }

    //TODO: BIG TODO, taking a break for now but this should be #1 priority for clean up (see function below) in future. Skeleton code for future
    private void cleanerRotatePlayerTowardsSlopeNormal() {

        Vector3 slopeNormalWithoutY = groundChecker.surfaceNormal;
        slopeNormalWithoutY.y = 0;
        slopeNormalWithoutY.Normalize();

        float absoluteHillSlope = groundChecker.normalRelativeToUp; //Stores how sharp the angle slope is regardless of our orientation (always positive)

        bool travellingDownhill = true;

        if (groundChecker.groundSlopeAngle > 0)
            travellingDownhill = false;


    }

    //TODO: Clean this up, also use ratios instead of magic numbers. It's pretty resistant though, a lot of the issues have been in speed adjustments so check there first.
    private void rotatePlayerTowardsSlopeNormal() {

        Vector3 slopeNormalWithoutY = groundChecker.surfaceNormal;
        //But what about with y? NOTE: DON'T
        slopeNormalWithoutY.y = 0;

        //?Attempt 2: Try, if groundSlopeAngle is positive, turning speed is much higher. Messy but getting closer conceptually
        //TODO: Turn divisor values into uphill/downhill

        float rotDivisor = 30;

        //turn much harder on upward slopes
        if (groundChecker.groundSlopeAngle > 0)
        {
            rotDivisor = 10f;
        }

        float calculatedRotSpeed = Mathf.Abs(groundChecker.groundSlopeAngle) / rotDivisor; //slope of 45 will result in old average rot speed

        //minimum rot speed when transitioning from uphill to downhill
        if (calculatedRotSpeed < 1.0f && rotDivisor == 10) //slowest rotation of 0.1
            calculatedRotSpeed = 1.0f;

        //minimum rotspeed once we transition to downhill
        if (calculatedRotSpeed < 0.75f && rotDivisor == 30)
            calculatedRotSpeed = 0.75f;

        float angleLeftToRotate = Vector3.Angle(transform.forward, slopeNormalWithoutY);

        Debug.Log("ANGLE BETWEEN CURRENT AND TARGET ROTATION: " + angleLeftToRotate);

        //dont rotate if slope angle isn't sharper than 12 degrees uphill
        if (groundChecker.normalRelativeToUp <= 12)
            return;

        transform.rotation = Quaternion.LookRotation(Vector3.RotateTowards(transform.forward, slopeNormalWithoutY, calculatedRotSpeed * Time.deltaTime, 0.0f));

        slideVelocity =  Vector3.RotateTowards(slideVelocity, slopeNormalWithoutY, calculatedRotSpeed * Time.deltaTime, 0.0f);

    }

    //Just using flat scaling because I can't think of another way
    //! I think this is fine for now, next adjustment should be a cleaner rotation speed based on slope steepness
    //TODO: Turn this into a formula rather than piece-wise
    private void adjustSlideSpeed2() {

        float speedCap = 40f;

        float slopeAngle = groundChecker.groundSlopeAngle;

        float slopeScalar = 0.001f;

        //!Not the best implementation but... it works

        //Hurry up slide decay when magnitude gets low on flat(ish) ground

        if (slideVelocity.magnitude < 4 && groundChecker.groundSlopeAngle > -5 && groundChecker.groundSlopeAngle < 5)
            slopeScalar *= 3;

        else if (slideVelocity.magnitude < 8 && groundChecker.groundSlopeAngle > -5 && groundChecker.groundSlopeAngle < 5)
            slopeScalar *= 2;

        //if moving really slow, just end slide
        if (slideVelocity.magnitude < 1)
        {
            endSlide();
            return;
        }

        //if faster than speed cap, limit magnitude to speedcap
        if (slideVelocity.magnitude > speedCap)
            slideVelocity *= speedCap / slideVelocity.magnitude;


        //!GainSpeed Check
        if (slideVelocity.magnitude < speedCap && slopeAngle < -8)
        {

            /*if (slopeAngle < -30)
                slideVelocity *= 1 + (slopeScalar*3f);
            else */if (slopeAngle < -25)
                slideVelocity *= 1 + (slopeScalar*2.5f);
            else if (slopeAngle < -20)
                slideVelocity *= 1 + (slopeScalar*2.0f);
            else if (slopeAngle < -15)
                slideVelocity *= 1 + (slopeScalar*1.5f);
            else if (slopeAngle < -10)
                slideVelocity *= 1 + slopeScalar;
            else if (slopeAngle < -5)
                slideVelocity *= 1 + (slopeScalar/2.0f);
        }
        //!LoseSpeed Check
        else
        {
            //if on downward slope but over speedcap, do nothing
            if (slopeAngle < -5)
                return;
            //else if on slope shallow enough to lose speed
            /*else if (slopeAngle < 5)
                slideVelocity *= 1 - (slopeScalar/4.0f);*/
            else if (slopeAngle < 8)
                slideVelocity *= 1 - slopeScalar;
            else if (slopeAngle < 12)
                slideVelocity *= 1 - (slopeScalar * 1.5f);
            else if (slopeAngle < 16)
                slideVelocity *= 1 - (slopeScalar * 2.0f);
            else if (slopeAngle < 20)
                slideVelocity *= 1 - (slopeScalar * 2.5f);
            else if (slopeAngle < 25)
                slideVelocity *= 1 - (slopeScalar * 3.0f);
            else if (slopeAngle < 30)
                slideVelocity *= 1 - (slopeScalar * 3.5f);
        }

    }

    public void checkIfCancelSlide() {
        //if cancel slide flag set and not forced to slide
        if (cancelSlide && Time.time - timeSlideStarted > timeForcedToSlide && sliding)
        {
            endSlide();
            Debug.Log("Slide cancelled by flag");

            //!for testing
            timeOfAdjustmentSet = false;
        }
    }

    public void endSlide() {
        sliding = false;
        slideVelocity = Vector3.zero;
        cancelSlide = false;
        StartCoroutine(ccManager.growCharControllerFromSliding());

        //!for testing
        timeOfAdjustmentSet = false;

        Debug.Log("Slide ended");
    }

}
