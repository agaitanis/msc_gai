using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleGenerator : MonoBehaviour
{
    public GameObject particle;
    public float minVelocity;
    public float maxVelocity;
    //static int cnt = 0;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        //if (cnt > 0) return;
        //cnt++;
        Instantiate(particle);

        particle.GetComponent<ParticleController>().velocity = new Vector3(
            Random.Range(minVelocity, maxVelocity),
            Random.Range(minVelocity, maxVelocity),
            Random.Range(minVelocity, maxVelocity)
            );
    }
}
