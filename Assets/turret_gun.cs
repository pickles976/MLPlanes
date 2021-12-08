using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class turret_gun : MonoBehaviour
{
    public GameObject target;
    public GameObject parent;
    public float rof = 0.0003f;
    public float offset;
    float timer;
    float spread = 0.035f;
    float range = 5000.0f;

    public GameObject projectile;

    private void Start()
    {
        timer = offset;
    }

    // Update is called once per frame
    void Update()
    {
        float opposite = target.transform.position.y;
        opposite *= Random.Range(1.0f - spread, 1.0f + spread);
        float hypotenuse = (target.transform.position - transform.position).magnitude;

        float angle = Mathf.Asin(opposite / hypotenuse) * Mathf.Rad2Deg;

        transform.localRotation = Quaternion.Euler(new Vector3(90.0f-angle, 0.0f, 0.0f));

        if (hypotenuse < range)
        {

            timer += Time.deltaTime;

            if (timer > rof)
            {
                Instantiate(projectile, transform.position, transform.rotation).transform.rotation *= Quaternion.Euler(new Vector3(-90.0f + Random.Range(-spread, spread), Random.Range(-spread, spread), Random.Range(-spread, spread)));
                timer = 0.0f;
            }
        }
        
    }
}
