﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerController : NetworkBehaviour
{

    #region Variables

    [Header("Player Options")]
    public float playerHeight;
    public Material[] mats;

    [Header("Movement Options")]
    public float sneakSpeed;
    public float movementSpeed;
    public float runSpeed;
    public bool smooth;
    public float smoothSpeed;


    [Header("Jump Options")]
    public float jumpForce;
    public float jumpSpeed;
    public float jumpDecrease;
    public float incrementJumpFallSpeed = 0.1f;

    [Header("Gravity")]
    public float gravity = 2.5f;

    [Header("Physics")]
    public LayerMask discludePlayer;

    [Header("References")]
    public SphereCollider sphereCol;
    public Renderer rend;

    [SyncVar] private int selfColor = 0;
    [SyncVar] private GameObject objectID;
    private NetworkIdentity objNetID;


    //Private Variables

    //Movement Vectors
    private Vector3 velocity;
    private Vector3 move;
    private Vector3 vel;

    #endregion

    #region Main Methods

    private void Update()
    {
        Gravity();
        SimpleMove();
        Jump();
        FinalMove();
        GroundChecking();
        CollisionCheck();
        CheckColor();
    }

    #endregion

    #region Movement Methods

    private void SimpleMove()
    {
        move = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        velocity += move;
    }

    private void FinalMove()
    {
        if (Input.GetKey(KeyCode.LeftShift)) {

            selfColor = 2;

            Vector3 vel = new Vector3(velocity.x * runSpeed, velocity.y * movementSpeed, velocity.z * runSpeed);

            vel = transform.TransformDirection(vel);
            transform.position += vel * Time.deltaTime;

            velocity = Vector3.zero;
        }
        else if (Input.GetKey(KeyCode.LeftControl))
        {
            selfColor = 0;

            Vector3 vel = new Vector3(velocity.x * sneakSpeed, velocity.y * movementSpeed, velocity.z * sneakSpeed);

            vel = transform.TransformDirection(vel);
            transform.position += vel * Time.deltaTime;

            velocity = Vector3.zero;
        }
        else
        {

            selfColor = 1;

            Vector3 vel = new Vector3(velocity.x, velocity.y, velocity.z) * movementSpeed;
            //velocity = (new Vector3 (move.x, -currentGravity, move.z)+vel)*movementSpeed;
            //velocity = transform.TransformDirection (velocity);

            vel = transform.TransformDirection(vel);
            transform.position += vel * Time.deltaTime;

            velocity = Vector3.zero;
        }
    }

    #endregion

    #region Gravity/Grounding
    //Gravity Private Variables
    private bool grounded;
    //	private float currentGravity = 0;

    //Grounded Private Variables
    private Vector3 liftPoint = new Vector3(0, 1.2f, 0);
    private RaycastHit groundHit;
    private Vector3 groundCheckPoint = new Vector3(0, -0.87f, 0);

    private void Gravity()
    {
        if (grounded == false)
        {
            velocity.y -= gravity;
        }
        else
        {
            //currentGravity = 0;
        }
    }

    private void GroundChecking()
    {
        Ray ray = new Ray(transform.TransformPoint(liftPoint), Vector3.down);
        RaycastHit tempHit = new RaycastHit();

        if (Physics.SphereCast(ray, 0.17f, out tempHit, 20, discludePlayer))
        {
            GroundConfirm(tempHit);
        }
        else
        {
            grounded = false;
        }

    }


    private void GroundConfirm(RaycastHit tempHit)
    {

        Collider[] col = new Collider[3];
        int num = Physics.OverlapSphereNonAlloc(transform.TransformPoint(groundCheckPoint), 0.55f, col, discludePlayer);

        grounded = false;

        for (int i = 0; i < num; i++)
        {

            if (col[i].transform == tempHit.transform)
            {
                groundHit = tempHit;
                grounded = true;

                //Snapping 
                if (inputJump == false)
                {

                    Vector3 avg = new Vector3(transform.position.x, (groundHit.point.y + playerHeight / 2), transform.position.z);

                    if (!smooth)
                    {
                        transform.position = avg;
                    }
                    else
                    {

                        transform.position = Vector3.Lerp(transform.position, new Vector3(transform.position.x, (groundHit.point.y + playerHeight / 2), transform.position.z), (smoothSpeed) * Time.deltaTime);


                    }
                }

                break;

            }

        }

        if (num <= 1 && tempHit.distance <= 3.1f && inputJump == false)
        {

            if (col[0] != null)
            {
                Ray ray = new Ray(transform.TransformPoint(liftPoint), Vector3.down);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, 3.1f, discludePlayer))
                {
                    if (hit.transform != col[0].transform)
                    {
                        grounded = false;
                        return;
                    }
                }

            }

        }




    }



    #endregion

    #region Collision

    private void CollisionCheck()
    {
        Collider[] overlaps = new Collider[4];
        Collider myCollider = new Collider();
        int num = 0;
        if (sphereCol != null)
        {
            num = Physics.OverlapSphereNonAlloc(transform.TransformPoint(sphereCol.center), sphereCol.radius, overlaps, discludePlayer, QueryTriggerInteraction.UseGlobal);
            myCollider = sphereCol;
        }

        for (int i = 0; i < num; i++)
        {

            Transform t = overlaps[i].transform;
            Vector3 dir;
            float dist;

            if (Physics.ComputePenetration(myCollider, transform.position, transform.rotation, overlaps[i], t.position, t.rotation, out dir, out dist))
            {
                Vector3 penetrationVector = dir * dist;
                Vector3 velocityProjected = Vector3.Project(velocity, -dir);
                transform.position = transform.position + penetrationVector;
                vel -= velocityProjected;
            }

        }

    }

    #endregion

    #region Jumping

    private float jumpHeight = 0;
    private bool inputJump = false;

    private float fallMultiplier = -1;

    private void Jump()
    {
        bool canJump = false;

        canJump = !UnityEngine.Physics.Raycast(new Ray(transform.position, Vector3.up), playerHeight, discludePlayer);

        if (grounded && jumpHeight > 0.2f || jumpHeight <= 0.2f && grounded)
        {
            jumpHeight = 0;
            inputJump = false;
            fallMultiplier = -1;
        }

        if (grounded && canJump)
        {

            if (Input.GetKeyDown(KeyCode.Space))
            {
                inputJump = true;
                transform.position += Vector3.up * 0.2f;
                jumpHeight += jumpForce;
            }

        }
        else
        {
            if (!grounded)
            {

                jumpHeight -= (jumpHeight * jumpDecrease * Time.deltaTime) + fallMultiplier * Time.deltaTime;
                fallMultiplier += incrementJumpFallSpeed;

            }
        }

        velocity.y += jumpHeight;


    }

    #endregion

    void CheckColor()
    {
        //Debug.Log("test1");
        if(isLocalPlayer)
        {
            objectID = gameObject;
            CmdPaint(objectID, selfColor);
        }
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
        objNetID = obj.GetComponent<NetworkIdentity>();
        //objNetID.AssignClientAuthority(connectionToClient);
        RpcPaint(obj, col);
        //objNetID.RemoveClientAuthority(connectionToClient);
        //Debug.Log("test3");
    }

}