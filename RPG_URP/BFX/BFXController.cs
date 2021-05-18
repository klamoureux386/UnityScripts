
using UnityEngine;
using UnityEngine.Rendering;

public class BFXController : MonoBehaviour
{

    public bool InfiniteDecal;
    public Light DirLight;
    public bool isVR = true;
    public GameObject BloodAttach;
    public GameObject[] BloodFX;

    public LayerMask IgnoreEnemyGroundCheckMask;


    Transform GetNearestObject(Transform hit, Vector3 hitPos)
    {
        var closestPos = 100f;
        Transform closestBone = null;
        var childs = hit.GetComponentsInChildren<Transform>();

        foreach (var child in childs)
        {
            var dist = Vector3.Distance(child.position, hitPos);
            if (dist < closestPos)
            {
                closestPos = dist;
                closestBone = child;
            }
        }

        var distRoot = Vector3.Distance(hit.position, hitPos);
        if (distRoot < closestPos)
        {
            closestPos = distRoot;
            closestBone = hit;
        }
        return closestBone;
    }

    public Vector3 direction;
    int effectIdx;
    void Update()
    {
        //if (isVR)
        //{

        //    if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger) || OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger))
        //    {
        //        RaycastHit hit;
        //        if (Physics.Raycast(Dir.position, Dir.forward, out hit))
        //        {
        //            // var randRotation = new Vector3(0, Random.value * 360f, 0);
        //            // var dir = CalculateAngle(Vector3.forward, hit.normal);
        //            float angle = Mathf.Atan2(hit.normal.x, hit.normal.z) * Mathf.Rad2Deg + 180;

        //            var effectIdx = Random.Range(0, BloodFX.Length);
        //            var instance = Instantiate(BloodFX[effectIdx], hit.point, Quaternion.Euler(0, angle + 90, 0));
        //            var settings = instance.GetComponent<BFX_BloodSettings>();
        //            settings.DecalLiveTimeInfinite = InfiniteDecal;
        //            settings.LightIntencityMultiplier = DirLight.intensity;

        //            if (!InfiniteDecal) Destroy(instance, 20);

        //        }

        //    }
        //}
      //  else
        /*!{
            if (Input.GetMouseButtonDown(0))
            {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    // var randRotation = new Vector3(0, Random.value * 360f, 0);
                    // var dir = CalculateAngle(Vector3.forward, hit.normal);
                    float angle = Mathf.Atan2(hit.normal.x, hit.normal.z) * Mathf.Rad2Deg + 180;

                    //var effectIdx = Random.Range(0, BloodFX.Length);
                    if (effectIdx == BloodFX.Length) effectIdx = 0;

                    var instance = Instantiate(BloodFX[effectIdx], hit.point, Quaternion.Euler(0, angle + 90, 0));
                    effectIdx++;

                    var settings = instance.GetComponent<BFX_BloodSettings>();
                    //settings.FreezeDecalDisappearance = InfiniteDecal;
                    settings.LightIntensityMultiplier = DirLight.intensity;

                    var nearestBone = GetNearestObject(hit.transform.root, hit.point);
                    if(nearestBone != null)
                    {
                        var attachBloodInstance = Instantiate(BloodAttach);
                        var bloodT = attachBloodInstance.transform;
                        bloodT.position = hit.point;
                        bloodT.localRotation = Quaternion.identity;
                        bloodT.localScale = Vector3.one * Random.Range(0.75f, 1.2f);
                        bloodT.LookAt(hit.point + hit.normal, direction);
                        bloodT.Rotate(90, 0, 0);
                        bloodT.transform.parent = nearestBone;
                        //Destroy(attachBloodInstance, 20);
                    }

                   // if (!InfiniteDecal) Destroy(instance, 20);

                }

            }
        }*/

    }

    public void spawnBloodAtRaycastHit(RaycastHit hit, float bloodScale)
    {

        // var randRotation = new Vector3(0, Random.value * 360f, 0);
        // var dir = CalculateAngle(Vector3.forward, hit.normal);
        float angle = Mathf.Atan2(hit.normal.x, hit.normal.z) * Mathf.Rad2Deg + 180;

        //var effectIdx = Random.Range(0, BloodFX.Length);
        if (effectIdx == BloodFX.Length) effectIdx = 0;

        var instance = Instantiate(BloodFX[effectIdx], hit.point, Quaternion.Euler(0, angle + 90, 0));
        effectIdx++;

        var settings = instance.GetComponent<BFX_BloodSettings>();

        //!Scale up blood size
        instance.transform.localScale *= bloodScale;

        //!Set groundHeight per instance

        RaycastHit groundCheck;

        if (Physics.Raycast(hit.transform.position, Vector3.down, out groundCheck, 10.0f, IgnoreEnemyGroundCheckMask))
            instance.GetComponent<BFX_BloodSettings>().GroundHeight = /*transform.position.y - */groundCheck.transform.position.y;

        Debug.Log("Ground height: " + instance.GetComponent<BFX_BloodSettings>().GroundHeight);

        //settings.FreezeDecalDisappearance = InfiniteDecal;
        settings.LightIntensityMultiplier = DirLight.intensity;

        var nearestBone = GetNearestObject(hit.transform.root, hit.point);
        if (nearestBone != null)
        {
            var attachBloodInstance = Instantiate(BloodAttach);
            var bloodT = attachBloodInstance.transform;
            bloodT.position = hit.point;
            bloodT.localRotation = Quaternion.identity;
            bloodT.localScale = Vector3.one * Random.Range(0.75f, 1.2f);
            bloodT.LookAt(hit.point + hit.normal, direction);
            bloodT.Rotate(90, 0, 0);
            bloodT.transform.parent = nearestBone;
            //Destroy(attachBloodInstance, 20);

        }

        Debug.Log($"Spawned blood {effectIdx}");

        // if (!InfiniteDecal) Destroy(instance, 20);

    }


    public float CalculateAngle(Vector3 from, Vector3 to)
    {

        return Quaternion.FromToRotation(Vector3.up, to - from).eulerAngles.z;

    }

}
