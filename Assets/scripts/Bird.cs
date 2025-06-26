using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BirdInput))]
public abstract class Bird : MonoBehaviour
{
    public enum FlightState { Idle, Takeoff, Flapping, Gliding, Diving, Landing }

    [Header("Flight State Debug")]
    public FlightState currentState = FlightState.Idle;

    [Tooltip("HingeJoints of wings")]
    public HingeJoint[] Rwings, Lwings;

    [Tooltip("Body of bird")]
    public Rigidbody body;

    [Tooltip("HingeJoints that connect shoulders to body")]
    public HingeJoint R_body, L_body;

    [Header("Flapping Settings")]
    public float FlapSpeed = 5f;
    public float flapAmplitude = 30f;
    public float flapUpForce = 30f;
    public float flapForwardForce = 10f;
    public float boostMultiplier = 1.5f;
    public AnimationCurve flapLiftCurve;
    public AnimationCurve flapForwardCurve;

    [Header("Rotation Settings")]
    public float rotateSpeed = 30f;

    [Header("Shoulder Joint Control")]
    public float shoulderAngle = 30f;

    [Header("Auto-Leveling")]
    public float rollThreshold = 0.05f;
    public float autoLevelLerp = 0.5f;
    public float rollLevel = 100f;

    [Header("Glide Physics Settings")]
    public float glideLiftFactor = 1.5f;
    public float glideDragFactor = 0.5f;

    [Header("Wing Behavior")]
    public float wingRelaxLerp = 0.2f;

    protected float rollAngle, pitchAngle, yawAngle;
    protected float wingPhase = 0.0f;

    protected List<float> RIntTargetPositions = new List<float>();
    protected List<float> LIntTargetPositions = new List<float>();

    protected BirdInput input;

    private FlightState previousState;
    private float lastStateChangeTime;
    private float stateChangeCooldown = 0.25f;

    public abstract void FlappingFunction(float wingPhase);

    void Start()
    {
        input = GetComponent<BirdInput>();

        foreach (HingeJoint Rwing in Rwings)
            RIntTargetPositions.Add(Rwing.spring.targetPosition);

        foreach (HingeJoint Lwing in Lwings)
            LIntTargetPositions.Add(Lwing.spring.targetPosition);
    }

    void Update()
    {
        UpdateState();

        switch (currentState)
        {
            case FlightState.Flapping:
                Flapping();
                break;
            case FlightState.Gliding:
                RelaxWings();
                ApplyGlidePhysics();
                break;
            case FlightState.Diving:
                RelaxWings();
                break;
            case FlightState.Idle:
                RelaxWings();
                break;
        }

        movementControl();
    }

    void UpdateState()
    {
        if (Time.time - lastStateChangeTime < stateChangeCooldown)
            return;

        float verticalSpeed = body.linearVelocity.y;
        Vector3 trueForward = body.transform.up;
        float forwardSpeed = Vector3.Dot(body.linearVelocity, trueForward);

        FlightState newState = currentState;

        if (input.flapPressed)
            newState = FlightState.Flapping;
        else if (verticalSpeed < -2f && forwardSpeed < 1f)
            newState = FlightState.Diving;
        else if (forwardSpeed > 2f)
            newState = FlightState.Gliding;
        else
            newState = FlightState.Idle;

        if (newState != currentState)
        {
            previousState = currentState;
            currentState = newState;
            lastStateChangeTime = Time.time;
        }
    }

    void ApplyGlidePhysics()
    {
        Vector3 forward = body.transform.up;
        float forwardSpeed = Vector3.Dot(body.linearVelocity, forward);
        float liftScale = Mathf.Clamp01(forwardSpeed / 10f);

        Vector3 lift = -body.transform.forward * liftScale * glideLiftFactor * body.mass;
        Vector3 drag = -body.linearVelocity.normalized * forwardSpeed * glideDragFactor * body.mass;

        body.AddForce(lift);
        body.AddForce(drag);

        Debug.DrawRay(body.position, lift * 0.1f, Color.green);
        Debug.DrawRay(body.position, drag * 0.1f, Color.blue);
    }

    void RelaxWings()
    {
        for (int i = 0; i < Rwings.Length; i++)
        {
            JointSpring Rspring = Rwings[i].spring;
            Rspring.targetPosition = Mathf.Lerp(Rspring.targetPosition, RIntTargetPositions[i], wingRelaxLerp);
            Rwings[i].spring = Rspring;
        }

        for (int i = 0; i < Lwings.Length; i++)
        {
            JointSpring Lspring = Lwings[i].spring;
            Lspring.targetPosition = Mathf.Lerp(Lspring.targetPosition, LIntTargetPositions[i], wingRelaxLerp);
            Lwings[i].spring = Lspring;
        }
    }

    public void Flapping()
    {
        wingPhase += Time.deltaTime * FlapSpeed;
        if (wingPhase > 2f * Mathf.PI) wingPhase = 0f;

        FlappingFunction(wingPhase);

        float flapStrength = Mathf.Sin(wingPhase);
        if (flapStrength > 0.3f && wingPhase < Mathf.PI)
        {
            float boost = input.boostHeld ? boostMultiplier : 1f;
            Vector3 flapUp = -body.transform.forward;
            Vector3 flapForward = body.transform.up;
            Vector3 facingDir = flapForward.normalized;
            float alignment = Mathf.Clamp01(Vector3.Dot(facingDir, body.linearVelocity.normalized));

            float upForce = flapLiftCurve.Evaluate(flapStrength) * flapUpForce;
            float forwardForce = flapForwardCurve.Evaluate(flapStrength) * flapForwardForce * alignment;

            Vector3 flapForce = flapUp * upForce * boost + flapForward * forwardForce * boost;
            body.AddForce(flapForce, ForceMode.Force);

            Debug.DrawRay(body.position, flapUp * 2f, Color.green);
            Debug.DrawRay(body.position, flapForward * 2f, Color.red);
        }
    }

    public virtual void movementControl()
    {
        float pitch = input.moveInput.y;
        float yaw = -input.moveInput.x;

        body.transform.RotateAround(body.transform.position, body.transform.right, rotateSpeed * Time.deltaTime * pitch);
        body.transform.RotateAround(body.transform.position, body.transform.up, rotateSpeed * Time.deltaTime * yaw);

        JointSpring _jointSpringrbody = R_body.spring;
        JointSpring _jointSpringlbody = L_body.spring;

        _jointSpringrbody.targetPosition = Mathf.Lerp(_jointSpringrbody.targetPosition, shoulderAngle * pitch, 0.02f);
        _jointSpringlbody.targetPosition = Mathf.Lerp(_jointSpringlbody.targetPosition, -shoulderAngle * pitch, 0.02f);

        R_body.spring = _jointSpringrbody;
        L_body.spring = _jointSpringlbody;

        CalculateRollAndPitchAngles();
        AutoLevelSimple();
    }

    private void CalculateRollAndPitchAngles()
    {
        Quaternion bodyDirection = body.transform.rotation;
        float x = bodyDirection.x, y = bodyDirection.y, z = bodyDirection.z, w = bodyDirection.w;

        yawAngle = Mathf.Atan2(2 * y * w - 2 * x * z, 1 - 2 * y * y - 2 * z * z);
        pitchAngle = Mathf.Atan2(2 * x * w - 2 * y * z, 1 - 2 * x * x - 2 * z * z);
        rollAngle = -Mathf.Asin(2 * x * y + 2 * z * w);
    }

    private void AutoLevelSimple()
    {
        if (Mathf.Abs(rollAngle) > rollThreshold && body.linearVelocity.magnitude > 1f)
        {
            float rollAdjustment = Mathf.LerpAngle(rollAngle, 0f, autoLevelLerp);
            body.transform.RotateAround(body.transform.position, body.transform.up, rollLevel * Time.deltaTime * rollAdjustment);
        }
    }

    void FixedUpdate()
    {
        AlignVelocityWithFacing();
    }

    void AlignVelocityWithFacing()
    {
        Vector3 desiredDirection = body.transform.up.normalized;
        float currentSpeed = body.linearVelocity.magnitude;

        body.linearVelocity = Vector3.Lerp(body.linearVelocity, desiredDirection * currentSpeed, 0.01f);
    }
}
