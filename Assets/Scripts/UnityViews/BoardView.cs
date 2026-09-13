using System;
using System.Collections.Generic;
using UnityEngine;
using WarChess.Domain;
using WarChess.Presentation;

namespace WarChess.UnityViews
{
    /// <summary>
    /// 棋盘的 Unity View：创建高亮格、转换网格/世界坐标，并响应 Presenter 的显示请求。
    /// </summary>
    public sealed class BoardView : MonoBehaviour, IBoardView
    {
        [Header("Grid visuals")]
        [SerializeField] private GridCellView cellPrefab;
        [SerializeField] private Transform cellContainer;
        [SerializeField] private float surfaceOffset = 0.02f;

        [Header("Highlight materials")]
        [SerializeField] private Material moveMaterial;
        [SerializeField] private Material attackMaterial;
        [SerializeField] private Material defendMaterial;

        private GridCellView[,] _cells;
        private float _cellSize;

        /// <summary>装配时只设坐标参数；实际高亮对象在入口 Start 创建。</summary>
        public void ConfigureGeometry(float cellSize)
        {
            if (cellSize <= 0f) throw new ArgumentOutOfRangeException(nameof(cellSize));
            _cellSize = cellSize;
        }

        public void Initialize(int width, int height, float cellSize)
        {
            // 所有场景引用在初始化边界一次性校验，配置错误可以尽早暴露。
            if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (cellSize <= 0f) throw new ArgumentOutOfRangeException(nameof(cellSize));
            if (cellPrefab == null) throw new InvalidOperationException("BoardView requires a cell prefab.");

            if (_cells != null) throw new InvalidOperationException("BoardView was already initialized.");
            _cellSize = cellSize;
            _cells = new GridCellView[width, height];
            Transform parent = cellContainer == null ? transform : cellContainer;

            // 高亮格只创建一次，后续通过显隐复用，避免行动预览时反复 Instantiate/Destroy。
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    var coord = new GridCoord(x, z);
                    GridCellView cell = Instantiate(
                        cellPrefab,
                        GridToWorld(coord) + transform.up * surfaceOffset,
                        transform.rotation * cellPrefab.transform.localRotation,
                        parent);
                    cell.name = $"Cell_{x}_{z}";
                    cell.Initialize(cellSize);
                    _cells[x, z] = cell;
                }
            }
        }

        public Vector3 GridToWorld(GridCoord coord)
        {
            // 使用 BoardView 自身朝向作为棋盘坐标系，因此棋盘可整体旋转。
            return transform.position +
                   transform.right * (coord.X * _cellSize) +
                   transform.forward * (coord.Z * _cellSize);
        }

        public GridCoord WorldToGrid(Vector3 worldPosition)
        {
            // 先转换到棋盘局部坐标，再按格子尺寸取最近的格子索引。
            Vector3 local = Quaternion.Inverse(transform.rotation) * (worldPosition - transform.position);
            return new GridCoord(
                Mathf.RoundToInt(local.x / _cellSize),
                Mathf.RoundToInt(local.z / _cellSize));
        }

        public void ClearHighlights()
        {
            if (_cells == null) return;
            for (int x = 0; x < _cells.GetLength(0); x++)
            {
                for (int z = 0; z < _cells.GetLength(1); z++)
                {
                    _cells[x, z].Hide();
                }
            }
        }

        public void ShowHighlights(IReadOnlyList<GridCoord> cells, BoardHighlight highlight)
        {
            if (_cells == null || cells == null) return;
            Material material = GetMaterial(highlight);

            for (int i = 0; i < cells.Count; i++)
            {
                GridCoord coord = cells[i];
                if (coord.X < 0 || coord.Z < 0 ||
                    coord.X >= _cells.GetLength(0) || coord.Z >= _cells.GetLength(1))
                {
                    continue;
                }

                _cells[coord.X, coord.Z].Show(material);
            }
        }

        private Material GetMaterial(BoardHighlight highlight)
        {
            switch (highlight)
            {
                case BoardHighlight.Attack:
                    return attackMaterial;
                case BoardHighlight.Defend:
                    return defendMaterial;
                default:
                    return moveMaterial;
            }
        }
    }
}
