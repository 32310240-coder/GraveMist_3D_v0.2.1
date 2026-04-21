using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class EvolutionGaugeUI
{
    public Image[] gauges; // Gauge_1 ～ Gauge_3
}

[System.Serializable]
public class MistSlotsUI
{
    public Image[] slots; // MistSlot_1 ～ MistSlot_7
}

[System.Serializable]
public class MPSlotsUI
{
    public Image[] slots; // MpSlot_1 ～ MpSlot_30
}
public class GameManager : MonoBehaviour
{
    [Header("Current Player Panel")]
    public GameObject currentPlayerPanel;
    public CanvasGroup currentPlayerPanelCanvasGroup;
    public Image currentPlayerImage;
    public EvolutionGaugeUI currentPlayerEvolutionGauge;
    public MistSlotsUI currentPlayerMistSlots;   // ← 追加

    [Header("Current Player Panel Animation")]
    public float currentPlayerFadeDuration = 0.25f;

    [Header("Top Player Bar")]
    public GameObject[] topPlayerRoots;         // PlayerInfo_1 ～ 4
    public GameObject[] topPlayerHighlights;    // Highlight
    public Image[] topPlayerNumberImages;       // PlayerNumber
    public Image[] topPlayerIcons;              // Icon
    public EvolutionGaugeUI[] topPlayerEvolutionGauges;

    [Header("Player Number Sprites")]
    public Sprite[] playerNumberSprites;        // 1P, 2P, 3P, 4P の画像

    [Header("Character Data")]
    public Sprite[] characterIcons;             // TopPlayerBar用
    public Sprite[] characterLargeSprites;      // CurrentPlayerPanel用
    public string[] characterNames;             // キャラ名（今は未使用でもOK）

    [Header("Evolution Gauge Sprites")]
    public Sprite evolutionOnSprite;
    public Sprite evolutionOffSprite;

    [Header("UI")]
    public GameObject dragArea;
    public GameObject turnButtons;   // Toss / Mist
    public GameObject mistPanel;

    [Header("Mist Panel")]
    public Transform mistIconHolder;
    public GameObject mistIconPrefab;
    public Sprite[] mistSprites;

    [Header("Grave")]
    public GameObject gravePrefab;
    public int graveCount = 4;
    public Material redMat;
    public Material blueMat;
    public Material yellowMat;
    public Material greenMat;
    public Material orangeMat;

    [Header("Grave Throw")]
    public float spawnHeight = 5f;
    public float baseForce = 7.5f;
    public float downwardForce = 0.8f;
    public float forceJitter = 0.15f;
    public float torquePower = 0.4f;

    public float[] distanceRatios = { 0.25f, 0.45f, 0.7f, 1.0f };
    public float[] sideOffsets = { -0.3f, 0.2f, -0.15f, 0.3f };

    public float spawnSpreadMultiplier = 1.35f;
    public float sideForceMultiplier = 0.55f;
    public float randomSpawnSideRange = 0.08f;
    public float randomSideForceRange = 0.10f;
    public float forwardVarianceMin = 0.92f;
    public float forwardVarianceMax = 1.08f;
    public float forwardFlipTorque = 1.2f;

    [Header("Board / Player")]
    public BoardManager boardManager;
    public GameObject playerPrefab;
    [Header("Layout Control")]
    public Behaviour rightInfoLayoutGroup;

    private readonly List<GameObject> spawnedGraves = new List<GameObject>();
    private readonly List<GameObject> players = new List<GameObject>();
    private readonly List<int> playerPathIndices = new List<int>();
    private readonly List<int> playerStartPathIndices = new List<int>();
    private readonly List<List<MistType>> playerMists = new List<List<MistType>>();

    private readonly int[] playerEvolutionLevels = new int[4];

    private const int MAX_MIST = 7;
    private const int START_MIST = 3;

    private int currentPlayerIndex = 0;
    private Coroutine moveCoroutine;
    private Coroutine currentPlayerPanelFadeCoroutine;

    private bool hasAnyFallen = false;
    private int stoppedCount = 0;
    private int totalSteps = 0;
    private readonly HashSet<GraveController> stoppedGraves = new HashSet<GraveController>();

    [Header("Mist Zoom View")]
    public RectTransform mistZoomTarget;
    public CanvasGroup dimOverlayCanvasGroup;
    public float mistZoomScale = 1.5f;
    public float mistZoomDuration = 0.2f;

    private bool isMistZoomed = false;
    private Coroutine mistZoomCoroutine;
    private Vector3 mistSlotsNormalScale = Vector3.one;

    [Header("Mist Zoom Back Button")]
    public GameObject mistZoomBackButton;
    public CanvasGroup mistZoomBackButtonCanvasGroup;
    public float backButtonFadeDuration = 0.2f;
    private Coroutine backButtonFadeCoroutine;

    private Vector2 mistSlotsNormalAnchoredPosition;
    private int mistSlotsNormalSiblingIndex;

    [Header("MP UI")]
    public MPSlotsUI currentPlayerMPSlots;
    public Sprite mpOnSprite;
    public Sprite mpOffSprite;
    private int[] playerMP = new int[4];
    private const int MAX_MP = 30;

    public enum GameState
    {
        Idle,
        Shake
    }
    public enum MistType
    {
        Hole = 1,
        Plus1 = 2,
        Double = 3,
        Uturn = 4
    }
    public GameState currentState = GameState.Idle;

    private GameObject CurrentPlayer => players[currentPlayerIndex];

    private int CurrentPathIndex
    {
        get => playerPathIndices[currentPlayerIndex];
        set => playerPathIndices[currentPlayerIndex] = value;
    }
    // =============================
    // Hole（落とし穴）管理
    // =============================

    // key   : outerPath の index（どの外周マスか）
    // value : その穴を設置したプレイヤー番号
    private Dictionary<int, int> holeOwnerByPathIndex = new Dictionary<int, int>();

    [Header("Hole Visual")]
    public GameObject holeMarkerPrefab;   // 穴の見た目用プレハブ
    public Sprite[] holeSprites;          // Hole1.png ～ Hole4.png

    // key   : outerPath の index
    // value : その穴の見た目オブジェクト
    private Dictionary<int, GameObject> holeVisualsByPathIndex = new Dictionary<int, GameObject>();

    void Awake()
    {
        Physics.gravity = new Vector3(0f, -20f, 0f);
    }

    void Start()
    {
        for (int i = 0; i < GameSession.PlayerCount; i++)
        {
            Debug.Log($"[MainScene] {i + 1}P GameSession.PlayerCharacters = {GameSession.PlayerCharacters[i]}");
        }

        ValidateGameSessionData();
        InitializePlayerUI();
        CreatePlayers();
        EnterTurnStart();
        RefreshAllPlayerUI();
        ShowCurrentPlayerPanelImmediate();

        if (mistZoomTarget != null)
        {
            mistSlotsNormalScale = mistZoomTarget.localScale;
            mistSlotsNormalAnchoredPosition = mistZoomTarget.anchoredPosition;
            mistSlotsNormalSiblingIndex = mistZoomTarget.GetSiblingIndex();
        }
        HideMistZoomBackButtonImmediate();
        if (dimOverlayCanvasGroup != null)
        {
            dimOverlayCanvasGroup.alpha = 0f;
            dimOverlayCanvasGroup.interactable = false;
            dimOverlayCanvasGroup.blocksRaycasts = false;
            dimOverlayCanvasGroup.gameObject.SetActive(false);
        }

    }

    void ValidateGameSessionData()
    {
        GameSession.PlayerCount = Mathf.Clamp(GameSession.PlayerCount, 2, 4);

        if (GameSession.PlayerCharacters == null || GameSession.PlayerCharacters.Length < 4)
        {
            GameSession.PlayerCharacters = new int[4];
        }
    }

    void InitializePlayerUI()
    {
        for (int i = 0; i < playerEvolutionLevels.Length; i++)
        {
            playerEvolutionLevels[i] = 0;
            playerMP[i] = 0;
        }
    }

    void RefreshAllPlayerUI()
    {
        RefreshTopPlayerBar();
        RefreshCurrentPlayerPanel();
    }

    void RefreshTopPlayerBar()
    {
        int playerCount = GameSession.PlayerCount;

        for (int i = 0; i < topPlayerRoots.Length; i++)
        {
            bool active = i < playerCount;

            if (topPlayerRoots[i] != null)
                topPlayerRoots[i].SetActive(active);

            if (!active) continue;

            int charIndex = GameSession.PlayerCharacters[i];

            if (topPlayerIcons != null &&
                i < topPlayerIcons.Length &&
                topPlayerIcons[i] != null &&
                charIndex >= 0 &&
                charIndex < characterIcons.Length)
            {
                topPlayerIcons[i].sprite = characterIcons[charIndex];
            }

            if (topPlayerHighlights != null &&
                i < topPlayerHighlights.Length &&
                topPlayerHighlights[i] != null)
            {
                topPlayerHighlights[i].SetActive(i == currentPlayerIndex);
            }

            if (topPlayerNumberImages != null &&
                i < topPlayerNumberImages.Length &&
                topPlayerNumberImages[i] != null &&
                playerNumberSprites != null &&
                i < playerNumberSprites.Length)
            {
                topPlayerNumberImages[i].sprite = playerNumberSprites[i];
            }

            if (topPlayerEvolutionGauges != null &&
                i < topPlayerEvolutionGauges.Length)
            {
                RefreshEvolutionGauge(topPlayerEvolutionGauges[i], playerEvolutionLevels[i]);
            }
        }
    }

    void RefreshCurrentPlayerPanel()
    {
        if (players.Count == 0) return;

        int playerIndex = currentPlayerIndex;
        int charIndex = GameSession.PlayerCharacters[playerIndex];

        // 今ターンのキャラ立ち絵
        if (currentPlayerImage != null &&
            charIndex >= 0 &&
            charIndex < characterLargeSprites.Length)
        {
            currentPlayerImage.sprite = characterLargeSprites[charIndex];
        }

        // 進化ゲージ
        RefreshEvolutionGauge(currentPlayerEvolutionGauge, playerEvolutionLevels[playerIndex]);

        // 手持ちMist
        if (playerIndex >= 0 && playerIndex < playerMists.Count)
        {
            RefreshMistSlots(currentPlayerMistSlots, playerMists[playerIndex]);
        }
        RefreshMPSlots(currentPlayerMPSlots, playerMP[playerIndex]);
    }

    void RefreshEvolutionGauge(EvolutionGaugeUI gaugeUI, int level)
    {
        if (gaugeUI == null || gaugeUI.gauges == null) return;

        for (int i = 0; i < gaugeUI.gauges.Length; i++)
        {
            if (gaugeUI.gauges[i] == null) continue;

            bool isOn = i < level;

            if (isOn)
            {
                gaugeUI.gauges[i].sprite = evolutionOnSprite;
            }
            else
            {
                gaugeUI.gauges[i].sprite = evolutionOffSprite;
            }

            gaugeUI.gauges[i].enabled = true;
            gaugeUI.gauges[i].color = Color.white;
        }
    }

    void RefreshMistSlots(MistSlotsUI mistUI, List<MistType> mists)
    {
        if (mistUI == null || mistUI.slots == null) return;

        for (int i = 0; i < mistUI.slots.Length; i++)
        {
            if (mistUI.slots[i] == null) continue;

            if (i < mists.Count)
            {
                int spriteIndex = (int)mists[i] - 1;

                if (mistSprites != null && spriteIndex >= 0 && spriteIndex < mistSprites.Length)
                {
                    mistUI.slots[i].sprite = mistSprites[spriteIndex];
                    mistUI.slots[i].enabled = true;
                    mistUI.slots[i].color = Color.white;
                }
            }
            else
            {
                mistUI.slots[i].enabled = false;
            }
        }
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
            pos.y = 5f;

            float yRot = 0f;

            if (grid == new Vector2Int(8, 0)) yRot = 0f;
            else if (grid == new Vector2Int(8, 8)) yRot = -90f;
            else if (grid == new Vector2Int(0, 8)) yRot = 180f;
            else if (grid == new Vector2Int(0, 0)) yRot = 90f;

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
        GiveInitialMists();
    }

    void GiveInitialMists()
    {
        for (int i = 0; i < players.Count; i++)
        {
            for (int j = 0; j < START_MIST; j++)
            {
                MistType mist = (MistType)Random.Range(1, 5);
                playerMists[i].Add(mist);
            }
        }
    }

    void GiveMist(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= playerMists.Count) return;
        if (playerMists[playerIndex].Count >= MAX_MIST) return;

        MistType mist = (MistType)Random.Range(1, 5);
        playerMists[playerIndex].Add(mist);

        Debug.Log($"Player {playerIndex + 1} got mist: {mist}");

        if (mistPanel != null && mistPanel.activeSelf && playerIndex == currentPlayerIndex)
        {
            RefreshMistPanelUI(playerIndex);
        }

        if (playerIndex == currentPlayerIndex)
        {
            RefreshCurrentPlayerPanel();
        }
    }

    // =========================================================
    // Mist Panel
    // =========================================================
    void ClearMistIcons()
    {
        if (mistIconHolder == null) return;

        foreach (Transform child in mistIconHolder)
        {
            Destroy(child.gameObject);
        }
    }

    void RefreshMistPanelUI(int playerIndex)
    {
        if (mistIconHolder == null || mistIconPrefab == null || mistSprites == null) return;
        if (playerIndex < 0 || playerIndex >= playerMists.Count) return;

        ClearMistIcons();

        List<MistType> mists = playerMists[playerIndex];

        foreach (MistType mist in mists)
        {
            GameObject icon = Instantiate(mistIconPrefab, mistIconHolder);
            Image img = icon.GetComponent<Image>();

            int index = (int)mist - 1;
            if (index >= 0 && index < mistSprites.Length)
            {
                img.sprite = mistSprites[index];
            }
        }
    }

    // =========================================================
    // UI制御
    // =========================================================
    public void OnTossPressed()
    {
        if (turnButtons != null) turnButtons.SetActive(false);

        FadeCurrentPlayerPanel(false);
        EnterShake();
    }

    public void OnMistPressed()
    {
        if (isMistZoomed) return;   // 戻るボタンで閉じる運用ならこれ推奨
        ToggleMistZoom();
    }
    void ToggleMistZoom()
    {
        isMistZoomed = !isMistZoomed;

        // ←ここ追加
        if (rightInfoLayoutGroup != null)
            rightInfoLayoutGroup.enabled = !isMistZoomed;

        if (mistZoomCoroutine != null)
            StopCoroutine(mistZoomCoroutine);

        mistZoomCoroutine = StartCoroutine(AnimateMistZoom(isMistZoomed));

        FadeMistZoomBackButton(isMistZoomed);
    }
    public void OnMistZoomBackButtonPressed()
    {
        if (!isMistZoomed) return;

        isMistZoomed = false;

        if (mistZoomCoroutine != null)
            StopCoroutine(mistZoomCoroutine);

        mistZoomCoroutine = StartCoroutine(AnimateMistZoom(false));

        FadeMistZoomBackButton(false);
    }
    IEnumerator AnimateMistZoom(bool zoomIn)
    {
        if (mistZoomTarget == null)
            yield break;

        if (dimOverlayCanvasGroup != null && zoomIn)
        {
            dimOverlayCanvasGroup.gameObject.SetActive(true);
        }

        // 拡大前の通常位置を毎回保存
        if (!zoomIn)
        {
            // 戻す時は保存済みを使う
        }
        else
        {
            mistSlotsNormalAnchoredPosition = mistZoomTarget.anchoredPosition;
            mistSlotsNormalSiblingIndex = mistZoomTarget.GetSiblingIndex();
        }

        // 前面へ
        if (zoomIn)
        {
            mistZoomTarget.SetAsLastSibling();
        }

        Vector3 startScale = mistZoomTarget.localScale;
        Vector3 targetScale = zoomIn
            ? mistSlotsNormalScale * mistZoomScale
            : mistSlotsNormalScale;

        Vector2 startPos = mistZoomTarget.anchoredPosition;
        Vector2 targetPos = zoomIn
            ? mistSlotsNormalAnchoredPosition
            : mistSlotsNormalAnchoredPosition;

        float startAlpha = dimOverlayCanvasGroup != null ? dimOverlayCanvasGroup.alpha : 0f;
        float targetAlpha = zoomIn ? 0.75f : 0f;

        float elapsed = 0f;

        while (elapsed < mistZoomDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / mistZoomDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            mistZoomTarget.localScale =
                Vector3.Lerp(startScale, targetScale, t);

            mistZoomTarget.anchoredPosition =
                Vector2.Lerp(startPos, targetPos, t);

            if (dimOverlayCanvasGroup != null)
            {
                dimOverlayCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            }

            yield return null;
        }

        mistZoomTarget.localScale = targetScale;
        mistZoomTarget.anchoredPosition = targetPos;

        if (!zoomIn)
        {
            mistZoomTarget.SetSiblingIndex(mistSlotsNormalSiblingIndex);
        }

        if (dimOverlayCanvasGroup != null)
        {
            dimOverlayCanvasGroup.alpha = targetAlpha;

            if (!zoomIn)
            {
                dimOverlayCanvasGroup.gameObject.SetActive(false);
            }
        }
        if (!zoomIn && rightInfoLayoutGroup != null)
        {
            rightInfoLayoutGroup.enabled = true;
        }
        mistZoomCoroutine = null;
    }
    void EnterTurnStart()
    {
        if (turnButtons != null) turnButtons.SetActive(true);
        if (dragArea != null) dragArea.SetActive(false);
        if (mistPanel != null) mistPanel.SetActive(false);

        DragAreaController dragController = dragArea != null
            ? dragArea.GetComponent<DragAreaController>()
            : null;

        if (dragController != null)
            dragController.SetDraggable(false);

        currentState = GameState.Idle;
    }

    void EnterShake()
    {
        currentState = GameState.Shake;

        if (turnButtons != null) turnButtons.SetActive(false);
        if (dragArea != null) dragArea.SetActive(true);

        DragAreaController dragController = dragArea != null
            ? dragArea.GetComponent<DragAreaController>()
            : null;

        if (dragController != null)
            dragController.SetDraggable(true);

        Debug.Log($"▶ {currentPlayerIndex + 1}P のターン");
    }
    void ShowMistZoomBackButtonImmediate()
    {
        if (mistZoomBackButton != null)
            mistZoomBackButton.SetActive(true);

        if (mistZoomBackButtonCanvasGroup != null)
        {
            mistZoomBackButtonCanvasGroup.alpha = 1f;
            mistZoomBackButtonCanvasGroup.interactable = true;
            mistZoomBackButtonCanvasGroup.blocksRaycasts = true;
        }
    }

    void HideMistZoomBackButtonImmediate()
    {
        if (mistZoomBackButtonCanvasGroup != null)
        {
            mistZoomBackButtonCanvasGroup.alpha = 0f;
            mistZoomBackButtonCanvasGroup.interactable = false;
            mistZoomBackButtonCanvasGroup.blocksRaycasts = false;
        }

        if (mistZoomBackButton != null)
            mistZoomBackButton.SetActive(false);
    }
    public void OnMistSlotClicked(int slotIndex)
    {
        UseMist(slotIndex);
    }

    void UseMist(int slotIndex)
    {
        int playerIndex = currentPlayerIndex;

        if (playerIndex < 0 || playerIndex >= playerMists.Count) return;
        if (slotIndex < 0 || slotIndex >= playerMists[playerIndex].Count) return;

        MistType usedMist = playerMists[playerIndex][slotIndex];

        Debug.Log($"Player {playerIndex + 1} used Mist: {usedMist}");

        // =========================
        // Mistの種類ごとに効果発動
        // =========================
        switch (usedMist)
        {
            case MistType.Hole:
                ActivateHole(playerIndex);
                break;

            case MistType.Plus1:
                ActivatePlus1(playerIndex);
                break;

            case MistType.Double:
                ActivateDouble(playerIndex);
                break;

            case MistType.Uturn:
                ActivateUturn(playerIndex);
                break;

            default:
                Debug.LogWarning("未対応のMistです");
                break;
        }

        // Mist削除
        playerMists[playerIndex].RemoveAt(slotIndex);

        // UI更新
        RefreshCurrentPlayerPanel();

        // MP加算
        AddMP(playerIndex, 1);
    }
    void ActivateHole(int playerIndex)
    {
        // 外周マスのうち、角を除いたマスだけ候補にする
        List<int> candidates = new List<int>();

        for (int i = 0; i < boardManager.outerPath.Count; i++)
        {
            Vector2Int grid = boardManager.outerPath[i];

            // 角は除外
            if (IsCorner(grid)) continue;

            // すでに穴があるマスは除外
            if (holeOwnerByPathIndex.ContainsKey(i)) continue;

            candidates.Add(i);
        }

        if (candidates.Count == 0)
        {
            Debug.Log("Holeを置けるマスがありません");
            return;
        }

        int randomIndex = Random.Range(0, candidates.Count);
        int holePathIndex = candidates[randomIndex];

        // ロジック上の穴を登録
        holeOwnerByPathIndex[holePathIndex] = playerIndex;

        Vector2Int holeGrid = boardManager.outerPath[holePathIndex];
        Debug.Log($"Player {playerIndex + 1} placed Hole at {holeGrid} (pathIndex={holePathIndex})");

        // =========================
        // 見た目を生成
        // =========================
        CreateHoleVisual(holePathIndex, playerIndex);
    }
    void RemoveHoleVisual(int holePathIndex)
    {
        if (holeVisualsByPathIndex.TryGetValue(holePathIndex, out GameObject marker))
        {
            if (marker != null)
            {
                Destroy(marker);
            }

            holeVisualsByPathIndex.Remove(holePathIndex);
        }
    }
    void CreateHoleVisual(int holePathIndex, int playerIndex)
    {
        if (holeMarkerPrefab == null) return;
        if (holeSprites == null || holeSprites.Length == 0) return;

        Vector2Int grid = boardManager.outerPath[holePathIndex];

        // Board のマス座標 → ワールド座標
        Vector3 pos = boardManager.GridToWorld(grid.x, grid.y);

        // 盤面より少し上に置く
        pos.y = 5.05f;

        GameObject marker = Instantiate(holeMarkerPrefab, pos, Quaternion.Euler(90f, 0f, 0f));

        SpriteRenderer sr = marker.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // playerIndex は 0始まりなのでそのまま対応
            if (playerIndex >= 0 && playerIndex < holeSprites.Length)
            {
                sr.sprite = holeSprites[playerIndex];
            }
        }

        holeVisualsByPathIndex[holePathIndex] = marker;
    }

    void ActivatePlus1(int playerIndex)
    {
        Debug.Log($"Player {playerIndex + 1} : Plus1 発動");
        // TODO: Plus1の効果を書く
    }

    void ActivateDouble(int playerIndex)
    {
        Debug.Log($"Player {playerIndex + 1} : Double 発動");
        // TODO: Doubleの効果を書く
    }

    void ActivateUturn(int playerIndex)
    {
        Debug.Log($"Player {playerIndex + 1} : Uturn 発動");
        // TODO: Uturnの効果を書く
    }
    void AddMP(int playerIndex, int amount)
    {
        if (playerIndex < 0 || playerIndex >= playerMP.Length) return;

        playerMP[playerIndex] += amount;

        if (playerMP[playerIndex] >= MAX_MP)
        {
            playerMP[playerIndex] -= MAX_MP;
            AddEvolutionLevel(playerIndex, 1);

            Debug.Log($"Player {playerIndex + 1} Evolution via MP!");
        }

        Debug.Log($"Player {playerIndex + 1} MP: {playerMP[playerIndex]}");

        RefreshAllPlayerUI();
    }
    void RefreshMPSlots(MPSlotsUI mpUI, int mpValue)
    {
        if (mpUI == null || mpUI.slots == null) return;

        mpValue = Mathf.Clamp(mpValue, 0, MAX_MP);

        for (int i = 0; i < mpUI.slots.Length; i++)
        {
            if (mpUI.slots[i] == null) continue;

            bool isOn = i < mpValue;

            mpUI.slots[i].enabled = true;
            mpUI.slots[i].sprite = isOn ? mpOnSprite : mpOffSprite;
            mpUI.slots[i].color = Color.white;
        }
    }
    // =========================================================
    // Drag → 発射
    // =========================================================

Vector3 ConvertToBoradPosition(Vector3 dragWorldPos)
    {
        Ray ray = new Ray(dragWorldPos + Vector3.up * 10f, Vector3.down);
        if(Physics.Raycast(ray,out RaycastHit hit,50f))
        {
            if(hit.collider.gameObject==boardManager.gameObject)
            {
                return hit.point;
            }
        }
        return new Vector3(dragWorldPos.x, 5f, dragWorldPos.z);
    }
    public void OnShakeRelease(Vector3 launchPos, Vector2 dir2D, float dragDistance, float speed)
    {
        if (currentState != GameState.Shake) return;
        if (dir2D.sqrMagnitude < 0.0001f) return;

        Vector3 dir3D = new Vector3(dir2D.x, 0f, dir2D.y).normalized;

        SpawnAndLaunchGraves(launchPos, dir3D, dragDistance);
    }

    void SpawnAndLaunchGraves(Vector3 launchPos, Vector3 dir, float dragDistance)
    {
        ClearSpawnedGraves();

        stoppedCount = 0;
        totalSteps = 0;
        stoppedGraves.Clear();
        hasAnyFallen = false;

        Vector3 forward = dir;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;

        forward.Normalize();

        Vector3 side = Vector3.Cross(Vector3.up, forward).normalized;

        float power = Mathf.Clamp(dragDistance, 0.5f, 10f);

        for (int i = 0; i < graveCount; i++)
        {
            float ratio = GetArrayValueOrDefault(distanceRatios, i, 1f);
            float sideOffset = GetArrayValueOrDefault(sideOffsets, i, 0f);

            float randomSpawnSide = Random.Range(-randomSpawnSideRange, randomSpawnSideRange);

            Vector3 spawnPos =
                launchPos
                + side * (sideOffset * spawnSpreadMultiplier + randomSpawnSide)
                + Vector3.up * spawnHeight;

            GameObject grave = Instantiate(gravePrefab, spawnPos, Quaternion.identity);

            Rigidbody rb = grave.GetComponent<Rigidbody>();
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.linearDamping = 1.0f;
            rb.angularDamping = 3.5f;

            // 進行方向を向きつつ、少し前傾＆ランダム姿勢
            rb.rotation =
                Quaternion.LookRotation(forward, Vector3.up) *
                Quaternion.Euler(
                    -25f,
                    Random.Range(-8f, 8f),
                    Random.Range(-8f, 8f)
                );

            GraveController gc = grave.GetComponent<GraveController>();
            gc.OnStopped -= OnGraveStopped;
            gc.OnStopped += OnGraveStopped;

            float forwardVariance = Random.Range(forwardVarianceMin, forwardVarianceMax);
            float force = baseForce * ratio * power * forwardVariance;
            float randomSide = Random.Range(-randomSideForceRange, randomSideForceRange);

            Vector3 finalForce =
                forward * (force + Random.Range(-forceJitter, forceJitter))
                + side * (sideOffset * sideForceMultiplier + randomSide)
                + Vector3.down * downwardForce;

            rb.AddForce(finalForce, ForceMode.Impulse);

            // 前後回転を少し入れる
            Vector3 torque =
                side * forwardFlipTorque +
                new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(-0.3f, 0.3f),
                    Random.Range(-1f, 1f)
                ) * torquePower;

            rb.AddTorque(torque, ForceMode.Impulse);

            spawnedGraves.Add(grave);
        }
    }

    // =========================================================
    // 墓停止処理
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
                    GiveMist(currentPlayerIndex);
                    break;

                case GraveFaceResult.Vertical:
                    grave.GetComponent<Renderer>().material = greenMat;
                    totalSteps += 10;
                    GiveMist(currentPlayerIndex);
                    GiveMist(currentPlayerIndex);
                    break;
            }
        }

        stoppedCount++;
        if (stoppedCount < graveCount) return;

        if (hasAnyFallen)
        {
            foreach (GameObject g in spawnedGraves)
            {
                if (g != null)
                    g.GetComponent<Renderer>().material = orangeMat;
            }

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
            CurrentPathIndex =
                (CurrentPathIndex + dir + boardManager.outerPath.Count)
                % boardManager.outerPath.Count;

            Vector2Int grid = boardManager.outerPath[CurrentPathIndex];

            Vector3 pos = boardManager.GridToWorld(grid.x, grid.y);
            pos.y = 5f;

            yield return MoveToPosition(CurrentPlayer.transform, pos, 0.15f);
            yield return new WaitForSeconds(0.05f);

            if (holeOwnerByPathIndex.TryGetValue(CurrentPathIndex, out int holeOwner))
            {
                // 自分が置いた穴なら無視
                if (holeOwner != currentPlayerIndex)
                {
                    Debug.Log($"Player {currentPlayerIndex + 1} fell into Hole at pathIndex={CurrentPathIndex}");

                    // 穴を消す（1回発動したら消滅）
                    holeOwnerByPathIndex.Remove(CurrentPathIndex);
                    RemoveHoleVisual(CurrentPathIndex);

                    // TODO:
                    // ここで穴マーカーを消したいなら消す

                    // このマスで強制停止
                    break;
                }
            }
            if (IsCorner(grid))
            {
                CurrentPlayer.transform.rotation =
                    Quaternion.Euler(
                        90f,
                        RoundTo90(CurrentPlayer.transform.eulerAngles.y - 90f),
                        0f
                    );

                GiveMist(currentPlayerIndex);
            }
        }

        Vector2Int stopGrid = boardManager.outerPath[CurrentPathIndex];
        PlayerController pc = CurrentPlayer.GetComponent<PlayerController>();

        bool wasFinal = pc.IsFinalStage();

        if (IsCorner(stopGrid))
        {
            if (GetPlayerEvolutionLevel(currentPlayerIndex) < 3)
            {
                pc.AdvanceEvolution();
                AddEvolutionLevel(currentPlayerIndex, 1);
            }
            else
            {
                pc.AdvanceEvolution();
            }
        }

        bool isFinalNow = pc.IsFinalStage();
        bool becameFinalHere = (!wasFinal && isFinalNow);
        bool onMyStart = CurrentPathIndex == playerStartPathIndices[currentPlayerIndex];

        if (isFinalNow && onMyStart && !becameFinalHere)
        {
            Debug.Log($"🏆 Player {currentPlayerIndex + 1} WIN!");
            GoToWinScene(currentPlayerIndex);
            yield break;
        }

        NextTurn();
    }
    float GetArrayValueOrDefault(float[] array, int index, float defaultValue)
    {
        if (array == null || index < 0 || index >= array.Length)
            return defaultValue;

        return array[index];
    }
    float RoundTo90(float y)
    {
        y = Mathf.Repeat(y, 360f);
        return Mathf.Round(y / 90f) * 90f;
    }

    void NextTurn()
    {
        ClearSpawnedGraves();
        ResetMistZoomImmediate();

        currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;

        EnterTurnStart();
        RefreshAllPlayerUI();
        FadeCurrentPlayerPanel(true);
    }
    void ResetMistZoomImmediate()
    {
        isMistZoomed = false;

        if (mistZoomCoroutine != null)
        {
            StopCoroutine(mistZoomCoroutine);
            mistZoomCoroutine = null;
        }

        if (mistZoomTarget != null)
        {
            mistZoomTarget.localScale = mistSlotsNormalScale;
        }

        if (dimOverlayCanvasGroup != null)
        {
            dimOverlayCanvasGroup.alpha = 0f;
            dimOverlayCanvasGroup.interactable = false;
            dimOverlayCanvasGroup.blocksRaycasts = false;
            dimOverlayCanvasGroup.gameObject.SetActive(false);
        }

        HideMistZoomBackButtonImmediate();
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
        {
            if (grave != null)
                Destroy(grave);
        }

        spawnedGraves.Clear();
    }

    bool IsCorner(Vector2Int grid)
    {
        int max = boardManager.gridSize - 1;
        return (grid.x == 0 || grid.x == max) &&
               (grid.y == 0 || grid.y == max);
    }

    public void SetEvolutionLevel(int playerIndex, int level)
    {
        if (playerIndex < 0 || playerIndex >= playerEvolutionLevels.Length) return;

        playerEvolutionLevels[playerIndex] = Mathf.Clamp(level, 0, 3);
        RefreshAllPlayerUI();
    }

    public void AddEvolutionLevel(int playerIndex, int amount)
    {
        if (playerIndex < 0 || playerIndex >= playerEvolutionLevels.Length) return;

        playerEvolutionLevels[playerIndex] += amount;
        playerEvolutionLevels[playerIndex] = Mathf.Clamp(playerEvolutionLevels[playerIndex], 0, 3);
        RefreshAllPlayerUI();
    }

    public int GetCurrentPlayerIndex()
    {
        return currentPlayerIndex;
    }

    public int GetPlayerEvolutionLevel(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= playerEvolutionLevels.Length) return 0;
        return playerEvolutionLevels[playerIndex];
    }

    public int GetPlayerMistCount(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= playerMists.Count) return 0;
        return playerMists[playerIndex].Count;
    }

    void ShowCurrentPlayerPanelImmediate()
    {
        if (currentPlayerPanel != null)
            currentPlayerPanel.SetActive(true);

        if (currentPlayerPanelCanvasGroup != null)
        {
            currentPlayerPanelCanvasGroup.alpha = 1f;
            currentPlayerPanelCanvasGroup.interactable = true;
            currentPlayerPanelCanvasGroup.blocksRaycasts = true;
        }
    }

    void HideCurrentPlayerPanelImmediate()
    {
        if (currentPlayerPanelCanvasGroup != null)
        {
            currentPlayerPanelCanvasGroup.alpha = 0f;
            currentPlayerPanelCanvasGroup.interactable = false;
            currentPlayerPanelCanvasGroup.blocksRaycasts = false;
        }

        if (currentPlayerPanel != null)
            currentPlayerPanel.SetActive(false);
    }

    void FadeCurrentPlayerPanel(bool fadeIn)
    {
        if (currentPlayerPanelCanvasGroup == null)
        {
            if (currentPlayerPanel != null)
                currentPlayerPanel.SetActive(fadeIn);
            return;
        }

        if (currentPlayerPanelFadeCoroutine != null)
            StopCoroutine(currentPlayerPanelFadeCoroutine);

        currentPlayerPanelFadeCoroutine = StartCoroutine(FadeCurrentPlayerPanelCoroutine(fadeIn));
    }

    IEnumerator FadeCurrentPlayerPanelCoroutine(bool fadeIn)
    {
        if (currentPlayerPanel != null && fadeIn)
            currentPlayerPanel.SetActive(true);

        float startAlpha = currentPlayerPanelCanvasGroup.alpha;
        float targetAlpha = fadeIn ? 1f : 0f;

        float elapsed = 0f;

        while (elapsed < currentPlayerFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / currentPlayerFadeDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            currentPlayerPanelCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        currentPlayerPanelCanvasGroup.alpha = targetAlpha;
        currentPlayerPanelCanvasGroup.interactable = fadeIn;
        currentPlayerPanelCanvasGroup.blocksRaycasts = fadeIn;

        if (!fadeIn && currentPlayerPanel != null)
            currentPlayerPanel.SetActive(false);

        currentPlayerPanelFadeCoroutine = null;
    }
    void FadeMistZoomBackButton(bool fadeIn)
    {
        if (mistZoomBackButtonCanvasGroup == null)
        {
            if (mistZoomBackButton != null)
                mistZoomBackButton.SetActive(fadeIn);
            return;
        }

        if (backButtonFadeCoroutine != null)
            StopCoroutine(backButtonFadeCoroutine);

        backButtonFadeCoroutine = StartCoroutine(FadeMistZoomBackButtonCoroutine(fadeIn));
    }

    IEnumerator FadeMistZoomBackButtonCoroutine(bool fadeIn)
    {
        if (mistZoomBackButton != null && fadeIn)
            mistZoomBackButton.SetActive(true);

        float startAlpha = mistZoomBackButtonCanvasGroup.alpha;
        float targetAlpha = fadeIn ? 1f : 0f;

        float elapsed = 0f;

        while (elapsed < backButtonFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / backButtonFadeDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            mistZoomBackButtonCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        mistZoomBackButtonCanvasGroup.alpha = targetAlpha;
        mistZoomBackButtonCanvasGroup.interactable = fadeIn;
        mistZoomBackButtonCanvasGroup.blocksRaycasts = fadeIn;

        if (!fadeIn && mistZoomBackButton != null)
            mistZoomBackButton.SetActive(false);

        backButtonFadeCoroutine = null;
    }
    
}