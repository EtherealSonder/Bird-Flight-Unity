using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BirdInput))]
public abstract class Bird : MonoBehaviour
{
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

    [Header("Rotation Settings")]
    public float rotateSpeed = 30f;

    [Header("Shoulder Joint Control")]
    public float shoulderAngle = 30f;

    [Header("Auto-Leveling")]
    public float rollThreshold = 0.05f;
    public float autoLevelLerp = 0.5f;

    [Header("Diving Behavior")]
    public float wingRelaxLerp = 0.2f;

    protected float rollAngle, pitchAngle, yawAngle;
    protected float wingPhase = 0.0f;

    protected List<float> RIntTargetPositions = new List<float>();
    protected List<float> LIntTargetPositions = new List<float>();

    protected BirdInput input;

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
        if (input.flapPressed)
            Flapping();
        else
            diving();

        movementControl();
    }

    public void Flapping()
    {
        wingPhase += Time.deltaTime * FlapSpeed;
        if (wingPhase > 2f * Mathf.PI)
            wingPhase = 0f;

        FlappingFunction(wingPhase);

        float flapStrength = Mathf.Sin(wingPhase);
        if (flapStrength > 0.3f)
        {
            float boost = input.boostHeld ? boostMultiplier : 1f;

            Vector3 flapUp = body.transform.forward;   
            Vector3 flapForward = body.transform.up;   

            Vector3 flapForce =
                flapUp * flapStrength * flapUpForce * boost +
                flapForward * flapStrength * flapForwardForce * boost;

            body.AddForce(flapForce, ForceMode.Force);

            Debug.DrawRay(body.position, flapForward * 2f, Color.red, 1f);
            Debug.DrawRay(body.position, flapUp * 2f, Color.green, 1f);
        }
        


    }

    public void diving()
    {
        if (wingPhase > 0.1f)
            wingPhase = Mathf.Lerp(wingPhase, 0f, 0.1f);
        else
            wingPhase = 0f;

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
        float x = bodyDirection.x;
        float y = bodyDirection.y;
        float z = bodyDirection.z;
        float w = bodyDirection.w;

        yawAngle = Mathf.Atan2(2 * y * w - 2 * x * z, 1 - 2 * y * y - 2 * z * z);
        pitchAngle = Mathf.Atan2(2 * x * w - 2 * y * z, 1 - 2 * x * x - 2 * z * z);
        rollAngle = -Mathf.Asin(2 * x * y + 2 * z * w);
    }

    private void AutoLevelSimple()
    {
        float rollAdjustment = 0f;
        if (Mathf.Abs(rollAngle) > rollThreshold)
        {
            rollAdjustment = Mathf.LerpAngle(rollAngle, 0f, autoLevelLerp);
            body.transform.RotateAround(body.transform.position, body.transform.up, 50f * Time.deltaTime * rollAdjustment);
        }
    }
}
