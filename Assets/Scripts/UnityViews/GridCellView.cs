using UnityEngine;

namespace WarChess.UnityViews
{
    /// <summary>单个棋盘高亮格的 Unity 表现组件。</summary>
    public sealed class GridCellView : MonoBehaviour
    {
        [SerializeField] private MeshRenderer meshRenderer;

        [SerializeField, Min(0.01f)] private float referenceCellSize = 2f;

        public void Initialize(float cellSize)
        {
            // 保留原预制体的平面方向与厚度；旧高亮格的基准格宽为 2。
            transform.localScale *= cellSize / Mathf.Max(0.01f, referenceCellSize);
            if (meshRenderer == null) meshRenderer = GetComponentInChildren<MeshRenderer>();
            Hide();
        }

        public void Show(Material material)
        {
            if (meshRenderer == null) return;
            // 使用 sharedMaterial 避免为每个格子创建独立材质实例。
            meshRenderer.sharedMaterial = material;
            meshRenderer.enabled = true;
        }

        public void Hide()
        {
            if (meshRenderer != null)
            {
                meshRenderer.enabled = false;
            }
        }
    }
}
