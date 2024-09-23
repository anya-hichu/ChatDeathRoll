using ChatDeathRoll.Utils;
using Dalamud.Configuration;
using System;

namespace ChatDeathRoll;

[Serializable]
public class Config : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool Enabled { get; set; } = true;

    public string MessageFormat { get; set; } = "{0} [{1}]";

    public string RollText { get; set; } = "Roll ";
    public UIColor? RollTextColor { get; set; }

    public string WinText { get; set; } = "Win";
    public UIColor? WinTextColor { get; set; } = UIColor.Green;

    public string LoseText { get; set; } = "Lose";
    public UIColor? LoseTextColor { get; set; } = UIColor.Red;

    public int MaxActiveLinks { get; set; } = 10;

    public bool MessageFormatValid()
    {
        try
        {
            var _ = string.Format(MessageFormat, string.Empty, string.Empty);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }


}
