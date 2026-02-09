using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject playButton;
    public GameObject dragArea;

    [Header("Grave")]
    public GameObject gravePrefab;
    public int graveCount = 4;
    private List<GameObject> spawnedGraves = new List<GameObject>();

    public Material redMat;
    public Material blueMat;
    public Material yellowMat;
    public Material greenMat;
    public Material purpleMat;
    public Material orangeMat;

    [Header("Board / Player")]
    public BoardManager boardManager;
    public GameObject playerPrefab;

    // ★ 4人プレイヤー管理
    private List<GameObject> players = new List<GameObject>();
    private List<int> playerPathIndices = new List<int>();
    private int currentPlayerIndex = 0;

    private Coroutine moveCoroutine;

    bool hasAnyFallen = false;
    int fallenCount = 0;

    int countRed = 0;
    int countBlue = 0;
    int countYellow = 0;
    int countGreen = 0;

    public enum GameState
    {
        Idle,
        Shake
    }

    public GameState currentState = GameState.Idle;

    // ===== 停止管理 =====
    int stoppedCount = 0;
    int totalSteps = 0;
    HashSet<GraveController> stoppedGraves = new HashSet<GraveController>();

    // =====================
    // Unity
    // =====================
    void Awake()
    {
        Physics.gravity = new Vector3(0, -20f, 0);
    }

    void Start()
    {
        CreatePlayers();
        EnterIdle();
    }

    // =====================
    // Player生成
    // =====================
    void CreatePlayers()
    {
        players.Clear();
        playerPathIndices.Clear();

        Vector2Int[] startGrids = new Vector2Int[]
        {
            new Vector2Int(8, 0), // 1P
            new Vector2Int(8, 8), // 2P
            new Vector2Int(0, 8), // 3P
            new Vector2Int(0, 0)  // 4P
        };

        for (int i = 0; i < startGrids.Length; i++)
        {
            Vector3 pos = boardManager.GridToWorld(startGrids[i].x, startGrids[i].y);
            pos.y = 0.5f;

            GameObject p = Instantiate(playerPrefab, pos, Quaternion.identity);

            PlayerController pc = p.GetComponent<PlayerController>();
            players.Add(p);

            int playerIndex = players.Count - 1;
            p.GetComponent<PlayerController>().SetPlayerIndex(playerIndex);

            int pathIndex = boardManager.outerPath.IndexOf(startGrids[i]);
            playerPathIndices.Add(pathIndex);
        }
    }

    GameObject CurrentPlayer => players[currentPlayerIndex];

    int CurrentPathIndex
    {
        get => playerPathIndices[currentPlayerIndex];
        set => playerPathIndices[currentPlayerIndex] = value;
    }

    // =====================
    // UI
    // =====================
    public void OnPlayButtonPressed()
    {
        if (currentState != GameState.Idle) return;
        EnterShake();
    }

    void EnterShake()
    {
        currentState = GameState.Shake;
        playButton.SetActive(false);
        dragArea.SetActive(true);
        dragArea.GetComponent<DragAreaController>().SetDraggable(true);

        Debug.Log($"▶ Player {currentPlayerIndex + 1} のターン");
    }

    void EnterIdle()
    {
        currentState = GameState.Idle;

        ClearSpawnedGraves();

        playButton.SetActive(true);
        dragArea.SetActive(false);
        dragArea.GetComponent<DragAreaController>().SetDraggable(false);
    }

    // =====================
    // Drag → Grave発射
    // =====================
    public void OnShakeRelease(
        Vector3 launchPos,
        Vector2 dir2D,
        float dragDistance,
        float speed
    )
    {
        if (currentState != GameState.Shake) return;

        Vector3 dir3D = new Vector3(dir2D.x, 0f, dir2D.y).normalized;

        float power = Mathf.Clamp(dragDistance / 300f, 0.2f, 1f);
        float speedFactor = Mathf.Clamp(speed / 1500f, 0.6f, 1.1f);

        Vector3 launchDir = (dir3D + Vector3.up * 0.1f).normalized;
        float finalPower = 2.5f * power * speedFactor;

        SpawnAndLaunchGraves(launchPos, launchDir, finalPower, dragDistance);
    }

    // =====================
    // Grave生成＆発射
    // =====================
    void SpawnAndLaunchGraves(
        Vector3 launchPos,
        Vector3 dir,
        float power,
        float dragDistance
    )
    {
        spawnedGraves.Clear();

        stoppedCount = 0;
        totalSteps = 0;
        stoppedGraves.Clear();
        hasAnyFallen = false;
        fallenCount = 0;

        countRed = countBlue = countYellow = countGreen = 0;

        float boardY = boardManager.transform.position.y;
        float halfHeight = gravePrefab.GetComponent<Collider>().bounds.extents.y;
        float spawnY = boardY + halfHeight + 4.5f;

        float spread = Mathf.Clamp(dragDistance / 300f, 0.4f, 1.2f);

        for (int i = 0; i < graveCount; i++)
        {
            Vector3 offset = new Vector3(
                Random.Range(-spread, spread),
                0f,
                Random.Range(-spread, spread)
            );

            Vector3 spawnPos = launchPos + offset;
            spawnPos.y = spawnY;

            GameObject grave = Instantiate(gravePrefab, spawnPos, Random.rotation);

            Rigidbody rb = grave.GetComponent<Rigidbody>();
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            GraveController gc = grave.GetComponent<GraveController>();
            gc.OnStopped -= OnGraveStopped;
            gc.OnStopped += OnGraveStopped;

            rb.AddForce(dir * power + Random.insideUnitSphere * 0.3f, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 4f, ForceMode.Impulse);

            spawnedGraves.Add(grave);
        }
    }

    bool HasOverlappingGraves()
    {
        for (int i = 0; i < spawnedGraves.Count; i++)
        {
            var colA = spawnedGraves[i].GetComponent<Collider>();
            if (!colA) continue;

            for (int j = i + 1; j < spawnedGraves.Count; j++)
            {
                var colB = spawnedGraves[j].GetComponent<Collider>();
                if (!colB) continue;

                if (Physics.ComputePenetration(
                    colA, colA.transform.position, colA.transform.rotation,
                    colB, colB.transform.position, colB.transform.rotation,
                    out _, out _))
                {
                    return true;
                }
            }
        }
        return false;
    }

    // =====================
    // Grave停止 → 判定
    // =====================
    void OnGraveStopped(GraveController grave)
    {
        if (stoppedGraves.Contains(grave)) return;
        stoppedGraves.Add(grave);

        if (grave.IsOutOfBoard())
        {
            hasAnyFallen = true;
            fallenCount++;
        }
        else
        {
            GraveFaceResult result = grave.GetResult();
            Renderer r = grave.GetComponent<Renderer>();

            switch (result)
            {
                case GraveFaceResult.Front:
                    r.material = redMat;
                    totalSteps += 1;
                    countRed++;
                    break;
                case GraveFaceResult.Back:
                    r.material = blueMat;
                    countBlue++;
                    break;
                case GraveFaceResult.Side:
                    r.material = yellowMat;
                    totalSteps += 5;
                    countYellow++;
                    break;
                case GraveFaceResult.Vertical:
                    r.material = greenMat;
                    totalSteps += 10;
                    countGreen++;
                    break;
            }
        }

        stoppedCount++;
        if (stoppedCount < graveCount) return;

        if (hasAnyFallen)
        {
            foreach (var g in spawnedGraves)
                if (g) g.GetComponent<Renderer>().material = orangeMat;

            Debug.Log("❌ 落下あり → 移動なし");
            NextTurn();
            return;
        }

        if (HasOverlappingGraves())
        {
            foreach (var g in spawnedGraves)
                if (g) g.GetComponent<Renderer>().material = orangeMat;

            Debug.Log("⚠ 重なりあり → 移動なし");
            NextTurn();   // or EnterIdle();
            return;
        }

        // 重なりなしの場合のみ進む
        Debug.Log($"✅ {totalSteps} マス進む");

        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        moveCoroutine = StartCoroutine(MovePlayerCoroutine(totalSteps));

    }

    // =====================
    // Player移動
    // =====================
    IEnumerator MovePlayerCoroutine(int steps)
    {
        int dir = steps >= 0 ? 1 : -1;
        int count = Mathf.Abs(steps);

        for (int i = 0; i < count; i++)
        {
            CurrentPathIndex =
                (CurrentPathIndex + dir + boardManager.outerPath.Count)
                % boardManager.outerPath.Count;

            Vector2Int grid = boardManager.outerPath[CurrentPathIndex];
            Vector3 pos = boardManager.GridToWorld(grid.x, grid.y);
            pos.y = 0.5f;

            yield return MoveToPosition(CurrentPlayer.transform, pos, 0.15f);
            yield return new WaitForSeconds(0.05f);
        }

        if (IsCorner(boardManager.outerPath[CurrentPathIndex]))
        {
            CurrentPlayer.GetComponent<PlayerController>().AdvanceEvolution();
        }

        NextTurn();
    }

    void NextTurn()
    {
        currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
        EnterIdle();
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

    // =====================
    // Utility
    // =====================
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
