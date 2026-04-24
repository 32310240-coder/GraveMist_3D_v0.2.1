using UnityEngine;

public enum EvolutionStage
{
    White,
    Gray,
    Black
}

public class PlayerController : MonoBehaviour
{
    [SerializeField] int playerIndex = -1;

    [Header("Player Sprites")]
    public SpriteRenderer playerSpriteRenderer;
    public Sprite[] playerSprites; // Element 0=1P, 1=2P, 2=3P, 3=4P

    EvolutionStage currentStage = EvolutionStage.White;

    void Awake()
    {
        if (playerSpriteRenderer == null)
            playerSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void SetPlayerIndex(int index)
    {
        playerIndex = index;
        ApplySprite();
    }

    void ApplySprite()
    {
        if (playerSpriteRenderer == null)
        {
            Debug.LogWarning("PlayerのSpriteRendererが見つかりません");
            return;
        }

        if (playerIndex < 0 || playerIndex >= playerSprites.Length)
        {
            Debug.LogWarning("PlayerSpritesの範囲外です");
            return;
        }

        if (playerSprites[playerIndex] == null)
        {
            Debug.LogWarning($"{playerIndex + 1}PのSpriteが設定されていません");
            return;
        }

        playerSpriteRenderer.sprite = playerSprites[playerIndex];
    }

    public void AdvanceEvolution()
    {
        if (currentStage == EvolutionStage.Black) return;

        currentStage++;

        int stageNumber = (int)currentStage + 1;
        Debug.Log($"{playerIndex + 1}P が第{stageNumber}段階に進化しました！");
    }

    public bool IsFinalStage()
    {
        return currentStage == EvolutionStage.Black;
    }
}