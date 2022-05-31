using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class MazeGenerator : MonoBehaviour
{   public GameObject player;
    public GameObject Straight;
    public GameObject Turn;
    public GameObject TCross;
    public GameObject Crossing;
    public GameObject DeadEnd;
    // public int wallWidth = 24;
    // public GameObject enemy;
    public int size = 10;
    public int renderDistance = 5;
    public int destroyDistance = 7;
    public int enemycount = 3;
    public bool pieces = true;
    
    // Maze values
    private int cellCount;
    private int visitedCellCount = 0;
    // walls are stored as 1's in array of size = (size + 1)**2
    // walls[wallType, row, col] holds all walls
    private int[,,] walls;
   
    // Wall list keeps track of which walls are open for visiting
    // and have the potential of being deleted
    private ArrayList seenWalls;
    
    // Visited cells
    private int[,] visitedCells;

    private Tuple<PieceType, int>[,] MazePieces;
    private int[,] IsInstantiated;
    private GameObject[,] InstantiatedPieces;
    
    enum WallType : int
    {
        Horizontal = 0,
        Vertical = 1
    }

    enum PieceType : int {
        DeadEnd = 0,
        Straight = 1,
        Turn = 2,
        TCross = 3,
        Crossing = 4,
    }
    
    // Start is called before the first frame update
    void Start()
    {
        cellCount = size * size;
        walls = new int[2, size + 1, size + 1];
        visitedCells = new int[size, size];
        seenWalls = new ArrayList();
        MazePieces = new Tuple<PieceType, int>[size, size];
        IsInstantiated = new int[size, size];
        InstantiatedPieces = new GameObject[size, size];
        
        InitializeWalls();
        InitializeVisitedCells();
        GenerateMaze();
        // Remove dead ends with length 1
        // FilterDeadEnds();
        // PrintWalls();
        InstantiateMaze();
    }

    void Update() {
        // Instantiate maze while the player walks through it at some distance away, perhaps the next one and a half turns away or create some mist I guess
        // We want to make sure to instantiate new pieces as the player comes near and remove pieces that the player leaves
        // each piece has a width/height of 15 units so by dividing the player position we adjust the values such that it's well suited for accessing the indexes of rows/cols
        var playerRow = (int)Mathf.Floor((player.transform.position.z + 14F) / 15F);
        var playerCol = (int)Mathf.Floor((player.transform.position.x + 14F) / 15F);
        // We want pieces in accordance with the following schema to always be instantiated:
        //      o
        //    o o o
        //  o o x o o
        //    o o o
        //      o
        // It's possible to assign a number to each piece in the maze measuring the distance from the starting point
        // this could be used to render pieces more efficiently.
        var startRow = (int)Mathf.Max(0, playerRow - renderDistance);
        var startCol = (int)Mathf.Max(0, playerCol - renderDistance);
        var endRow = (int)Mathf.Min(playerRow + renderDistance, size);
        var endCol = (int)Mathf.Min(playerCol + renderDistance, size);
        for (var row = startRow; row < endRow; row++) {
            for (var col = startCol; col < endCol; col++) {
                if (row + col < playerRow + playerCol + renderDistance && row + col > playerRow + playerCol - renderDistance && !(IsInstantiated[row, col] == 1)) {
                    // If piece isn't already instantiated then instantiate it
                    var gameObject = DeadEnd;
                    if (MazePieces[row, col].Item1 == PieceType.Straight) {
                        gameObject = Straight;
                    } else if (MazePieces[row, col].Item1 == PieceType.Turn) {
                        gameObject = Turn;
                    } else if (MazePieces[row, col].Item1 == PieceType.TCross) {
                        gameObject = TCross;
                    } else if (MazePieces[row, col].Item1 == PieceType.Crossing) {
                        gameObject = Crossing;
                    }
                    InstantiatedPieces[row, col] = Instantiate(gameObject, new Vector3(col * 15f, 0, row * 15f), Quaternion.Euler(Vector3.up * MazePieces[row,  col].Item2));
                    IsInstantiated[row, col] = 1;
                }
            }
        }
        // // Check if we can destroy any piece
        var destroyStartRow = Mathf.Max(0, startRow - destroyDistance);
        var destroyStartCol = Mathf.Max(0, startCol - destroyDistance);
        var destroyEndRow = Mathf.Min(size, endRow + destroyDistance);
        var destroyEndCol = Mathf.Min(size, endCol + destroyDistance);
        for (var row = destroyStartRow; row < destroyEndRow; row++) {
            for (var col = destroyStartCol; col < destroyEndCol; col++) {
                if (!(row >= startRow && row < endRow && col >= startCol && col < endCol) && IsInstantiated[row, col] == 1) {
                    if (!(row == playerRow && col > startCol && col < endCol) && !(col == playerCol && row > startRow && row < endRow)) {
                        Destroy(InstantiatedPieces[row, col]);
                        IsInstantiated[row, col] = 0;
                    }
                }
            }
        }
        // if (startRow > 0 && IsInstantiated[startRow - 1, playerCol] == 1) {
        //     Destroy(InstantiatedPieces[startRow - 1, playerCol]);
        //     IsInstantiated[startRow - 1, playerCol] = 0;
        // }
        // if (endRow < size && IsInstantiated[endRow, playerCol] == 1) {
        //     Destroy(InstantiatedPieces[endRow, playerCol]);
        //     IsInstantiated[endRow, playerCol] = 0;
        // }
        // if (startCol > 0 && IsInstantiated[playerRow, startCol] == 1) {
        //     Destroy(InstantiatedPieces[playerRow, startCol]);
        //     IsInstantiated[playerRow, startCol] = 0;
        // }
        // if (endCol < size && IsInstantiated[playerRow, endCol] == 1) {
        //     Destroy(InstantiatedPieces[playerRow, endCol]);
        //     IsInstantiated[playerRow, endCol] = 0;
        // }
    }

    /**
     * generateMaze uses Randomized Prim's algorithm:
     * Start by picking random cell and add it's walls to list.
     * Pick random wall from list. If wall is adjacent to an unvisited
     * cell remove the wall and add the walls of the adjacent cell to
     * the list. Repeat.
     */
    void GenerateMaze()
    {
        // We start out by picking a random cell within the maze boundaries
        var startingCell = Tuple.Create(Random.Range(0, size), Random.Range(0, size));
        // We now consider this starting cell as seen
        visitedCells[startingCell.Item1, startingCell.Item2] = 1;
        visitedCellCount++;
        // We also consider the walls adjacent to the sell as seen
        seenWalls.AddRange(GetWalls(startingCell.Item1, startingCell.Item2));
        // We will remove walls until the point where each cell in within the maze boundaries have been visited
        while (visitedCellCount < cellCount) {
            // We want to pick a random wall among the seen walls
            var randomWallIndex = Random.Range(0, seenWalls.Count);
            // Now we check if there's a cell on either side of the wall which is unvisited as of yet
            var willRemoveWall = CanRemoveWall(randomWallIndex);
            // If there's an adjacent cell which is unvisited we will remove the wall
            if (willRemoveWall) {
                // We start out by fetching the cell which is unvisited
                var unvisitedCell = GetUnvisitedCell((Tuple<WallType, int, int>)seenWalls[randomWallIndex]);
                // We can now consider the cell as having been visited
                visitedCells[unvisitedCell.Item1, unvisitedCell.Item2] = 1;
                visitedCellCount++;
                // Now we can "remove" the wall by assigning it the value 0
                walls[(int)((Tuple<WallType, int, int>)seenWalls[randomWallIndex]).Item1, ((Tuple<WallType, int, int>)seenWalls[randomWallIndex]).Item2, ((Tuple<WallType, int, int>)seenWalls[randomWallIndex]).Item3] = 0;
                // We also remove the wall from the seenWalls list so we don't pick it again since it's now adjacent to two visited cells
                seenWalls.RemoveAt(randomWallIndex);
                // We want to add the newly visited cells remaining walls to the list of seen walls for potential future removal
                seenWalls.AddRange(GetWalls(unvisitedCell.Item1, unvisitedCell.Item2));
            } else {
                // If the wall didn't have any unvisited adjacent cells we can remove it from the list of seen walls so we don't pick it again later
                seenWalls.RemoveAt(randomWallIndex);
            }
        }
    }
    /*
    * Initiates the main player
    */
    void InstantiatePlayer() {
        Instantiate(player, new Vector3(1.0F, 0, 1.0F), Quaternion.identity);
    }

    // Rotate uninteresting dead ends (length 1)
    // void FilterDeadEnds() {
    //     Debug.Log("Filtering dead ends");
    //     for (var row = 0; row < size; row++) {
    //         for (var col = 0; col < size; col++) {
    //             if (GetWallCount(row, col) == 3) {
    //                 var rotation = GetDeadEndRotation(row, col);
    //                 if (rotation == 0) {
    //                     var nextCellWallCount = GetWallCount(row, col - 1);
    //                     if (nextCellWallCount > 2) {
    //                         if (MazePieces[row, col].Item2 == 0) {
    //                             MazePieces[row, col] = Tuple.Create(MazePieces[row, col].Item1, 180);
    //                             Debug.Log("Rotated dead end");
    //                         } else if (MazePieces[row, col].Item2 == 180) {
    //                             MazePieces[row, col] = Tuple.Create(MazePieces[row, col].Item1, 0);
    //                             Debug.Log("Rotated dead end");
    //                         } else if (MazePieces[row, col].Item2 == -90) {
    //                             MazePieces[row, col] = Tuple.Create(MazePieces[row, col].Item1, 90);
    //                             Debug.Log("Rotated dead end");
    //                         } else if (MazePieces[row, col].Item2 == 90) {
    //                             MazePieces[row, col] = Tuple.Create(MazePieces[row, col].Item1, -90);
    //                             Debug.Log("Rotated dead end");
    //                         }
    //                     }
    //                 } else if (rotation == 90) {
    //                     var nextCellWallCount = GetWallCount(row + 1, col);
    //                     if (nextCellWallCount > 2) {
    //                         if (MazePieces[row, col].Item2 == 0) {
    //                             MazePieces[row, col] = Tuple.Create(MazePieces[row, col].Item1, 180);
    //                             Debug.Log("Rotated dead end");
    //                         } else if (MazePieces[row, col].Item2 == 180) {
    //                             MazePieces[row, col] = Tuple.Create(MazePieces[row, col].Item1, 0);
    //                             Debug.Log("Rotated dead end");
    //                         } else if (MazePieces[row, col].Item2 == -90) {
    //                             MazePieces[row, col] = Tuple.Create(MazePieces[row, col].Item1, 90);
    //                             Debug.Log("Rotated dead end");
    //                         } else if (MazePieces[row, col].Item2 == 90) {
    //                             MazePieces[row, col] = Tuple.Create(MazePieces[row, col].Item1, -90);
    //                             Debug.Log("Rotated dead end");
    //                         }
    //                     }
    //                 } else if (rotation == 180) {
    //                     var nextCellWallCount = GetWallCount(row, col + 1);
    //                     if (nextCellWallCount > 2) {
    //                         if (MazePieces[row, col].Item2 == 0) {
    //                             MazePieces[row, col] = Tuple.Create(MazePieces[row, col].Item1, 180);
    //                             Debug.Log("Rotated dead end");
    //                         } else if (MazePieces[row, col].Item2 == 180) {
    //                             MazePieces[row, col] = Tuple.Create(MazePieces[row, col].Item1, 0);
    //                             Debug.Log("Rotated dead end");
    //                         } else if (MazePieces[row, col].Item2 == -90) {
    //                             MazePieces[row, col] = Tuple.Create(MazePieces[row, col].Item1, 90);
    //                             Debug.Log("Rotated dead end");
    //                         } else if (MazePieces[row, col].Item2 == 90) {
    //                             MazePieces[row, col] = Tuple.Create(MazePieces[row, col].Item1, -90);
    //                             Debug.Log("Rotated dead end");
    //                         }
    //                     }
    //                 } else if (rotation == -90) {
    //                     var nextCellWallCount = GetWallCount(row - 1, col);
    //                     if (nextCellWallCount > 2) {
    //                         if (MazePieces[row, col].Item2 == 0) {
    //                             MazePieces[row, col] = Tuple.Create(MazePieces[row, col].Item1, 180);
    //                             Debug.Log("Rotated dead end");
    //                         } else if (MazePieces[row, col].Item2 == 180) {
    //                             MazePieces[row, col] = Tuple.Create(MazePieces[row, col].Item1, 0);
    //                             Debug.Log("Rotated dead end");
    //                         } else if (MazePieces[row, col].Item2 == -90) {
    //                             MazePieces[row, col] = Tuple.Create(MazePieces[row, col].Item1, 90);
    //                             Debug.Log("Rotated dead end");
    //                         } else if (MazePieces[row, col].Item2 == 90) {
    //                             MazePieces[row, col] = Tuple.Create(MazePieces[row, col].Item1, -90);
    //                             Debug.Log("Rotated dead end");
    //                         }
    //                     }
    //                 }
    //             }
    //         }
    //     }
    // }

    /*
    * Initiates the enemies
    */
    // void InstantiateEnemies() {
    //     for( var i=0; i<enemycount; i++) {
    //       Instantiate(enemy, new Vector3(Random.Range(0, (size-1))*3.0F+1, 0, Random.Range(0, (size-1))*3.0F+1), Quaternion.identity);
    //     }
    // }

    /**
     * After maze has been generated it can be instantiated with this method
     * by placing the loaded prefabs according to the generated maze.
     */
    void InstantiateMaze() {

        // Iterate over each cell and depending on which walls are present instantiate the appropriate piece
        for (var row = 0; row < size; row++) {
            for (var col = 0; col < size; col++) {
                // The cell wall count is an indicator of which piece should be picked
                var wallCount = GetWallCount(row, col);
                var rotation = 0;
                var pieceType = PieceType.Straight;
                // Pick the correct piece
                if (wallCount == 1) {
                    // The piece should be a t-crossing
                    rotation = GetTCrossRotation(row, col);
                    pieceType = PieceType.TCross;
                } else if (wallCount == 2) {
                    // The piece should either be straight or a turn
                    if ((walls[0, row, col] == 0 && walls[0, row + 1, col] == 0) || (walls[1, row, col] == 0 && walls[1, row, col + 1] == 0)) {
                        // The piece should be straight
                        rotation = GetStraightRotation(row, col);
                        pieceType = PieceType.Straight;
                    } else {
                        // The piece should be a turn
                        rotation = GetTurnRotation(row, col);
                        pieceType = PieceType.Turn;
                    }
                } else if (wallCount == 3) {
                    // The piece is a dead end
                    rotation = GetDeadEndRotation(row, col);
                    pieceType = PieceType.DeadEnd;
                } else if (wallCount == 0) {
                    // The piece should be a crossing
                    pieceType = PieceType.Crossing;
                } else if (wallCount == 4) {
                    Debug.LogError("Wall count = 4 so maze is not finished");
                }
                // Add piece to list of potential pieces to get instantiated in the future
                // Instantiate(gameObject, new Vector3(col * 15F, 0, row * 15F), Quaternion.Euler(Vector3.up * rotation));
                MazePieces[row, col] = new Tuple<PieceType, int>(pieceType, rotation);
            }
        }

        // InstantiateFloors();
        // InstantiateWalls();
    }
    
    int GetDeadEndRotation(int row, int col) {
        if (walls[1, row, col] == 0) {
            return 0;
        } else if (walls[0, row, col] == 0) {
            return -90;
        } else if (walls[1, row, col + 1] == 0) {
            return 180;
        } else if (walls[0, row + 1, col] == 0) {
            return 90;
        } else {
            Debug.LogError("Didn't find missing wall for getting dead end rotation on row: " + row + " col: " + col);
            return -45;
        }
    }
    
    int GetStraightRotation(int row, int col) {
        if (walls[0, row, col] == 0) {
            return 90;
        } else if (walls[1, row, col] == 0) {
            return 0;
        } else {
            Debug.LogError("Didn't find missing wall for getting straight rotation on row: " + row + " col: " + col);
            return -45;
        }
    }
    
    int GetTurnRotation(int row, int col) {
        if (walls[0, row + 1, col] == 0 && walls[1, row, col] == 0) {
            return 90;
        } else if (walls[1, row, col] == 0 && walls[0, row, col] == 0) {
            return 0;
        } else if (walls[0, row, col] == 0 && walls[1, row, col + 1] == 0) {
            return -90;
        } else if (walls[1, row, col + 1] == 0 && walls[0, row + 1, col] == 0) {
            return 180;
        } else {
            Debug.LogError("Didn't find missing walls for getting turn rotation on row: " + row + " col: " + col);
            return -45;
        }
    }
    
    int GetTCrossRotation(int row, int col) {
        if (walls[0, row, col] == 1) {
            return 180;
        } else if (walls[1, row, col + 1] == 1) {
            return 90;
        } else if (walls[0, row + 1, col] == 1) {
            return 0;
        } else if (walls[1, row, col] == 1) {
            return -90;
        } else {
            Debug.LogError("Didn't find present wall for getting t-cross rotation on row: " + row + " col: " + col);
            return -45;
        }
    }

    /**
     *  Instantiates the floor
     *
     *  (the strange -1's are there to put the floors evenly around the generated maze
     *  can probably be fixed to look a bit better somehow)
     */
    // void InstantiateFloors() {
    //     for (var i = 0; i < size * + 4; i++) {
    //         for (var j = 0; j < size + 4; j++) {
    //             Instantiate(floor, new Vector3((i - 2) * (float) wallWidth, 0, (j - 2) * (float) wallWidth), Quaternion.identity);
    //         }
    //     }
    // }

    /**
     *  Instantiates the walls
     */
    // void InstantiateWalls() {
    //     for (var i = 0; i < size + 1; i++) {
    //         for (var j = 0; j < size + 1; j++) {
    //             if (walls[(int) WallType.Horizontal, i, j] == 1) {
    //                 Instantiate(wall, new Vector3(i * (float) wallWidth, 0, j * (float) wallWidth), Quaternion.Euler(Vector3.up * -90));
    //             }
    //             if (walls[(int) WallType.Vertical, i, j] == 1) {
    //                 Instantiate(wall, new Vector3(i * (float) wallWidth, 0, j * (float) wallWidth), Quaternion.identity);
    //             }
    //         }
    //     }
    // }
    
    /*
     * Initializes the walls by assigning them the value 1 in the list of walls, each cell is surrounded by walls to
     * start with. 
     */
    void InitializeWalls()
    {
        // Initializes most of the maze
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                walls[(int) WallType.Horizontal, i, j] = 1;
                walls[(int) WallType.Vertical, i, j] = 1;
            }
        }
        // Initializes bottom and right border walls
        for (int i = 0; i < size; i++)
        {
            walls[(int)WallType.Horizontal, size, i] = 1;
            walls[(int)WallType.Vertical, i, size] = 1;
        }
    }

    void InitializeVisitedCells() {
        for (var i = 0; i < size; i++) {
            for (var j = 0; j < size; j++) {
                visitedCells[i, j] = 0;
            }
        }
    }
    
    /*
     * Get the list of walls around specified cell
     */
    ArrayList GetWalls(int cellRow, int cellCol) {
        ArrayList seenWalls = new ArrayList();
        // Check for top wall
        if (walls[(int)WallType.Horizontal, cellRow, cellCol] == 1) {
            seenWalls.Add(new Tuple<WallType, int, int>(WallType.Horizontal, cellRow, cellCol));
        }
        // Check for left wall
        if (walls[(int)WallType.Vertical, cellRow, cellCol] == 1) {
            seenWalls.Add(new Tuple<WallType, int, int>(WallType.Vertical, cellRow, cellCol));
        }
        // Check for bottom wall
        if (walls[(int)WallType.Vertical, cellRow + 1, cellCol] == 1) {
            seenWalls.Add(new Tuple<WallType, int, int>(WallType.Horizontal, cellRow + 1, cellCol));
        }
        // Check for right wall
        if (walls[(int)WallType.Horizontal, cellRow, cellCol + 1] == 1) {
            seenWalls.Add(new Tuple<WallType, int, int>(WallType.Vertical, cellRow, cellCol + 1));
        }
        return seenWalls;
    }

    int GetWallCount(int row, int col) {
        int wallCount = 0;
        // Check for top wall
        if (walls[1, row, col] == 1) {
            wallCount++;
        }
        // Check for left wall
        if (walls[0, row, col] == 1) {
            wallCount++;
        }
        // Check for bottom wall
        if (walls[1, row, col + 1] == 1) {
            wallCount++;
        }
        // Check for right wall
        if (walls[0, row + 1, col] == 1) {
            wallCount++;
        }
        return wallCount;
    }

    /*
     * CanRemoveWall figures out if one of the cells adjacent to the
     * wall is unvisited. If so then the wall can be removed.
     */
    bool CanRemoveWall(int seenWallIndex) {
        // Start by fetching the wall from the seen walls list
        var wall = (Tuple<WallType, int, int>)seenWalls[seenWallIndex];
        // First element of wall indicates if it's vertical or horizontal
        var wallType = wall.Item1;
        // Second element of wall indicates which row it exists on
        var wallRow = wall.Item2;
        // Third element of wall indicates which col it exists on
        var wallCol = wall.Item3;
        // If it's a horizontal wall we want to check if either the cell above or below is unvisited
        // if so we can safely remove the wall.
        if (wallType == (int)WallType.Horizontal) {
            // We don't want to check cells outside the maze since they don't exist.
            if (wallRow > 0 && wallRow < size) {
                // If either the cell above or below the wall is unvisited then we return true
                if (visitedCells[wallRow - 1, wallCol] == 0 || visitedCells[wallRow, wallCol] == 0) {
                    return true;
                }
            }
            return false;
            // If it's a vertical wall we want to check if either the cell to the left or to the
            // right are unvisited and if so we can safely remove the wall.
        } else {
            // We don't want to check cells outside maze
            if (wallCol > 0 && wallCol < size) {
                // If either the cell to the left or the cell to the right are unvisited wall can be removed
                if (visitedCells[wallRow, wallCol - 1] == 0 || visitedCells[wallRow, wallCol] == 0) {
                    return true;
                }
            }
            return false;
        }
    }

    /*
     * GetUnvisitedCell returns which of the adjacent cells are unvisited
     */
    Tuple<int, int> GetUnvisitedCell(Tuple<WallType, int, int> wall) {
        // sw = seen wall which is what we should put into this method
        var swType = wall.Item1;
        var swRow = wall.Item2;
        var swCol = wall.Item3;
        // Return first unvisited cell since one is most certainly visited
        if (visitedCells[swRow, swCol] == 0) {
            return new Tuple<int, int>(swRow, swCol);
        }
        // (Cell can't be near edge so no need to check)
        if (swType == (int)WallType.Horizontal) {
            if (visitedCells[swRow - 1, swCol] == 0) {
                return new Tuple<int, int>(swRow - 1, swCol);
            }
        } else {
            if (visitedCells[swRow, swCol - 1] == 0) {
                return new Tuple<int, int>(swRow, swCol - 1);
            }
        }
        return new Tuple<int, int>(-1, -1);
    }

    /*
     * For debugging
     */
    void PrintWalls()
    {
        for (int i = 0; i <= size; i++)
        {
            for (int j = 0; j <= size; j++)
            {
                int hw = walls[(int) WallType.Horizontal, i, j];
                int vw = walls[(int) WallType.Vertical, i, j];
                Debug.Log(i + " " + j +" hw = " + hw + ", vw = " + vw);
            }
        }
    }

    void PrintVisitedCells() {
        for (int i = 0; i < size; i++) {
            for (int j = 0; j < size; j++) {
                Debug.Log("Cell at i = " + i + ", j = " + j + ", visited = " + visitedCells[i, j]);
            }
        }
    }

    void PrintSeenWalls() {
        Debug.Log("Seen Walls:");
        for (int i = 0; i < seenWalls.Count; i++) {
            Debug.Log(seenWalls[i].ToString());
        }
    }
}
