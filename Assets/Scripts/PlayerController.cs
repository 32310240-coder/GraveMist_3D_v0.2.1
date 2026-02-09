using UnityEngine;

public enum EvolutionStage
{
    White,
    Gray,
    Black
}

public class PlayerController : MonoBehaviour
{
    [Header("Player Info")]
    [SerializeField] int playerIndex = -1;

    [Header("Evolution Materials")]
    public Material whiteMat;
    public Material grayMat;
    public Material blackMat;

    [Header("Outline Colors (0=1P,1=2P,2=3P,3=4P)")]
    public Color[] outlineColors = new Color[4]
    {
        Color.red,
        Color.blue,
        Color.yellow,
        Color.green
    };

    Renderer rend;
    EvolutionStage currentStage = EvolutionStage.White;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        rend.material = Instantiate(whiteMat);

        ApplyOutlineColor(); // ← ここで outline も含めて反映
    }

    // ★ GameManagerから必ず呼ぶ
    public void SetPlayerIndex(int index)
    {
        playerIndex = index;
        ApplyOutlineColor();
    }

    public void AdvanceEvolution()
    {
        if (currentStage == EvolutionStage.Black) return;
        currentStage++;
        ApplyMaterial();
    }

    // =====================
    // マテリアル適用（超重要）
    // =====================
    void ApplyMaterial()
    {
        Material baseMat = null;

        switch (currentStage)
        {
            case EvolutionStage.White:
                baseMat = whiteMat;
                break;
            case EvolutionStage.Gray:
                baseMat = grayMat;
                break;
            case EvolutionStage.Black:
                baseMat = blackMat;
                break;
        }

        // ★ 進化時も必ず複製
        rend.material = Instantiate(baseMat);
        ApplyOutlineColor();
    }


    void ApplyOutlineColor()
    {
        if (playerIndex < 0 || playerIndex >= outlineColors.Length)
            return;

        if (rend.material.HasProperty("_OutlineColor"))
        {
            rend.material.SetColor("_OutlineColor", outlineColors[playerIndex]);
        }
    }
}
