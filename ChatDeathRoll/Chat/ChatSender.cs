using Dalamud.Plugin.Services;
using Quack.Utils;
using System;
using System.Collections.Generic;

namespace ChatDeathRoll.Chat;

public class ChatSender : IDisposable
{
    private IFramework Framework { get; init; }
    private ChatServer ChatServer { get; init; }

    private Queue<string> PendingMessages { get; init; } = [];

    public ChatSender(IFramework framework, ChatServer chatServer)
    {
        Framework = framework;
        ChatServer = chatServer;

        Framework.Update += OnFrameworkUpdate;
    }

    public void Dispose()
    {
        Framework.Update -= OnFrameworkUpdate;
    }

    public void OnFrameworkUpdate(IFramework framework)
    {
        while (PendingMessages.TryDequeue(out var message))
        {
            ChatServer.SendMessage(message);
        }
    }

    public void Enqueue(string message)
    {
        PendingMessages.Enqueue(message);
    }
}
