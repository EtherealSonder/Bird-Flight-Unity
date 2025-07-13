using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
public class WindManager : MonoBehaviour
{
    [Header("Wind Settings")]
    public Vector3 windDirection = Vector3.right;
    public float windForce = 10.0f;
    public bool dynamicWind = false;
    public float dayLengthInSeconds = 60.0f;

    [Header("Turbulence Settings")]
    public bool applyTurbulence = false;
    public float turbulenceIntensity = 1.0f;
    public float turbulenceScale = 0.1f;
    public AnimationCurve turbulenceCurve;
    public float turbulenceTimeScale = 1.0f;

    [Header("Rigidbody Settings")]
    public bool dynamicRigidbodies = false;
    public LayerMask windAffectedLayers;
    public float rigidbodyScanRadius = 500f;
    private List<Rigidbody> cachedRigidbodies;

    [Header("Particle Visualization")]
    public ParticleSystem windParticles;

    private Vector3 initialWindDirection;
    private float initialWindForce;
    private float elapsedTime = 0.0f;

    private void Start()
    {
        initialWindDirection = windDirection;
        initialWindForce = windForce;

        if (!dynamicRigidbodies)
            CacheRigidbodies();
    }

    private void FixedUpdate()
    {
        if (dynamicWind)
            UpdateWind();

        if (applyTurbulence)
            ApplyTurbulence();

        List<Rigidbody> rbs = dynamicRigidbodies ? GetCurrentRigidbodies() : cachedRigidbodies;
        foreach (Rigidbody rb in rbs)
        {
            if (rb != null && !rb.isKinematic)
            {
                rb.AddForce(windDirection.normalized * windForce);
            }
        }

        UpdateWindVisuals();
    }

    private void CacheRigidbodies()
    {
        cachedRigidbodies = new List<Rigidbody>();
        Collider[] colliders = Physics.OverlapSphere(transform.position, rigidbodyScanRadius, windAffectedLayers);
        foreach (var col in colliders)
        {
            Rigidbody rb = col.attachedRigidbody;
            if (rb != null && !cachedRigidbodies.Contains(rb))
                cachedRigidbodies.Add(rb);
        }
    }

    private List<Rigidbody> GetCurrentRigidbodies()
    {
        List<Rigidbody> list = new List<Rigidbody>();
        Collider[] colliders = Physics.OverlapSphere(transform.position, rigidbodyScanRadius, windAffectedLayers);
        foreach (var col in colliders)
        {
            Rigidbody rb = col.attachedRigidbody;
            if (rb != null && !list.Contains(rb))
                list.Add(rb);
        }
        return list;
    }

    private void ApplyTurbulence()
    {
        float curveTime = Time.time * turbulenceTimeScale;
        float curveValue = turbulenceCurve.Evaluate(curveTime % turbulenceCurve.keys[turbulenceCurve.length - 1].time);

        float xTurb = (Mathf.PerlinNoise(Time.time * turbulenceScale, 0) * 2 - 1) * curveValue;
        float yTurb = (Mathf.PerlinNoise(0, Time.time * turbulenceScale) * 2 - 1) * curveValue;
        float zTurb = (Mathf.PerlinNoise(Time.time * turbulenceScale, Time.time * turbulenceScale) * 2 - 1) * curveValue;

        Vector3 turbulence = new Vector3(xTurb, yTurb, zTurb) * turbulenceIntensity;
        windDirection = initialWindDirection + turbulence;

        float forceTurbulence = (Mathf.PerlinNoise(Time.time * turbulenceScale, 1000) * 2 - 1) * curveValue;
        windForce = initialWindForce + forceTurbulence;
    }

    private void UpdateWind()
    {
        elapsedTime += Time.fixedDeltaTime;
        float dayProgress = elapsedTime / dayLengthInSeconds;

        float angle = 360.0f * dayProgress;
        float forceVariation = Mathf.Sin(2 * Mathf.PI * dayProgress);

        windDirection = Quaternion.Euler(0, angle, 0) * Vector3.right;
        windForce = initialWindForce + forceVariation;

        if (elapsedTime >= dayLengthInSeconds)
            elapsedTime = 0.0f;
    }

    private void UpdateWindVisuals()
    {
        if (windParticles == null) return;

        var vel = windParticles.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = windDirection.normalized.x * windForce;
        vel.y = windDirection.normalized.y * windForce;
        vel.z = windDirection.normalized.z * windForce;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, windDirection.normalized * windForce);
    }
}
