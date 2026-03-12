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
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI selectingPlayerText;

    public Image[] playerSlotIcons;

    [Header("Character Data")]
    public Sprite[] characterSprites;     // 中央の大きい立ち絵
    public Sprite[] characterIcons;       // 上部スロット用小アイコン
    public string[] characterNames;

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

        if (characterNameText != null &&
            characterNames != null &&
            currentCharacter >= 0 &&
            currentCharacter < characterNames.Length)
        {
            characterNameText.text = characterNames[currentCharacter];
        }

        initializedPreview = true;
    }
    void UpdateCharacterPreviewAnimated()
    {
        if (characterNameText != null &&
            characterNames != null &&
            currentCharacter >= 0 &&
            currentCharacter < characterNames.Length)
        {
            characterNameText.text = characterNames[currentCharacter];
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

        previewAnimCoroutine = StartCoroutine(PlayPreviewChangeAnimation(characterSprites[currentCharacter]));
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

        // 1) 今の画像をズームアウト
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            rt.localScale = Vector3.Lerp(startScale, shrinkScale, t);
            yield return null;
        }

        rt.localScale = shrinkScale;

        // 2) 画像切り替え
        characterPreviewImage.sprite = nextSprite;

        // 3) 少し大きめで表示してから通常サイズへ
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
        GameSession.PlayerCharacters = selectedCharacters;
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