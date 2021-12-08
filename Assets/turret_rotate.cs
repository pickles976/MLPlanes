using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class turret_rotate : MonoBehaviour
{

    public GameObject target;
    float spread = 0.035f;

    // Update is called once per frame
    void Update()
    {
        Vector2 v2 = new Vector2(target.transform.position.x, target.transform.position.z);
        v2 += new Vector2(target.GetComponent<Rigidbody>().velocity.x, target.GetComponent<Rigidbody>().velocity.z);
        v2 *= Random.Range(1.0f - spread, 1.0f + spread);

        float angle = Vector2.SignedAngle(v2, Vector2.up);

        transform.eulerAngles = new Vector3(0.0f,angle,0.0f);
    }
}
