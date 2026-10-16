using System;
using System.Collections.Generic;

[Serializable]
public sealed class ControlCommand
{
    public int protocolVersion = 1;
    public string commandId, runtimeId, sessionId, action;
    public int expectedRevision = -1;
}

[Serializable]
public sealed class CommandReply
{
    public string commandId, code, message, runtimeId, sessionId;
    public bool accepted;
    public int revision;
}

/// <summary>Main-thread command validation. Transport authentication belongs to the adapter.</summary>
public sealed class CommandDispatcher
{
    readonly SessionState state;
    readonly string runtimeId;
    readonly Func<string, bool> execute;
    readonly Dictionary<string, CommandReply> replies = new Dictionary<string, CommandReply>();
    readonly Queue<string> order = new Queue<string>();
    public CommandDispatcher(SessionState state, string runtimeId, Func<string, bool> execute)
    { this.state = state; this.runtimeId = runtimeId; this.execute = execute; }

    public CommandReply Dispatch(ControlCommand command)
    {
        if (command == null || string.IsNullOrEmpty(command.commandId) || command.commandId.Length > 128)
            return Reply(command, false, "InvalidCommand", "A command ID of 1–128 characters is required.");
        if (command.protocolVersion != 1) return Reply(command, false, "UnsupportedVersion", "Expected protocol 1.");
        if (command.runtimeId != runtimeId) return Reply(command, false, "WrongRuntime", "Fetch a fresh snapshot after application restart.");
        if (replies.TryGetValue(command.commandId, out var cached)) return cached;
        CommandReply result;
        if (command.sessionId != state.SessionId.ToString() || command.expectedRevision != state.Revision)
            result = Reply(command, false, "StaleState", "Fetch a fresh snapshot; do not automatically retry with a new revision.");
        else if (command.action != "Start" && command.action != "Pause" && command.action != "Resume" && command.action != "Skip" && command.action != "Reset")
            result = Reply(command, false, "UnknownAction", "Supported: Start, Pause, Resume, Skip, Reset.");
        else
        {
            try
            {
                bool accepted = execute(command.action);
                result = Reply(command, accepted, accepted ? "Applied" : "InvalidState", accepted ? "Applied on Unity main thread." : "Action unavailable in the current state.");
            }
            catch (Exception error) { result = Reply(command, false, "ExecutionFailed", error.Message); }
        }
        replies.Add(command.commandId, result); order.Enqueue(command.commandId);
        if (order.Count > 512) replies.Remove(order.Dequeue());
        return result;
    }
    CommandReply Reply(ControlCommand command, bool accepted, string code, string message)
    {
        return new CommandReply { commandId = command?.commandId, accepted = accepted, code = code,
            message = message, runtimeId = runtimeId, sessionId = state.SessionId.ToString(), revision = state.Revision };
    }
}
