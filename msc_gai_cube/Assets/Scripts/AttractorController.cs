using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttractorController : MonoBehaviour
{
    public static AttractorController instance;
    public GameObject[] attractors;
    public float coeff;

    // Start is called before the first frame update
    void Start()
    {
        instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
