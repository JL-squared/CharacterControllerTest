using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.UI.Image;

public class EntityMovement : MonoBehaviour {
    [Header("Main")]
    public GameObject wrapper;

    [Header("Speed")]
    public float baseSpeed=7;
    public float sprintSpeed = 10;
    public float jumpSpeed=4;
    public float maxAcceleration = 100f;
    public bool accelOppositeFix = true;

    [Header("Movement Restrictions")]
    public float maxMovementAllowedSlope = 45f;
    
    [Header("Normal Snapping")]
    public float snapToNormalOffset = -0.4f;
    public float maxAntiSlideSlope = 45f;
    public float snapToNormalStrength = -30f;
    public float snapToNormalDist = 0.5f;
    
    private Vector2 localWishDirection;
    private int instanceId;
    private float angle;
    private Rigidbody rb;
    private CapsuleCollider cc;
    private float speed;

    private void Start() {
        rb = GetComponent<Rigidbody>();
        cc = GetComponent<CapsuleCollider>();
        cc.hasModifiableContacts = true;
        Physics.ContactModifyEvent += Physics_ContactModifyEvent;
        instanceId = cc.GetInstanceID();
        speed = baseSpeed;
    }

    public void OnDisable() {
        Physics.ContactModifyEvent -= Physics_ContactModifyEvent;
    }

    private void Physics_ContactModifyEvent(PhysicsScene arg1, Unity.Collections.NativeArray<ModifiableContactPair> pairs) {
        foreach (var pair in pairs) {
            if (pair.colliderInstanceID == instanceId || pair.otherColliderInstanceID == instanceId) {
                for (int i = 0; i < pair.contactCount; ++i) {
                    Vector3 normal = pair.GetNormal(i);
                    
                    // TODO: Fix weird thing when big terrain slopey slopey penetration thingy
                    if (Vector3.Angle(normal, Vector3.up) > maxMovementAllowedSlope) {
                        normal.y = 0;
                        normal.Normalize();
                        pair.SetNormal(i, normal);
                        //pair.SetDynamicFriction(i, 0.1f);
                        //pair.SetStaticFriction(i, 0.1f);
                        //pair.SetSeparation(i, pair.GetSeparation(i) + 0.1f);
                    }
                }
            }
        }
    }

    public void QueueMove(Vector2 localWishDirection) {
        this.localWishDirection = localWishDirection;
    }

    public void QueueSetRotate(float angle) {
        this.angle = angle;
    }

    public void ToggleSprint(bool sprint) {
        speed = sprint ? sprintSpeed : baseSpeed;
    }

    public void ToggleCrouch(bool crouch) {
    }

    public void QueueJump() {
    }

    private void Update() {
        wrapper.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
    }

    private float GetDist(Vector3 origin, Vector3 direction) {
        if (Physics.Raycast(origin, direction, out RaycastHit hit, ~LayerMask.GetMask("Player"))) {
            return hit.distance;
        }

        return 0f;
    }

    private void FixedUpdate() {
        Vector3 point1 = rb.position + Vector3.down * cc.radius;
        float oldGravity = rb.linearVelocity.y;
        Vector3 test = new Vector3(localWishDirection.x, 0f, localWishDirection.y).normalized;
        Vector3 globalWishVelocity = wrapper.transform.TransformDirection(test);

        Vector3 origin = rb.position + Vector3.up * snapToNormalOffset;
        if (Physics.SphereCast(origin, cc.radius, Vector3.down, out RaycastHit hit3, snapToNormalDist, ~LayerMask.GetMask("Player"))) {
            Vector3 temp = Vector3.ProjectOnPlane(globalWishVelocity, hit3.normal);
            globalWishVelocity = temp;
            globalWishVelocity.y = 0f;
        }

        // TODO: Fix weird acceleration on slopes
        globalWishVelocity *= speed;
        Vector3 currentVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);



        // Handle some pretty simple acceleration and max acceleration
        Vector3 acceleration = globalWishVelocity - currentVelocity;

        // Makes movement "feel" more natural, even though max acceleration should be preserved, this makes it feel better imho
        float factor = 1f;
        if (Vector3.Dot(globalWishVelocity.normalized, currentVelocity.normalized) < 0f && accelOppositeFix) {
            factor = 2f;
        }

        // Clamp the acceleration to some "maxAcceleration" constant
        Vector3 finalVelocity = Vector3.ClampMagnitude(acceleration, maxAcceleration * factor * Time.fixedDeltaTime);

        /*
        // Simple jump system
        if (jump) {
            jump = false;
            finalVelocity.y = jumpSpeed;
            ground = null;
        }
        */

        //rb.AddForce(globalDir, ForceMode.VelocityChange);
        rb.linearVelocity += finalVelocity;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, oldGravity, rb.linearVelocity.z);

        // Used to stop the player from sliding down slopes and to "snap" the player to the ground normal
        if (Physics.SphereCast(origin, cc.radius, Vector3.down, out RaycastHit hit, snapToNormalDist, ~LayerMask.GetMask("Player"))) {
            Vector3 offset = origin + Vector3.down * hit.distance;
            DebugUtils.DrawSphere(offset, cc.radius, Color.white);

            //bool grounded = GetDist(rb.position, Vector3.down * 2.0f) > 0f;

            // TODO: Use a better ground check, still fuck ups sometimes
            // TODO: Fix weird glitchyness when going on non-even grounds (ex. terrain) (note: it is because the snapToNormalStrength is too strong
            // Maybe make it proportional to some factor?
            if (Vector3.Angle(hit.normal, Vector3.up) < maxAntiSlideSlope) {
                //temp += -hit.normal * snapToNormalStrength;
                float diff = GetDist(origin, Vector3.down) - hit.distance;
                //temp += -hit.normal * snapToNormalStrength * Vector3.Distance(point1, offset);
                rb.linearVelocity += -hit.normal * diff * snapToNormalStrength;
                rb.useGravity = false;
                Debug.DrawRay(hit.point, hit.normal);
            } else {
                rb.useGravity = true;
            }
        } else {
            rb.useGravity = true;
        }
    }
}
