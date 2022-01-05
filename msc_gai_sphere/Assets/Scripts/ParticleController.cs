using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class ParticleController : MonoBehaviour
{
    public Vector3 velocity;
    public float mass;
    public float forceCoeff;
    public float restitutionCoeff;
    public Vector3 sphereCenter;
    public float sphereRadius;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    private Vector3 FindIntersectionLineWithSphere(Vector3 linePoint1, Vector3 linePoint2)
    {
        Vector3 lineOrigin = linePoint1;
        Vector3 lineUniVec = (linePoint2 - linePoint1).normalized;
        float dotProduct = Vector3.Dot(lineUniVec, lineOrigin - sphereCenter);
        float discriminant = dotProduct * dotProduct - Vector3.SqrMagnitude(lineOrigin - sphereCenter) + sphereRadius * sphereRadius;

        if (discriminant < 0f) discriminant = 0f;

        float d1 = - dotProduct + Mathf.Sqrt(discriminant);
        float d2 = - dotProduct - Mathf.Sqrt(discriminant);
        Vector3 point1 = lineOrigin + d1 * lineUniVec;
        Vector3 point2 = lineOrigin + d2 * lineUniVec;

        if (Vector3.Distance(linePoint2, point1) < Vector3.Distance(linePoint2, point2)) {
            return point1;
        } else {
            return point2;
        }
    }

    private bool CheckForCollisionCore(Vector3 oldPosition, Vector3 oldVelocity, Vector3 newPosition, Vector3 newVelocity, float dt, 
        out Vector3 correctedPosition, out Vector3 correctedVelocity, out Vector3 collisionPosition, out Vector3 collisionVelocity, out float collisionTime)
    {
        if (Vector3.Distance(newPosition, sphereCenter) <= sphereRadius ||
            Vector3.Distance(newPosition, oldPosition) < float.Epsilon) {
            correctedPosition = newPosition;
            correctedVelocity = newVelocity;
            collisionPosition = newPosition;
            collisionVelocity = newVelocity;
            collisionTime = dt;
            return false;
        }

        Vector3 velocity = (newPosition - oldPosition) / dt;

        collisionPosition = FindIntersectionLineWithSphere(oldPosition, newPosition);
        collisionTime = Vector3.Distance(oldPosition, collisionPosition) / velocity.magnitude;

        float remainingTime = dt - collisionTime;
        Vector3 normal = (sphereCenter - collisionPosition).normalized;
        Vector3 normalVelocity = Vector3.Dot(velocity, normal) * normal;
        Vector3 newNormalVelocity = -restitutionCoeff * normalVelocity;
        Vector3 tangentVelocity = velocity - normalVelocity;
        
        collisionVelocity = newNormalVelocity + tangentVelocity;

        CalcNewState(collisionPosition, collisionVelocity, remainingTime, out correctedPosition, out correctedVelocity);

        return Vector3.Distance(newPosition, sphereCenter) > sphereRadius;
    }

    private void CheckForCollision(Vector3 oldPosition, Vector3 oldVelocity, Vector3 newPosition, Vector3 newVelocity, float dt,
        out Vector3 correctedPosition, out Vector3 correctedVelocity)
    {
        correctedPosition = newPosition;
        correctedVelocity = newVelocity;

        int cnt = 0;

        while (dt >= 0 && cnt < 100) {
            Vector3 collisionPosition;
            Vector3 collisionVelocity;
            float collisionTime;

            if (CheckForCollisionCore(oldPosition, oldVelocity, newPosition, newVelocity, dt,
                out correctedPosition, out correctedVelocity, out collisionPosition, out collisionVelocity, out collisionTime)) {
                oldPosition = collisionPosition;
                oldVelocity = collisionVelocity;
                newPosition = correctedPosition;
                newVelocity = correctedVelocity;
                dt -= collisionTime;
            } else {
                break;
            }

            cnt++;
        }
    }

    private void CalcNewState(Vector3 oldPosition, Vector3 oldVelocity, float dt, out Vector3 newPosition, out Vector3 newVelocity)
    {
        Vector3 totalForce = new Vector3(0f, 0f, 0f);
        float eps = 1e-6f;

        foreach (GameObject otherParticle in ParticleGenerator.instance.particles) {
            if (otherParticle == this) continue;

            Vector3 d = transform.position - otherParticle.transform.position;
            Vector3 force = forceCoeff * d / (d.magnitude * d.magnitude + eps);

            totalForce += force;
        }

        Vector3 acceleration = totalForce / mass;

        newPosition = oldPosition + dt * oldVelocity;
        newVelocity = oldVelocity + dt * acceleration;
    }

    // Update is called once per frame
    void Update()
    {
        float dt = Time.deltaTime;
        Vector3 oldPosition = transform.position;
        Vector3 oldVelocity = velocity;
        Vector3 newPosition;
        Vector3 newVelocity;

        CalcNewState(oldPosition, oldVelocity, dt, out newPosition, out newVelocity);

        Vector3 correctedPosition;
        Vector3 correctedVelocity;

        CheckForCollision(oldPosition, oldVelocity, newPosition, newVelocity, dt,
            out correctedPosition, out correctedVelocity);

        transform.position = correctedPosition;
        velocity = correctedVelocity;
    }
}
