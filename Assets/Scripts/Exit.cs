using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;


public class Exit : MonoBehaviour
{
    public GameObject textBox;
    public GameObject player;
    public AudioSource laugh;
    public bool startRoomBoolean = false;

    void Start()
    {
        textBox.GetComponent<Text>().text = "";
    }
    void Update()
    {

    }

  private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player" && !player.GetComponent<PlayerController>().doorLocked)
        {
            textBox.GetComponent<Text>().text = "Press E to exit";
        }
    }   

  private void OnTriggerExit(Collider other)
  {
    if (other.tag == "Player")
    {
      if (player.GetComponent<PlayerController>().doorLocked)
        {
            textBox.GetComponent<Text>().text = "";
            if (!startRoomBoolean)
            {
                laugh.Play();
            }
        }
    }
  }
}
