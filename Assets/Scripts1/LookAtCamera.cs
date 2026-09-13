using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    [SerializeField] private bool invert;
    private Transform camera;

    private void Awake()
    {
        camera = Camera.main.transform;
    }

    private void LateUpdate()
    {
        if(invert)
        {
            Vector3 dirToCamera = (camera.position - transform.position).normalized;
            transform.LookAt(transform.position + dirToCamera * -1);
        }
        else
        {
            transform.LookAt(camera.position);
        }
    }
}
