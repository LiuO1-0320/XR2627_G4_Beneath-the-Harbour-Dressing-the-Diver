using System;
using System.Collections.Generic;

public enum SessionControlMode { Exhibition, TeamTest }

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
    sealed class CachedCommand
    {
        public string sessionId, action;
        public int expectedRevision;
        public CommandReply reply;
    }

    readonly Dictionary<string, CachedCommand> replies = new Dictionary<string, CachedCommand>();
    readonly Queue<string> order = new Queue<string>();
    public CommandDispatcher(SessionState state, string runtimeId, Func<string, bool> execute)
    { this.state = state; this.runtimeId = runtimeId; this.execute = execute; }

    public SessionControlMode Mode { get; private set; } = SessionControlMode.Exhibition;
    public bool TrySetMode(SessionControlMode mode)
    {
        if (state.Status != SessionStatus.Idle || (mode != SessionControlMode.Exhibition && mode != SessionControlMode.TeamTest))
            return false;
        Mode = mode;
        return true;
    }

    // Headset controls have their own entry point. Staff and paired staff clients use Dispatch.
    public CommandReply DispatchVisitor(ControlCommand command)
    {
        if (command == null) return Dispatch(null);
        bool comfortAction = command.action == "Pause" || command.action == "Resume";
        bool testShortcut = Mode == SessionControlMode.TeamTest && (command.action == "Start" || command.action == "Skip");
        if (!comfortAction && !testShortcut)
            return Reply(command, false, "StaffOnly", "This action is unavailable on the visitor headset.");
        return Dispatch(command);
    }

    public static bool IsWebAction(string action)
    {
        return action == "Start" || action == "Pause" || action == "Resume" || action == "Skip" || action == "Reset";
    }

    public CommandReply DispatchWeb(ControlCommand command, out bool replay)
    {
        replay = false;
        if (command == null || !Guid.TryParseExact(command.commandId, "D", out _) ||
            !Guid.TryParseExact(command.sessionId, "D", out _) || command.expectedRevision < 0)
            return Reply(command, false, "InvalidCommand", "A request ID, visit ID and current revision are required.");
        if (command.protocolVersion != 1) return Reply(command, false, "UnsupportedVersion", "Expected protocol 1.");
        if (command.runtimeId != runtimeId) return Reply(command, false, "WrongRuntime", "Open this run's staff link.");
        if (!IsWebAction(command.action)) return Reply(command, false, "UnknownAction", "Action unavailable on the web.");
        if (replies.TryGetValue(command.commandId, out var cached))
        {
            if (cached.action != command.action || cached.sessionId != command.sessionId ||
                cached.expectedRevision != command.expectedRevision)
                return Reply(command, false, "CommandIdConflict", "This request ID was already used for a different request.");
            // Start and Reset change the visit ID. Only replay while their resulting visit
            // (or the exact post-reset waiting state) is still the one on the headset.
            if (cached.reply.sessionId != state.SessionId.ToString() ||
                (state.SessionId == Guid.Empty && cached.reply.revision != state.Revision))
                return Reply(command, false, "StaleState", "The visit has changed.");
            replay = true;
            return cached.reply;
        }
        return Dispatch(command);
    }

    public CommandReply Dispatch(ControlCommand command)
    {
        if (command == null || string.IsNullOrEmpty(command.commandId) || command.commandId.Length > 128)
            return Reply(command, false, "InvalidCommand", "A command ID of 1–128 characters is required.");
        if (command.protocolVersion != 1) return Reply(command, false, "UnsupportedVersion", "Expected protocol 1.");
        if (command.runtimeId != runtimeId) return Reply(command, false, "WrongRuntime", "Fetch a fresh snapshot after application restart.");
        if (replies.TryGetValue(command.commandId, out var cached)) return cached.reply;
        CommandReply result;
        if (command.sessionId != state.SessionId.ToString() || command.expectedRevision != state.Revision)
            result = Reply(command, false, "StaleState", "Fetch a fresh snapshot; do not automatically retry with a new revision.");
        else if (command.action != "Start" && command.action != "Pause" && command.action != "Resume" && command.action != "Skip" && command.action != "Reset" && command.action != "Back")
            result = Reply(command, false, "UnknownAction", "Supported: Start, Pause, Resume, Back, Skip, Reset.");
        else
        {
            try
            {
                bool accepted = execute(command.action);
                result = Reply(command, accepted, accepted ? "Applied" : "InvalidState", accepted ? "Applied on Unity main thread." : "Action unavailable in the current state.");
            }
            catch (Exception error) { result = Reply(command, false, "ExecutionFailed", error.Message); }
        }
        replies.Add(command.commandId, new CachedCommand { sessionId = command.sessionId, action = command.action,
            expectedRevision = command.expectedRevision, reply = result });
        order.Enqueue(command.commandId);
        if (order.Count > 512) replies.Remove(order.Dequeue());
        return result;
    }
    CommandReply Reply(ControlCommand command, bool accepted, string code, string message)
    {
        return new CommandReply { commandId = command?.commandId, accepted = accepted, code = code,
            message = message, runtimeId = runtimeId, sessionId = state.SessionId.ToString(), revision = state.Revision };
    }
}
