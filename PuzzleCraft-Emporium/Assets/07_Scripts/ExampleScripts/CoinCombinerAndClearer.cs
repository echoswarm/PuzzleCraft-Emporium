using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CoinCombinerAndClearer : MonoBehaviour
{
    // ====================== FIELD INITIALIZATIONS ======================

    private readonly Dictionary<int, int> nextCoinValueMap = new Dictionary<int, int>
    {
        {1, 5}, {5, 10}, {10, 50}, {50, 100}, {100, 500}, {500, 1000}, {1000, 2000}, {2000, 0}
    };

    private readonly Dictionary<int, int> requiredCoinsForCombinationMap = new Dictionary<int, int>
    {
        {1, 5}, {5, 2}, {10, 5}, {50, 2}, {100, 5}, {500, 2}, {1000, 2}, {2000, 2}
    };

    private Dictionary<int, GameObject> coinPrefabLookup = new Dictionary<int, GameObject>();
    private Dictionary<Vector2Int, int> coinGroups = new Dictionary<Vector2Int, int>();
    private HashSet<int> updatingColumns = new HashSet<int>();

    public GridManager gridManager;
    public CombinedController combinedController;
    public GameObject[] coinPrefabs;

    private bool checkAndCombineCoinsInProgress = false;
    private int lastCombinedCoinValue = 0;
    private int nextGroupID = 1;

    // ====================== INITIALIZATION & CLEANUP ======================

    private void Start()
    {
        InitializeCoinPrefabLookup();
        combinedController.OnCoinShot += OnCoinShot;
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    private void InitializeCoinPrefabLookup()
    {
        foreach (var coinPrefab in coinPrefabs)
        {
            Coin coin = coinPrefab.GetComponent<Coin>();
            if (coin != null)
            {
                coinPrefabLookup[coin.Value] = coinPrefab;
            }
        }
    }

    private void Cleanup()
    {
        StopAllCoroutines();
        combinedController.OnCoinShot -= OnCoinShot;
    }

    // ====================== EVENT HANDLING & MAIN LOOP ======================

    private void Update()
    {
        HandleCoinShotEvents();
    }

    private void HandleCoinShotEvents()
    {
        if (combinedController.HasShotCoin && !checkAndCombineCoinsInProgress)
        {
            Vector2Int lastCoinGridPosition = combinedController.LastShotCoinGridPosition;
            StartCoroutine(CheckAndCombineCoinsAfterDelay(lastCoinGridPosition));
            combinedController.HasShotCoin = false;
            checkAndCombineCoinsInProgress = true;
        }
    }

    public void OnCoinShot(GameObject shotCoin)
    {
        if (shotCoin == null) return;
        Vector2Int newCoinPosition = gridManager.WorldToGridPosition(shotCoin.transform.position);
        UpdateCoinGroups(newCoinPosition);
    }

    // ====================== COIN VALUE & GROUP MANAGEMENT ======================

    public int GetNextCoinValue(int currentValue)
    {
        nextCoinValueMap.TryGetValue(currentValue, out int nextValue);
        return nextValue;
    }

    private void UpdateCoinGroups(Vector2Int newCoinPosition)
    {
        int targetCoinValue = GetTargetCoinValue(newCoinPosition);
        List<Vector2Int> connectedCoins = CollectConnectedCoins(newCoinPosition, targetCoinValue, true);
        int newGroupID = nextGroupID++;
        connectedCoins.ForEach(pos => coinGroups[pos] = newGroupID);
    }

    private int GetTargetCoinValue(Vector2Int position)
    {
        return gridManager.GetCoinValue(position);
    }

    // ====================== COIN COMBINATION LOGIC ======================

    private List<Vector2Int> CollectConnectedCoins(Vector2Int startPosition, int targetCoinValue, bool skipInitialCheck)
    {
        if (skipInitialCheck)
        {
            return FindAdjacentConnectedCoins(startPosition, targetCoinValue);
        }
        else
        {
            return FindConnectedCoins(startPosition, targetCoinValue);
        }
    }

    private List<Vector2Int> FindAdjacentConnectedCoins(Vector2Int startPosition, int targetCoinValue)
    {
        List<Vector2Int> connectedCoins = new List<Vector2Int>();
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var direction in directions)
        {
            Vector2Int adjacentPosition = startPosition + direction;
            if (gridManager.IsWithinGridBounds(adjacentPosition.x, adjacentPosition.y))
            {
                connectedCoins.AddRange(FindConnectedCoins(adjacentPosition, targetCoinValue));
            }
        }
        return connectedCoins;
    }

    private List<Vector2Int> FindConnectedCoins(Vector2Int startPosition, int targetCoinValue)
    {
        HashSet<Vector2Int> connectedCoins = new HashSet<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(startPosition);
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            connectedCoins.Add(current);

            foreach (var direction in directions)
            {
                Vector2Int newPosition = current + direction;
                if (gridManager.IsWithinGridBounds(newPosition.x, newPosition.y) &&
                    !connectedCoins.Contains(newPosition) &&
                    gridManager.GetCoinValue(newPosition) == targetCoinValue)
                {
                    queue.Enqueue(newPosition);
                }
            }
        }
        return connectedCoins.ToList();
    }

    private bool HasEnoughCoinsForCombination(int coinCount, int requiredCoins)
    {
        return coinCount >= requiredCoins;
    }

    private void HandleCoinCombination(List<Vector2Int> connectedCoins, int targetCoinValue, Vector2Int startPosition)
    {
        StartCoroutine(targetCoinValue == 500 && connectedCoins.Count >= 2
            ? Handle500CoinCombination(connectedCoins)
            : HandleNon500CoinCombination(connectedCoins, targetCoinValue, startPosition));
    }

    // ====================== COROUTINE METHODS ======================

    private IEnumerator HandleNon500CoinCombination(List<Vector2Int> connectedCoins, int targetCoinValue, Vector2Int startPosition)
    {
        yield return new WaitForSeconds(0.1f);
        if (!(targetCoinValue == 2000 && connectedCoins.Count < 2))
        {
            foreach (var position in connectedCoins)
            {
                gridManager.ClearCoin(position);
            }
            FillEmptyCells(connectedCoins);
            int combinedCoinValue = GetNextCoinValue(targetCoinValue);
            SpawnCombinedCoin(startPosition, combinedCoinValue);
            lastCombinedCoinValue = combinedCoinValue;
            yield return CheckAndCombineCoinsAfterDelay(startPosition, 0.1f);
        }
    }

    private IEnumerator Handle500CoinCombination(List<Vector2Int> connectedCoins)
    {
        yield return new WaitForSeconds(0.1f);
        foreach (var position in connectedCoins)
        {
            gridManager.ClearCoin(position);
        }
        FillEmptyCells(connectedCoins);
        int combinedCoinValue = GetNextCoinValue(500);
        SpawnCombinedCoin(connectedCoins[0], combinedCoinValue);
        lastCombinedCoinValue = combinedCoinValue;
        yield return CheckAndCombineCoinsAfterDelay(connectedCoins[0], 0.1f);
    }

    // ====================== GRID & COLUMN MANAGEMENT ======================

    private void FillEmptyCells(List<Vector2Int> clearedPositions)
    {
        var columnsToUpdate = new HashSet<int>(clearedPositions.Select(position => position.x));
        foreach (var column in columnsToUpdate)
        {
            UpdateColumn(column);
        }
    }

    private void UpdateColumn(int column)
    {
        int emptyCellCount = 0;
        for (int row = gridManager.gridHeight - 1; row >= 0; row--)
        {
            Vector2Int currentPosition = new Vector2Int(column, row);
            if (gridManager.GetCoinValue(currentPosition) == 0)
            {
                emptyCellCount++;
            }
            else if (emptyCellCount > 0)
            {
                Vector2Int newPosition = currentPosition + new Vector2Int(0, emptyCellCount);
                StartCoroutine(gridManager.MoveCoin(currentPosition, newPosition, 0.2f));
                currentPosition = newPosition;
            }
        }
    }

    // ====================== COIN SPAWNING ======================

    private void SpawnCombinedCoin(Vector2Int position, int combinedCoinValue)
    {
        GameObject coinPrefab = GetCoinPrefab(combinedCoinValue);
        if (coinPrefab)
        {
            Vector2Int highestEmptyCell = FindHighestEmptyCell(position);
            var newCoin = Instantiate(coinPrefab, gridManager.GridToWorldPosition(highestEmptyCell.x, highestEmptyCell.y), Quaternion.identity);
            gridManager.SetCoin(highestEmptyCell, newCoin);
            StartCoroutine(CheckAndCombineCoinsAfterDelay(highestEmptyCell, 0.7f));
        }
    }

    private Vector2Int FindHighestEmptyCell(Vector2Int startPosition)
    {
        Vector2Int highestEmptyCell = startPosition;
        while (gridManager.IsWithinGridBounds((highestEmptyCell += Vector2Int.up).x, highestEmptyCell.y) &&
               gridManager.GetCoinValue(highestEmptyCell) == 0);
        return highestEmptyCell -= Vector2Int.up;
    }

    // ====================== UTILITY FUNCTIONS ======================

    private GameObject GetCoinPrefab(int coinValue)
    {
        return coinPrefabLookup.TryGetValue(coinValue, out var coinPrefab) ? coinPrefab : null;
    }

    private IEnumerator CheckAndCombineCoinsAfterDelay(Vector2Int startPosition, float delay = 0f)
    {
        yield return new WaitForSeconds(delay);
        CheckAndCombineCoins(startPosition);
        checkAndCombineCoinsInProgress = false;
    }

    public void CheckAndCombineCoins(Vector2Int startPosition, bool skipInitialCheck = false)
    {
        int targetCoinValue = GetTargetCoinValue(startPosition);
        if (!IsValidCoinValue(targetCoinValue))
        {
            return;
        }

        List<Vector2Int> connectedCoins = CollectConnectedCoins(startPosition, targetCoinValue, skipInitialCheck);
        if (ShouldCombineCoins(connectedCoins, targetCoinValue))
        {
            HandleCoinCombination(connectedCoins, targetCoinValue, startPosition);
        }
    }

    private bool IsValidCoinValue(int coinValue)
    {
        return coinValue != 0;
    }

    private bool ShouldCombineCoins(List<Vector2Int> connectedCoins, int targetCoinValue)
    {
        return HasEnoughCoinsForCombination(connectedCoins.Count, GetRequiredCoinsForCombination(targetCoinValue));
    }

    private int GetRequiredCoinsForCombination(int targetCoinValue)
    {
        return requiredCoinsForCombinationMap.TryGetValue(targetCoinValue, out int requiredCoins) ? requiredCoins : 0;
    }

    public bool IsColumnUpdating(int columnIndex)
    {
        return updatingColumns.Contains(columnIndex);
    }
}
