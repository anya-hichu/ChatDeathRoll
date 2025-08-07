using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;

namespace ChatDeathRoll.Chat;

public class ChatLinks : IDisposable
{
    private Config Config { get; init; }
    private IChatGui ChatGui { get; init; }
    private IFramework Framework { get; init; }
    private uint MonotonicCounter { get; set; } = 0;
    private Queue<uint> CommandIds { get; init; } = [];

    public ChatLinks(Config config, IChatGui chatGui, IFramework framework)
    {
        Config = config;
        ChatGui = chatGui;
        Framework = framework;

        Framework.Update += OnFrameworkUpdate;
    }


    public void Dispose()
    {
        while (CommandIds.TryDequeue(out var commandId))
        {
            ChatGui.RemoveChatLinkHandler(commandId);
        }
        Framework.Update -= OnFrameworkUpdate;
    }

    public void OnFrameworkUpdate(IFramework framework)
    {
        MonotonicCounter++;
    }

    public DalamudLinkPayload AddChatLinkHandler(Action<uint, SeString> commandAction)
    {
        var commandId = MonotonicCounter;
        var payload = ChatGui.AddChatLinkHandler(commandId, commandAction);
        CommandIds.Enqueue(commandId);
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
