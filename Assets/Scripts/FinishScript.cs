using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FinishScript : MonoBehaviour
{
    public GameObject player;
    public Transform finish;
    public float finishDistance = 10.0F;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // If the player reaches the end of the maze (opposite corner of start) then trigger some particle effect or sumthin
        // overlay the "press E" text and if player presses E he gets catapulted into another dimension, perhaps heaven?
        if (Vector3.Distance(player.transform.position, finish.position) < finishDistance) {
            // Trigger end of scene
        }
    }
}
