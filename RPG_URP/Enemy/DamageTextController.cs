using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DamageTextController : MonoBehaviour
{
    public GameObject damageTextPrefab;

    private float defaultNumberDuration = 1.5f;
    private GameObject player;

    private void Awake()
    {
        //can change later, for testing now
        player = GameObject.Find("Player");
    }

    public void createDamageNumberText(float damage)
    {

        //Debug.Log("Creating damage number");

        //Spawn damage Text as independent gameObject at transform position
        GameObject damageText = Instantiate(damageTextPrefab, transform.position, Quaternion.identity);
        TextMeshPro tmpComponent = damageText.transform.GetChild(0).GetComponent<TextMeshPro>();
        tmpComponent.SetText(damage.ToString());
        //damageText.transform.GetChild(0).GetComponent<TextMeshPro>().SetText(damage.ToString());

        StartCoroutine(driftAndFadeNumber(damageText, tmpComponent, defaultNumberDuration));

    }

    public void createDamageNumberText(float damage, Vector3 hitLocation)
    {

        //Debug.Log("Creating damage number");

        //Spawn damage Text as independent gameObject at transform position
        GameObject damageText = Instantiate(damageTextPrefab, hitLocation, Quaternion.identity);
        TextMeshPro tmpComponent = damageText.transform.GetChild(0).GetComponent<TextMeshPro>();
        tmpComponent.SetText(damage.ToString());
        //damageText.transform.GetChild(0).GetComponent<TextMeshPro>().SetText(damage.ToString());

        StartCoroutine(driftAndFadeNumber(damageText, tmpComponent, defaultNumberDuration));

    }


    IEnumerator driftAndFadeNumber(GameObject damageText, TextMeshPro tmpComponent, float duration)
    {

        Vector3 startingPos = damageText.transform.position;
        Vector3 targetPos = startingPos + (Vector3.up * 2);
        float elapsedTime = 0f;

        float t;
        //float t2;

        //EaseOutQuadratic lerp: https://easings.net/
        while (elapsedTime < duration)
        {


            Vector3 lookPos = damageText.transform.position - Camera.main.transform.position;
            //lookPos.y = 0; //Turn this back on if number should remain vertically aligned
            damageText.transform.rotation = Quaternion.LookRotation(lookPos);

            t = elapsedTime / duration;
            //!ease out quintic
            t = 1 - (1 - t) * (1 - t) * (1 - t) * (1 - t) * (1 - t);

            //!can use separate time for alpha if desired
            /*t2 = elapsedTime / duration;
            //!ease in cubic
            t = t * t * t;*/


            damageText.transform.position = Vector3.Lerp(startingPos, targetPos, t);
            tmpComponent.alpha = Mathf.Lerp(1, 0, t);

            elapsedTime += Time.deltaTime;

            yield return null;
        }

        //damageText.transform.position = targetPos;

        Destroy(damageText);

    }

}
