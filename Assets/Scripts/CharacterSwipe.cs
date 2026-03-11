using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSwipe : MonoBehaviour
{
    [Header("References")]
    public RectTransform[] displaySlots;   // 画面上の5枠
    public Image[] displaySlotImages;      // 5枠のImage
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

    int selectedCharacterIndex = 0; // 真ん中にいるキャラ
    Vector2 swipeStart;
    bool touching = false;
    bool isAnimating = false;

    const int CENTER_SLOT = 2; // 5枠の真ん中

    void Start()
    {
        if (flow == null)
            flow = FindObjectOfType<GameFlowController>();

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
            SwipeLeft();
        }
        else if (swipe.x > horizontalThreshold)
        {
            SwipeRight();
        }
    }

    void SwipeLeft()
    {
        if (characterIcons == null || characterIcons.Length == 0) return;

        selectedCharacterIndex = (selectedCharacterIndex + 1) % characterIcons.Length;
        StartCoroutine(AnimateRefresh());
    }

    void SwipeRight()
    {
        if (characterIcons == null || characterIcons.Length == 0) return;

        selectedCharacterIndex = (selectedCharacterIndex - 1 + characterIcons.Length) % characterIcons.Length;
        StartCoroutine(AnimateRefresh());
    }

    IEnumerator AnimateRefresh()
    {
        isAnimating = true;

        Vector3[] startScales = new Vector3[displaySlots.Length];
        Vector3[] targetScales = new Vector3[displaySlots.Length];

        for (int i = 0; i < displaySlots.Length; i++)
        {
            startScales[i] = displaySlots[i].localScale;
            float scale = (i == CENTER_SLOT) ? centerScale : sideScale;
            targetScales[i] = Vector3.one * scale;
        }

        // 画像だけ先に切り替える
        RefreshDisplayImages();

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            for (int i = 0; i < displaySlots.Length; i++)
            {
                displaySlots[i].localScale = Vector3.Lerp(startScales[i], targetScales[i], t);
            }

            yield return null;
        }

        for (int i = 0; i < displaySlots.Length; i++)
        {
            displaySlots[i].localScale = targetScales[i];
        }

        NotifyCenterCharacter();
        isAnimating = false;
    }

    void RefreshDisplayImmediate()
    {
        RefreshDisplayImages();

        for (int i = 0; i < displaySlots.Length; i++)
        {
            float scale = (i == CENTER_SLOT) ? centerScale : sideScale;
            displaySlots[i].localScale = Vector3.one * scale;
        }
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

    void NotifyCenterCharacter()
    {
        if (flow == null) return;
        flow.SetCurrentCharacter(selectedCharacterIndex);
    }
}