using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class ParticleController : MonoBehaviour
{
    public Vector3 velocity;
    public float mass;
    public float maxSpeed;
    public float maxForce;
    public float rotateSpeed;
    public float separationCoeff;
    public float alignmentCoeff;
    public float cohesionCoeff;
    public float boundariesCoeff;
    public float desiredSeparation;
    public float neighborDist;
    public Vector3 cubeMinPosition;
    public Vector3 cubeMaxPosition;
    public float playSoundProbability;

    private Animator animator;
    private AudioSource audioSource;
    private float timeAfterPlayingSound = float.MaxValue;
    private float timeAfterAskingToPlaySound = float.MaxValue;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        animator.SetTrigger("Soar");
        animator.SetBool("IsFlying", true);
        animator.SetBool("IsGrounded", false);

        audioSource = GetComponent<AudioSource>();
    }

    private Vector3 GetPlaneNormal(int facetIndex)
    {
        switch (facetIndex) {
            case 0:
                return new Vector3(1f, 0f, 0f);
            case 1:
                return new Vector3(0f, 1f, 0f);
            case 2:
                return new Vector3(0f, 0f, 1f);
            case 3:
                return new Vector3(-1f, 0f, 0f);
            case 4:
                return new Vector3(0f, -1f, 0f);
            case 5:
                return new Vector3(0f, 0f, -1f);
            default:
                Assert.IsTrue(false);
                return new Vector3(0f, 1f, 0f);
        }
    }

    private Vector3 GetPointOnPlane(int facetIndex)
    {
        if (facetIndex < 3) {
            return cubeMinPosition;
        } else {
            return cubeMaxPosition;
        }
    }

    private float CalcDistFromPlane(Vector3 point, Vector3 planeNormal, Vector3 planePoint)
    {
        return Vector3.Dot(point - planePoint, planeNormal);
    }

    private Vector3 CalcBoundariesForce()
    {
        Vector3 totalForce = new Vector3(0f, 0f, 0f);

        for (int i = 0; i < 6; i++) {
            Vector3 planeNormal = GetPlaneNormal(i);
            Vector3 planePoint = GetPointOnPlane(i);
            float dist = CalcDistFromPlane(transform.position, planeNormal, planePoint);

            if (dist > 1e-2) {
                totalForce += planeNormal / (dist * dist);
            } else {
                totalForce += planeNormal * 100f;
            }
        }

        return CalcSteeringForce(totalForce);
    }

    private Vector3 CalcSteeringForce(Vector3 dir)
    {
        if (dir.magnitude == 0f) return dir;

        Vector3 steer = dir;

        steer.Normalize();
        steer *= maxSpeed;
        steer -= velocity;
        steer = Vector3.ClampMagnitude(steer, maxForce);

        return steer;
    }

    private Vector3 CalcSeparationForce()
    {
        Vector3 dir = new Vector3(0f, 0f, 0f);

        foreach (GameObject other_particle in ParticleGenerator.instance.particles) {
            if (other_particle == this) continue;

            float dist = Vector3.Distance(transform.position, other_particle.transform.position);

            if (dist < 1e-6f || dist >= desiredSeparation) continue;

            Vector3 diff = transform.position - other_particle.transform.position;

            diff.Normalize();
            diff /= dist;
            dir += diff;
        }

        return CalcSteeringForce(dir);
    }

    private Vector3 CalcAlignmentForce()
    {
        Vector3 avgVelocity = new Vector3(0f, 0f, 0f);

        foreach (GameObject other_particle in ParticleGenerator.instance.particles) {
            if (other_particle == this) continue;

            float dist = Vector3.Distance(transform.position, other_particle.transform.position);

            if (dist == 0f || dist >= neighborDist) continue;

            avgVelocity += other_particle.GetComponent<ParticleController>().velocity;
        }

        return CalcSteeringForce(avgVelocity);
    }

    private Vector3 CalcCohesionForce()
    {
        Vector3 center = new Vector3(0f, 0f, 0f);
        int cnt = 0;

        foreach (GameObject other_particle in ParticleGenerator.instance.particles) {
            if (other_particle == this) continue;

            float dist = Vector3.Distance(transform.position, other_particle.transform.position);

            if (dist == 0f || dist >= neighborDist) continue;

            center += other_particle.transform.position;
            cnt++;
        }

        if (cnt == 0) return new Vector3(0f, 0f, 0f);

        center /= cnt;

        return CalcSteeringForce(center - transform.position);
    }

    // Update is called once per frame
    void Update()
    {
        float dt = Time.deltaTime;
        Vector3 oldPosition = transform.position;
        Vector3 oldVelocity = velocity;
        Vector3 totalForce = separationCoeff * CalcSeparationForce() +
            alignmentCoeff * CalcAlignmentForce() +
            cohesionCoeff * CalcCohesionForce() +
            boundariesCoeff * CalcBoundariesForce();
        Vector3 acceleration = totalForce / mass;

        transform.position = oldPosition + dt * oldVelocity;
        velocity = oldVelocity + dt * acceleration;

        Vector3 moveDirection = velocity.normalized;
        float forwardAccelaration = Vector3.Dot(moveDirection, acceleration);

        animator.SetFloat("Forward", forwardAccelaration);

        Quaternion oldRotation = transform.rotation;
        Quaternion newRotation = Quaternion.LookRotation(moveDirection);
        float t = rotateSpeed * dt;
        transform.rotation = Quaternion.Slerp(oldRotation, newRotation, t);

        if (!audioSource.isPlaying && timeAfterAskingToPlaySound > 2f && Random.Range(0f, 1f) < playSoundProbability) {
            timeAfterAskingToPlaySound = 0f;

            if (timeAfterPlayingSound > 20f) {
                audioSource.Play();
                timeAfterPlayingSound = 0f;
            }
        }

        if (timeAfterAskingToPlaySound < 2f) {
            timeAfterAskingToPlaySound += dt;
        }
        if (timeAfterPlayingSound < 20f) {
            timeAfterPlayingSound += dt;
        }
    }
}
