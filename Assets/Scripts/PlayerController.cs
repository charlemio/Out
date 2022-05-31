using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


[RequireComponent(typeof(CharacterController))]

public class PlayerController : MonoBehaviour
{
  public float walkingSpeed = 7.5f;
  public float runningSpeed = 11.5f;
  public float jumpSpeed = 8.0f;
  public float gravity = 20.0f;
  public Camera playerCamera;
  public float lookSpeed = 2.0f;
  public float lookXLimit = 360.0f;
  public float drawDistance = 10.0f;
  public GameObject projectile;
  public GameObject drawingLinePrefab;
  private GameObject aimPoint;
  public GameObject playerPen;
  public bool isStartRoom = false;
	public bool isStoryRoom2 = false;
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
  public bool penActivated = true;


  CharacterController characterController;
  public Vector3 moveDirection = Vector3.zero;
  float rotationX = 0;

  [HideInInspector]
  public bool canMove = true;
  public bool canFire = true;

  // Pattern recognition stuff
  private List<List<Vector3>> newLines;
  private List<List<Vector3>> validLines;
  private List<GameObject> lineInstantiations;
  // Two-dimensional list where each sublist represents a line through given points
  private List<Vector3> pattern;

  // Puzzle values
  public GameObject doorPrefab;
	public GameObject Exit;
  public bool doorLocked;
  public GameObject linePrefab;
  public int lockLineCount = 2;
  public float validDistanceError = 0.025f;
  public float validLengthError = 0.1f;
  public GameObject puzzleLock;
  public Transform puzzleLockCenter;
  public Transform puzzleLockEdge;
  public List<GameObject> puzzlePieces;
  public List<Transform> puzzleLockPoints;
  public int lockPointCount = 5;
  public int pointCountIntervall = 1;

  private float patternLength;
  private float puzzleLockRadius;
	public string nextSceneName;

  void Start()
  {
    characterController = GetComponent<CharacterController>();
    // Lock cursor
    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;
    if (showAimPoint)
    {
      aimPoint = Instantiate(projectile, transform.position, Quaternion.identity);
    }

    if (!penActivated)
    {
      aimPoint.SetActive(false);
      playerPen.SetActive(false);
    }
    newLines = new List<List<Vector3>>();
    newLines.Add(new List<Vector3>());
    lineInstantiations = new List<GameObject>();

    // Puzzle values
    if (!isStoryRoom2 && (hasPuzzles || isStartRoom))
    {
      doorLocked = true;
      puzzleLockRadius = Vector3.Distance(puzzleLockCenter.position, puzzleLockEdge.position);

      pattern = new List<Vector3>();
      lockPointCount = puzzleLockPoints.Count;

      var pointIndices = new List<int>();

      if (!isStartRoom)
      {// Instantiate the pattern puzzle
        for (var i = 0; i < lockLineCount; i++)
        {
          // For each line pick two random points on the lock and draw the line between those two points
          var foundUniqueLine = false;
          var r1 = -1;
          var r2 = -1;
          while (!foundUniqueLine)
          {
            r1 = Random.Range(0, lockPointCount);
            r2 = r1;
            while (r1 == r2)
            {
              r2 = Random.Range(0, lockPointCount);
            }
            // check previously added pattern lines for duplicates
            var foundDuplicate = false;
            for (var j = 1; j < pointIndices.Count; j += 2)
            {
              if ((pointIndices[j - 1] == r1 && pointIndices[j] == r2) || (pointIndices[j - 1] == r2 && pointIndices[j] == r1))
              {
                foundDuplicate = true;
              }
            }
            if (!foundDuplicate)
            {
              foundUniqueLine = true;
            }
          }
          pointIndices.Add(r1);
          pointIndices.Add(r2);
          var p1 = puzzleLockPoints[r1];
          var p2 = puzzleLockPoints[r2];

          // Add the line to the pattern list for lock pattern matching
          pattern.Add(p1.position);
          pattern.Add(p2.position);
        }
      }
      else
      {
        pattern.Add(puzzleLockPoints[0].position);
        pattern.Add(puzzleLockPoints[1].position);
        pattern.Add(puzzleLockPoints[1].position);
        pattern.Add(puzzleLockPoints[2].position);
        pattern.Add(puzzleLockPoints[2].position);
        pattern.Add(puzzleLockPoints[0].position);
      }

      var pieceIndex = 0;
      for (int j = 1; j < pointIndices.Count; j += 2)
      {
        var puzzlePiece = puzzlePieces[pieceIndex++];
        var t1 = puzzlePiece.transform.Find("Point" + (pointIndices[j - 1] + 1));
        var t2 = puzzlePiece.transform.Find("Point" + (pointIndices[j] + 1));
        var newLine = Instantiate(linePrefab, t1.position, Quaternion.identity);
        float scale = (t1.position - t2.position).magnitude;
        newLine.transform.localScale = new Vector3(1f, 1f, scale);
        newLine.transform.LookAt(t2.position);
      }

      patternLength = GetPatternLength();

      // Initiate the pattern recognition method and associated values
      validLines = new List<List<Vector3>>();
      InvokeRepeating("CheckPattern", 1f, 3f);
    } else {
			doorLocked = false;
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
    if (penActivated)
    {
      aimPoint.SetActive(true);
      playerPen.SetActive(true);
    }
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

    if (Input.GetKeyDown(KeyCode.W))
    {
      if (!isRunning)
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


    if (Input.GetButton("Jump") && canMove && characterController.isGrounded)
    {
      moveDirection.y = jumpSpeed;
    }
    else
    {
      moveDirection.y = movementDirectionY;
    }
    RaycastHit hit;
    var didHit = Physics.Raycast(playerCamera.transform.position, playerCamera.transform.TransformDirection(Vector3.forward), out hit, drawDistance);
    // Display point at the drawing position if reachable
    if (showAimPoint)
    {
      if (didHit)
      {
        aimPoint.transform.position = Vector3.Lerp(aimPoint.transform.position, hit.point, 0.1f);
      }
      else
      {
        aimPoint.transform.position = Vector3.Lerp(aimPoint.transform.position, transform.position + Vector3.up, 0.05f);
      }
    }

    // If shooting button pressed launch projectile at point where aiming
    if (penActivated && Input.GetButton("Fire") && canFire)
    {
      // Does the ray intersect any objects excluding the player layer
      if (didHit)
      {
        Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow);
        // Instantiate(projectile, hit.point, Quaternion.identity);

        // Add point to list used for pattern recognition
        if (hasPuzzles || isStartRoom)
        {
          newLines[newLines.Count - 1].Add(hit.point);
          // Check if more than one point has been drawn in the latest drawn line
          if (newLines[newLines.Count - 1].Count > 1)
          {
            // * Instantiate the new line
            // Fetch the two last points from the latest drawn line
            if (pointCountIntervall > 0 && newLines[newLines.Count - 1].Count % pointCountIntervall == 0)
            {
              var p1 = newLines[newLines.Count - 1][newLines[newLines.Count - 1].Count - 1 - pointCountIntervall];
              var p2 = newLines[newLines.Count - 1][newLines[newLines.Count - 1].Count - 1];
              // Instantiate new gameobject
              var newLine = Instantiate(drawingLinePrefab, p1, Quaternion.identity);
              // Set the length of the new line
              float scale = (p1 - p2).magnitude;
              newLine.transform.localScale = new Vector3(1f, 1f, scale);
              // Rotate the line so it goes from p1 to p2
              newLine.transform.LookAt(p2);
              lineInstantiations.Add(newLine);
            }
          }
        }
        else
        {
          newLines[newLines.Count - 1].Add(hit.point);
          if (newLines[newLines.Count - 1].Count > 1)
          {
            if (pointCountIntervall > 0 && newLines[newLines.Count - 1].Count % pointCountIntervall == 0)
            {
              var newLine = Instantiate(drawingLinePrefab, newLines[newLines.Count - 1][newLines[newLines.Count - 1].Count - 1 - pointCountIntervall], Quaternion.identity);
              float scale = (newLines[newLines.Count - 1][newLines[newLines.Count - 1].Count - 1 - pointCountIntervall] - hit.point).magnitude;
              newLine.transform.localScale = new Vector3(1f, 1f, scale);
              newLine.transform.LookAt(hit.point);
              lineInstantiations.Add(newLine);
            }
          }
        }

        Debug.Log("Did Hit");
        if (!isDrawing)
        {
          pen.Play();
        }
        isDrawing = true;
      }
      else
      {
        newLines.Add(new List<Vector3>());
        Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.TransformDirection(Vector3.forward) * 1000, Color.white);
        Debug.Log("Did not Hit");
        pen.Stop();
        isDrawing = false;
      }
    }
    if (Input.GetButtonUp("Fire") && isDrawing)
    {
      // Instantiate new list in points for representing a new line
      newLines.Add(new List<Vector3>());
      pen.Stop();
      isDrawing = false;
    }

    // When pressing right mouse button and drawn points exist nearby remove the lines connected to those points
    if (Input.GetMouseButton(1))
    {
      var aimPos = aimPoint.transform.position;
      // Look for points near the aimPoint
      foreach (var line in lineInstantiations)
      {
        if (Vector3.Distance(line.transform.position, aimPos) < 0.1f)
        {
          Destroy(line);
          lineInstantiations.Remove(line);
        }
      }
      if (hasPuzzles || isStartRoom)
      {
        foreach (var line in validLines)
        {
          foreach (var point in line)
          {
            if (Vector3.Distance(point, aimPos) < 0.1f)
            {
              validLines.Remove(line);
            }
          }
        }
      }
    }

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
      playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
      transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
    }

    if (hasDoor)
    {
      if (!doorLocked)
      {
        Debug.Log("Door unlocked");
        doorPrefab.transform.rotation = Quaternion.Lerp(doorPrefab.transform.rotation, Quaternion.Euler(0, 90f, 0), 1f * Time.deltaTime);
    		if (Input.GetKeyDown(KeyCode.E) && Vector3.Distance(transform.position, doorPrefab.transform.position) < 2.0F)
    		{
    		  SceneManager.LoadScene(sceneName: nextSceneName);
    		}
      }
      else
      {
        // Debug.Log("Door locked");
        doorPrefab.transform.rotation = Quaternion.Lerp(doorPrefab.transform.rotation, Quaternion.Euler(0, 0, 0), 1f * Time.deltaTime);
      }
    }

		
  }
  void CheckPattern()
  {
    Debug.Log("Checking pattern");
    // Don't do anything if currently drawing
    if (!Input.GetButton("Fire") && !Input.GetMouseButton(1))
    {
      // ensure nothing new is added while executing this method
      canFire = false;
      var patternFound = false;

      // Filter out valid lines, only check lines which haven't yet been encountered
      for (var i = 0; i < newLines.Count; i++)
      {
        Debug.Log("Found newly drawn line to check with point count: " + newLines[i].Count);
        foreach (var p in newLines[i])
        {
          Debug.Log(p);
        }
        var validLineFound = false;
        // newPoints is a temporary list which only gets added to validLines if
        // sufficient data is found
        var newLine = new List<Vector3>();
        var numberOfAddedPoints = 0;
        // Iterate over each point in each new line
        for (var j = 0; j < newLines[i].Count; j++)
        {
          // We keep track of whether a valid line is found
          // Verify that point lies within the puzzle boundaries
          var dst = Vector3.Distance(puzzleLockCenter.position, newLines[i][j]);
          Debug.Log("Checking distance between new point and lock center: " + dst);
          if (Vector3.Distance(puzzleLockCenter.position, newLines[i][j]) < puzzleLockRadius)
          {
            newLine.Add(newLines[i][j]);
            numberOfAddedPoints++;
            if (numberOfAddedPoints > 1)
            {
              validLineFound = true;
            }
          }
        }
        // If sufficient valid data for representing line was found then add new data to list for checking pattern
        if (validLineFound)
        {
          Debug.Log("Found new valid line");
          validLines.Add(newLine);
        }
      }

      // reset the new lines list
      newLines = new List<List<Vector3>>();
      newLines.Add(new List<Vector3>());

      // Verify that the drawn shape is similar enough to the pattern (TODO: only checks distance right now but 
      // might extend with some least squares approximation in the future)
      var drawnShapeIsValid = true;
      Debug.Log("Finding worst fit");
      foreach (var line in validLines)
      {
        // find the closest point to a line in the pattern
        var worstBestFit = -1.0f;
        foreach (var point in line)
        {
          var bestFit = float.PositiveInfinity;
          for (int i = 1; i < pattern.Count; i += 2)
          {
            var baseDst = Vector3.Distance(pattern[i - 1], pattern[i]);
            var currentFit = Vector3.Distance(pattern[i - 1], point) + Vector3.Distance(point, pattern[i]) - baseDst;
            bestFit = Mathf.Min(bestFit, currentFit);
          }
          worstBestFit = Mathf.Max(worstBestFit, bestFit);
        }
        // make sure the point lies within a valid margin of error
        Debug.Log("worst best fit: " + worstBestFit);
        if (!(worstBestFit < validDistanceError))
        {
          drawnShapeIsValid = false;
        }
      }

      // Ensure the total drawn length is sufficient to pass the pattern lock
      var totLength = 0.0f;
      foreach (var line in validLines)
      {
        var lineLength = 0.0f;
        for (var i = 1; i < line.Count; i++)
        {
          lineLength += Vector3.Distance(line[i - 1], line[i]);
        }
        totLength += lineLength;
      }

      if (drawnShapeIsValid)
      {
        Debug.Log("totLength: " + totLength + " patternLength: " + patternLength);
        if (totLength > (patternLength - validLengthError) && totLength < (patternLength + validLengthError))
        {
          patternFound = true;
        }
      }

      if (patternFound)
      {
        doorLocked = false;
      }
      else
      {
        doorLocked = true;
      }
      canFire = true;
    }
  }


  float GetPatternLength()
  {
    var patternLength = 0.0f;
    for (var i = 1; i < pattern.Count; i += 2)
    {
      patternLength += Vector3.Distance(pattern[i - 1], pattern[i]);
    }
    return patternLength;
  }

  public void ActivatePen()
  {
    penActivated = true;
  }
}