using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GridDebugObject: MonoBehaviour 
{
    // 该代码用于测试体显示数据
    [SerializeField] private TextMeshPro textMeshPro;

    private GridObject gridObject;

    private void Update()
    {
        textMeshPro.text = gridObject.ToString();
    }

    public void SetGridObject(GridObject gridObject)
    {
        this.gridObject = gridObject;
    }

}
