using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;

namespace WarChess.UnityViews
{
    /// <summary>
    /// Cinemachine 3 相机适配器：保留 WASD/QE/滚轮操作。
    /// 使用公共相机基类保留 cvc 的序列化引用，兼容尚未升级的 CM2 虚拟相机。
    /// </summary>
    public sealed class BattleCameraView : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private float rotationSpeed = 100f;
        [SerializeField] private float zoomSpeed = 5f;
        [Tooltip("绑定 CinemachineCamera，或尚未迁移的 CinemachineVirtualCamera；不是 Main Camera。")]
        [SerializeField] private CinemachineVirtualCameraBase cvc;

        private CinemachineFollow _follow;
#if !CINEMACHINE_NO_CM2_SUPPORT
        // Cinemachine 3 提供的旧组件兼容层；场景升级后优先使用上面的 Follow。
        private CinemachineTransposer _legacyTransposer;
#endif
        private Vector3 _targetOffset;

        private void Start()
        {
            if (cvc == null)
            {
                Debug.LogWarning("BattleCameraView: 请在 Cvc 字段绑定 Cinemachine 相机。", this);
                return;
            }

            // 通过 Body 管线取组件，同时适用于 CM3 同物体组件和 CM2 的隐藏管线子物体。
            CinemachineComponentBase body = cvc.GetCinemachineComponent(CinemachineCore.Stage.Body);
            _follow = body as CinemachineFollow;
#if !CINEMACHINE_NO_CM2_SUPPORT
            _legacyTransposer = body as CinemachineTransposer;
#endif
            if (!TryGetFollowOffset(out _targetOffset))
                Debug.LogWarning("BattleCameraView: 滚轮缩放需要 CinemachineFollow，或旧版 Transposer。移动和旋转仍可用。", this);
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            float x = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
            float z = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
            Vector3 move = Vector3.ClampMagnitude(transform.right * x + transform.forward * z, 1f);
            transform.position += move * (moveSpeed * Time.deltaTime);
            float rotation = (Input.GetKey(KeyCode.Q) ? 1f : 0f) - (Input.GetKey(KeyCode.E) ? 1f : 0f);
            transform.Rotate(Vector3.up, rotation * rotationSpeed * Time.deltaTime, Space.World);

            if (!TryGetFollowOffset(out Vector3 currentOffset)) return;
            if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
                _targetOffset.y = Mathf.Clamp(_targetOffset.y - Input.mouseScrollDelta.y, 2f, 12f);
            SetFollowOffset(Vector3.Lerp(currentOffset, _targetOffset, zoomSpeed * Time.deltaTime));
        }

        private bool TryGetFollowOffset(out Vector3 offset)
        {
            if (_follow != null)
            {
                offset = _follow.FollowOffset;
                return true;
            }
#if !CINEMACHINE_NO_CM2_SUPPORT
            if (_legacyTransposer != null)
            {
                offset = _legacyTransposer.m_FollowOffset;
                return true;
            }
#endif
            offset = default;
            return false;
        }

        private void SetFollowOffset(Vector3 offset)
        {
            if (_follow != null)
                _follow.FollowOffset = offset;
#if !CINEMACHINE_NO_CM2_SUPPORT
            else if (_legacyTransposer != null)
                _legacyTransposer.m_FollowOffset = offset;
#endif
        }
    }
}
