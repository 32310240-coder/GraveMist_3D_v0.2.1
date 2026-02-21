using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSelectController : MonoBehaviour
{
    public void Select2()
    {
        GameSession.PlayerCount = 2;
        SceneManager.LoadScene("MainScene"); // ©‚ ‚È‚½‚ÌƒƒCƒ“ƒV[ƒ“–¼‚É‡‚í‚¹‚Ä
    }

    public void Select3()
    {
        GameSession.PlayerCount = 3;
        SceneManager.LoadScene("MainScene");
    }

    public void Select4()
    {
        GameSession.PlayerCount = 4;
        SceneManager.LoadScene("MainScene");
    }
}