using ChatDeathRoll.Chat;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ChatDeathRoll;

public partial class ChatEnricher : IDisposable
{
    private static readonly Range MESSAGE_RANDOM_DELAY = 20..40;
    private static readonly int MAPPED_CHAT_TYPE_MAX_VALUE = Enum.GetValues<XivChatType>().Cast<ushort>().Max();

    public enum RollType
    {
        None,
        Random,
        Dice,
    }

    [GeneratedRegexAttribute(@"(?=\{[0-1]\})|(?<=\{[0-1]\})")]
    private static partial Regex MessageFormatSplitGeneratedRegex();

    [GeneratedRegexAttribute(@"^Random! (.*?) rolls? a (\d+)(?: \(out of \d+\))?\.")]
    private static partial Regex RandomRollGeneratedRegex();

    [GeneratedRegexAttribute(@"^Random! (?:\(\d+-\d+\) )?(\d+)")]
    private static partial Regex DiceRollGeneratedRegex();


    private static readonly Dictionary<XivChatType, string> SWITCH_COMMAND_BY_CHAT_TYPE = new()
    {
        { XivChatType.Shout, "/sh" },
        { XivChatType.Party, "/p" },
        { XivChatType.Alliance, "/a" },
        { XivChatType.Ls1, "/l1" },
        { XivChatType.Ls2, "/l2" },
        { XivChatType.Ls3, "/l3" },
        { XivChatType.Ls4, "/l4" },
        { XivChatType.Ls5, "/l5" },
        { XivChatType.Ls6, "/l6" },
        { XivChatType.Ls7, "/l7" },
        { XivChatType.Ls8, "/l8" },
        { XivChatType.FreeCompany, "/fc" },
        { XivChatType.Yell, "/y" },
        { XivChatType.CrossParty, "/cp" },
        { XivChatType.CrossLinkShell1, "/cwl1" },
        { XivChatType.CrossLinkShell2, "/cwl2" },
        { XivChatType.CrossLinkShell3, "/cwl3" },
        { XivChatType.CrossLinkShell4, "/cwl4" },
        { XivChatType.CrossLinkShell5, "/cwl5" },
        { XivChatType.CrossLinkShell6, "/cwl6" },
        { XivChatType.CrossLinkShell7, "/cwl7" },
        { XivChatType.CrossLinkShell8, "/cwl8" }
    };

    private Random Random { get; init; } = new();
    private IChatGui ChatGui { get; init; }
    private ChatLinks ChatLinks { get; init; }
    private ChatSender ChatSender { get; init; }
    private IClientState ClientState { get; init; }
    private Config Config { get; init; }
    private IPluginLog PluginLog { get; init; }

    public ChatEnricher(IChatGui chatGui, ChatLinks chatLinks, ChatSender chatSender, IClientState clientState, Config config, IPluginLog pluginLog)
    {
        ChatGui = chatGui;
        ChatLinks = chatLinks;
        ChatSender = chatSender;
        ClientState = clientState;
        Config = config;
        PluginLog = pluginLog;

        ChatGui.ChatMessage += OnChatMessage;
    }

    public void Dispose()
    {
        ChatGui.ChatMessage -= OnChatMessage;
    }

    private void OnChatMessage(XivChatType chatType, int a2, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        var localPlayer = ClientState.LocalPlayer;
        if (Config.Enabled && Config.MessageFormatValid() && localPlayer != null)
        {
            if (TryParseRollMessage(chatType, sender, message, out var senderName, out var rollType, out var rollValue))
            {
                var messageBuilder = new SeStringBuilder();
                var messageFormatParts = MessageFormatSplitGeneratedRegex().Split(Config.MessageFormat);

                var isOwnMessage = senderName == string.Empty || senderName == localPlayer.Name.TextValue;

                PluginLog.Debug($"Parsed {(isOwnMessage ? "own" : $"other player's ({senderName})")} roll with value {rollValue}");

                if (rollValue == 1)
                {
                    var endText = isOwnMessage ? Config.LoseText : Config.WinText;
                    var endTextColor = isOwnMessage ? Config.LoseTextColor : Config.WinTextColor;

                    foreach (var part in messageFormatParts)
                    {
                        if (part == "{0}")
                        {
                            messageBuilder.Append(message);
                        }
                        else if (part == "{1}")
                        {
                            if (endTextColor != null)
                            {
                                messageBuilder.AddUiForeground((ushort)endTextColor);
                                messageBuilder.Append(endText);
                                messageBuilder.AddUiForegroundOff();
                            }
                            else
                            {
                                messageBuilder.Append(endText);
                            }
                        }
                        else
                        {
                            messageBuilder.Append(part);
                        }
                    }

                    message = messageBuilder.Build();
                } 
                else if (!isOwnMessage)
                {
                    foreach (var part in messageFormatParts)
                    {
                        if (part == "{0}")
                        {
                            messageBuilder.Append(message);
                        }
                        else if (part == "{1}")
                        {
                            var rollChatLinkHander = BuildChatRollLinkHandler(chatType, sender, rollType, rollValue);
                            var rollLinkPayload = ChatLinks.AddChatLinkHandler(rollChatLinkHander);

                            messageBuilder.Add(rollLinkPayload);
                            if (Config.RollTextColor != null)
                            {
                                messageBuilder.AddUiForeground((ushort)Config.RollTextColor);
                                messageBuilder.Append(Config.RollText);
                                messageBuilder.AddUiForegroundOff();
                            }
                            else
                            {
                                messageBuilder.Append(Config.RollText);
                            }
                            messageBuilder.Add(RawPayload.LinkTerminator);
                        }
                        else
                        {
                            messageBuilder.Append(part);
                        }
                    }

                    message = messageBuilder.Build();
                }
            }
        }
    }

    private bool TryParseRollMessage(XivChatType chatType, SeString sender, SeString message, out string senderName, out RollType rollType, out int rollValue)
    {
        if ((ushort)chatType > MAPPED_CHAT_TYPE_MAX_VALUE)
        {
            var match = RandomRollGeneratedRegex().Match(message.TextValue);
            if (match.Success)
            {
                // No sender for /random
                var senderValue = match.Groups[1].Value;

                senderName = senderValue == "You" ? ClientState.LocalPlayer!.Name.TextValue : senderValue;
                rollType = RollType.Random;
                rollValue = int.Parse(match.Groups[2].Value);
                return true;
            }
        }
        else
        {
            var match = DiceRollGeneratedRegex().Match(message.TextValue);
            if (match.Success)
            {
                senderName = GetSenderName(sender);
                rollType = RollType.Dice;
                rollValue = int.Parse(match.Groups[1].Value);
                return true;
            }
        }

        senderName = string.Empty;
        rollType = RollType.None;
        rollValue = -1;
        return false;
    }

    private Action<uint, SeString> BuildChatRollLinkHandler(XivChatType chatType, SeString sender, RollType rollType, int rollValue)
    {
        return (commandId, originalMessage) => {
            Task.Run(() =>
            {
                foreach (var message in GenerateChatRollMessages(chatType, sender, rollType, rollValue))
                {
                    ChatSender.Enqueue(message);
                    Thread.Sleep(Random.Next(MESSAGE_RANDOM_DELAY.Start.Value, MESSAGE_RANDOM_DELAY.End.Value));
                }
            });
        };
    }

    private static string[] GenerateChatRollMessages(XivChatType chatType, SeString sender, RollType rollType, int rollValue)
    {
        var rollMessage = BuildRollMessage(rollType, rollValue);
        if (SWITCH_COMMAND_BY_CHAT_TYPE.TryGetValue(chatType, out var switchChannelCommand))
        {
            return [switchChannelCommand, rollMessage];
        } 
        else
        {
            return [rollMessage];
        }
    }

    private static string BuildRollMessage(RollType rollType, int rollValue)
    {
        if (rollType == RollType.Random)
        {
            return $"/random {rollValue}";
        } 
        else if (rollType == RollType.Dice)
        {
            return $"/dice {rollValue}";
        } 
        else
        {
            throw new UnreachableException($"Unsupported roll type {rollType}");
        }
    }

    private static string GetSenderName(SeString sender)
    {
        foreach (var payload in sender.Payloads)
        {
            if (payload is PlayerPayload playerPayload)
            {
                return playerPayload.PlayerName;
            }
        }

        foreach (var payload in sender.Payloads.Reverse<Payload>())
        {
            if (payload is TextPayload textPayload)
            {
                return textPayload.Text!;
            }
        }

        return string.Empty;
    }
}
