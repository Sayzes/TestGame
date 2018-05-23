using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Networking;

public class FOVDetection : NetworkBehaviour
{
    public GameObject[] Players;
    public Transform playerT;
    public float maxAngle;
    public float maxRadius;

    public Renderer rend;
    public Material[] mats;
    [SyncVar] private int slefColor = 0;
    [SyncVar] private GameObject objectID;
    private NetworkIdentity objNetID;

    private bool isInFov = false;

    private void Update()
    {
        Players = GameObject.FindGameObjectsWithTag("Player");

        GetPlayer();

        isInFov = inFOV(transform, playerT, maxAngle, maxRadius);

        CheckColor();
    }

    private void GetPlayer()
    {

        float shortest = float.PositiveInfinity;
        Transform closest = null;

        foreach (GameObject player in Players)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < shortest)
            {
                shortest = distance;
                closest = player.transform;
                playerT = closest;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxRadius);

        Vector3 fovLine1 = Quaternion.AngleAxis(maxAngle, transform.up) * transform.forward * maxRadius;
        Vector3 fovLine2 = Quaternion.AngleAxis(-maxAngle, transform.up) * transform.forward * maxRadius;

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, fovLine1);
        Gizmos.DrawRay(transform.position, fovLine2);

        Gizmos.color = Color.black;
        Gizmos.DrawRay(transform.position, transform.forward * maxRadius);

        if(playerT != null)
        {
            if (!isInFov)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(transform.position, (playerT.position - transform.position).normalized * maxRadius);
                slefColor = 0;
                //Debug.Log("red");
            }
            else 
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(transform.position, (playerT.position - transform.position).normalized * maxRadius);
                slefColor = 1;
                //Debug.Log("green");
            }
        }


    }

    public static bool inFOV(Transform checkingObject, Transform target, float maxAngle, float maxRadius)
    {
        Collider[] overlaps = new Collider[20];

        int count = Physics.OverlapSphereNonAlloc(checkingObject.position, maxRadius, overlaps);

        for (int i = 0; i < count + 1; i++)
        {

            if (overlaps[i] != null)
            {

                if (overlaps[i].transform == target)
                {

                    Vector3 directionBetween = (target.position - checkingObject.position).normalized;
                    directionBetween.y *= 0;

                    float angle = Vector3.Angle(checkingObject.forward, directionBetween);

                    if (angle <= maxAngle)
                    {

                        Ray ray = new Ray(checkingObject.position, target.position - checkingObject.position);
                        RaycastHit hit;

                        if (Physics.Raycast(ray, out hit, maxRadius))
                        {

                            if (hit.transform == target)
                            {
                                return true;
                            }

                        }

                    }

                }
            }
        }

        return false;
    }

    void CheckColor()
    {
        //Debug.Log("test1");

        objectID = gameObject;
        CmdPaint(objectID, slefColor);
        //Debug.Log("test2");
    }

    [ClientRpc]
    void RpcPaint(GameObject obj, int col)
    {
        rend.material = mats[col];
        //Debug.Log("test4");
    }

    [Command]
    void CmdPaint(GameObject obj, int col)
    {
        //objNetID = obj.GetComponent<NetworkIdentity>();
        //objNetID.AssignClientAuthority(connectionToClient);
        RpcPaint(obj, col);
        //objNetID.RemoveClientAuthority(connectionToClient);
        //Debug.Log("test3");
    }
}