using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseWorld : Singleton<MouseWorld>
{
    [SerializeField] private LayerMask mousePlaneMask;

    void Update()
    {
        transform.position = GetPosition();
    }

    public Vector3 GetPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Physics.Raycast(ray, out RaycastHit raycastGit, float.MaxValue, instance.mousePlaneMask);
        return raycastGit.point;

    }
}
