using UnityEngine;

// TODO FIX: For some reason breaks with moving platforms, just phases through it (from the top) 
public class EntityMovement : MonoBehaviour {
    [Header("Main")]
    public GameObject wrapper;

    [Header("Speed")]
    public float baseSpeed=7;
    public float sprintSpeed = 10;
    public float jumpSpeed=4;
    public float gravity = 9.81f;
    public float maxAcceleration = 100f;
    public bool accelOppositeFix = true;

    [Header("Movement Restrictions")]
    public float maxMovementAllowedSlope = 45f;
    
    [Header("Normal Snapping")]
    public float snapToNormalOffset = -0.4f;
    public float maxAntiSlideSlope = 45f;
    public float snapToNormalStrength = -30f;
    public float snapToNormalDist = 0.5f;
    public float speedMagnitudeFactor = 0.25f;
    public float speedMagnitudeOffset = 1.0f;
    public float verticalNormalFactor = 1.0f;
    public float verticalNormalOffset = 1.0f;

    [Header("Friction")]
    public float frictionFactor = 3f;
    public float frictionOffset = 0.2f;

    private Vector2 localWishDirection;
    private int instanceId;
    private float angle;
    private Rigidbody rb;
    private CapsuleCollider cc;
    private float speed;

    private bool jump;
    private bool grounded;
    private float lastJumpTime;
    private float summedGravity;
    private float dynamicFriction;
    private float staticFriction;
    private Vector3 tempPosition;

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
                    float invertFactor = pair.otherColliderInstanceID == instanceId ? -1f : 1f;

                    // TODO: Fix weird thing when big terrain slopey slopey penetration thingy
                    if (Vector3.Angle(normal * invertFactor, Vector3.up) > maxMovementAllowedSlope) {
                        normal.y = 0;
                        normal.Normalize();
                        pair.SetNormal(i, normal);
                        //pair.SetDynamicFriction(i, 0.1f);
                        //pair.SetStaticFriction(i, 0.1f);
                        //pair.SetSeparation(i, pair.GetSeparation(i) + 0.1f);
                    } else {
                        dynamicFriction = pair.GetDynamicFriction(i);
                        staticFriction = pair.GetStaticFriction(i);
                    }

                    pair.SetDynamicFriction(i, 0);
                    pair.SetStaticFriction(i, 0);

                    // TODO: FIX THIS WHEN PUSHING HEAVY BOXES!!
                    /*
                    Vector3 point = pair.GetPoint(i);
                    Vector3 offsettino = point - tempPosition;
                    if (offsettino.y > -0.45f && offsettino.y < 0.45) {
                        Debug.DrawRay(point, -pair.GetNormal(i));
                        //pair.SetPoint(i, tempPosition + Vector3.down * 0.4f);
                        pair.SetNormal(i, (pair.GetNormal(i) + Vector3.down * 2.0f).normalized);
                        //pair.SetNormal(i, Vector3.down);
                    }
                    //pair.SetPoint(i, point);
                    //Debug.Log();
                    */
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
        jump = true;
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
        tempPosition = rb.position;
        Vector3 point1 = rb.position + Vector3.down * cc.radius;
        Vector3 test = new Vector3(localWishDirection.x, 0f, localWishDirection.y).normalized;
        Vector3 globalWishVelocity = wrapper.transform.TransformDirection(test);
        Vector3 s1 = rb.position + Vector3.down * 0.5f;
        Vector3 s2 = rb.position + Vector3.up * 0.5f;

        // Project velocity on slope so that we can lower player speed in that direction
        Vector3 origin = rb.position + Vector3.up * snapToNormalOffset;

        // TODO: Fix weird acceleration on slopes
        // TODO: See if we want to implement friction slow down for very rough surfaces
        globalWishVelocity *= speed;
        Vector3 currentVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        // Handle some pretty simple acceleration and max acceleration
        Vector3 acceleration = globalWishVelocity - currentVelocity;

        // Makes movement "feel" more natural, even though max acceleration should be preserved, this makes it feel better imho
        float factor = 1f;
        if (Vector3.Dot(globalWishVelocity.normalized, currentVelocity.normalized) < 0f && accelOppositeFix) {
            factor = 2f;
        }

        // Clamp the acceleration to some "maxAcceleration" constant and take account the surface we're on (friction wise
        float frictionThing = Mathf.Clamp01(dynamicFriction * frictionFactor + frictionOffset);
        Vector3 accel = Vector3.ClampMagnitude(acceleration, maxAcceleration * factor * Time.fixedDeltaTime * frictionThing);
        Vector3 velocity = rb.linearVelocity + accel;

        /*
        float ae = 0.1f;
        if (Physics.SphereCast(s1 + Time.fixedDeltaTime * finalVelocity + Vector3.up * ae, cc.radius, Vector3.down, out RaycastHit hit, snapToNormalDist, ~LayerMask.GetMask("Player"))) {
            Vector3 offset = s1 + Time.fixedDeltaTime * finalVelocity + Vector3.down * hit.distance + Vector3.up * ae;
            DebugUtils.DrawSphere(s1, 0.5f, Color.blue);
            DebugUtils.DrawSphere(offset, 0.5f, Color.red);
            rb.useGravity = false;
            Vector3 gya = offset - s1;
            Debug.DrawLine(s1, offset);
            Debug.Log(gya);
            finalVelocity += gya * snapToNormalStrength;
        } else {
            rb.useGravity = true;
        }
        */

        //rb.AddForce(globalDir, ForceMode.VelocityChange);


        // Used to stop the player from sliding down slopes and to "snap" the player to the ground normal
        // TODO: Instead of "snapping" the player to the normal, take current velocity, use another capsule cast at the next physics step and check position
        bool useGravity = true;
        if (Physics.SphereCast(origin, cc.radius, Vector3.down, out RaycastHit hit, snapToNormalDist, ~LayerMask.GetMask("Player"))) {
            grounded = true;
            if (Time.fixedTime > (lastJumpTime + Time.fixedDeltaTime * 5) && !jump) {
                Vector3 offset = origin + Vector3.down * hit.distance;
                //DebugUtils.DrawSphere(offset, cc.radius, Color.white);

                //bool grounded = GetDist(rb.position, Vector3.down * 2.0f) > 0f;

                // TODO: Use a better ground check, still fuck ups sometimes
                // TODO: Fix weird glitchyness when going on non-even grounds (ex. terrain) (note: it is because the snapToNormalStrength is too strong
                // Maybe make it proportional to some factor?
                Vector3 normal = hit.normal.normalized;
                if (Vector3.Angle(normal, Vector3.up) < maxAntiSlideSlope) {
                    //temp += -hit.normal * snapToNormalStrength;
                    float diff = GetDist(origin, Vector3.down) - hit.distance;
                    //temp += -hit.normal * snapToNormalStrength * Vector3.Distance(point1, offset);
                    float fac = (1 - Vector3.Dot(normal, Vector3.up)) * verticalNormalFactor + verticalNormalOffset;
                    velocity += -normal * diff * snapToNormalStrength * fac * (globalWishVelocity.magnitude * speedMagnitudeFactor + speedMagnitudeOffset);
                    useGravity = false;
                    //Debug.DrawRay(hit.point, hit.normal);
                }
            }
        } else {
            grounded = false;
        }

        summedGravity = rb.linearVelocity.y;
        if (!useGravity) {
            summedGravity = 0f;
        } else {
            summedGravity -= gravity * Time.fixedDeltaTime;
            velocity.y = summedGravity;
        }

        // Simple jump system
        if (jump) {
            jump = false;
            if (grounded) {
                velocity.y = jumpSpeed;
                grounded = false;
                lastJumpTime = Time.fixedTime;
                //rb.position = new Vector3(rb.position.x, rb.position.y + 0.2f, rb.position.z);
            }
        }

        rb.linearVelocity = velocity;
    
    }
}
