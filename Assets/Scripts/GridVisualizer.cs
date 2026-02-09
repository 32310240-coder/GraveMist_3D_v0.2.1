using UnityEngine;

public class GridVisualizer : MonoBehaviour
{
    public int gridSize = 9;
    public float boardSize = 9f;
    public float lineWidth = 0.05f;
    public Material lineMaterial;

    void Start()
    {
        DrawGrid();
    }

    void DrawGrid()
    {
        float half = boardSize / 2f;
        float cell = boardSize / gridSize;

        // 縦線（X方向）
        for (int i = 0; i <= gridSize; i++)
        {
            float x = -half + i * cell;
            DrawLine(
                new Vector3(x, 0.01f, -half),
                new Vector3(x, 0.01f, half)
            );
        }

        // 横線（Z方向）
        for (int i = 0; i <= gridSize; i++)
        {
            float z = -half + i * cell;
            DrawLine(
                new Vector3(-half, 0.01f, z),
                new Vector3(half, 0.01f, z)
            );
        }

        Debug.Log("▶ グリッド描画完了");
    }

    void DrawLine(Vector3 start, Vector3 end)
    {
        GameObject lineObj = new GameObject("GridLine");
        lineObj.transform.parent = transform;

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = lineMaterial;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.useWorldSpace = true;
    }
}
