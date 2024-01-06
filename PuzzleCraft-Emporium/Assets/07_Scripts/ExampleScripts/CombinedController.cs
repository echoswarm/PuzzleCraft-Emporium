using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using System.Linq;
using TMPro;  // Added this for TextMeshProUGUI

public class CombinedController : MonoBehaviour
{
    // ========== Field Initializations ==========

    public GridManager gridManager;
    private MatchDetector matchDetector;
    public GameObject pointer;
    public float moveSpeed = 5.0f;
    public float grabSpeed = 5.0f;
    public float shotSpeed = 10.0f;
    public float gridSize = 1.0f;
    public float shotDelay = 0.5f;
    private Rigidbody2D rb2d;
    private Vector2 touchStartPos;
    private Vector2 touchEndPos;
    private Vector2 targetPosition;
    public Transform holdPosition;
    private bool isMoving;
    private GameObject heldObject;
    private int matchCount;
    private float lastShotTime;
    public bool IsCoinMoving { get; set; }
    public bool HasShotCoin { get; set; }
    public Vector2Int LastShotCoinGridPosition { get; set; }
    private List<GameObject> heldCoins = new List<GameObject>();
    public event Action<GameObject> OnCoinShot;
    public CoinCombinerAndClearer coinCombinerAndClearer;
    public TextMeshProUGUI notifyText;  // Changed to TextMeshProUGUI
    public GameObject notifySmallDot;
    public GameObject greenNotifySmallDot;
    public GameObject yellowNotifySmallDot;
    public LineRenderer lineRenderer;
    public Color originalLineColor;
    public Color grabbedCoinLineColor;
    public GameSetupManager gameSetupManager;
    //Animator component on the player character
    public Animator animator;
    

    private readonly Dictionary<int, int> requiredAmountForValue = new Dictionary<int, int>
    {
        { 1, 5 },
        { 5, 2 },
        { 50, 2 },
        { 10, 5 },
        { 100, 5 },
        { 500, 2 },
        { 1000, 2 },
        { 2000, 2 }
    };
    // ========== MonoBehaviour Methods ==========

    void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();
        targetPosition = rb2d.position;
        isMoving = false;
        matchDetector = GetComponent<MatchDetector>();
        //get the Animator component from your gameObject
        animator = GetComponent<Animator>();
        gameSetupManager = GameSetupManager.Instance;
    }

    void Update()
    {
        if (MainGameSettings.IsGamePaused)
        {
            return;
        }

        HandleInput();
        UpdatePointerPosition();
    }
    // ========== Input Handling ==========

    private void HandleInput()
    {
        if (MainGameSettings.IsGamePaused || gameSetupManager.AreCoinsMoving)
        {
            return;
        }

        Vector2 currentInputPosition;
        bool touchBegan;
        bool touchEnded;

        GetPlatformSpecificInput(out currentInputPosition, out touchBegan, out touchEnded);

        if (touchBegan)
        {
            touchStartPos = currentInputPosition;
        }

        if (touchEnded)
        {
            touchEndPos = currentInputPosition;
            ProcessInputAction();
        }
    }

    private void GetPlatformSpecificInput(out Vector2 currentInputPosition, out bool touchBegan, out bool touchEnded)
    {
        currentInputPosition = Vector2.zero;
        touchBegan = false;
        touchEnded = false;

    #if UNITY_EDITOR
        touchBegan = Input.GetMouseButtonDown(0);
        touchEnded = Input.GetMouseButtonUp(0);
        currentInputPosition = Input.mousePosition;
    #else
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            touchBegan = touch.phase == TouchPhase.Began;
            touchEnded = touch.phase == TouchPhase.Ended;
            currentInputPosition = touch.position;
        }
    #endif
    }

    private void ProcessInputAction()
    {
        Vector2 delta = touchEndPos - touchStartPos;
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            OnMove((int)Mathf.Sign(delta.x));
        }
        else
        {
            if (delta.y > 25) OnShoot();
            if (delta.y < -25) OnGrab();
        }
    }

    public void OnButtonPressed(string action)
    {
        if (MainGameSettings.IsGamePaused)
        {
            return; // Skip if game is paused.
        }
        switch (action)
        {
            case "Left": OnMove(-1); break;
            case "Right": OnMove(1); break;
            case "Shoot": OnShoot(); break;
            case "Grab": OnGrab(); break;
        }
    }
    // ========== Movement and Pointer Handling ==========

    private void UpdatePointerPosition()
    {
        if (pointer != null && lineRenderer != null)
        {
            Vector2Int gridPosition = gridManager.WorldToGridPosition(rb2d.position);
            int targetY = FindNextAvailableSpace((int)gridPosition.x, (int)gridPosition.y, 1);
            Vector2 worldTargetPosition = gridManager.GridToWorldPosition((int)gridPosition.x, targetY);
            pointer.transform.position = worldTargetPosition;
            Vector2 snappedPlayerPosition = gridManager.GridToWorldPosition(gridPosition.x, gridPosition.y);
            lineRenderer.SetPosition(0, snappedPlayerPosition);
            lineRenderer.SetPosition(1, worldTargetPosition);
        }
    }

    private void OnMove(int direction)
    {
        if (!isMoving)
        {
            Vector2 moveDirection = new Vector2(direction, 0);
            Vector2Int gridPosition = gridManager.WorldToGridPosition(rb2d.position);
            Vector2Int newGridPosition = gridPosition + new Vector2Int((int)moveDirection.x, (int)moveDirection.y);
            if (gridManager.IsWithinGridBounds(newGridPosition.x, newGridPosition.y) && gridManager.GetObjectAtGridPosition(newGridPosition.x, newGridPosition.y) == null)
            {
                targetPosition = gridManager.GridToWorldPosition(newGridPosition.x, newGridPosition.y);
                StartCoroutine(MoveCharacter());
            }
            //if the player moves left, play the left animation and vice versa
            if (direction == -1)
            {
                animator.Play("MoveLeft");
            }
            else
            {
                animator.Play("MoveRight");
            }
        }
    }

    IEnumerator MoveCharacter()
    {
        isMoving = true;
        float remainingDistance = (targetPosition - rb2d.position).sqrMagnitude;
        while (remainingDistance > float.Epsilon)
        {
            rb2d.position = Vector2.MoveTowards(rb2d.position, targetPosition, moveSpeed * Time.deltaTime);
            remainingDistance = (targetPosition - rb2d.position).sqrMagnitude;
            yield return null;
        }
        isMoving = false;
    }
    // ========== Coin Shooting Mechanics ==========

    private void OnShoot()
    {
        if (MainGameSettings.IsGamePaused || heldCoins.Count <= 0 || IsCoinMoving)
        {
            return; // Skip if game is paused.
        }
        if (heldCoins.Count > 0 && !IsCoinMoving)
        {
            ShootObject();
            //if the player shoots, play the shoot animation, but first check if the animator is enabled, if not, enable it
            if (animator.enabled == false)
            {
                animator.enabled = true;
            }
            //play the shoot animation
            animator.Play("Shoot");
            lastShotTime = Time.time;
        }
    }

    private void ShootObject()
    {
        if (heldCoins.Count > 0 && !IsCoinMoving)
        {
            StartCoroutine(ShootCoinsSequentially());
        }
    }

    IEnumerator ShootCoinsSequentially()
    {
        GameSetupManager.IsPlayerShotCompleted = false;
        while (heldCoins.Any())
        {
            Vector2 gridPosition = gridManager.WorldToGridPosition(transform.position);
            
            if (coinCombinerAndClearer.IsColumnUpdating((int)gridPosition.x))
            {
                yield return new WaitForSeconds(0.15f);
                continue;
            }
            
            int targetY = FindNextAvailableSpace((int)gridPosition.x, (int)gridPosition.y, 1);
            if (targetY == -1) yield break;

            GameObject coin = heldCoins.First();
            heldCoins.RemoveAt(0);

            coin.transform.SetParent(null);
            UpdateNotifyText();
            
            yield return StartCoroutine(ShootObjectCoroutine(coin, gridPosition, targetY, moveSpeed));
            GameSetupManager.IsPlayerShotCompleted = true;
        }
    }

    IEnumerator ShootObjectCoroutine(GameObject obj, Vector2 startPos, int targetY, float speed)
    {
        Vector2 targetPos = new Vector2(startPos.x, targetY);
        Vector2 worldTargetPos = gridManager.GridToWorldPosition((int)targetPos.x, (int)targetPos.y);
        Vector2 currentObjPosition = obj.transform.position;

        float preCalculatedSpeed = shotSpeed * Time.deltaTime;

        IsCoinMoving = true;

        while (Vector2.SqrMagnitude(worldTargetPos - currentObjPosition) > float.Epsilon)
        {
            currentObjPosition = Vector2.MoveTowards(currentObjPosition, worldTargetPos, preCalculatedSpeed);
            obj.transform.position = currentObjPosition;
            yield return null;
        }

        IsCoinMoving = false;
        gridManager.PlaceObjectAtGridPosition(obj, (int)targetPos.x, (int)targetPos.y);
        StartCoroutine(CheckForMatchesWithDelay(obj, new Vector2Int((int)targetPos.x, (int)targetPos.y), 0.25f));
    }

    private int FindNextAvailableSpace(int x, int y, int direction)
    {
        int nextY = y;
        while (gridManager.IsWithinGridBounds(x, nextY + direction))
        {
            GameObject obj = gridManager.GetObjectAtGridPosition(x, nextY + direction);
            if (obj == null)
            {
                nextY += direction;
            }
            else
            {
                break;
            }
        }
        return nextY;
    }
    // ========== Match Checking and Notification Update ==========

    IEnumerator CheckForMatchesWithDelay(GameObject obj, Vector2Int targetPos, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (matchDetector != null)
        {
            Dictionary<Vector2Int, int> matches = matchDetector.FindMatchesAtPosition((int)targetPos.x, (int)targetPos.y);
            matchCount = matches.Values.Sum();
            if (matchCount > 0)
            {
                // Handle matches here
            }
        }
        OnCoinShot?.Invoke(obj);
        LastShotCoinGridPosition = new Vector2Int((int)targetPos.x, (int)targetPos.y);
        HasShotCoin = true;
        if (lineRenderer != null)
        {
            lineRenderer.startColor = originalLineColor;
            lineRenderer.endColor = originalLineColor;
        }
    }

    private void UpdateNotifyText()
    {
        if (notifyText != null)
        {
            notifyText.text = heldCoins.Count.ToString();
            notifySmallDot.SetActive(false);
            greenNotifySmallDot.SetActive(false);
            yellowNotifySmallDot.SetActive(false);
            bool hasHeldCoins = heldCoins.Count > 0;
            notifyText.gameObject.transform.parent.gameObject.SetActive(hasHeldCoins);
            if (hasHeldCoins)
            {
                int heldCoinValue = heldCoins[0].GetComponent<Coin>().Value;
                int requiredAmount = GetRequiredAmountForValue(heldCoinValue);
                if (heldCoins.Count < requiredAmount)
                {
                    notifySmallDot.SetActive(true);
                }
                else if (heldCoins.Count == requiredAmount)
                {
                    greenNotifySmallDot.SetActive(true);
                }
                else 
                {
                    yellowNotifySmallDot.SetActive(true);
                }
            }
        }
    }

    private int GetRequiredAmountForValue(int coinValue)
    {
        if (requiredAmountForValue.TryGetValue(coinValue, out int requiredAmount))
        {
            return requiredAmount;
        }
        return 0;
    }
    // ========== Coin Grabbing Mechanics ==========

    private void OnGrab()
    {
        if (MainGameSettings.IsGamePaused || IsCoinMoving)
        {
            return; // Skip if game is paused.
        }
        if (!IsCoinMoving)
        {
            //play the grab animation
            animator.Play("Grab");
            GrabObject();
        }
    }

    private void GrabObject()
    {
        if (!IsCoinMoving)
        {
            Vector2 gridPosition = gridManager.WorldToGridPosition(transform.position);
            int closestRowIndex = -1;
            float closestRowDistance = float.MaxValue;
            for (int y = 0; y < gridManager.gridHeight; y++)
            {
                GameObject obj = gridManager.GetObjectAtGridPosition((int)gridPosition.x, y);
                if (obj != null)
                {
                    float distance = Mathf.Abs(gridPosition.y - y);
                    if (distance < closestRowDistance)
                    {
                        closestRowIndex = y;
                        closestRowDistance = distance;
                    }
                }
            }
            if (closestRowIndex != -1)
            {
                GrabCoinsInColumn((int)gridPosition.x, closestRowIndex);
            }
        }
    }

    //method to pause the animator in the grab animation after the player grabs a coin
    public void PauseAnimation()
    {
        animator.enabled = false;
        
    }


    private void GrabCoinsInColumn(int x, int closestRowIndex)
    {
        GameObject obj = gridManager.GetObjectAtGridPosition(x, closestRowIndex);
        int coinValue = obj.GetComponent<Coin>().Value;
        if (heldCoins.Count > 0)
        {
            if (heldCoins[0].GetComponent<Coin>().Value != coinValue)
            {
                return;
            }
        }
        for (int y = closestRowIndex; y < gridManager.gridHeight; y++)
        {
            obj = gridManager.GetObjectAtGridPosition(x, y);
            if (obj == null || obj.GetComponent<Coin>().Value != coinValue)
            {
                break;
            }
            heldCoins.Add(obj);
            UpdateNotifyText();
            StartCoroutine(GrabObjectCoroutine(obj, holdPosition.position, grabSpeed));
            obj.transform.SetParent(holdPosition);
            gridManager.RemoveObjectAtGridPosition(x, y);
            if (lineRenderer != null)
            {
                lineRenderer.startColor = grabbedCoinLineColor;
                lineRenderer.endColor = grabbedCoinLineColor;
            }
        }
    }

    IEnumerator GrabObjectCoroutine(GameObject obj, Vector3 targetPos, float speed)
    {
        float remainingDistance = (targetPos - obj.transform.position).sqrMagnitude;
        while (remainingDistance > float.Epsilon)
        {
            obj.transform.position = Vector3.MoveTowards(obj.transform.position, targetPos, speed * Time.deltaTime);
            remainingDistance = (targetPos - obj.transform.position).sqrMagnitude;
            yield return null;
        }
    }
    // ========== Coin Combination and Value Calculation ==========

    public int CalculateCombinedCoinValue(string coinTag, int maxMatchLength)
    {
        int coinValue = int.Parse(coinTag.Replace("Coin", ""));
        int finalCoinValue = 0;
        switch (coinValue)
        {
            case 1:
                if (matchCount >= 5) finalCoinValue = 5;
                break;
            case 5:
                if (matchCount >= 2) finalCoinValue = 10;
                break;
            case 10:
                if (matchCount >= 5) finalCoinValue = 50;
                break;
            case 50:
                if (matchCount >= 2) finalCoinValue = 100;
                break;
            case 100:
                if (matchCount >= 5) finalCoinValue = 500;
                break;
            case 500:
                if (matchCount >= 2) finalCoinValue = 1000;
                break;
            case 1000:
                if (matchCount >= 2) finalCoinValue = 2000;
                break;
            case 2000:
                if (matchCount >= 2) finalCoinValue = 0;            
                break;
        }
        return finalCoinValue;
    }

    private GameObject CreateCombinedCoin(int coinValue)
    {
        GameObject coinPrefab = Resources.Load<GameObject>($"Coin{coinValue}");
        GameObject newCoin = Instantiate(coinPrefab);
        return newCoin;
    }

    private int CheckAvailableSpaceInColumn(int column)
    {
        int availableSpaces = 0;
        for (int row = 0; row < gridManager.gridHeight; row++)
        {
            if (gridManager.GetObjectAtGridPosition(column, row) == null)
            {
                availableSpaces++;
            }
        }
        return availableSpaces;
    }
}
