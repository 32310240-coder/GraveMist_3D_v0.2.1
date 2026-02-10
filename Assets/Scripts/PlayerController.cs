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
    float GetValueByStage(EvolutionStage stage)
    {
        switch (stage)
        {
            case EvolutionStage.White: return 1.0f;  // 100%
            case EvolutionStage.Gray: return 0.5f;  // 50%
            case EvolutionStage.Black: return 0.15f; // 15%
        }
        return 1.0f;
    }

    // =====================
    // マテリアル適用（超重要）
    // =====================
    void ApplyMaterial()
    {
        // ベースマテリアルは whiteMat を使い回す
        rend.material = Instantiate(whiteMat);

        // ===== プレイヤーカラーを HSV で暗くする =====
        Color baseColor = outlineColors[playerIndex];

        Color.RGBToHSV(baseColor, out float h, out float s, out float v);

        float newV = GetValueByStage(currentStage);
        Color evolvedColor = Color.HSVToRGB(h, s, newV);

        // Base Color（Standard / URP どちらでもだいたいこれ）
        if (rend.material.HasProperty("_BaseColor"))
            rend.material.SetColor("_BaseColor", evolvedColor);
        else
            rend.material.color = evolvedColor;

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
