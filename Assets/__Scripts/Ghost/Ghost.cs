using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using static GameManager;

public class Ghost : MonoBehaviour
{
    [SerializeField] GhostState firstState;
    [SerializeField] Transform scatterPoint;
    [SerializeField] Transform returnPoint;
    public GhostState State { get; private set; }
    public enum GhostState
    {
        InHome,
        ExitingHome,
        Scattering,
        Chasing,
        Fleeing,
        ReturnHome,
    }

    [SerializeField] Transform pacman;
    [SerializeField] float timeHome = 5f;
    [SerializeField] float timeExiting = 2f;
    [SerializeField] float timeScattering = 5f;
    float timeScatteringSpent = 0f;
    [SerializeField] float timeChasing = 20f;
    float timeChasingSpent = 0f;
    [SerializeField] float timeFleeing = 15f;
    float timeFleeingSpent = 0f;

    [SerializeField] Sprite[] eyes;
    [SerializeField] SpriteRenderer eyeRenderer; 
    [SerializeField] SpriteRenderer bodyRenderer; 
    [SerializeField] SpriteRenderer blueRenderer; 
    [SerializeField] SpriteRenderer whiteRenderer; 
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] LayerMask wallLayer;
    [SerializeField] Vector2 direction = Vector2.up;
    [SerializeField] Vector2 eyeDirection = Vector2.up;
    [SerializeField] int currentSlot;

    Bounds ghostBounds;
    Rigidbody2D rb;
    float stayingHomeTime;
    bool moveBetweenSlot;
    [SerializeField] List<Vector2> OpenPath = new List<Vector2>();
    [SerializeField] bool directionPicked = false;

    public static Action<GhostState> OnBeforeGhostStateChange;
    public static Action<GhostState> OnAfterGhostStateChange;
    
    void Awake()
    {
        GameManager.OnGamePlaying += OnGamePlaying;
        GameManager.OnPowerUpFading += OnPowerUpFading;
        GameManager.OnBeforeGameStateChange += OnBeforeGameStateChange;
        ghostBounds = GetComponent<Collider2D>().bounds;
        rb = GetComponent<Rigidbody2D>();
    }

    void OnDestroy()
    {
        GameManager.OnGamePlaying -= OnGamePlaying;
        GameManager.OnPowerUpFading -= OnPowerUpFading;
        GameManager.OnBeforeGameStateChange -= OnBeforeGameStateChange;
    }

    void Start()
    {
        GhostStateChange(firstState);
    }

    void OnGamePlaying()
    {
        switch (State)
        {
            case GhostState.InHome:
                GhostInHome();
                break;
            case GhostState.ExitingHome:
                GhostExitingHome();
                break;
            case GhostState.Scattering:
                GhostScattering();
                break;
            case GhostState.Chasing:
                GhostChasing();
                break;
            case GhostState.Fleeing:
                GhostFleeing();
                break;
            case GhostState.ReturnHome:
                break;
        }
    }

    public void GhostStateChange(GhostState newState)
    {
        OnBeforeGhostStateChange?.Invoke(newState);
        BeforeGhostStateChange(newState);
        State = newState;
        OnAfterGhostStateChange?.Invoke(newState);
    }

    void BeforeGhostStateChange(GhostState newState)
    {
        switch (newState)
        {
            case GhostState.InHome:
                for (int i = 0; i < 3; i++)
                {
                    if (transform.position.x == Home.Instance.SlotXPos[i])
                    {
                        currentSlot = i;
                    }
                }
            break;
            case GhostState.ExitingHome:
                //add left and right as initial available path
                OpenPath.Clear();
                OpenPath.Add(Vector2.left);
                OpenPath.Add(Vector2.right);
                break;
            case GhostState.Scattering:
                //directionPicked = false;
                timeScatteringSpent = 0;
                break;
            case GhostState.Chasing:
                //directionPicked = false;
                timeChasingSpent = 0;
                break;
            case GhostState.Fleeing:
                //directionPicked = false;
                timeFleeingSpent = 0;
                break;
        }
    }

    GhostState savedState;
    void OnBeforeGameStateChange(GameManager.GameState gameState)
    {
        switch (gameState)
        {
            case GameState.Starting:
                break;
            case GameState.Playing:
                if (State == GhostState.Fleeing)
                {
                    Debug.Log("back to normal");
                    GhostStateChange(savedState);
                }
                GhostSetSprite(true, true, false, false);
                break;
            case GameState.Paused:
                break;
            case GameState.PacmanPowerUp:
                if (State == GhostState.Scattering || State == GhostState.Chasing)
                {
                    savedState = State;
                    GhostStateChange(GhostState.Fleeing);
                }
                GhostSetSprite(false, false, true, false);
                break;
            case GameState.PacmanDying:
                break;
            case GameState.LevelCompleted:
                break;
            case GameState.GameOver:
                break;
        }
    }

    void OnPowerUpFading()
    {
        GhostSetSprite(false, false, false, true);
    }

    void GhostSetSprite(bool eye, bool body, bool blue, bool white)
    {
        eyeRenderer.enabled = eye;
        bodyRenderer.enabled = body;
        blueRenderer.enabled = blue;
        whiteRenderer.enabled = white;
    }

    #region --- Fleeing ---
    void GhostFleeing()
    {
        timeFleeingSpent += Time.deltaTime;

        direction = CheckDirectionToFlee();
        DrawEyes(direction);
        transform.Translate(direction * moveSpeed * Time.deltaTime);
    }

    Vector2 CheckDirectionToFlee()
    {
        Debug.Log("Fleeing");
        if (directionPicked == true)
        {
            return direction;
        }

        Vector2 currentPos = transform.position;
        Vector2 bestDirection = Vector2.zero;

        while (bestDirection == Vector2.zero)
        {
            
            int i = UnityEngine.Random.Range(0, OpenPath.Count);
            Debug.Log(OpenPath.Count + "/" + i);
            var dir = OpenPath[i];

            //prevent to go back
            if (dir == -direction)
            {
                continue;
            }
            bestDirection = dir;
            Debug.Log(bestDirection);
        }
        directionPicked = true;
        return bestDirection;
    }
    #endregion
    #region --- Chasing ---
    void GhostChasing()
    {
        timeChasingSpent += Time.deltaTime;

        direction = CheckDirectionToChase();
        DrawEyes(direction);
        transform.Translate(direction * moveSpeed * Time.deltaTime);
    }

    Vector2 CheckDirectionToChase()
    {
        if (directionPicked == true)
        {
            return direction;
        }

        Vector2 currentPos = transform.position;
        Vector2 bestDirection = Vector2.zero;
        float minDistance = float.MaxValue;

        foreach (var dir in OpenPath)
        {
            //prevent to go back
            if (dir == -direction)
            {
                continue;
            }
            Vector2 newPos = currentPos + dir * 2f;
            float distance = Vector2.Distance(newPos, pacman.position);

            if (distance < minDistance)
            {
                minDistance = distance;
                bestDirection = dir;
            }
        }
        directionPicked = true;
        return bestDirection;
    }
    #endregion
    #region --- Scattering ---


    void GhostScattering()
    {
        timeScatteringSpent += Time.deltaTime;

        direction = CheckDirectionToScatter();
        DrawEyes(direction);
        transform.Translate(direction * moveSpeed * Time.deltaTime);
    }

    Vector2 CheckDirectionToScatter()
    {
        if (directionPicked == true)
        {
            return direction;
        }

        Vector2 currentPos = transform.position;
        Vector2 bestDirection = Vector2.zero;
        float minDistance = float.MaxValue;

        foreach (var dir in OpenPath)
        {
            //prevent to go back
            if (dir == -direction)
            {
                continue;
            }
            Vector2 newPos = currentPos + dir * 2f;
            float distance = Vector2.Distance(newPos, scatterPoint.position);

            if (distance < minDistance)
            {
                minDistance = distance;
                bestDirection = dir;
            }
        }
        directionPicked = true;
        return bestDirection;
    }
    #endregion
    #region --- Exiting Home ---
    void GhostExitingHome()
    {
        float exitPos = Home.Instance.SlotExitYPos;

        transform.Translate(direction * moveSpeed * Time.deltaTime);

        if (transform.position.y >= exitPos)
        {
            transform.position = new Vector3(transform.position.x, exitPos, 0);
            Home.Instance.GhostInSlot[1] = false;
            GhostStateChange(GhostState.Scattering);
        }
    }
    #endregion
    #region --- In Home ---
    void GhostInHome()
    {
        if (GameManager.State != GameState.PacmanPowerUp)
        {
            stayingHomeTime += Time.deltaTime;
        }

        transform.Translate(direction * moveSpeed * Time.deltaTime);

        DrawEyes(direction);
        CheckIfHitWall();
        MoveBetweenSlot();
        MoveBetweenSlotCompleted();
    }

    private void MoveBetweenSlotCompleted()
    {
        if (!moveBetweenSlot)
        {
            return;
        }

        if (currentSlot == 0 && transform.position.x >= 0 ||
            currentSlot == 2 && transform.position.x <= 0)
        {
            transform.position = new Vector3(0, transform.position.y, 0);
            Home.Instance.GhostInSlot[currentSlot] = false;
            currentSlot = 1;
            direction = Vector2.up;
        }
    }

    private void CheckIfHitWall()
    {
        float layLength = 0.7f;

        Vector2 startPos = transform.position + (Vector3)(ghostBounds.extents * direction);
        RaycastHit2D hit = Physics2D.Raycast(startPos, direction, layLength, wallLayer);

        if (hit.collider != null)
        {
            direction *= -1;
            //ghost is at the door
            if (transform.position.y > 0)
            {
                CheckIfReadyToExit();
            }
        }
    }

    void MoveBetweenSlot()
    {
        //there's a ghost in center slot
        if (Home.Instance.GhostInSlot[1])
        {
            return;
        }

        if (currentSlot == 0)
        {
            moveBetweenSlot = true;
            direction = Vector2.right;
        }
        else if (currentSlot == 2 && Home.Instance.GhostInSlot[0] == false)
        {
            moveBetweenSlot = true;
            direction = Vector2.left;
        }
    }

    void CheckIfReadyToExit()
    {
        //if the ghost is in the center slot
        if (transform.position.x == Home.Instance.SlotXPos[1])
        {
            if (stayingHomeTime > timeHome)
            {
                direction = Vector2.up;
                stayingHomeTime = 0;
                GhostStateChange(GhostState.ExitingHome);
            }
        }
    }
    #endregion

    void DrawEyes(Vector2 newDirection)
    {
        int eyeIndex = 0;

        if (eyeDirection == newDirection)
        {
            return;
        }

        eyeDirection = newDirection;

        if (newDirection == Vector2.up)
        {
            eyeIndex = 0;
        }
        else if (newDirection == Vector2.down)
        {
            eyeIndex = 1;
        }
        else if (newDirection == Vector2.left)
        {
            eyeIndex = 2;
        }
        else if (newDirection == Vector2.right)
        {
            eyeIndex = 3;
        }

        eyeRenderer.sprite = eyes[eyeIndex];
    }

    void OnTriggerEnter2D(Collider2D other)
    {

        if (other.CompareTag("Intersection"))
        {
            HandleIntersection(other);
        }
        if (other.CompareTag("Portal"))
        {
            HandlePortal(other);
        }
        if (other.CompareTag("Pacman"))
        {
            HandlePacman();
        }
    }

    void HandlePacman()
    {
        if (GameManager.State == GameManager.GameState.PacmanPowerUp)
        {
            SoundManager.Play("GhostEaten");
        }
    }

    void HandlePortal(Collider2D other)
    {
        transform.position = other.GetComponent<Portal>().otherPortal.position;
    }

    void HandleIntersection(Collider2D other)
    {
        directionPicked = false;
        var intersection = other.GetComponent<Intersection>();
        transform.position = intersection.transform.position;

        OpenPath.Clear();
        foreach (var path in intersection.OpenPath)
        {
            OpenPath.Add(path);
        }

        if (State == GhostState.Scattering)
        {
            if (timeScatteringSpent >= timeScattering)
            {
                GhostStateChange(GhostState.Chasing);
            }
        }
        else if (State == GhostState.Chasing)
        {
            if (timeChasingSpent >= timeChasing)
            {
                GhostStateChange(GhostState.Scattering);
            }
        }
    }
}
