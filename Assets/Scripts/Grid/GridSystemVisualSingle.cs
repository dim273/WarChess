using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridSystemVisualSingle : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private MeshRenderer m_Renderer;

    public void Hide()
    {
        m_Renderer.enabled = false;
    }

    public void Show()
    {
        m_Renderer.enabled = true;
    }
}
