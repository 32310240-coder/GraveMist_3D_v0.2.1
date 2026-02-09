using UnityEngine;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
    public int gridSize = 9;
    public float boardSize = 9f; // 実際に使うサイズ（9×9）
    private float cellSize;

    public List<Vector2Int> outerPath = new List<Vector2Int>();
    void Awake()
    {
        cellSize = boardSize / gridSize;
        GenerateOuterPath();
        Debug.Log($"外周ルート生成完了：{outerPath.Count}マス");
    }

    void GenerateOuterPath()
    {
        outerPath.Clear();
        int max = gridSize - 1;

        // 右下 → 右上
        for (int z = 0; z <= max; z++)
            outerPath.Add(new Vector2Int(max, z));

        // 右上 → 左上
        for (int x = max - 1; x >= 0; x--)
            outerPath.Add(new Vector2Int(x, max));

        // 左上 → 左下
        for (int z = max - 1; z >= 0; z--)
            outerPath.Add(new Vector2Int(0, z));

        // 左下 → 右下（最後は除外）
        for (int x = 1; x < max; x++)
            outerPath.Add(new Vector2Int(x, 0));
    }

    // グリッド座標 → ワールド座標
    public Vector3 GridToWorld(int x, int z)
    {
        float start = -boardSize / 2f + cellSize / 2f;
        return new Vector3(start + x * cellSize, 0f, start + z * cellSize);
    }

    // ワールド座標 → グリッド座標
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        float start = -boardSize / 2f;
        int x = Mathf.FloorToInt((worldPos.x - start) / cellSize);
        int z = Mathf.FloorToInt((worldPos.z - start) / cellSize);

        Vector2Int grid = new Vector2Int(x, z);
        Debug.Log($"World{worldPos} → Grid{grid}");
        return grid;
    }

    public bool IsInsideBoard(int x, int z)
    {
        return x >= 0 && x < gridSize && z >= 0 && z < gridSize;
    }
}
