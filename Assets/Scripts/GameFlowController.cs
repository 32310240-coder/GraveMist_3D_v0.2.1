using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class GameFlowController : MonoBehaviour
{
    public TextMeshProUGUI winnerText;

    [Header("Character Select UI")]
    public GameObject settingsPanel;
    public GameObject settingsButton;
    public GameObject[] playerSlots;

    public Image characterPreviewImage;
    public Image characterNameImage; // ← 変更（Text → Image）
    public TextMeshProUGUI selectingPlayerText;

    public Image[] playerSlotIcons;

    [Header("Character Data")]
    public Sprite[] characterSprites;      // 立ち絵（中央）
    public Sprite[] characterIcons;        // 上部スロット用
    public Sprite[] characterNameSprites;

    [Header("Preview Animation")]
    public float previewAnimDuration = 0.18f;
    public float previewMinScale = 0.85f;
    public float previewMaxScale = 1.08f;

    public int currentCharacter = 0;

    int selectingPlayer = 0;
    int[] selectedCharacters = new int[4];

    Coroutine previewAnimCoroutine;
    bool initializedPreview = false;

    void Start()
    {
        if (winnerText != null)
        {
            int w = GameSession.WinnerIndex;
            winnerText.text = (w < 0) ? "Winner: ?" : $"{w + 1}P の勝利！";
        }

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        UpdatePlayerSlots();
        UpdateSelectingPlayerUI();
        ResetPlayerSlotIcons();

        // 初期プレビュー表示
        UpdateCharacterPreviewImmediate();
    }

    void ResetPlayerSlotIcons()
    {
        if (playerSlotIcons == null) return;

        for (int i = 0; i < playerSlotIcons.Length; i++)
        {
            if (playerSlotIcons[i] != null)
            {
                playerSlotIcons[i].sprite = null;
                playerSlotIcons[i].enabled = false;
            }
        }
    }

    public void SetCurrentCharacter(int index)
    {
        if (index < 0 || index >= characterSprites.Length) return;
        if (index == currentCharacter && initializedPreview) return;

        currentCharacter = index;
        UpdateCharacterPreviewAnimated();
    }

    void UpdateCharacterPreviewImmediate()
    {
        if (characterPreviewImage != null &&
            characterSprites != null &&
            currentCharacter >= 0 &&
            currentCharacter < characterSprites.Length)
        {
            characterPreviewImage.sprite = characterSprites[currentCharacter];
            characterPreviewImage.rectTransform.localScale = Vector3.one;
        }

        // 名前画像更新
        if (characterNameImage != null &&
            characterNameSprites != null &&
            currentCharacter >= 0 &&
            currentCharacter < characterNameSprites.Length)
        {
            characterNameImage.sprite = characterNameSprites[currentCharacter];
        }

        initializedPreview = true;
        UpdateCharacterNameImage();
    }
    void UpdateCharacterNameImage()
    {
        if (characterNameImage == null) return;
        if (characterNameSprites == null) return;
        if (currentCharacter < 0 || currentCharacter >= characterNameSprites.Length) return;

        characterNameImage.sprite = characterNameSprites[currentCharacter];
        characterNameImage.enabled = true;
    }
    void UpdateCharacterPreviewAnimated()
    {
        // 名前画像更新
        if (characterNameImage != null &&
            characterNameSprites != null &&
            currentCharacter >= 0 &&
            currentCharacter < characterNameSprites.Length)
        {
            characterNameImage.sprite = characterNameSprites[currentCharacter];
        }

        if (characterPreviewImage == null ||
            characterSprites == null ||
            currentCharacter < 0 ||
            currentCharacter >= characterSprites.Length)
        {
            return;
        }

        if (!initializedPreview || characterPreviewImage.sprite == null)
        {
            UpdateCharacterPreviewImmediate();
            return;
        }

        if (previewAnimCoroutine != null)
            StopCoroutine(previewAnimCoroutine);

        previewAnimCoroutine = StartCoroutine(
            PlayPreviewChangeAnimation(characterSprites[currentCharacter])
        );
        UpdateCharacterNameImage();
    }

    IEnumerator PlayPreviewChangeAnimation(Sprite nextSprite)
    {
        RectTransform rt = characterPreviewImage.rectTransform;

        Vector3 startScale = rt.localScale;
        Vector3 shrinkScale = Vector3.one * previewMinScale;
        Vector3 overshootScale = Vector3.one * previewMaxScale;
        Vector3 normalScale = Vector3.one;

        float halfDuration = previewAnimDuration * 0.5f;
        float elapsed = 0f;

        // 1) 縮小
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            rt.localScale = Vector3.Lerp(startScale, shrinkScale, t);
            yield return null;
        }

        rt.localScale = shrinkScale;

        // 2) 切り替え
        characterPreviewImage.sprite = nextSprite;

        // 3) 拡大→戻す
        elapsed = 0f;
        rt.localScale = overshootScale;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            rt.localScale = Vector3.Lerp(overshootScale, normalScale, t);
            yield return null;
        }

        rt.localScale = normalScale;
        previewAnimCoroutine = null;
    }

    void UpdateSelectingPlayerUI()
    {
        if (selectingPlayerText != null)
            selectingPlayerText.text = $"{selectingPlayer + 1}P キャラクター選択";
    }

    void UpdatePlayerSlots()
    {
        if (playerSlots == null) return;

        for (int i = 0; i < playerSlots.Length; i++)
        {
            if (playerSlots[i] != null)
                playerSlots[i].SetActive(i < GameSession.PlayerCount);
        }
    }

    public void ConfirmCharacter()
    {
        if (selectingPlayer >= GameSession.PlayerCount) return;
        if (currentCharacter < 0 || currentCharacter >= characterIcons.Length) return;

        selectedCharacters[selectingPlayer] = currentCharacter;

        if (playerSlotIcons != null &&
            selectingPlayer < playerSlotIcons.Length &&
            playerSlotIcons[selectingPlayer] != null)
        {
            playerSlotIcons[selectingPlayer].sprite = characterIcons[currentCharacter];
            playerSlotIcons[selectingPlayer].enabled = true;
        }

        selectingPlayer++;

        if (selectingPlayer >= GameSession.PlayerCount)
        {
            StartBattle();
        }
        else
        {
            UpdateSelectingPlayerUI();
        }
    }

    void StartBattle()
    {
        for (int i = 0; i < GameSession.PlayerCount; i++)
        {
            Debug.Log($"[CharacterSelect] {i + 1}P selectedCharacters = {selectedCharacters[i]}");
        }

        GameSession.PlayerCharacters = (int[])selectedCharacters.Clone();
        SceneManager.LoadScene("MainScene");
    }

    public void StartGame()
    {
        SceneManager.LoadScene("ModeSelectScene");
    }

    public void SelectClassic()
    {
        SceneManager.LoadScene("CharacterSelectScene");
    }

    public void SelectBasic()
    {
        Debug.Log("Basicモードは未実装");
    }

    public void BackToTitle()
    {
        SceneManager.LoadScene("StartScene");
    }

    public void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (settingsButton != null) settingsButton.SetActive(false);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (settingsButton != null) settingsButton.SetActive(true);
    }

    public void Set2Players()
    {
        GameSession.PlayerCount = 2;
        UpdatePlayerSlots();
        CloseSettings();
    }

    public void Set3Players()
    {
        GameSession.PlayerCount = 3;
        UpdatePlayerSlots();
        CloseSettings();
    }

    public void Set4Players()
    {
        GameSession.PlayerCount = 4;
        UpdatePlayerSlots();
        CloseSettings();
    }
}
