using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSwipe : MonoBehaviour
{
    [Header("References")]
    public RectTransform[] displaySlots;   // 7枠（左右バッファ込み）
    public Image[] displaySlotImages;      // 7枠分
    public GameFlowController flow;

    [Header("Character Data")]
    public Sprite[] characterIcons;

    [Header("Swipe Settings")]
    public float horizontalThreshold = 80f;
    public float verticalThreshold = 120f;
    public float directionLockThreshold = 30f;

    [Header("Animation")]
    public float moveDuration = 0.22f;
    public float centerScale = 1.2f;
    public float sideScale = 0.9f;

    int selectedCharacterIndex = 0;
    Vector2 swipeStart;
    bool touching = false;
    bool isAnimating = false;

    // 7枠の真ん中
    const int CENTER_SLOT = 3;

    Vector2[] basePositions;

    [Header("Layout")]
    public float slotSpacing = 180f;

    enum SwipeDirection
    {
        None,
        Horizontal,
        Vertical
    }

    SwipeDirection lockedDirection = SwipeDirection.None;

    void Start()
    {
        if (flow == null)
            flow = FindObjectOfType<GameFlowController>();

        if (!ValidateSetup()) return;

        basePositions = new Vector2[displaySlots.Length];

        for (int i = 0; i < displaySlots.Length; i++)
        {
            float x = (i - CENTER_SLOT) * slotSpacing;
            basePositions[i] = new Vector2(x, displaySlots[i].anchoredPosition.y);

            displaySlots[i].anchoredPosition = basePositions[i];
        }

        RefreshDisplayImmediate();
        NotifyCenterCharacter();
    }

    bool ValidateSetup()
    {
        if (displaySlots == null || displaySlots.Length != 7)
        {
            Debug.LogError("displaySlots は 7 個必要です。（左右バッファ込み）");
            return false;
        }

        if (displaySlotImages == null || displaySlotImages.Length != 7)
        {
            Debug.LogError("displaySlotImages は 7 個必要です。（左右バッファ込み）");
            return false;
        }

        if (characterIcons == null || characterIcons.Length == 0)
        {
            Debug.LogError("characterIcons が未設定です。");
            return false;
        }

        return true;
    }

    void Update()
    {
        HandleInput();
    }

    void HandleInput()
    {
#if UNITY_EDITOR
        HandleMouse();
#else
        HandleTouch();
#endif
    }

    void HandleMouse()
    {
        if (isAnimating) return;

        if (Input.GetMouseButtonDown(0))
            BeginSwipe(Input.mousePosition);

        if (touching && Input.GetMouseButton(0))
            UpdateSwipe(Input.mousePosition);

        if (Input.GetMouseButtonUp(0) && touching)
            EndSwipe(Input.mousePosition);
    }

    void HandleTouch()
    {
        if (isAnimating) return;
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            BeginSwipe(touch.position);
        }
        else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
        {
            if (touching)
                UpdateSwipe(touch.position);
        }
        else if ((touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) && touching)
        {
            EndSwipe(touch.position);
        }
    }

    void BeginSwipe(Vector2 startPos)
    {
        swipeStart = startPos;
        touching = true;
        lockedDirection = SwipeDirection.None;
    }

    void UpdateSwipe(Vector2 currentPos)
    {
        Vector2 delta = currentPos - swipeStart;

        if (lockedDirection == SwipeDirection.None)
        {
            if (Mathf.Abs(delta.x) >= directionLockThreshold || Mathf.Abs(delta.y) >= directionLockThreshold)
            {
                lockedDirection = Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                    ? SwipeDirection.Horizontal
                    : SwipeDirection.Vertical;
            }
        }

        // ドラッグ中はスロットを動かさない
    }

    void EndSwipe(Vector2 endPos)
    {
        touching = false;
        Vector2 swipe = endPos - swipeStart;

        if (lockedDirection == SwipeDirection.Vertical)
        {
            if (swipe.y > verticalThreshold)
            {
                if (flow != null)
                    flow.ConfirmCharacter();
            }
        }
        else if (lockedDirection == SwipeDirection.Horizontal)
        {
            if (swipe.x < -horizontalThreshold)
            {
                StartCoroutine(AnimateSwipe(+1)); // 左へ送る見た目
            }
            else if (swipe.x > horizontalThreshold)
            {
                StartCoroutine(AnimateSwipe(-1)); // 右へ送る見た目
            }
        }

        lockedDirection = SwipeDirection.None;
    }

    IEnumerator AnimateSwipe(int characterDelta)
    {
        if (isAnimating || characterIcons.Length == 0) yield break;
        isAnimating = true;

        Vector2[] startPositions = new Vector2[displaySlots.Length];
        Vector2[] endPositions = new Vector2[displaySlots.Length];

        for (int i = 0; i < displaySlots.Length; i++)
        {
            startPositions[i] = displaySlots[i].anchoredPosition;
        }

        if (characterDelta > 0)
        {
            // 左スワイプ
            // 1→0, 2→1, 3→2, 4→3, 5→4, 6→5, 0はそのまま画面外左
            endPositions[0] = basePositions[0];
            endPositions[1] = basePositions[0];
            endPositions[2] = basePositions[1];
            endPositions[3] = basePositions[2];
            endPositions[4] = basePositions[3];
            endPositions[5] = basePositions[4];
            endPositions[6] = basePositions[5];
        }
        else
        {
            // 右スワイプ
            // 0→1, 1→2, 2→3, 3→4, 4→5, 5→6, 6はそのまま画面外右
            endPositions[0] = basePositions[1];
            endPositions[1] = basePositions[2];
            endPositions[2] = basePositions[3];
            endPositions[3] = basePositions[4];
            endPositions[4] = basePositions[5];
            endPositions[5] = basePositions[6];
            endPositions[6] = basePositions[6];
        }

        yield return AnimateSlots(startPositions, endPositions);

        selectedCharacterIndex =
            (selectedCharacterIndex + characterDelta + characterIcons.Length) % characterIcons.Length;

        // アニメ終了後、位置を初期位置へ戻して画像を再割り当て
        for (int i = 0; i < displaySlots.Length; i++)
        {
            displaySlots[i].anchoredPosition = basePositions[i];
        }

        RefreshDisplayImages();
        RefreshScalesImmediate();
        NotifyCenterCharacter();

        isAnimating = false;
    }

    IEnumerator AnimateSlots(Vector2[] startPositions, Vector2[] endPositions)
    {
        Vector3[] startScales = new Vector3[displaySlots.Length];
        Vector3[] endScales = new Vector3[displaySlots.Length];

        for (int i = 0; i < displaySlots.Length; i++)
        {
            startScales[i] = displaySlots[i].localScale;

            int nearestSlot = FindNearestBaseSlotIndex(endPositions[i]);
            float scale = (nearestSlot == CENTER_SLOT) ? centerScale : sideScale;
            endScales[i] = Vector3.one * scale;
        }

        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            for (int i = 0; i < displaySlots.Length; i++)
            {
                displaySlots[i].anchoredPosition = Vector2.Lerp(startPositions[i], endPositions[i], t);
                displaySlots[i].localScale = Vector3.Lerp(startScales[i], endScales[i], t);
            }

            yield return null;
        }

        for (int i = 0; i < displaySlots.Length; i++)
        {
            displaySlots[i].anchoredPosition = endPositions[i];
            displaySlots[i].localScale = endScales[i];
        }
    }

    int FindNearestBaseSlotIndex(Vector2 pos)
    {
        float closest = Mathf.Infinity;
        int index = 0;

        for (int i = 0; i < basePositions.Length; i++)
        {
            float d = Vector2.Distance(pos, basePositions[i]);
            if (d < closest)
            {
                closest = d;
                index = i;
            }
        }

        return index;
    }

    void RefreshDisplayImmediate()
    {
        RefreshDisplayImages();
        RefreshScalesImmediate();
    }

    void RefreshDisplayImages()
    {
        for (int i = 0; i < displaySlotImages.Length; i++)
        {
            int offset = i - CENTER_SLOT;
            int charIndex = (selectedCharacterIndex + offset + characterIcons.Length) % characterIcons.Length;
            displaySlotImages[i].sprite = characterIcons[charIndex];
        }
    }

    void RefreshScalesImmediate()
    {
        for (int i = 0; i < displaySlots.Length; i++)
        {
            float scale = (i == CENTER_SLOT) ? centerScale : sideScale;
            displaySlots[i].localScale = Vector3.one * scale;
        }
    }

    void NotifyCenterCharacter()
    {
        if (flow == null) return;
        flow.SetCurrentCharacter(selectedCharacterIndex);
    }
}