using System;

public static class CommandTests
{
    public static void Run()
    {
        int count = 0;
        Action<bool, string> check = (ok, label) => { count++; if (!ok) throw new Exception(label); };
        var state = new SessionState();
        int executions = 0;
        var dispatcher = new CommandDispatcher(state, "runtime", action =>
        {
            executions++;
            switch (action)
            {
                case "Start": return state.Start();
                case "Skip": return state.Skip();
                case "Pause": return state.Pause();
                case "Resume": return state.Resume();
                case "Reset": return state.Reset();
                default: return false;
            }
        });
        Func<string, ControlCommand> command = action => new ControlCommand { commandId = Guid.NewGuid().ToString(),
            runtimeId = "runtime", sessionId = state.SessionId.ToString(), expectedRevision = state.Revision, action = action };
        check(dispatcher.Dispatch(null).code == "InvalidCommand", "Null command");
        var start = command("Start");
        check(dispatcher.Dispatch(start).accepted, "Start accepted");
        check(dispatcher.Dispatch(start).accepted && executions == 1, "Duplicate Start returns original receipt");
        var skip = command("Skip");
        check(dispatcher.Dispatch(skip).accepted && state.Stage == ExperienceStage.TheCase, "Skip once");
        check(dispatcher.Dispatch(skip).accepted && state.Stage == ExperienceStage.TheCase && executions == 2, "Repeated Skip cannot advance twice");
        var old = command("Skip");
        check(dispatcher.Dispatch(command("Pause")).accepted, "Pause accepted");
        check(dispatcher.Dispatch(old).code == "StaleState", "Optimistic revision rejects concurrent staff command");
        check(dispatcher.Dispatch(command("Pause")).code == "InvalidState", "Cannot pause twice");
        check(dispatcher.Dispatch(command("Resume")).accepted, "Resume accepted");
        var previousSession = command("Skip");
        check(dispatcher.Dispatch(command("Reset")).accepted, "Reset accepted");
        check(dispatcher.Dispatch(command("Start")).accepted, "Second session starts");
        previousSession.expectedRevision = state.Revision;
        check(dispatcher.Dispatch(previousSession).code == "StaleState", "Old session rejected even at current revision");
        var wrongRuntime = command("Skip"); wrongRuntime.runtimeId = "previous-process";
        check(dispatcher.Dispatch(wrongRuntime).code == "WrongRuntime", "Old process rejected");
        var wrongVersion = command("Skip"); wrongVersion.protocolVersion = 2;
        check(dispatcher.Dispatch(wrongVersion).code == "UnsupportedVersion", "Version negotiation");
        check(dispatcher.Dispatch(command("CompleteTask")).code == "UnknownAction", "Staff cannot fabricate task success");
        var failure = new CommandDispatcher(state, "runtime", action => { throw new Exception("reset failure"); });
        var reset = command("Reset");
        check(failure.Dispatch(reset).code == "ExecutionFailed", "Execution error has receipt");
        check(failure.Dispatch(reset).code == "ExecutionFailed", "Failed command is deduplicated too");
        // Cache eviction still cannot replay an applied mutation because its revision is old.
        for (int i = 0; i < 520; i++) dispatcher.Dispatch(command("Unknown"));
        check(dispatcher.Dispatch(skip).code == "StaleState", "Evicted mutation cannot execute again");
        Console.WriteLine("PASS: " + count + " Experience command assertions");
    }
}
