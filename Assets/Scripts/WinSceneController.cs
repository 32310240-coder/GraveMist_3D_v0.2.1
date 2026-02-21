using UnityEngine;
using TMPro;

public class WinSceneController : MonoBehaviour
{
    public TextMeshProUGUI winnerText;

    void Start()
    {
        int w = GameSession.WinnerIndex;
        if (w < 0)
        {
            winnerText.text = "Winner: ?";
        }
        else
        {
            winnerText.text = $"{w + 1}P ‚ÌŸ—˜I";
        }
    }
}