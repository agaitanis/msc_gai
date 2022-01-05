using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleGenerator : MonoBehaviour
{
    public static ParticleGenerator instance;
    public GameObject particle;
    public int numParticles;
    public List<GameObject> particles;
    
    // Start is called before the first frame update
    void Start()
    {
        instance = this;

        for (int i = 0; i < numParticles; i++) {
            GameObject clone = Instantiate(particle);

            particles.Add(clone);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
