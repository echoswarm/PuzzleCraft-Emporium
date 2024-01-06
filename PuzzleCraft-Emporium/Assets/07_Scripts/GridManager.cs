using UnityEngine;
using System.Collections;
public class GridManager : MonoBehaviour
{
    public int gridWidth = 6;
    public int gridHeight = 12;
    public float cellSize = 1.0f;
    public Vector2 gridOrigin = new Vector2(-2.5f, -5.5f);
    public GameObject gridBackgroundPrefab;
    private GameObject[,] gridObjects;

    // Singleton pattern
    private static GridManager instance;
    public static GridManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GridManager>();
                if (instance == null)
                {
                    instance = new GameObject("Spawned GridManager", typeof(GridManager)).GetComponent<GridManager>();
                }
            }
            return instance;
        }
        set
        {
            instance = value;
        }
    }
    //don't destroy on load
    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
        InitializeGrid();
    }
    private void OnDrawGizmos()
    {
        DrawGridGizmos();
    }
    private void InitializeGrid()
    {
        gridObjects = new GameObject[gridWidth, gridHeight];
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Instantiate(gridBackgroundPrefab, GridToWorldPosition(x, y), Quaternion.identity, transform);
            }
        }
    }
    private void DrawGridGizmos()
    {
        Gizmos.color = Color.white;
        for (int x = 0; x <= gridWidth; x++)
        {
            Gizmos.DrawLine(new Vector2(gridOrigin.x + x * cellSize, gridOrigin.y), 
                            new Vector2(gridOrigin.x + x * cellSize, gridOrigin.y + gridHeight * cellSize));
        }
        for (int y = 0; y <= gridHeight; y++)
        {
            Gizmos.DrawLine(new Vector2(gridOrigin.x, gridOrigin.y + y * cellSize), 
                            new Vector2(gridOrigin.x + gridWidth * cellSize, gridOrigin.y + y * cellSize));
        }
    }
    public Vector2Int WorldToGridPosition(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt((worldPosition.x - gridOrigin.x) / cellSize);
        int y = Mathf.FloorToInt((worldPosition.y - gridOrigin.y) / cellSize);
        return new Vector2Int(x, y);
    }
    public Vector3 GridToWorldPosition(int x, int y)
    {
        return new Vector3(gridOrigin.x + x * cellSize + cellSize / 2, gridOrigin.y + y * cellSize + cellSize / 2, 0);
    }
    public bool IsWithinGridBounds(int x, int y)
    {
        return x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;
    }
    public bool IsPositionInsideGrid(Vector2 worldPosition)
    {
        Vector2Int gridPosition = WorldToGridPosition(worldPosition);
        return IsWithinGridBounds(gridPosition.x, gridPosition.y);
    }
    public void PlaceObjectAtGridPosition(GameObject obj, int x, int y)
    {
        if (IsWithinGridBounds(x, y))
        {
            gridObjects[x, y] = obj;
            obj.transform.position = GridToWorldPosition(x, y);
        }
    }
    public GameObject GetObjectAtGridPosition(int x, int y)
    {
        if (IsWithinGridBounds(x, y))
        {
            return gridObjects[x, y];
        }
        return null;
    }
    public void RemoveObjectAtGridPosition(int x, int y)
    {
        if (IsWithinGridBounds(x, y))
        {
            gridObjects[x, y] = null;
        }
    }
    public void ClearGridPosition(int x, int y)
    {
        if (IsWithinGridBounds(x, y))
        {
            gridObjects[x, y] = null;
        }
    }

}
