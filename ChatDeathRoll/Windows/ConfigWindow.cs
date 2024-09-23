using ChatDeathRoll.Utils;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChatDeathRoll.Windows;

public class ConfigWindow : Window
{
    private static readonly List<string> COLOR_NAMES_WITH_BLANK = new(Enum.GetNames(typeof(UIColor)).Prepend(string.Empty));

    private Config Config { get; init; }

    public ConfigWindow(Config config) : base("ChatDeathRoll Config##configWindow")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new(350, 390),
            MaximumSize = new(float.MaxValue, float.MaxValue)
        };

        Config = config;
    }

    public override void Draw()
    {
        var enabled = Config.Enabled;
        if (ImGui.Checkbox("Enabled##enabled", ref enabled))
        {
            Config.Enabled = enabled;
            Config.Save();
        }

        ImGui.PushStyleVar(ImGuiStyleVar.IndentSpacing, 5);
        if (ImGui.CollapsingHeader("General##general", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent();
            var messageFormat = Config.MessageFormat;
            var messageFormatInput = ImGui.InputText("Message Format##rollText", ref messageFormat, ushort.MaxValue);
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Placeholders are '{0}' for original message and '{1}' for roll link button");
            }
            if (messageFormatInput)
            {
                Config.MessageFormat = messageFormat;
                Config.Save();
            }
            
            if (!Config.MessageFormatValid())
            {
                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
                ImGui.Text("Invalid message format");
                ImGui.PopStyleColor();
            }
            var maxActiveLinks = Config.MaxActiveLinks;
            if (ImGui.InputInt("Max Active Links##maxActiveLinks", ref maxActiveLinks))
            {
                Config.MaxActiveLinks = maxActiveLinks;
                Config.Save();
            }
            ImGui.Unindent();
        }

        var colorNames = COLOR_NAMES_WITH_BLANK.ToArray();
        if (ImGui.CollapsingHeader("Roll Button##roll", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent();
            var rollText = Config.RollText;
            if (ImGui.InputText("Text##rollText", ref rollText, ushort.MaxValue))
            {
                Config.RollText = rollText;
                Config.Save();
            }

            var rollTextColorindex = GetColorIndexByColor(Config.RollTextColor);
            if (ImGui.Combo("Color##rollTextColor", ref rollTextColorindex, colorNames, colorNames.Length))
            {
                Config.RollTextColor = GetColorByColorNameIndex(rollTextColorindex);
                Config.Save();
            }
            ImGui.Unindent();
        }

        if (ImGui.CollapsingHeader("Win Info##win", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent();
            var winText = Config.WinText;
            if (ImGui.InputText("Content##winText", ref winText, ushort.MaxValue))
            {
                Config.WinText = winText;
                Config.Save();
            }
            var winTextColorindex = GetColorIndexByColor(Config.WinTextColor);
            if (ImGui.Combo("Color##winTextColor", ref winTextColorindex, colorNames, colorNames.Length))
            {
                Config.WinTextColor = GetColorByColorNameIndex(winTextColorindex);
                Config.Save();
            }
            ImGui.Unindent();
        }

        if (ImGui.CollapsingHeader("Lose Info##lose", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent();
            var loseText = Config.LoseText;
            if (ImGui.InputText("Text##winText", ref loseText, ushort.MaxValue))
            {
                Config.LoseText = loseText;
                Config.Save();
            }
            var loseTextColorindex = GetColorIndexByColor(Config.LoseTextColor);
            if (ImGui.Combo("Color##loseMessageColor", ref loseTextColorindex, colorNames, colorNames.Length))
            {
                Config.LoseTextColor = GetColorByColorNameIndex(loseTextColorindex);
                Config.Save();
            }
            ImGui.Unindent();
        }
        ImGui.PopStyleVar();
    }

    private static int GetColorIndexByColor(UIColor? color)
    {
        var colorName = color == null ? string.Empty : Enum.GetName(typeof(UIColor), color)!;
        return COLOR_NAMES_WITH_BLANK.IndexOf(colorName);
    }

    private static UIColor? GetColorByColorNameIndex(int index)
    {
        var colorName = COLOR_NAMES_WITH_BLANK[index];
        return colorName == string.Empty ? null : (UIColor)Enum.Parse(typeof(UIColor), colorName);
    }
}
