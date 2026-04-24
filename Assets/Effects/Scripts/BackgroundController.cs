using UnityEngine;

/// ===============================================
/// 役割：
///   ・星の位置・色・サイズを C# で管理し、シェーダに渡す
///   ・画面外に出た星は中央付近に再生成
///   ・波紋5色ゆるやかループも制御
///
/// アタッチ先：Background（Quad）
/// ===============================================

public class WarpBackgroundController : MonoBehaviour
{
    public Material mat;

    [Header("▼ 波紋設定")]
    public float rippleSpeed = 1.0f;
    public float rippleThicknessMin = 0.01f;
    public float rippleThicknessMax = 0.05f;
    public float rippleColorSpeed = 0.2f;

    [Header("▼ 星設定")]
    public int starCount = 200;
    public float starSpeedBase = 0.2f;
    public float starSpeedSizeMultiplier = 2.0f;
    public float starSizeMin = 0.01f;
    public float starSizeMax = 0.03f;

    [Header("▼ 共通5色（White / Red / Blue / Green / Yellow）")]
    public Color colorWhite = Color.white;
    public Color colorRed = Color.red;
    public Color colorBlue = Color.blue;
    public Color colorGreen = Color.green;
    public Color colorYellow = Color.yellow;

    Vector2[] starPos;
    float[] starSize;
    int[] starColorIndex;

    Vector4[] starPosV4;
    float[] starColorIndexFloat;

    void Start()
    {
        starPos = new Vector2[starCount];
        starSize = new float[starCount];
        starColorIndex = new int[starCount];

        starPosV4 = new Vector4[starCount];
        starColorIndexFloat = new float[starCount];

        for (int i = 0; i < starCount; i++)
            InitializeStar(i);
    }

    // 初期生成（画面全体に散らす）
    void InitializeStar(int i)
    {
        starPos[i] = Random.insideUnitCircle * 1.2f;
        starSize[i] = Random.Range(starSizeMin, starSizeMax);
        starColorIndex[i] = Random.Range(0, 5);
    }

    // 再生成（中央付近に戻す）
    void RespawnStar(int i)
    {
        starPos[i] = Random.insideUnitCircle * 0.2f;
        starSize[i] = Random.Range(starSizeMin, starSizeMax);
        starColorIndex[i] = Random.Range(0, 5);
    }

    void Update()
    {
        if (!mat) return;

        // 波紋
        mat.SetFloat("_TimeValue", Time.time);
        mat.SetFloat("_RippleSpeed", rippleSpeed);
        mat.SetFloat("_RippleMin", rippleThicknessMin);
        mat.SetFloat("_RippleMax", rippleThicknessMax);
        mat.SetFloat("_RippleColorSpeed", rippleColorSpeed);

        // 共通5色
        mat.SetColor("_Color0", colorWhite);
        mat.SetColor("_Color1", colorRed);
        mat.SetColor("_Color2", colorBlue);
        mat.SetColor("_Color3", colorGreen);
        mat.SetColor("_Color4", colorYellow);

        // 星の更新（開始オフセットは廃止）
        for (int i = 0; i < starCount; i++)
        {
            float speed = starSpeedBase + (starSize[i] * starSpeedSizeMultiplier);
            starPos[i] += starPos[i].normalized * (speed * Time.deltaTime);

            if (starPos[i].magnitude > 1.3f)
                RespawnStar(i);

            starPosV4[i] = new Vector4(starPos[i].x, starPos[i].y, 0, 0);
            starColorIndexFloat[i] = (float)starColorIndex[i];
        }

        // シェーダへ送信
        mat.SetInt("_StarCount", starCount);
        mat.SetVectorArray("_StarPos", starPosV4);
        mat.SetFloatArray("_StarSize", starSize);
        mat.SetFloatArray("_StarColorIndex", starColorIndexFloat);
    }
}