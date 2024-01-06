using UnityEngine;
using System.Collections;

public class GridManager : MonoBehaviour
{
    #region Fields

    // Public fields
    public int gridWidth = 6;
    public int gridHeight = 12;
    public float cellSize = 1.0f;
    public Vector2 gridOrigin = new Vector2(-2.5f, -5.5f);
    public GameObject gridBackgroundPrefab;
    public GameObject player;

    // Private fields
    private GameObject[,] gridObjects;

    #endregion

    #region Enums

    // Enumerations
    public enum CoinState
    {
        Idle,
        Moving,
        Combining
    }

    #endregion

    #region MonoBehaviour Callbacks

    // Awake callback
    private void Awake()
    {
        InitializeGrid();
    }

    // OnDrawGizmos callback
    private void OnDrawGizmos()
    {
        DrawGridGizmos();
    }

    #endregion

    #region Initialization and Drawing

    // Grid initialization
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

    // Draw grid gizmos
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

    #endregion

    #region Grid Position and Bounds Handling

    // Convert world position to grid position
    public Vector2Int WorldToGridPosition(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt((worldPosition.x - gridOrigin.x) / cellSize);
        int y = Mathf.FloorToInt((worldPosition.y - gridOrigin.y) / cellSize);
        return new Vector2Int(x, y);
    }

    // Convert grid position to world position
    public Vector3 GridToWorldPosition(int x, int y)
    {
        return new Vector3(gridOrigin.x + x * cellSize + cellSize / 2, gridOrigin.y + y * cellSize + cellSize / 2, 0);
    }

    // Check if position is within grid bounds
    public bool IsWithinGridBounds(int x, int y)
    {
        return x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;
    }

    // Check if position is inside grid
    public bool IsPositionInsideGrid(Vector2 worldPosition)
    {
        Vector2Int gridPosition = WorldToGridPosition(worldPosition);
        return IsWithinGridBounds(gridPosition.x, gridPosition.y);
    }

    #endregion

    #region Grid Object Management

    // Place object at grid position
    public void PlaceObjectAtGridPosition(GameObject obj, int x, int y)
    {
        if (IsWithinGridBounds(x, y))
        {
            gridObjects[x, y] = obj;
            obj.transform.position = GridToWorldPosition(x, y);
        }
    }

    // Get object at grid position
    public GameObject GetObjectAtGridPosition(int x, int y)
    {
        if (IsWithinGridBounds(x, y))
        {
            return gridObjects[x, y];
        }
        return null;
    }

    // Remove object at grid position
    public void RemoveObjectAtGridPosition(int x, int y)
    {
        if (IsWithinGridBounds(x, y))
        {
            gridObjects[x, y] = null;
        }
    }

    // Clear grid position
    public void ClearGridPosition(int x, int y)
    {
        if (IsWithinGridBounds(x, y))
        {
            gridObjects[x, y] = null;
        }
    }

    // Clear coin
    public void ClearCoin(Vector2Int position)
    {
        GameObject coinObj = GetObjectAtGridPosition(position.x, position.y);
        if (coinObj != null)
        {
            Destroy(coinObj);
            RemoveObjectAtGridPosition(position.x, position.y);
        }
    }

    #endregion

    #region Coin Management

    // Check if coin is moving
    public bool IsCoinMoving(GameObject coin)
    {
        Vector2Int gridPosition = WorldToGridPosition(coin.transform.position);
        Vector3 expectedWorldPosition = GridToWorldPosition(gridPosition.x, gridPosition.y);
        return Vector3.SqrMagnitude(coin.transform.position - expectedWorldPosition) > 0.0001f;
    }

    // Get coin value
    public int GetCoinValue(Vector2Int position)
    {
        GameObject coinObj = GetObjectAtGridPosition(position.x, position.y);
        if (coinObj != null)
        {
            Coin coin = coinObj.GetComponent<Coin>();
            if (coin != null)
            {
                return coin.Value;
            }
        }
        return 0;
    }

    // Get coin at grid position
    public Coin GetCoinAtGridPosition(int x, int y)
    {
        GameObject obj = GetObjectAtGridPosition(x, y);
        if (obj != null)
        {
            return obj.GetComponent<Coin>();
        }
        return null;
    }

    // Set coin
    public void SetCoin(Vector2Int position, GameObject coin)
    {
        // Move upwards in the column until an empty slot is found or the top is reached.
        while (position.y < gridHeight && GetObjectAtGridPosition(position.x, position.y) != null)
        {
            position.y += 1;
        }

        // If the top is reached and still no slot is found, don't place the coin.
        if (position.y >= gridHeight)
        {
            Destroy(coin);
            return;
        }

        // Place the coin in the empty slot found.
        PlaceObjectAtGridPosition(coin, position.x, position.y);
    }

    #endregion

    #region Coin Movement and Placement

    // Check if position is available for player
    public bool IsPositionAvailableForPlayer(Vector2Int gridPosition)
    {
        return IsWithinGridBounds(gridPosition.x, gridPosition.y) && GetObjectAtGridPosition(gridPosition.x, gridPosition.y) == null;
    }

    // Check if move is valid
    public bool IsValidMove(Vector2Int gridPosition, CoinState state)
    {
        if (state == CoinState.Idle)
        {
            return IsWithinGridBounds(gridPosition.x, gridPosition.y) && GetObjectAtGridPosition(gridPosition.x, gridPosition.y) == null;
        }
        else if (state == CoinState.Moving || state == CoinState.Combining)
        {
            return true;
        }
        return false;
    }

    // Atomic place or move coin
    public bool AtomicPlaceOrMoveCoin(GameObject coin, Vector2Int newPosition)
    {
        if (!IsValidMove(newPosition, CoinState.Moving))
        {
            return false;
        }
        PlaceObjectAtGridPosition(coin, newPosition.x, newPosition.y);
        return true;
    }

    // Move coin
    public IEnumerator MoveCoin(Vector2Int startPosition, Vector2Int endPosition, float duration)
    {
        GameObject coin = GetObjectAtGridPosition(startPosition.x, startPosition.y);
        if (coin == null) yield break;

        Vector3 startWorldPosition = GridToWorldPosition(startPosition.x, startPosition.y);
        Vector3 endWorldPosition = GridToWorldPosition(endPosition.x, endPosition.y);

        // Update position in the grid array before moving the coin
        UpdateCoinPositionInGrid(startPosition, endPosition, coin);

        for (float elapsedTime = 0f; elapsedTime < duration; elapsedTime += Time.deltaTime)
        {
            if (coin == null) yield break;

            float t = Mathf.Clamp01(elapsedTime / duration);
            coin.transform.position = Vector3.Lerp(startWorldPosition, endWorldPosition, t);

            yield return null;
        }

        coin.transform.position = endWorldPosition;
    }

    private void UpdateCoinPositionInGrid(Vector2Int startPosition, Vector2Int endPosition, GameObject coin)
    {
        RemoveObjectAtGridPosition(startPosition.x, startPosition.y);
        PlaceObjectAtGridPosition(coin, endPosition.x, endPosition.y);
    }

    #endregion
}
