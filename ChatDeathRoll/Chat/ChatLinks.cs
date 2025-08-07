using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;

namespace ChatDeathRoll.Chat;

public class ChatLinks(Config config, IChatGui chatGui) : IDisposable
{
    private Config Config { get; init; } = config;
    private IChatGui ChatGui { get; init; } = chatGui;
    private Queue<Guid> CommandIds { get; init; } = [];

    public void Dispose()
    {
        while (CommandIds.TryDequeue(out var commandId))
        {
            ChatGui.RemoveChatLinkHandler(commandId);
        }
    }

    public DalamudLinkPayload AddChatLinkHandler(Action<Guid, SeString> commandAction)
    {
        var payload = ChatGui.AddChatLinkHandler(commandAction);
        CommandIds.Enqueue(payload.CommandId);
        EnforceMaxActiveLinks();
        return payload;
    }

    private void EnforceMaxActiveLinks()
    {
        while (CommandIds.Count > Config.MaxActiveLinks)
        {
            ChatGui.RemoveChatLinkHandler(CommandIds.Dequeue());
        }
    }
}
