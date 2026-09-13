using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActionButtonUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI TMP;
    [SerializeField] private Button button;

    private Image image;
    private Color baseColor;

    private BaseAction baseAction;

    private void Awake()
    {
        image = GetComponent<Image>();
        baseColor = image.color;
    }

    public void SetBaseAction(BaseAction action)
    {
        TMP.text = action.GetName();
        baseAction = action;
        button.onClick.AddListener(() =>
        {
            RoleActionSystem.instance.SetSelectedAction(action);
        });
    }

    public void UpdateSelectedVisual()
    {
        BaseAction selectedAction = RoleActionSystem.instance.GetSelectedAction();
        if (selectedAction == baseAction) image.color = Color.green;
        else image.color = baseColor;
    }
}
