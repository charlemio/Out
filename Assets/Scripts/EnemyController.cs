using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public float walkingSpeed = 1f;
    public float runningSpeed = 11.5f;
    public float viewingDistance = 5.0f;
    public Transform player;
    public AudioSource sound;
    private bool seen;
    private bool hasPlayed;

    Vector3 moveDirection = Vector3.zero;

    [HideInInspector]
    public bool canMove = true;

    void Start()
    {
    }

    private void Update()
    {
        if (seen && !hasPlayed)
        {
            sound.Play();
            seen = false;
            hasPlayed = true;
        }
    }

    void FixedUpdate()
    {
        // Dumb enemy just moves to the player and does nothing (?)
        if (canMove) {
            transform.LookAt(player);
            if(0.5f < Vector3.Distance(transform.position, player.position) && Vector3.Distance(transform.position, player.position) < viewingDistance){
                seen = true;
                transform.position += transform.forward * walkingSpeed * Time.deltaTime * 0.5f;
            }
        }
    }
}