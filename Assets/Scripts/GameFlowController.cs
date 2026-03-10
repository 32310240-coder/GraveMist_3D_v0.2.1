using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameFlowController : MonoBehaviour
{
    public TextMeshProUGUI winnerText;

    void Start()
    {
        if (winnerText == null) return;

        int w = GameSession.WinnerIndex;

        if (w < 0)
            winnerText.text = "Winner: ?";
        else
            winnerText.text = $"{w + 1}P ‚ÌŸ—˜I";
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
        Debug.Log("Basicƒ‚[ƒh‚Í–¢ŽÀ‘•");
    }

    // =========================
    // PlayerSelectScene
    // =========================

    public void Select2Players()
    {
        GameSession.PlayerCount = 2;
        SceneManager.LoadScene("MainScene");
    }

    public void Select3Players()
    {
        GameSession.PlayerCount = 3;
        SceneManager.LoadScene("MainScene");
    }

    public void Select4Players()
    {
        GameSession.PlayerCount = 4;
        SceneManager.LoadScene("MainScene");
    }

    // =========================
    // WinScene
    // =========================

    public void BackToTitle()
    {
        SceneManager.LoadScene("StartScene");
    }
}