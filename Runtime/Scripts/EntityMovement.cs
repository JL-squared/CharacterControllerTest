using UnityEngine;

public class EntityMovement : MonoBehaviour {
    public float speed;
    private Vector2 localWishDirection;
    private float angle;
    public float height = 2.0f;
    public float radius = 0.5f;

    private Vector3 nextPosition;
    private Vector3 prevPosition;
    private Vector3 velocity;

    private void Start() {
        speed = 7;
        localWishDirection = Vector2.zero;
        angle = 0;

        nextPosition = transform.position;
        prevPosition = transform.position;
        velocity = Vector3.zero;
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

        Vector3 s1 = prevPosition + Vector3.down * height * 0.25f;
        Vector3 s2 = prevPosition + Vector3.up * height * 0.25f;


        // gravity check
        {
            if (Physics.CapsuleCast(s1, s2, radius, Vector3.down, out RaycastHit hit, 0.1f)) {
                MyLogger.Instance.Log(hit.point, hit.distance);
                velocity.y = 0f;
            } else {
                velocity.y -= 10f * Time.fixedDeltaTime;
            }
        }

        /*
        // movement check
        {
            if (Physics.CapsuleCast(s1, s2, radius, wishVelocity, out RaycastHit hit2, 0.1f)) {
                velocity.x = 0f; 
                velocity.z = 0f;

                Rigidbody other = hit2.rigidbody;

                if (other != null) {
                    other.AddForceAtPosition(wishVelocity, hit2.point, ForceMode.VelocityChange);
                }
            } else {
                velocity.x = wishVelocity.x;
                velocity.z = wishVelocity.z;
            }
        }
        */

        float maxPushStrength = 0.1f;
        float pushStength = 10f;

        {
            //s1 -= move.normalized;
            //s2 -= move.normalized;
            DebugUtils.DrawSphere(s1, radius, Color.yellow);
            DebugUtils.DrawSphere(s2, radius, Color.yellow);
            velocity.x = projected.x;
            velocity.z = projected.z;
        }

        nextPosition = prevPosition + velocity * Time.fixedDeltaTime;
    }
}
