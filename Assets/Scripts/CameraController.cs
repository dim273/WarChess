using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    private const int MIN_FOLLOW_Y_OFFSET = 2;
    private const int MAX_FOLLOW_Y_OFFSET = 12;

    [Header("Config")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float rotationSpeed;
    [SerializeField] private float zoomSpeed;
    [SerializeField] private CinemachineVirtualCamera cvc;

    private CinemachineTransposer ct;
    private Vector3 targetFollowOffset;
    private void Start()
    {
        ct = cvc.GetCinemachineComponent<CinemachineTransposer>();
        targetFollowOffset = ct.m_FollowOffset;
    }

    private void Update()
    {
        if (UIManager.instance.gameStop)
            return;
        MoveHandle();
        RotateHandle();
        ZoomHandle();
    }

    private void RotateHandle()
    {
        Vector3 rotationVector = new Vector3(0, 0, 0);
        if (Input.GetKey(KeyCode.Q))
            rotationVector.y = 1f;
        if (Input.GetKey(KeyCode.E))
            rotationVector.y = -1f;
        transform.eulerAngles += rotationVector * rotationSpeed * Time.deltaTime;
    }

    private void MoveHandle()
    {
        Vector3 inputMoveDir = new Vector3(0, 0, 0);

        if (Input.GetKey(KeyCode.W))
            inputMoveDir.z = 1f;
        if (Input.GetKey(KeyCode.S))
            inputMoveDir.z = -1f;
        if (Input.GetKey(KeyCode.A))
            inputMoveDir.x = -1f;
        if (Input.GetKey(KeyCode.D))
            inputMoveDir.x = 1f;

        Vector3 moveVector = transform.forward * inputMoveDir.z + transform.right * inputMoveDir.x;
        transform.position += moveVector * moveSpeed * Time.deltaTime;
    }

    private void ZoomHandle()
    {
        float zoomAmount = 1f;
        if (Input.mouseScrollDelta.y > 0)
            targetFollowOffset.y -= zoomAmount;
        if (Input.mouseScrollDelta.y < 0)
            targetFollowOffset.y += zoomAmount;
        targetFollowOffset.y = Mathf.Clamp(targetFollowOffset.y, MIN_FOLLOW_Y_OFFSET, MAX_FOLLOW_Y_OFFSET);

        ct.m_FollowOffset = Vector3.Lerp(ct.m_FollowOffset, targetFollowOffset, zoomSpeed * Time.deltaTime);
    }
}
