using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleGenerator : MonoBehaviour
{
    public static ParticleGenerator instance;
    public GameObject particle;
    public int numParticles;
    public float minPosition;
    public float maxPosition;
    public float minVelocity;
    public float maxVelocity;
    public List<GameObject> particles;
    
    // Start is called before the first frame update
    void Start()
    {
        instance = this;

        for (int i = 0; i < numParticles; i++) {
            Vector3 position = new Vector3(
                Random.Range(minPosition, maxPosition),
                Random.Range(minPosition, maxPosition),
                Random.Range(minPosition, maxPosition)
                );
            GameObject clone = Instantiate(particle, position, Quaternion.identity);

            clone.GetComponent<ParticleController>().velocity = new Vector3(
                Random.Range(minVelocity, maxVelocity),
                Random.Range(minVelocity, maxVelocity),
                Random.Range(minVelocity, maxVelocity)
                );

            particles.Add(clone);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
