using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RealtimeCatenaryDrawer : MonoBehaviour
{

    public Transform startPoint;
    public Transform endPoint;

    //Variables for equatiions
    //l = length of cable (must have at least equal to distance between points)
    //h = horizontal distance between start and end points
    //v = vertical distance between start and end points
    [SerializeField] float l;
    [SerializeField] float h;
    [SerializeField] float v;
    
    [SerializeField] float a = 0;

    //Additional cable length cannot be negative
    [Min(0)]
    public float additionalSlack = 5f;

    //Vars to check for realtime updates
    Vector3 lastStartPos;
    Vector3 lastEndPos;
    float lastL;


    private void Start()
    {

        lastStartPos = startPoint.position;
        lastEndPos = endPoint.position;
        lastL = l;

        calculateA();
    }

    private void Update()
    {

        //!Originally used for recalculations on move but if repositioned very slow then cable would go beyond the end point
        /*float epsilon = 0.0000000001f;

        if (Vector3.SqrMagnitude(startPoint.position - lastStartPos) > epsilon || Vector3.SqrMagnitude(endPoint.position - lastEndPos) > epsilon || l != lastL)
        {
            Debug.Log("Recalculating...");
            calculateA();
        }

        lastStartPos = startPoint.position;
        lastEndPos = endPoint.position;
        lastL = l;*/

        //?Change wire length based on distance
        l = Vector3.Distance(startPoint.position, endPoint.position) + additionalSlack;

        calculateA();

        //Currently sometimes draws 1 extra interval
        piecewiseDrawLine(50);

    }

    private void calculateA() {

        if (l < Vector3.Distance(startPoint.position, endPoint.position))
        {
            Debug.LogError("Cable must be at least as long as the distance between points");
            l = Vector3.Distance(startPoint.position, endPoint.position);
            return;
        }

        h = Mathf.Abs(Vector3.Distance(startPoint.position, new Vector3(endPoint.position.x, startPoint.position.y, endPoint.position.z))); //horizontal distance between start point and end
        v = endPoint.position.y - startPoint.position.y; //vertical distance between start and end

        const float IntervalStep = 1.0f;

        int iterations = 0;

        a = 0;

        do
        {
            a += IntervalStep;

            iterations++;
        }
        while (Mathf.Sqrt(Mathf.Pow(l, 2) - Mathf.Pow(v, 2)) < 2 * a * (float)System.Math.Sinh(h / (2 * a)) && iterations < 100000);

        const float Precision = 0.0001f;

        float a_prev = a - IntervalStep;
        float a_next = a;

        iterations = 0;

        //find a
        do
        {
            a = (a_prev + a_next) / 2f;

            if (Mathf.Sqrt(Mathf.Pow(l, 2) - Mathf.Pow(v, 2)) < 2 * a * (float)System.Math.Sinh(h / (2 * a)))
                a_prev = a;
            else
                a_next = a;

            iterations++;

        } while (a_next - a_prev > Precision && iterations < 10000);

    }

    private void piecewiseDrawLine(int intervals) {

        Vector3 horizDirToEndPoint = endPoint.position - startPoint.position;
        horizDirToEndPoint.y = 0;
        horizDirToEndPoint.Normalize();

        float currentX = 0;

        float scaleFactor = h / intervals;

        float nextX = 0;

        float x1 = 0;
        float x2 = h;
        float y1 = 0;
        float y2 = v;

        float p = (x1 + x2 - (a * Mathf.Log((l + v) / (l - v)))) / 2;
        float q = (y1 + y2 - (l * (1/(float)System.Math.Tanh((h/(2*a)))))) / 2;

        Debug.Log($"p: {p}, q: {q}");

        while (nextX < h) {

            nextX = currentX + scaleFactor;

            float currentY = a * (float)System.Math.Cosh((currentX - p) / a) + q;
            float nextY = a * (float)System.Math.Cosh((nextX - p) / a) + q;

            Debug.Log($"current Y: {currentY}, next Y: {nextY}");

            //Only works on 2D
            //Debug.DrawLine(startPoint.position + (Vector3.right * currentX) + (Vector3.up * currentY), startPoint.position + (Vector3.right * nextX) + (Vector3.up * nextY), Color.red);
            //Attempt at 3D
            Debug.DrawLine(startPoint.position + (horizDirToEndPoint * currentX) + (Vector3.up * currentY), startPoint.position + (horizDirToEndPoint * nextX) + (Vector3.up * nextY), Color.red);

            currentX += scaleFactor;

        }
    

    }


}
