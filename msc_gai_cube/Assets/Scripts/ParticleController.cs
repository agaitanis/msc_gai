using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class ParticleController : MonoBehaviour
{
    public Vector3 velocity;
    public float mass;
    public float dragCoeff;
    public float restitutionCoeff;
    public Vector3 cubeMinPosition;
    public Vector3 cubeMaxPosition;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    private Vector3 GetNormal(int facetIndex)
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

    private Vector3 FindIntersectionLineWithPlane(Vector3 planePoint, Vector3 planeNormal, Vector3 linePoint1, Vector3 linePoint2)
    {
        Vector3 lineVec = linePoint2 - linePoint1;
        float d = Vector3.Dot(planePoint - linePoint1, planeNormal) / Vector3.Dot(lineVec, planeNormal);

        return linePoint1 + lineVec * d;
    }

    private bool CheckForCollisionWithPlane(Vector3 oldPosition, Vector3 oldVelocity, Vector3 newPosition, Vector3 newVelocity, float dt, int facetIndex,
        out Vector3 correctedPosition, out Vector3 correctedVelocity, out Vector3 collisionPosition, out Vector3 collisionVelocity, out float collisionTime)
    {
        Vector3 normal = GetNormal(facetIndex);
        Vector3 pointOnPlane = GetPointOnPlane(facetIndex);

        if (Vector3.Dot(normal, newPosition - pointOnPlane) >= 0 ||
            Vector3.Distance(newPosition, oldPosition) < float.Epsilon) {
            correctedPosition = newPosition;
            correctedVelocity = newVelocity;
            collisionPosition = newPosition;
            collisionVelocity = newVelocity;
            collisionTime = dt;
            return false;
        }

        Vector3 velocity = (newPosition - oldPosition) / dt;
        
        collisionPosition = FindIntersectionLineWithPlane(pointOnPlane, normal, oldPosition, newPosition);
        collisionTime = Vector3.Distance(oldPosition, collisionPosition) / velocity.magnitude;

        float remainingTime = dt - collisionTime;
        Vector3 normalVelocity = Vector3.Dot(velocity, normal) * normal;
        Vector3 newNormalVelocity = -restitutionCoeff * normalVelocity;
        Vector3 tangentVelocity = velocity - normalVelocity;
        
        collisionVelocity = newNormalVelocity + tangentVelocity;

        CalcNewState(collisionPosition, collisionVelocity, remainingTime, out correctedPosition, out correctedVelocity);

        return Vector3.Dot(normal, newPosition - pointOnPlane) < 0;
    }

    private bool CheckForCollisionCore(Vector3 oldPosition, Vector3 oldVelocity, Vector3 newPosition, Vector3 newVelocity, float dt,
        out Vector3 correctedPosition, out Vector3 correctedVelocity, out Vector3 collisionPosition, out Vector3 collisionVelocity, out float collisionTime)
    {
        Vector3 bestCorrectedPosition = newPosition;
        Vector3 bestCorrectedVelocity = newVelocity;
        Vector3 bestCollisionPosition = newPosition;
        Vector3 bestCollisionVelocity = newVelocity;
        float bestCollisionTime = dt;
        float minDist = Vector3.Distance(oldPosition, newPosition);
        bool ret = false;

        for (int i = 0; i < 6; i++) {
            Vector3 tempCorrectedPosition;
            Vector3 tempCorrectedVelocity;
            Vector3 tempCollisionPosition;
            Vector3 tempCollisionVelocity;
            float tempCollisionTime;

            if (CheckForCollisionWithPlane(oldPosition, oldVelocity, newPosition, newVelocity, dt, i,
                out tempCorrectedPosition, out tempCorrectedVelocity, out tempCollisionPosition, out tempCollisionVelocity,
                out tempCollisionTime)) {
                float dist = Vector3.Distance(oldPosition, tempCollisionPosition);

                if (dist < minDist) {
                    minDist = dist;
                    bestCorrectedPosition = tempCorrectedPosition;
                    bestCorrectedVelocity = tempCorrectedVelocity;
                    bestCollisionPosition = tempCollisionPosition;
                    bestCollisionVelocity = tempCollisionVelocity;
                    bestCollisionTime = tempCollisionTime;
                }

                ret = true;
            }
        }

        correctedPosition = bestCorrectedPosition;
        correctedVelocity = bestCorrectedVelocity;
        collisionPosition = bestCollisionPosition;
        collisionVelocity = bestCollisionVelocity;
        collisionTime = bestCollisionTime;

        return ret;
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
        Vector3 g = new Vector3(0f, -9.807f, 0f);
        Vector3 gravityForce = mass * g;
        Vector3 dragForce = -dragCoeff * oldVelocity;
        Vector3 totalForce = gravityForce + dragForce;
        float eps = 1e-6f;

        foreach (GameObject attractor in AttractorController.instance.attractors) {
            if (attractor.activeInHierarchy) {
                Vector3 d = attractor.transform.position - oldPosition;
                float dMagnCube = d.magnitude * d.magnitude * d.magnitude;
                Vector3 attractiveForce = AttractorController.instance.coeff * d / (dMagnCube + eps);

                totalForce += attractiveForce;
            }
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
