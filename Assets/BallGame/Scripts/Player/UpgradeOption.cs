using UnityEngine;

public struct UpgradeOption
{
    public string Title { get; private set; }
    public string Description { get; private set; }
    public Color ButtonColor { get; private set; }
    public System.Action ApplyAction { get; private set; }

    public UpgradeOption(string title, string description, Color buttonColor, System.Action applyAction)
    {
        Title = title;
        Description = description;
        ButtonColor = buttonColor;
        ApplyAction = applyAction;
    }
}