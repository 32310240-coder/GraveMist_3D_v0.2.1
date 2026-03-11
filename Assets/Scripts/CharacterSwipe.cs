using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSwipe : MonoBehaviour
{
    [Header("References")]
    public RectTransform[] displaySlots;   // 画面上の5枠
    public Image[] displaySlotImages;      // 各枠のImage
    public GameFlowController flow;

    [Header("Character Data")]
    public Sprite[] characterIcons;        // 全キャラ分の小アイコン

    [Header("Swipe Settings")]
    public float horizontalThreshold = 80f;
    public float verticalThreshold = 120f;

    [Header("Animation")]
    public float moveDuration = 0.2f;
    public float centerScale = 1.2f;
    public float sideScale = 0.9f;

    int selectedCharacterIndex = 0; // 真ん中にいるキャラ番号
    Vector2 swipeStart;
    bool touching = false;
    bool isAnimating = false;

    const int CENTER_SLOT = 2;

    Vector2[] basePositions;

    void Start()
    {
        if (flow == null)
            flow = FindObjectOfType<GameFlowController>();

        if (displaySlots == null || displaySlots.Length == 0)
        {
            Debug.LogError("displaySlots が未設定です。");
            return;
        }

        basePositions = new Vector2[displaySlots.Length];
        for (int i = 0; i < displaySlots.Length; i++)
        {
            basePositions[i] = displaySlots[i].anchoredPosition;
        }

        RefreshDisplayImmediate();
        NotifyCenterCharacter();
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
        {
            swipeStart = Input.mousePosition;
            touching = true;
        }

        if (Input.GetMouseButtonUp(0) && touching)
        {
            touching = false;
            Vector2 swipe = (Vector2)Input.mousePosition - swipeStart;
            EvaluateSwipe(swipe);
        }
    }

    void HandleTouch()
    {
        if (isAnimating) return;
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            swipeStart = touch.position;
            touching = true;
        }

        if (touch.phase == TouchPhase.Ended && touching)
        {
            touching = false;
            Vector2 swipe = touch.position - swipeStart;
            EvaluateSwipe(swipe);
        }
    }

    void EvaluateSwipe(Vector2 swipe)
    {
        // 上フリックで決定
        if (swipe.y > verticalThreshold && Mathf.Abs(swipe.y) > Mathf.Abs(swipe.x))
        {
            if (flow != null)
                flow.ConfirmCharacter();
            return;
        }

        if (swipe.x < -horizontalThreshold)
        {
            StartCoroutine(AnimateSwipeLeft());
        }
        else if (swipe.x > horizontalThreshold)
        {
            StartCoroutine(AnimateSwipeRight());
        }
    }

    IEnumerator AnimateSwipeLeft()
    {
        if (isAnimating || characterIcons.Length == 0) yield break;
        isAnimating = true;

        Vector2[] startPositions = new Vector2[displaySlots.Length];
        Vector2[] targetPositions = new Vector2[displaySlots.Length];

        for (int i = 0; i < displaySlots.Length; i++)
        {
            startPositions[i] = displaySlots[i].anchoredPosition;

            if (i < displaySlots.Length - 1)
                targetPositions[i] = basePositions[i];
            else
                targetPositions[i] = basePositions[i];
        }

        // 実際の移動先は「1つ左の見た目」
        for (int i = 0; i < displaySlots.Length; i++)
        {
            int from = i + 1;
            if (from >= displaySlots.Length)
                from = 0;

            targetPositions[i] = basePositions[i];
        }

        // 表示枠を「左へ流す」見た目にする
        // slot i に、もともと slot i+1 が来るように動かす
        Vector2[] animatedTargets = new Vector2[displaySlots.Length];
        for (int i = 0; i < displaySlots.Length; i++)
        {
            int next = i - 1;
            if (next < 0) next = displaySlots.Length - 1;
            animatedTargets[i] = basePositions[next];
        }

        yield return AnimateSlots(startPositions, animatedTargets);

        // データ側を1つ進める
        selectedCharacterIndex = (selectedCharacterIndex + 1) % characterIcons.Length;

        // 位置を戻して画像だけ更新
        for (int i = 0; i < displaySlots.Length; i++)
            displaySlots[i].anchoredPosition = basePositions[i];

        RefreshDisplayImages();
        RefreshScalesImmediate();
        NotifyCenterCharacter();

        isAnimating = false;
    }

    IEnumerator AnimateSwipeRight()
    {
        if (isAnimating || characterIcons.Length == 0) yield break;
        isAnimating = true;

        Vector2[] startPositions = new Vector2[displaySlots.Length];
        Vector2[] animatedTargets = new Vector2[displaySlots.Length];

        for (int i = 0; i < displaySlots.Length; i++)
        {
            startPositions[i] = displaySlots[i].anchoredPosition;

            int next = i + 1;
            if (next >= displaySlots.Length) next = 0;
            animatedTargets[i] = basePositions[next];
        }

        yield return AnimateSlots(startPositions, animatedTargets);

        // データ側を1つ戻す
        selectedCharacterIndex = (selectedCharacterIndex - 1 + characterIcons.Length) % characterIcons.Length;

        // 位置を戻して画像だけ更新
        for (int i = 0; i < displaySlots.Length; i++)
            displaySlots[i].anchoredPosition = basePositions[i];

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

            // アニメ中は「移動先のスロット基準」でスケールを決める
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
        if (displaySlotImages == null || characterIcons == null) return;

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