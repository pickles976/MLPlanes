using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;

public class BombingAgent : Agent
{
    public ControlSurface elevator;
    public ControlSurface aileronLeft;
    public ControlSurface aileronRight;
    public ControlSurface rudder;
    public Engine engine;

    public WeaponDropper[] weapons;

    public Rigidbody Rigidbody { get; internal set; }

    private float throttle = 1.0f;
    private bool yawDefined = false;

    public GameObject target;

    //ml vars
    Vector3 startPos;
    Vector3 startVel;
    Quaternion startRot;
    Vector3 startAngular;
    float[] actions;
    Vector3 obstacle;
    GameObject[] missiles;

    public override void Initialize()
    {
        Rigidbody = GetComponent<Rigidbody>();
        startPos = transform.position;
        startVel = transform.forward * 150.0f;
        startRot = transform.rotation;
        startAngular = Rigidbody.angularVelocity;
        obstacle = Vector3.zero;
    }

    //send observations to the academy
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.position);
        sensor.AddObservation(transform.rotation.eulerAngles);
        sensor.AddObservation(Rigidbody.velocity);
        sensor.AddObservation(throttle);
        sensor.AddObservation(elevator.targetDeflection);
        sensor.AddObservation(aileronLeft.targetDeflection);
        sensor.AddObservation(aileronRight.targetDeflection);
        sensor.AddObservation(rudder.targetDeflection);
        sensor.AddObservation(target.transform.position);
    }

    //output actions
    public override void OnActionReceived(float[] vectorAction)
    {
        var vert = Mathf.Clamp(vectorAction[0], -1f, 1f);
        var hor = Mathf.Clamp(vectorAction[1], -1f, 1f);
        var yaw = Mathf.Clamp(vectorAction[2], -1f, 1f);
        var accel = Mathf.Clamp(vectorAction[3], -1f, 1f);
        var shoot = vectorAction[4];

        elevator.targetDeflection = -vert;
        aileronLeft.targetDeflection = -hor;
        aileronRight.targetDeflection = hor;
        rudder.targetDeflection = yaw;

        throttle += accel / 30.0f;
        throttle = Mathf.Clamp(throttle, 0.0f, 1.0f);

        //fire weapons
            if (shoot > 0.0f)
            {
            SetReward(-0.25f);
                foreach (WeaponDropper dropper in weapons)
                {
                    dropper.Fire(Rigidbody.GetPointVelocity(dropper.transform.position));
                }
            }

        //punish the longer the task takes
        SetReward(-0.001f);

        //dont go out of bounds
        if ((transform.position - target.transform.position).magnitude > 6000.0f)
        {
            SetReward(-1.0f);
            EndEpisode();
        }

    }

    //check for crashes or hit by bullet
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == 8)
        {
            SetReward(-1.0f);
            EndEpisode();
        }
        else if (other.gameObject.layer == 12)
        {
            SetReward(-1.0f);
            Debug.Log("hit!");
        }
    }

    public override void Heuristic(float[] actionsOut)
    {
        if (elevator != null)
        {
            actionsOut[0] = -Input.GetAxis("Vertical");
        }
        if (aileronLeft != null)
        {
            actionsOut[1] = -Input.GetAxis("Horizontal");
        }
        if (aileronRight != null)
        {
            actionsOut[1] = Input.GetAxis("Horizontal");
        }
        if (rudder != null && yawDefined)
        {
            // YOU MUST DEFINE A YAW AXIS FOR THIS TO WORK CORRECTLY.
            // Imported packages do not carry over changes to the Input Manager, so
            // to restore yaw functionality, you will need to add a "Yaw" axis.
            actionsOut[2] = Input.GetAxis("Yaw");
        }

        if (engine != null)
        {
            // Fire 1 to speed up, Fire 2 to slow down. Make sure throttle only goes 0-1.
            throttle += Input.GetAxis("Fire1") * Time.deltaTime;
            throttle -= Input.GetAxis("Fire2") * Time.deltaTime;
            throttle = Mathf.Clamp01(throttle);

            actionsOut[3] = throttle;
        }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                actionsOut[4] = 1.0f;
            }
            else
            {
                actionsOut[4] = -1.0f;
            }
    }

    public override void OnEpisodeBegin()
    {
        ResetScene();
    }

    private void ResetScene()
    {
        transform.position = startPos;
        Rigidbody.velocity = startVel;
        transform.rotation = startRot;
        Rigidbody.angularVelocity = startAngular;
        elevator.targetDeflection = 0;
        aileronLeft.targetDeflection = 0;
        aileronRight.targetDeflection = 0;
        rudder.targetDeflection = 0;
        throttle = 1.0f;

        //reset weapons
        if (weapons.Length > 0)
        {
                foreach (WeaponDropper dropper in weapons)
                {
                    dropper.cooldown = 0.0f;
                }
        }

        //delete all child missiles in the air
        missiles = GameObject.FindGameObjectsWithTag("missile");
        foreach (GameObject missile in missiles)
        {
            if (missile.GetComponent<missile_training>().parent == gameObject)
            {
                Destroy(missile);
            }
        }

    }

    public void MissileHit(Vector3 hit)
    {

        float distance = (hit - target.transform.position).magnitude;

        if (distance < 30.0f)
        {
            SetReward(100.0f);
        }
        else
        {
            SetReward(1000.0f/distance);
        }

        EndEpisode();
    }

    //GUI
    //===================================//

    private float CalculatePitchG()
    {
        // Angular velocity is in radians per second.
        Vector3 localVelocity = transform.InverseTransformDirection(Rigidbody.velocity);
        Vector3 localAngularVel = transform.InverseTransformDirection(Rigidbody.angularVelocity);

        // Local pitch velocity (X) is positive when pitching down.

        // Radius of turn = velocity / angular velocity
        float radius = (Mathf.Approximately(localAngularVel.x, 0.0f)) ? float.MaxValue : localVelocity.z / localAngularVel.x;

        // The radius of the turn will be negative when in a pitching down turn.

        // Force is mass * radius * angular velocity^2
        float verticalForce = (Mathf.Approximately(radius, 0.0f)) ? 0.0f : (localVelocity.z * localVelocity.z) / radius;

        // Express in G (Always relative to Earth G)
        float verticalG = verticalForce / -9.81f;

        // Add the planet's gravity in. When the up is facing directly up, then the full
        // force of gravity will be felt in the vertical.
        verticalG += transform.up.y * (Physics.gravity.y / -9.81f);

        return verticalG;
    }

    private void OnGUI()
    {
        const float msToKnots = 1.94384f;
        GUI.Label(new Rect(10, 40, 300, 20), string.Format("Speed: {0:0.0} knots", Rigidbody.velocity.magnitude * msToKnots));
        GUI.Label(new Rect(10, 60, 300, 20), string.Format("Throttle: {0:0.0}%", throttle * 100.0f));
        GUI.Label(new Rect(10, 80, 300, 20), string.Format("G Load: {0:0.0} G", CalculatePitchG()));
    }



}