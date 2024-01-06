using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using System.Linq;

public class CombinedController : MonoBehaviour
{

    public GridManager gridManager;
    public float moveSpeed = 5.0f;
    public float gridSize = 1.0f;
    private Rigidbody2D rb2d;
    private Vector2 touchStartPos;
    private Vector2 touchEndPos;
    private Vector2 targetPosition;
    private bool isMoving = false;
    private GameObject heldObject = null;

    void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();
        targetPosition = rb2d.position;
    }

    void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {

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
            if (delta.y > 0)
            {
                OnPull();
            }
            else
            {
                OnShoot();
            }
        }
    }

    public void OnButtonPressed(string action)
    {

        switch (action)
        {
            case "Left": OnMove(-1); break;
            case "Right": OnMove(1); break;
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
        }
    }

    private void OnPull()
    {
        if (!isMoving && heldObject == null)
        {
            Vector2Int gridPosition = gridManager.WorldToGridPosition(rb2d.position);
            Vector2Int newGridPosition = gridPosition + Vector2Int.up;
            while (gridManager.IsWithinGridBounds(newGridPosition.x, newGridPosition.y))
            {
                heldObject = gridManager.GetObjectAtGridPosition(newGridPosition.x, newGridPosition.y);
                if (heldObject != null)
                {
                    gridManager.ClearGridPosition(newGridPosition.x, newGridPosition.y);
                    newGridPosition += Vector2Int.up;
                }
                else
                {
                    break;
                }
            }
        }
    }

    private void OnShoot()
    {
        if (!isMoving && heldObject != null)
        {
            Vector2Int gridPosition = gridManager.WorldToGridPosition(rb2d.position);
            Vector2Int newGridPosition = gridPosition + Vector2Int.up;
            while (gridManager.IsWithinGridBounds(newGridPosition.x, newGridPosition.y) && gridManager.GetObjectAtGridPosition(newGridPosition.x, newGridPosition.y) == null)
            {
                newGridPosition += Vector2Int.up;
            }
            newGridPosition -= Vector2Int.up;
            gridManager.PlaceObjectAtGridPosition(heldObject, newGridPosition.x, newGridPosition.y);
            heldObject = null;
            gridManager.CheckForCombinationAt(newGridPosition.x, newGridPosition.y);
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
}
