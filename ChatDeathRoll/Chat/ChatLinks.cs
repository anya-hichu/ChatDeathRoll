using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;

namespace ChatDeathRoll.Chat;

public class ChatLinks : IDisposable
{
    private Config Config { get; init; }
    private IDalamudPluginInterface PluginInterface { get; init; }
    private IFramework Framework { get; init; }
    private uint MonotonicCounter { get; set; } = 0;
    private Queue<uint> CommandIds { get; init; } = [];

    public ChatLinks(Config config, IDalamudPluginInterface pluginInterface, IFramework framework)
    {
        Config = config;
        PluginInterface = pluginInterface;
        Framework = framework;

        Framework.Update += OnFrameworkUpdate;
    }


    public void Dispose()
    {
        while (CommandIds.TryDequeue(out var commandId))
        {
            PluginInterface.RemoveChatLinkHandler(commandId);
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
        var payload = PluginInterface.AddChatLinkHandler(commandId, commandAction);
        CommandIds.Enqueue(commandId);
        EnforceMaxActiveLinks();
        return payload;
    }

    private void EnforceMaxActiveLinks()
    {
        while (CommandIds.Count > Config.MaxActiveLinks)
        {
            PluginInterface.RemoveChatLinkHandler(CommandIds.Dequeue());
        }
    }
}
