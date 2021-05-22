using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DrawNavmeshPathDebug : MonoBehaviour
{
    public static Vector3[] path = new Vector3[0];

    public bool active = true;

    private LineRenderer line;
    private NavMeshAgent agent;

    void Start()
    {
        line = GetComponent<LineRenderer>();
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (active)
            line.enabled = true;
        else
            line.enabled = false;
    }

    public void drawPath(NavMeshPath path) {

        line.SetPosition(0, agent.transform.position);

        if (path.corners.Length < 2) //if the path has 1 or no corners, there is no need
            return;

        //line.SetVertexCount(path.corners.Length); //set the array of positions to the amount of corners
        line.positionCount = path.corners.Length; //set the array of positions to the amount of corners

        for (var i = 1; i < path.corners.Length; i++)
        {
            line.SetPosition(i, path.corners[i]); //go through each corner and set that to the line renderer's position
        }

    }

}
