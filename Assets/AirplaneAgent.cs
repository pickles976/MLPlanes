using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;

public class AirplaneAgent : Agent
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

    public GameObject marker1;
    public GameObject marker2;

    //ml vars
    Vector3 startPos;
    Vector3 startVel;
    Quaternion startRot;
    Vector3 startAngular;
    float[] actions;
    Vector3 obstacle;

    public override void Initialize()
    {
        Rigidbody = GetComponent<Rigidbody>();
        startPos = transform.position;
        startVel = transform.forward * 150.0f;
        startRot = transform.rotation;
        startAngular = Rigidbody.angularVelocity;
        obstacle = Vector3.zero;

        actions = new float[4];
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
        sensor.AddObservation(marker1.transform.position);
        sensor.AddObservation(marker2.transform.position);
    }

    //output actions
    public override void OnActionReceived(float[] vectorAction)
    {
        var vert = Mathf.Clamp(vectorAction[0], -1f, 1f);
        var hor = Mathf.Clamp(vectorAction[1], -1f, 1f);
        var yaw = Mathf.Clamp(vectorAction[2], -1f, 1f);
        var accel = Mathf.Clamp(vectorAction[3], -1f, 1f);

            elevator.targetDeflection = -vert;
            aileronLeft.targetDeflection = -hor;
            aileronRight.targetDeflection = hor;
            rudder.targetDeflection = yaw;

            throttle += accel / 30.0f;
            throttle = Mathf.Clamp(throttle, 0.0f, 1.0f);


        //reward for low height
        if (transform.position.y < 100.0f)
        {
            SetReward(Mathf.Clamp01(1.0f/ transform.position.y));
        }
        else
        {
            SetReward(-transform.position.y/1000.0f);
        }

        //reward low throttle
        SetReward(0.01f * Mathf.Clamp01(1 / (100.0f * (throttle + 0.01f))));

        //reward for approaching points
        SetReward(Mathf.Clamp01(50.0f / (transform.position - marker1.transform.position).magnitude));
        SetReward(Mathf.Clamp01(50.0f / (transform.position - ((marker1.transform.position + marker2.transform.position) / 2)).magnitude));
        SetReward(Mathf.Clamp01(100.0f / (transform.position - marker2.transform.position).magnitude));

        //reward less rolling
        SetReward(0.01f * Mathf.Clamp01(1 / (1 + Mathf.Abs(transform.rotation.eulerAngles.z))));

        //reward complete stop
        if (Rigidbody.velocity.magnitude <= 20.0f)
        {
            if (Mathf.Abs(transform.rotation.eulerAngles.z) < 15.0f)
            {
                SetReward(200.0f);
            }
            EndEpisode();
        }

        //dont go out of bounds
        /*
        if (transform.position.y < 0 || transform.position.x > marker2.transform.position.x || transform.position.x < -5000.0f || transform.position.z > -1800.0f || transform.position.z < -2600.0f || transform.position.y > 1000.0f)
        {
            SetReward(-1.0f);
            EndEpisode();
        }
        */

    }

    //check for crashes
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == 8)
        {
            SetReward(-0.3f);
            EndEpisode();
        }
        else if (other.gameObject.layer == 10)
        {

            //reward hard landings less
            Debug.Log(Rigidbody.velocity.y);
            if (Mathf.Abs(Rigidbody.velocity.y) > 30.0f) //hard landing
            {
                if (Mathf.Abs(transform.rotation.eulerAngles.z) < 15.0f && Mathf.Abs(transform.rotation.eulerAngles.x) < 15.0f) //reward upright landings more heavily
                {
                    SetReward(5.0f - (Mathf.Abs(Rigidbody.velocity.y) / 15.0f));
                    EndEpisode();
                }
                else
                {
                    SetReward(1.0f);
                    EndEpisode();
                }
            }
            else //soft landing
            {
                if (Mathf.Abs(transform.rotation.eulerAngles.z) < 15.0f && Mathf.Abs(transform.rotation.eulerAngles.x) < 15.0f)
                {
                    SetReward(15.0f - (Mathf.Abs(Rigidbody.velocity.y) / 15.0f));
                    Debug.Log("landed");
                    Rigidbody.drag = 1.1f;
                }
                else
                {
                    SetReward(3.0f);
                    EndEpisode();
                }
            }
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
        Rigidbody.drag = 0.1f;
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