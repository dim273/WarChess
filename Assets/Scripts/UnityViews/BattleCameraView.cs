using Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;

namespace WarChess.UnityViews
{
    /// <summary>保留现有 Cinemachine 2 相机的 WASD/QE/滚轮操作，移除 UIManager 单例依赖。</summary>
    public sealed class BattleCameraView : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private float rotationSpeed = 100f;
        [SerializeField] private float zoomSpeed = 5f;
        [SerializeField] private CinemachineVirtualCamera cvc;
        private CinemachineTransposer _transposer;
        private Vector3 _targetOffset;

        private void Start()
        {
            if (cvc != null) _transposer = cvc.GetCinemachineComponent<CinemachineTransposer>();
            if (_transposer != null) _targetOffset = _transposer.m_FollowOffset;
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
            if (_transposer == null) return;
            if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
                _targetOffset.y = Mathf.Clamp(_targetOffset.y - Input.mouseScrollDelta.y, 2f, 12f);
            _transposer.m_FollowOffset = Vector3.Lerp(_transposer.m_FollowOffset, _targetOffset, zoomSpeed * Time.deltaTime);
        }
    }
}
