using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class StartRoomController : MonoBehaviour
{

    public GameObject PenStatic;
    public GameObject PlayerPen;
    public GameObject Player;
    public GameObject textBox;
    private bool playerHasPen = false;


  // Start is called before the first frame update
  void Start()
    {
        textBox.GetComponent<Text>().text = "";
    }

    // Update is called once per frame
    void Update()
    {
    if (!playerHasPen && Vector3.Distance(PenStatic.transform.position, Player.transform.position) < 2.0F)
    {
        // If the player is near the pen show a promt for the player to pick up pen
        textBox.GetComponent<Text>().text = "Press E to pick up pen";
        if (Input.GetKeyDown(KeyCode.E))
        {
            // Activate the player pen capabilities
            Player.GetComponent<PlayerController>().ActivatePen();
            playerHasPen = true;
            // Remove the pen object from the fountain
            Destroy(PenStatic);
            // Deactivate the panel when the player has picked up the pen
            textBox.GetComponent<Text>().text = "";
        }
    }
    else
        {
        textBox.GetComponent<Text>().text = "";
        }
    }
}
