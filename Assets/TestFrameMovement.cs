using UnityEngine;

public class TestFrameMovement : MonoBehaviour {
    public float speed;
    private Vector3 start;

    private void Start() {
        start = transform.position;
    }


    // Update is called once per frame
    void FixedUpdate() {
        float fac = 1.0f;
        if (Time.fixedTime % 2 < 1) {
            fac = -1;
        }

        GetComponent<Rigidbody>().linearVelocity = Vector3.forward * speed * fac;
    }
}
