using UnityEngine;

public class EntityMovement : MonoBehaviour {
    public float speed;
    private Vector2 localWishDirection;
    private float angle;
    public float height = 2.0f;
    public float radius = 0.5f;

    private Vector3 nextPosition;
    private Vector3 prevPosition;
    private CapsuleCollider cc;
    private Vector3 velocity;

    private void Start() {
        localWishDirection = Vector2.zero;
        angle = 0;

        nextPosition = transform.position;
        prevPosition = transform.position;

        cc = GetComponent<CapsuleCollider>();
        cc.radius = radius;    
        cc.height = height;
    }

    public void QueueMove(Vector2 localWishDirection) {
        this.localWishDirection = localWishDirection;
    }

    public void QueueSetRotate(float angle) {
        this.angle = angle;
    }

    private void Update() {
        Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
        float interpolator = Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime);
        Vector3 position = Vector3.Lerp(prevPosition, nextPosition, interpolator);
        transform.SetPositionAndRotation(position, rotation);
    }


    private void OnDrawGizmosSelected() {
        Gizmos.DrawWireSphere(transform.position + Vector3.down * height * 0.25f, radius);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * height * 0.25f, radius);
    }

    private void FixedUpdate() {
        prevPosition = nextPosition;

        Vector2 clamped = Vector2.ClampMagnitude(localWishDirection, 1);
        Vector3 flat = new Vector3(clamped.x, 0f, clamped.y);
        Vector3 wishVelocity = transform.TransformDirection(flat) * speed;

        // gravity check
        {
            Vector3 s1_1 = nextPosition + Vector3.down * height * 0.25f;
            Vector3 s2_1 = nextPosition + Vector3.up * height * 0.25f;
            if (Physics.CapsuleCast(s1_1, s2_1, radius, Vector3.down, out RaycastHit hit, 0.1f)) {
                MyLogger.Instance.Log(hit.point, hit.distance);
                velocity.y = 0f;
            } else {
                velocity.y -= 10f * Time.fixedDeltaTime;
            }
        }

        velocity.x = wishVelocity.x;
        velocity.z = wishVelocity.z;

        nextPosition += velocity * Time.fixedDeltaTime;

        /*
        // move in direction (snap to ground and proper terrain handle...)
        {
            // check if there's something in front of us, above us 
            float upFactor = 0.15f;
            Vector3 s1_1 = nextPosition + Vector3.down * height * 0.25f + Vector3.up * upFactor;
            Vector3 s2_1 = nextPosition + Vector3.up * height * 0.25f + Vector3.up * upFactor;
            if (Physics.CapsuleCast(s1_1, s2_1, radius, Vector3.down, out RaycastHit hit, height + upFactor * 2f)) {
                MyLogger.Instance.Log(hit.point, hit.distance);

                float newY = hit.point.y + height * 0.5f;

                if (Mathf.Abs(nextPosition.y - newY) < upFactor) {
                    nextPosition.y = newY + 0.02f;
                }

                if (hit.normal.y < 0.9) {
                    velocity.y = -2f;
                    velocity.x = 0f;
                    velocity.z = 0f;
                }
            }
        }
        */


        {
            Vector3 s1 = nextPosition + Vector3.down * height * 0.25f;
            Vector3 s2 = nextPosition + Vector3.up * height * 0.25f;

            Collider[] colliders = Physics.OverlapCapsule(s1, s2, radius);

            foreach (Collider collider in colliders) {
                if (collider == cc) continue;

                if (Physics.ComputePenetration(cc, nextPosition, Quaternion.identity, collider, collider.transform.position, collider.transform.rotation, out Vector3 dir, out float dist)) {
                    if (collider.attachedRigidbody == null) {
                        nextPosition += dir * dist;
                    } else {
                        nextPosition += dir * dist;
                        nextPosition += collider.attachedRigidbody.linearVelocity * Time.fixedDeltaTime;
                    }
                }
            }
        }
    }
}
