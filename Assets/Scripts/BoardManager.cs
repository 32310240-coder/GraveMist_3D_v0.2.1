using UnityEngine;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
    [Header("Board Settings")]
    public int gridSize = 9;
    public float boardSize = 9f;
    public float pieceY = 0.1f;
    public Vector3 boardOffset = Vector3.zero;

    private float cellSize;

    [Header("Path")]
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

    public Vector3 GridToWorld(int x, int z)
    {
        float start = -boardSize / 2f + cellSize / 2f;

        Vector3 localPos = new Vector3(
            start + x * cellSize,
            pieceY,
            start + z * cellSize
        );

        return transform.position + boardOffset + localPos;
    }

    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        Vector3 localPos = worldPos - transform.position - boardOffset;

        float start = -boardSize / 2f;
        int x = Mathf.FloorToInt((localPos.x - start) / cellSize);
        int z = Mathf.FloorToInt((localPos.z - start) / cellSize);

        Vector2Int grid = new Vector2Int(x, z);
        Debug.Log($"World {worldPos} → Grid {grid}");
        return grid;
    }

    public bool IsInsideBoard(int x, int z)
    {
        return x >= 0 && x < gridSize && z >= 0 && z < gridSize;
    }

    public Vector3 PathIndexToWorld(int pathIndex)
    {
        if (outerPath.Count == 0) return transform.position;

        pathIndex = ((pathIndex % outerPath.Count) + outerPath.Count) % outerPath.Count;
        Vector2Int cell = outerPath[pathIndex];
        return GridToWorld(cell.x, cell.y);
    }

    void OnDrawGizmosSelected()
    {
        if (gridSize <= 0) return;

        float cell = boardSize / gridSize;
        float start = -boardSize / 2f + cell / 2f;

        Gizmos.color = Color.cyan;

        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                Vector3 pos = transform.position + boardOffset + new Vector3(
                    start + x * cell,
                    0.05f,
                    start + z * cell
                );

                Gizmos.DrawSphere(pos, 0.06f);
            }
        }
    }
}