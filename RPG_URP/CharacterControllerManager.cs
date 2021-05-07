using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(GroundChecker))]
public class CharacterControllerManager : MonoBehaviour
{

    private CharacterController charController;
    private GroundChecker groundChecker;

    public bool regularGrounded;
    public bool customGrounded;
    //public bool raycastGrounded;

    //CharacterController Settings:
    private float ccStandingHeight;
    private float ccStandingCenterY;
    private float ccCrouchHeight;
    private float ccCrouchCenterY;
    private float ccSlidingHeight;
    private float ccSlidingCenterY;

    private void Awake()
    {
        charController = GetComponent<CharacterController>();
        groundChecker = GetComponent<GroundChecker>();
        saveCharacterControllerSettings();
    }

    private void Update()
    {
        regularGrounded = charController.isGrounded;
        customGrounded = groundChecker.customIsGrounded;
        //raycastGrounded = groundChecker.raycastGrounded;
    }

    public void Move(Vector3 moveDirection)
    {
        charController.Move(moveDirection);
    }

    private void saveCharacterControllerSettings()
    {

        //Standing Settings
        ccStandingHeight = charController.height;
        ccStandingCenterY = 1;
        //Crouching Settings
        ccCrouchHeight = charController.height * 0.75f;
        ccCrouchCenterY = 0.5f;
        //Sliding Settings
        ccSlidingHeight = charController.height * 0.5f;
        ccSlidingCenterY = 0;
    }

    #region CharController Grow/Shrink
    //https://stackoverflow.com/questions/38473399/unity3d-using-time-deltatime-as-wait-time-for-a-coroutine
    //Take 0.5s to lerp to slide position
    public IEnumerator shrinkCharControllerSliding()
    {

        //Debug.Log("shrinking char controller");
        Debug.Log("Character controller height at start: " + charController.height);

        float maxDuration = 0.5f;
        float duration = 0;

        while (duration < maxDuration)
        {
            Debug.Log("shrinking... Duration: " + duration);
            //change char controller from standing height (4) to sliding height (2) over 0.5s
            charController.height = Mathf.Lerp(ccStandingHeight, ccSlidingHeight, duration / maxDuration);

            Debug.Log("Character controller height: " + charController.height);
            //change char controller center from standing center ([0,1,0]) to sliding center ([0,0,0]) over 0.5s
            charController.center = new Vector3(0, Mathf.Lerp(ccStandingCenterY, ccSlidingCenterY, duration / maxDuration), 0);

            duration += Time.deltaTime;

            yield return null;
        }


        charController.height = ccSlidingHeight;
        charController.center = new Vector3(0, ccSlidingCenterY, 0);

        Debug.Log("Character controller height at end: " + charController.height);

        //yield break;
    }

    public IEnumerator shrinkCharControllerCrouching()
    {

        //Debug.Log("shrinking char controller");
        Debug.Log("Character controller height at start: " + charController.height);

        float maxDuration = 0.5f;
        float duration = 0;

        while (duration < maxDuration)
        {
            Debug.Log("shrinking... Duration: " + duration);
            //change char controller from standing height (4) to sliding height (2) over 0.5s
            charController.height = Mathf.Lerp(ccStandingHeight, ccCrouchHeight, duration / maxDuration);

            Debug.Log("Character controller height: " + charController.height);
            //change char controller center from standing center ([0,1,0]) to sliding center ([0,0,0]) over 0.5s
            charController.center = new Vector3(0, Mathf.Lerp(ccStandingCenterY, ccCrouchCenterY, duration / maxDuration), 0);

            duration += Time.deltaTime;

            yield return null;
        }

        charController.height = ccCrouchHeight;
        charController.center = new Vector3(0, ccCrouchCenterY, 0);

        Debug.Log("Character controller height at end: " + charController.height);

        //yield break;
    }

    //Take 0.5s to lerp to raised position
    //Note: do not stand up under a completely flat surface (rotation [0,0,0]). Anything else will push you out but 0,0,0 will not
    //To do: use raycast checks to determine if ok to crouch -> ok to stand all the way up
    //also to do: make sure the final setting of controller height/center doesnt overwrite the other when spamming B
    public IEnumerator growCharControllerFromSliding()
    {
        Debug.Log("growing char controller");

        float maxDuration = 0.5f;
        float duration = 0;

        while (duration < maxDuration)
        {

            //Debug.Log("growing... Duration: " + duration);

            //change char controller from sliding height (2) to standing height (4) over 0.5s
            charController.height = Mathf.Lerp(ccSlidingHeight, ccStandingHeight, duration / maxDuration);
            //change char controller center from sliding center ([0,0,0]) to standing center ([0,1,0]) over 0.5s
            charController.center = new Vector3(0, Mathf.Lerp(ccSlidingCenterY, ccStandingCenterY, duration / maxDuration), 0);

            duration += Time.deltaTime;

            yield return null;
        }

        charController.height = ccStandingHeight;
        charController.center = new Vector3(0, ccStandingCenterY, 0);

        //yield break;
    }

    public IEnumerator growCharControllerFromCrouching()
    {
        Debug.Log("growing char controller");

        float maxDuration = 0.5f;
        float duration = 0;

        while (duration < maxDuration)
        {

            //Debug.Log("growing... Duration: " + duration);

            //change char controller from sliding height (2) to standing height (4) over 0.5s
            charController.height = Mathf.Lerp(ccCrouchHeight, ccStandingHeight, duration / maxDuration);
            //change char controller center from sliding center ([0,0,0]) to standing center ([0,1,0]) over 0.5s
            charController.center = new Vector3(0, Mathf.Lerp(ccCrouchCenterY, ccStandingCenterY, duration / maxDuration), 0);

            duration += Time.deltaTime;

            yield return null;
        }


        charController.height = ccStandingHeight;
        charController.center = new Vector3(0, ccStandingCenterY, 0);

        //yield break;
    }

    #endregion

}
