using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


[RequireComponent(typeof(CharacterController))]

public class DopplegangerController : MonoBehaviour
{
    public float walkingSpeed = 7.5f;
    public float runningSpeed = 11.5f;
    public float jumpSpeed = 8.0f;
    public float gravity = 20.0f;
    public float lookSpeed = 2.0f;
    public float lookXLimit = 45.0f;
    public float drawDistance = 10.0f;
    public GameObject projectile;
    public GameObject drawingLinePrefab;
    private GameObject aimPoint;
    public bool showAimPoint = true;
    public bool hasPuzzles = true;
    public bool hasDoor = true;
    public bool isDrawing;
    public AudioSource pen;
    public AudioSource walking;
    public AudioSource running;
    public AudioClip draw;
    public AudioClip walk;
    public AudioClip run;
    public bool isRunning;
    public bool isPlayingRun;


    CharacterController characterController;
    public Vector3 moveDirection = Vector3.zero;
    float rotationX = 0;

    [HideInInspector]
    public bool canMove = true;
    public bool canFire = true;

    // Pattern recognition stuff
    private List<List<Vector3>> newLines;
    private List<GameObject> lineInstantiations;
    // Puzzle values
    public GameObject linePrefab;
    public int lockLineCount = 2;
    public float validDistanceError = 0.025f;
    public float validLengthError = 0.1f;
    public int lockPointCount = 5;
    public int pointCountIntervall = 1;
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        
        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (showAimPoint) {
            aimPoint = Instantiate(projectile, transform.position, Quaternion.identity);
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Respawn")
        {
            SceneManager.LoadScene("MazeRoom1");
        }
    }  

        void Update()
    {
        // We are grounded, so recalculate move direction based on axes
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);
        // Press Left Shift to run
        isRunning = Input.GetKey(KeyCode.LeftShift);
        float curSpeedX = canMove ? (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Vertical") : 0;
        float curSpeedY = canMove ? (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Horizontal") : 0;
        float movementDirectionY = moveDirection.y;
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        if (isRunning && !isPlayingRun && Input.GetKey(KeyCode.W))
        {
            walking.Stop();
            running.PlayOneShot(run);
            isPlayingRun = true;

        }

        if (Input.GetKeyDown(KeyCode.W)){
            if(!isRunning)
            {
                walking.Play();
            }
            
        }

        if (Input.GetKeyUp(KeyCode.W))
        {
            walking.Stop();
            running.Stop();
            isPlayingRun = false;
        }

        // If shooting button pressed launch projectile at point where aiming
        
        // Apply gravity. Gravity is multiplied by deltaTime twice (once here, and once below
        // when the moveDirection is multiplied by deltaTime). This is because gravity should be applied
        // as an acceleration (ms^-2)
        if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }

        // Move the controller
        characterController.Move(moveDirection * Time.deltaTime);

        // Player and Camera rotation
        if (canMove)
        {
            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
        }
    }
}