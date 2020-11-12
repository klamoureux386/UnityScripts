using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class CameraController : MonoBehaviour
{

    public Camera playerCamera;
    public Volume postProcessingVolume;
    public GameObject playerObject;

    public float lookSpeed = 2.0f;
    public float lookXLimit = 75.0f;
    public float bobLerpDownSpeed = 0.05f;
    public float bobLerpUpSpeed = 0.03f;

    //private float vignWalkLerpTime = 0.175f; //take 0.175s to lerp to walk vignette
    //private float vignSprintLerpTime = 0.25f; //take 0.25s to lerp to sprint (enrage) vignette
    //private float vignWalkLerpStart = 0;
    //private float vignSprintLerpStart = 0;

    public float minFallDistanceToBob = -10.0f;
    public float maxFallDistanceBobCap = -30.0f;

    private Vignette vignetteComponent;
    private float vignLerpSpeed = 0.025f;
    private float vignWalkIntensity = 0.25f;
    private float vignSprintIntensity = 0.3f;
    private Color vignWalkColor = Color.black;
    private Color vignSprintColor = Color.red;

    private bool cameraBobDown = false;
    private bool cameraBobUp = false;
    private Vector3 cameraBobDownTarget = new Vector3(0, 0.0025f, 0); //magic number to use as a ratio for lowering a maximum of 0.050 units when multiplied by Y velocity

    private float rotationX = 0;

    private Vector3 cameraRestingLocation; //SET AS LOCAL POSITION, Default [0, 0.7, 0]
    private Vector3 cameraSlidingLocation = new Vector3(0, 0.15f, 0.05f); //Default [0, 0.15, 0]
    private Vector3 cameraCrouchingLocation; //Default [0, 0.3, 0]

    void Start()
    {
        cameraRestingLocation = playerCamera.transform.localPosition;
        /*cameraSlidingLocation = cameraRestingLocation - new Vector3(0, 0.55f, 0); //Sliding height 0.55 lower than resting height
        cameraCrouchingLocation = cameraRestingLocation - new Vector3(0, 0.4f, 0); //Crouching height 0.4 lower than resting height*/

        Debug.Log("Camera resting location: " + cameraRestingLocation);
        Debug.Log("Camera sliding location: " + cameraSlidingLocation);
        Debug.Log("Camera crouching location: " + cameraCrouchingLocation);

        //Get Vignette
        Vignette tmp;
        if (postProcessingVolume.profile.TryGet<Vignette>(out tmp))
            vignetteComponent = tmp;

    }

    // Update is called once per frame
    void Update()
    {

        if (cameraBobDown && !cameraBobUp) {
            bobCameraDown();
        }

        else if (cameraBobUp && !cameraBobDown) {
            bobCameraUp();
        }

        rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
    }

    public void checkBobCamera(float lastJumpYPos, bool jumpToAir)
    {
        float fallDistance = playerObject.transform.position.y - lastJumpYPos;

        if (jumpToAir)
            fallDistance += 2;

        Debug.Log("Hit ground; Fall distance: " + fallDistance);
        Debug.Log("Fall distance to bob: " + minFallDistanceToBob);

        if (fallDistance > minFallDistanceToBob || cameraBobDown || cameraBobUp) //return if fall distance not < -5 or if camera bobbing
            return;


        if (fallDistance < maxFallDistanceBobCap)
        {
            fallDistance = maxFallDistanceBobCap;
        }

        //Start downward bob
        cameraBobDownTarget = new Vector3(0, 0.0025f, 0) * fallDistance; //magic number to use as a ratio for how far target is moved down
        cameraBobDown = true;

        Debug.Log("Start Bob");
    }

    
    private void bobCameraDown() {

        
        float distanceToTarget = Vector3.SqrMagnitude(playerCamera.transform.localPosition - cameraBobDownTarget);

        /*Debug.Log("Camera location: " + playerCamera.transform.localPosition);
        Debug.Log("Target to bob to: " + cameraBobDownTarget);
        Debug.Log("Distance to target: " + distanceToTarget);*/

        if (distanceToTarget < 0.01) {
            cameraBobDown = false;
            cameraBobUp = true;
            Debug.Log("Done bobbing down");
        }

        else
            playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, cameraBobDownTarget, bobLerpDownSpeed); //Lerps camera down to target
            

        /*
        float timeSinceStarted = Time.time - timeBobDownStart;
        float percentageComplete = timeSinceStarted / bobLerpDownTime;

        Debug.Log("Time since started: " + timeSinceStarted);

        playerCamera.transform.localPosition = Vector3.Lerp(cameraRestingLocation, cameraBobDownTarget, percentageComplete);

        //Start upward bob
        if (percentageComplete >= 1.0f) {
            cameraBobDown = false;
            timeBobUpStart = Time.time;
            cameraBobUp = true;
        }
        */
    }

    private void bobCameraUp() {

        
        float distanceToTarget = Vector3.SqrMagnitude(playerCamera.transform.localPosition - cameraRestingLocation);

        /*Debug.Log("Camera location: " + playerCamera.transform.localPosition);
        Debug.Log("Target to bob to: " + cameraRestingLocation);
        Debug.Log("Distance to target: " + distanceToTarget);*/

        if (distanceToTarget < 0.01)
        {
            cameraBobUp = false;
            Debug.Log("Done bobbing up");
        }

        else
            playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, cameraRestingLocation, bobLerpUpSpeed); //bobs back up to camera resting location
        

        /*
        float timeSinceStarted = Time.time - timeBobUpStart;
        float percentageComplete = timeSinceStarted / bobLerpUpTime;

        playerCamera.transform.localPosition = Vector3.Lerp(cameraBobDownTarget, cameraRestingLocation, percentageComplete);

        if (percentageComplete >= 1.0f)
        {
            cameraBobUp = false;
        }
        */

    }

    //https://forum.unity.com/threads/hdrp-2019-1-how-do-you-change-effects-volume-overrides-through-script.668842/
    //http://www.blueraja.com/blog/404/how-to-use-unity-3ds-linear-interpolation-vector3-lerp-correctly
    public void setSprintVignette() {

        //cancel if vignette the same
        if (vignetteComponent.color == vignSprintColor && vignetteComponent.intensity == vignSprintIntensity) {
            //Debug.Log("Vignette equal to sprint color/intensity");
            return;
        }

        //Debug.Log("Changing Vignette to sprint color/intensity");

        //if current color is similar to target color within 1 rgb, set to target color
        if (compareSimilarColors(vignetteComponent.color.value, vignSprintColor)) {
            vignetteComponent.color.value = vignSprintColor;
            vignetteComponent.intensity.value = vignSprintIntensity;
        }

        else
        {
            vignetteComponent.color.Interp(vignetteComponent.color.value, vignSprintColor, vignLerpSpeed);
            vignetteComponent.intensity.Interp(vignetteComponent.intensity.value, vignSprintIntensity, vignLerpSpeed);
        }

    }

    public void setWalkVignette(){

        if (vignetteComponent.color == vignWalkColor && vignetteComponent.intensity == vignWalkIntensity) {
            //Debug.Log("Vignette equal to walk color/intensity");
            return;
        }

        //Debug.Log("Changing Vignette to walk color/intensity");

        //if current color is similar to target color within 1 rgb, set to target color
        if (compareSimilarColors(vignetteComponent.color.value, vignWalkColor)) {
            vignetteComponent.color.value = vignWalkColor;
            vignetteComponent.intensity.value = vignWalkIntensity;
        }

        //else interpolate
        else
        {
            vignetteComponent.color.Interp(vignetteComponent.color.value, vignWalkColor, vignLerpSpeed);
            vignetteComponent.intensity.Interp(vignetteComponent.intensity.value, vignWalkIntensity, vignLerpSpeed);
        }
    }

    //https://answers.unity.com/questions/787056/comparing-2-color-variables.html
    private bool compareSimilarColors(Color color1, Color color2) {

        int decMultiplier = 1000; //multiply by 1000 to show get 3 sig figs
        int margin = 1;
        //this makes margin essentially = 0.001 on a scale of [0, 1] for normal colors

        float color1r = color1.r * decMultiplier;
        float color1g = color1.g * decMultiplier;
        float color1b = color1.b * decMultiplier;
        float color1a = color1.a * decMultiplier;
        float color2r = color2.r * decMultiplier;
        float color2g = color2.g * decMultiplier;
        float color2b = color2.b * decMultiplier;
        float color2a = color2.a * decMultiplier;


        Debug.Log("Red Colors: " + color1r + ", " + color2r + "\n" +
            "Green Colors: " + color1g + ", " + color2g + "\n" +
            "Blue Colors: " + color1b + ", " + color2b + "\n");

        if (Mathf.Abs(color1r - color2r) <= margin &&
            Mathf.Abs(color1g - color2g) <= margin &&
            Mathf.Abs(color1b - color2b) <= margin &&
            Mathf.Abs(color1a - color2a) <= margin) {
            return true;
        }

        return false;
    }

    public IEnumerator dipCameraForSlide(float timeForcedToSlide, float startTime) {

        Debug.Log("Dropping camera for slide");

        float percentageComplete = 0;

        Vector3 startLocation = cameraRestingLocation;
        Vector3 target = cameraSlidingLocation;

        while (percentageComplete < 1.0f) {

            percentageComplete = lerpCameraLocal(startLocation, target, timeForcedToSlide, startTime);
            yield return null;

        }

        /*
        float timeSinceStarted = Time.time - startTime;
        float percentageComplete = timeSinceStarted / timeForcedToSlide;

        playerCamera.transform.localPosition = Vector3.Lerp(cameraRestingLocation, cameraSlidingLocation, percentageComplete);

        if (percentageComplete >= 1.0f)
        {

            playerCamera.transform.localPosition = cameraSlidingLocation;

            yield return null;
        }*/

    }

    public IEnumerator raiseCameraFromSlide(float timeToGetUpFromSlide, float startTime) {

        Debug.Log("Raising camera from slide");

        float percentageComplete = 0;

        Vector3 startLocation = cameraSlidingLocation;
        Vector3 target = cameraRestingLocation;

        while (percentageComplete < 1.0f) {

            percentageComplete = lerpCameraLocal(startLocation, target, timeToGetUpFromSlide, startTime);
            yield return null;

        }

    }

    //Returns percentage completion
    private float lerpCameraLocal(Vector3 startLocation, Vector3 target, float duration, float startTime) {

        float timeSinceStarted = Time.time - startTime;
        float percentageComplete = timeSinceStarted / duration;

        playerCamera.transform.localPosition = Vector3.Lerp(startLocation, target, percentageComplete);

        Debug.Log("Lerping. Percentage complete: " + percentageComplete);

        if (percentageComplete >= 1.0f) {
            playerCamera.transform.localPosition = target;
        }

        return percentageComplete;

    }

}
