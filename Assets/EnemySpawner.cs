using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemy;
    private bool hasHappened;
    

    // Start is called before the first frame update
    void Start()
    {
        enemy.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Player" && !hasHappened)
        {
            enemy.SetActive(true);
            hasHappened = true;
        }
        
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Respawn")
        {
            enemy.SetActive(false);
        }

    }

    // Update is called once per frame
    void Update()
    {
      
    }
}
