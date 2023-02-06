using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Detextion ray is lunched from the plane object
//each ray collect the normal vector and calculate the average of them at last.
//erase all Debug.DrawRay if unnecessary
public class NormalDetector : MonoBehaviour
{
    [Tooltip("it won't reach the target if too short")]
    [SerializeField] public float rayLength = 10f;

    [Tooltip("The object where normal will be detected")]
    [SerializeField] private LayerMask targetLayer;

    [Tooltip("DivisionN * DivisionN of ray will be lanched")]
    [SerializeField] public int divisionN = 30;

    [Tooltip("0 for one corner, and 1, 2 should be the adjacenting corner of 0")]
    [SerializeField] private GameObject[] edgesObjects;
    private Vector3[] edges;

    public Vector3 direction { get; private set; }

    //results
    public Vector3 detectedNormal { get; private set; }
    public Vector3 detectedPoint { get; private set; }

    private void Update()

    {
        Vector3[] detectionInfo = Detect();
        Debug.DrawRay(detectionInfo[1], detectionInfo[0], Color.green);
    }

    private void UpdatePosition()
    {
        //positions of edges
        edges = new Vector3[3];
        for (int cnt = 0; cnt < 3; cnt++)
        {
            edges[cnt] = edgesObjects[cnt].transform.position;
        }

        //direction
        direction = transform.up.normalized;
    }

    /// <summary>
    /// Detect normal of the target by lunching bunches of rays
    /// </summary>
    /// <returns>
    /// [0]: average normals
    /// [1]: average hitted points
    /// </returns> 
    /// 
    public Vector3[] Detect()
    {
        UpdatePosition();

        Vector3 xVecUnit = (edges[1] - edges[0]) / divisionN;
        Vector3 yVecUnit = (edges[2] - edges[0]) / divisionN;

        Vector3[] normals = new Vector3[(divisionN + 1) * (divisionN + 1)];
        Vector3[] hittedPoints = new Vector3[(divisionN + 1) * (divisionN + 1)];

        //lunch ray from each luncher point
        for (int cnt1 = 0; cnt1 <= divisionN; cnt1++)
        {
            for (int cnt2 = 0; cnt2 <= divisionN; cnt2++)
            {
                Vector3[] hitInfo = LanchRay(edges[0] + xVecUnit * cnt1 + yVecUnit * cnt2);
                normals[cnt1 * (divisionN + 1) + cnt2] = hitInfo[0];
                hittedPoints[cnt1 * (divisionN + 1) + cnt2] = hitInfo[1];
            }
        }

        //get average
        detectedNormal = GetAverageVector(normals, false);
        detectedPoint = GetAverageVector(hittedPoints, false);

        return new Vector3[] { detectedNormal, detectedPoint };
    }

    /// <summary>
    /// Lunch a single ray and get hit information
    /// </summary>
    /// <param name="startPoint"> where ray starts</param>
    /// <returns>
    /// [0]: normal
    /// [1]: hitted point
    /// returns Vector3.zero when don't hit
    /// </returns>
    private Vector3[] LanchRay(Vector3 startPoint)
    {
        //Debug.DrawRay(startPoint, direction * rayLength);

        RaycastHit raycastHit;

        bool hitted = Physics.Raycast(startPoint,
            direction,
            out raycastHit,
            maxDistance: rayLength,
            layerMask: targetLayer);
        if (hitted)
        {
            Debug.DrawRay(raycastHit.point, raycastHit.normal, Color.blue);
            //hitted 
            return new Vector3[] { raycastHit.normal, raycastHit.point };
        }

        //don't hit
        return new Vector3[] { Vector3.zero, Vector3.zero };
    }

    private Vector3 GetAverageVector(Vector3[] vectors, bool shouldIncludeZero = false)
    {
        Vector3 sumVector = Vector3.zero;

        int zeros = 0;

        foreach (Vector3 vector in vectors)
        {
            if (!shouldIncludeZero && vector == Vector3.zero)
            {
                //shouldn't include zero vector
                zeros++;
            }
            else
            {
                sumVector += vector;
            }
        }

        if (vectors.Length - zeros == 0)
        {
            return Vector3.zero;
        }
        else
        {
            return sumVector / (vectors.Length - zeros);
        }
    }
}