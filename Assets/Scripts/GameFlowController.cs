using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameFlowController : MonoBehaviour
{
    public TextMeshProUGUI winnerText;

    // ===== 追加：キャラ選択画面用 =====
    public GameObject settingsPanel;
    public GameObject settingsButton;
    public GameObject[] playerSlots; // 上部の1P〜4P表示

    public RectTransform characterBar;
    public RectTransform[] characterIcons;

    public int currentCharacter = 0;

    // ===== キャラプレビュー =====
    public UnityEngine.UI.Image characterPreviewImage;
    public TMPro.TextMeshProUGUI characterNameText;

    public Sprite[] characterSprites;
    public string[] characterNames;

    int lastCharacter = -1;

    public int selectingPlayer = 0;
    int[] selectedCharacters = new int[4];
    public TMPro.TextMeshProUGUI selectingPlayerText;

    void Start()
    {
        // ===== WinScene用 =====
        if (winnerText != null)
        {
            int w = GameSession.WinnerIndex;

            if (w < 0)
                winnerText.text = "Winner: ?";
            else
                winnerText.text = $"{w + 1}P の勝利！";
        }

        // ===== キャラ選択画面用 =====
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        UpdatePlayerSlots();
        UpdateSelectingPlayerUI();
    }

    // =========================
    // StartScene
    // =========================

    public void StartGame()
    {
        SceneManager.LoadScene("ModeSelectScene");
    }

    // =========================
    // ModeSelectScene
    // =========================

    public void SelectClassic()
    {
        SceneManager.LoadScene("PlayerSelectScene");
    }

    public void SelectBasic()
    {
        Debug.Log("Basicモードは未実装");
    }

    // =========================
    // WinScene
    // =========================

    public void BackToTitle()
    {
        SceneManager.LoadScene("StartScene");
    }

    // =================================================
    // ここから追加：キャラクター選択UI
    // =================================================

    public void OpenSettings()
    {
        if (settingsPanel == null) return;

        settingsPanel.SetActive(true);

        if (settingsButton != null)
            settingsButton.SetActive(false);
    }

    public void CloseSettings()
    {
        if (settingsPanel == null) return;

        settingsPanel.SetActive(false);

        if (settingsButton != null)
            settingsButton.SetActive(true);
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

    void UpdatePlayerSlots()
    {
        if (playerSlots == null) return;

        for (int i = 0; i < playerSlots.Length; i++)
        {
            if (playerSlots[i] != null)
                playerSlots[i].SetActive(i < GameSession.PlayerCount);
        }
    }
    void Update()
    {
        DetectCenterCharacter();
        UpdateCharacterPreview();
    }

    void DetectCenterCharacter()
    {
        if (characterBar == null || characterIcons == null) return;

        float closestDistance = Mathf.Infinity;
        int closestIndex = 0;

        float centerX = characterBar.position.x;

        for (int i = 0; i < characterIcons.Length; i++)
        {
            float distance = Mathf.Abs(characterIcons[i].position.x - centerX);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        currentCharacter = closestIndex;
    }
    void UpdateCharacterPreview()
    {
        if (currentCharacter == lastCharacter) return;

        lastCharacter = currentCharacter;

        if (characterPreviewImage != null && characterSprites.Length > currentCharacter)
            characterPreviewImage.sprite = characterSprites[currentCharacter];

        if (characterNameText != null && characterNames.Length > currentCharacter)
            characterNameText.text = characterNames[currentCharacter];
    }
    void UpdateSelectingPlayerUI()
    {
        if (selectingPlayerText != null)
            selectingPlayerText.text = $"{selectingPlayer + 1}P キャラクター選択";
    }
    public void ConfirmCharacter()
    {
        selectedCharacters[selectingPlayer] = currentCharacter;

        Debug.Log($"{selectingPlayer + 1}P キャラ決定: {currentCharacter}");

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
}

