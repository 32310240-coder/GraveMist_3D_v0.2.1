using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject playButton;
    public GameObject dragArea;
    public GameObject turnButtons;   // トス / ミスト
    public GameObject mistPanel;

    [Header("Grave")]
    public GameObject gravePrefab;
    public int graveCount = 4;
    private List<GameObject> spawnedGraves = new List<GameObject>();

    public Material redMat;
    public Material blueMat;
    public Material yellowMat;
    public Material greenMat;
    public Material orangeMat;

    [Header("Board / Player")]
    public BoardManager boardManager;
    public GameObject playerPrefab;

    private List<GameObject> players = new List<GameObject>();
    private List<int> playerPathIndices = new List<int>();
    private List<int> playerStartPathIndices = new List<int>();

    List<List<MistType>> playerMists = new List<List<MistType>>();
    const int MAX_MIST = 7;

    public Transform mistIconHolder;
    public GameObject mistIconPrefab;
    public Transform mistMiniHolder;
    public GameObject mistMiniIconPrefab;

    public Sprite[] mistSprites;

    private int currentPlayerIndex = 0;
    private Coroutine moveCoroutine;

    bool hasAnyFallen = false;
    int stoppedCount = 0;
    int totalSteps = 0;
    HashSet<GraveController> stoppedGraves = new HashSet<GraveController>();

    public enum GameState { Idle, Shake }
    public GameState currentState = GameState.Idle;



    void Awake()
    {
        Physics.gravity = new Vector3(0, -20f, 0);
    }

    void Start()
    {
        CreatePlayers();
        EnterTurnStart();
    }
    void GoToWinScene(int winnerIndex)
    {
        GameSession.WinnerIndex = winnerIndex;
        SceneManager.LoadScene("WinScene");
    }
    // =========================================================
    // Player生成
    // =========================================================
    void CreatePlayers()
    {
        players.Clear();
        playerPathIndices.Clear();
        playerStartPathIndices.Clear();
        playerMists.Clear();

        Vector2Int[] allStartGrids = new Vector2Int[]
        {
        new Vector2Int(8, 0), // 1P
        new Vector2Int(8, 8), // 2P
        new Vector2Int(0, 8), // 3P
        new Vector2Int(0, 0)  // 4P
        };

        int playerCount = Mathf.Clamp(GameSession.PlayerCount, 2, 4);

        List<Vector2Int> startGrids = new List<Vector2Int>();

        if (playerCount == 2)
        {
            // 1P(8,0) と 2P(0,8)
            startGrids.Add(allStartGrids[0]);
            startGrids.Add(allStartGrids[2]);
        }
        else
        {
            for (int i = 0; i < playerCount; i++)
                startGrids.Add(allStartGrids[i]);
        }

        for (int i = 0; i < startGrids.Count; i++)
        {
            Vector2Int grid = startGrids[i];

            Vector3 pos = boardManager.GridToWorld(grid.x, grid.y);
            pos.y = 0.5f;

            // ===== 角ごとの固定向き =====
            float yRot = 0f;

            if (grid == new Vector2Int(8, 0)) yRot = 0f;      // 上へ
            else if (grid == new Vector2Int(8, 8)) yRot = -90f;    // 左へ
            else if (grid == new Vector2Int(0, 8)) yRot = 180f;    // 下へ
            else if (grid == new Vector2Int(0, 0)) yRot = 90f;     // 右へ

            Quaternion rot = Quaternion.Euler(90f, yRot, 0f);

            GameObject p = Instantiate(playerPrefab, pos, rot);

            PlayerController pc = p.GetComponent<PlayerController>();
            players.Add(p);

            int playerIndex = players.Count - 1;
            pc.SetPlayerIndex(playerIndex);

            int pathIndex = boardManager.outerPath.IndexOf(grid);
            playerPathIndices.Add(pathIndex);
            playerStartPathIndices.Add(pathIndex);
            playerMists.Add(new List<MistType>());
        }

        currentPlayerIndex = 0;
    }
    void GiveMist(int playerIndex)
    {
        if (playerMists[playerIndex].Count >= MAX_MIST)
            return;

        MistType mist = (MistType)Random.Range(1, 5);

        playerMists[playerIndex].Add(mist);

        Debug.Log($"Player {playerIndex + 1} got mist: {mist}");
        RefreshMistMiniUI(playerIndex);

    }

    void ClearMistIcons()
    {
        foreach (Transform child in mistIconHolder)
        {
            Destroy(child.gameObject);
        }
    }
    void RefreshMistUI(int playerIndex)
    {
        ClearMistIcons();

        List<MistType> mists = playerMists[playerIndex];

        foreach (MistType mist in mists)
        {
            GameObject icon = Instantiate(mistIconPrefab, mistIconHolder);

            Image img = icon.GetComponent<Image>();

            int index = (int)mist - 1;
            img.sprite = mistSprites[index];
        }
    }
    GameObject CurrentPlayer => players[currentPlayerIndex];

    int CurrentPathIndex
    {
        get => playerPathIndices[currentPlayerIndex];
        set => playerPathIndices[currentPlayerIndex] = value;
    }

    // =========================================================
    // UI制御
    // =========================================================
    public void OnPlayButtonPressed()
    {
        playButton.SetActive(false);
        turnButtons.SetActive(true);
    }

    public void OnTossPressed()
    {
        turnButtons.SetActive(false);
        EnterShake();
    }
    public void OnMistPressed()
    {
        bool active = mistPanel.activeSelf;

        if (active)
        {
            mistPanel.SetActive(false);
        }
        else
        {
            mistPanel.SetActive(true);
            RefreshMistUI(currentPlayerIndex);
        }
    }
    void EnterShake()
    {
        currentState = GameState.Shake;

        turnButtons.SetActive(false);
        dragArea.SetActive(true);

        dragArea.GetComponent<DragAreaController>().SetDraggable(true);

        Debug.Log($"▶{currentPlayerIndex + 1}P のターン");
    }
    void RefreshMistMiniUI(int playerIndex)
    {
        foreach (Transform child in mistMiniHolder)
            Destroy(child.gameObject);

        List<MistType> mists = playerMists[playerIndex];

        foreach (MistType mist in mists)
        {
            GameObject icon = Instantiate(mistMiniIconPrefab, mistMiniHolder);

            Image img = icon.GetComponent<Image>();

            int index = (int)mist - 1;
            img.sprite = mistSprites[index];
        }
    }
    void EnterTurnStart()
    {
        playButton.SetActive(true);
        turnButtons.SetActive(false);
        dragArea.SetActive(false);
        mistPanel.SetActive(false);

        RefreshMistMiniUI(currentPlayerIndex);
        dragArea.GetComponent<DragAreaController>().SetDraggable(false);
        currentState = GameState.Idle;
    }

    // =========================================================
    // Drag → 発射
    // =========================================================
    public void OnShakeRelease(Vector3 launchPos, Vector2 dir2D, float dragDistance, float speed)
    {
        if (currentState != GameState.Shake) return;

        Vector3 dir3D = new Vector3(dir2D.x, 0f, dir2D.y).normalized;

        float power = Mathf.Clamp(dragDistance / 300f, 0.2f, 1f);
        float speedFactor = Mathf.Clamp(speed / 1500f, 0.6f, 1.1f);

        Vector3 launchDir = (dir3D + Vector3.up * 0.1f).normalized;
        float finalPower = 2.5f * power * speedFactor;

        SpawnAndLaunchGraves(launchPos, launchDir, finalPower);
    }

    void SpawnAndLaunchGraves(Vector3 launchPos, Vector3 dir, float power)
    {
        ClearSpawnedGraves();
        stoppedCount = 0;
        totalSteps = 0;
        stoppedGraves.Clear();
        hasAnyFallen = false;

        float boardY = boardManager.transform.position.y;
        float halfHeight = gravePrefab.GetComponent<Collider>().bounds.extents.y;
        float spawnY = boardY + halfHeight + 4.5f;

        for (int i = 0; i < graveCount; i++)
        {
            Vector3 spawnPos = launchPos;
            spawnPos.y = spawnY;

            GameObject grave = Instantiate(gravePrefab, spawnPos, Random.rotation);

            Rigidbody rb = grave.GetComponent<Rigidbody>();
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            GraveController gc = grave.GetComponent<GraveController>();
            gc.OnStopped -= OnGraveStopped;
            gc.OnStopped += OnGraveStopped;

            rb.AddForce(dir * power, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 4f, ForceMode.Impulse);

            spawnedGraves.Add(grave);
        }
    }

    // =========================================================
    // 駒停止処理
    // =========================================================
    void OnGraveStopped(GraveController grave)
    {
        if (stoppedGraves.Contains(grave)) return;
        stoppedGraves.Add(grave);

        if (grave.IsOutOfBoard())
        {
            hasAnyFallen = true;
        }
        else
        {
            switch (grave.GetResult())
            {
                case GraveFaceResult.Front:
                    grave.GetComponent<Renderer>().material = redMat;
                    totalSteps += 1;
                    break;

                case GraveFaceResult.Back:
                    grave.GetComponent<Renderer>().material = blueMat;
                    break;

                case GraveFaceResult.Side:
                    grave.GetComponent<Renderer>().material = yellowMat;
                    totalSteps += 5;
                    break;

                case GraveFaceResult.Vertical:
                    grave.GetComponent<Renderer>().material = greenMat;
                    totalSteps += 10;
                    break;
            }
        }

        stoppedCount++;
        if (stoppedCount < graveCount) return;

        if (hasAnyFallen)
        {
            foreach (var g in spawnedGraves)
                if (g) g.GetComponent<Renderer>().material = orangeMat;

            NextTurn();
            return;
        }

        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        moveCoroutine = StartCoroutine(MovePlayerCoroutine(totalSteps));
    }

    // =========================================================
    // 移動処理
    // =========================================================
    IEnumerator MovePlayerCoroutine(int steps)
    {
        int dir = steps >= 0 ? 1 : -1;
        int count = Mathf.Abs(steps);

        for (int i = 0; i < count; i++)
        {
            // 1) 次マスへ進める
            CurrentPathIndex =
                (CurrentPathIndex + dir + boardManager.outerPath.Count)
                % boardManager.outerPath.Count;

            Vector2Int grid = boardManager.outerPath[CurrentPathIndex];

            // 2) 位置へ移動
            Vector3 pos = boardManager.GridToWorld(grid.x, grid.y);
            pos.y = 0.5f;
            yield return MoveToPosition(CurrentPlayer.transform, pos, 0.15f);
            yield return new WaitForSeconds(0.05f);

            // 3) ★到着後に角チェック → 角なら左90°回転して止まる
            if (IsCorner(grid))
            {
                CurrentPlayer.transform.rotation =
                    Quaternion.Euler(90f, RoundTo90(CurrentPlayer.transform.eulerAngles.y - 90f), 0f);
                GiveMist(currentPlayerIndex);
            }
        }

        // ---- 最終停止位置で「進化」と「勝利判定」はここでやる（到着後処理） ----
        Vector2Int stopGrid = boardManager.outerPath[CurrentPathIndex];
        var pc = CurrentPlayer.GetComponent<PlayerController>();

        bool wasFinal = pc.IsFinalStage();

        // ★進化は「最終停止位置が角」だけ
        if (IsCorner(stopGrid))
            pc.AdvanceEvolution();

        bool isFinalNow = pc.IsFinalStage();
        bool becameFinalHere = (!wasFinal && isFinalNow);

        bool onMyStart = CurrentPathIndex == playerStartPathIndices[currentPlayerIndex];

        if (isFinalNow && onMyStart && !becameFinalHere)
        {
            Debug.Log($"🏆 Player {currentPlayerIndex + 1} WIN!");
            // ここで終了処理（必要なら gameEnded フラグ推奨）
            GoToWinScene(currentPlayerIndex);
            yield break;
        }

        NextTurn();
    }

    float RoundTo90(float y)
    {
        y = Mathf.Repeat(y, 360f);
        return Mathf.Round(y / 90f) * 90f;
    }

    void NextTurn()
    {
        ClearSpawnedGraves();
        currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
        EnterTurnStart();
    }

    IEnumerator MoveToPosition(Transform obj, Vector3 target, float time)
    {
        Vector3 start = obj.position;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / time;
            obj.position = Vector3.Lerp(start, target, t);
            yield return null;
        }

        obj.position = target;
    }

    void ClearSpawnedGraves()
    {
        foreach (GameObject grave in spawnedGraves)
            if (grave) Destroy(grave);

        spawnedGraves.Clear();
    }

    bool IsCorner(Vector2Int grid)
    {
        int max = boardManager.gridSize - 1;
        return (grid.x == 0 || grid.x == max)
            && (grid.y == 0 || grid.y == max);
    }
}