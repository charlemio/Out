using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundAtLocation : MonoBehaviour
{
    public AudioSource location;
    public bool hasPlayed;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Player" && !hasPlayed)
        {
            location.Play();
            hasPlayed = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
