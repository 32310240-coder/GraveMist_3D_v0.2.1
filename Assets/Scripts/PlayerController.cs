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

    public Material baseMat;

    Color[] playerColors = new Color[]
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
        rend.material = Instantiate(baseMat);
    }

    public void SetPlayerIndex(int index)
    {
        playerIndex = index;
        ApplyMaterial(); // ★ 初期色もここで反映
    }

    public void AdvanceEvolution()
    {
        if (currentStage == EvolutionStage.Black) return;
        currentStage++;
        ApplyMaterial();

        int stageNumber = (int)currentStage + 1;
        Debug.Log($"{playerIndex + 1}P が第{stageNumber}段階に進化しました！");
    }

    float GetValueByStage(EvolutionStage stage)
    {
        switch (stage)
        {
            case EvolutionStage.White: return 1.0f;
            case EvolutionStage.Gray: return 0.5f;
            case EvolutionStage.Black: return 0.15f;
        }
        return 1.0f;
    }

    void ApplyMaterial()
    {
        if (playerIndex < 0 || playerIndex >= playerColors.Length)
            return;

        Color baseColor = playerColors[playerIndex];

        Color.RGBToHSV(baseColor, out float h, out float s, out float v);
        float newV = GetValueByStage(currentStage);

        Color finalColor = Color.HSVToRGB(h, s, newV);

        if (rend.material.HasProperty("_BaseColor"))
            rend.material.SetColor("_BaseColor", finalColor);
        else
            rend.material.color = finalColor;
    }

    public bool IsFinalStage()
    {
        return currentStage == EvolutionStage.Black;
    }
}