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
        // Exercise the same headset and staff entry points used by the runtime.
        var venue = new SessionState();
        var controls = new CommandDispatcher(venue, "venue", action => {
            switch (action) {
                case "Start": return venue.Start();
                case "Pause": return venue.Pause();
                case "Resume": return venue.Resume();
                case "Skip": return venue.Skip();
                case "Reset": return venue.Reset();
                default: return false;
            }
        });
        Func<string, ControlCommand> make = action => new ControlCommand {
            commandId = Guid.NewGuid().ToString(), runtimeId = "venue", action = action,
            sessionId = venue.SessionId.ToString(), expectedRevision = venue.Revision
        };
        check(controls.Mode == SessionControlMode.Exhibition, "Exhibition is the startup default");
        check(controls.DispatchVisitor(make("Start")).code == "StaffOnly" && venue.Status == SessionStatus.Idle, "Headset waits for staff");
        check(controls.Dispatch(make("Start")).accepted, "Staff start in exhibition");
        check(!controls.TrySetMode(SessionControlMode.TeamTest), "Mode locked during visit");
        check(controls.DispatchVisitor(make("Pause")).accepted && venue.Status == SessionStatus.Paused, "Comfort pause");
        check(controls.DispatchVisitor(make("Skip")).code == "StaffOnly" && venue.Stage == ExperienceStage.Briefing, "Pausing never grants skip");
        check(!controls.TrySetMode(SessionControlMode.TeamTest), "Mode locked while paused");
        check(controls.DispatchVisitor(make("Resume")).accepted, "Visitor resumes");
        check(controls.Dispatch(make("Skip")).accepted && venue.Result(ExperienceStage.Briefing) == StageResult.Skipped, "Staff skip in exhibition");
        controls.Dispatch(make("Reset"));
        check(controls.TrySetMode(SessionControlMode.TeamTest), "Staff choose team test while waiting");
        var queuedStart = make("Start");
        controls.TrySetMode(SessionControlMode.Exhibition);
        check(controls.DispatchVisitor(queuedStart).code == "StaffOnly", "Pending headset shortcut cannot start an exhibition");
        controls.TrySetMode(SessionControlMode.TeamTest);
        check(controls.DispatchVisitor(make("Start")).accepted, "Team test headset start");
        check(controls.DispatchVisitor(make("Skip")).accepted && venue.Result(ExperienceStage.Briefing) == StageResult.Skipped, "Team test headset skip");
        check(controls.DispatchVisitor(make("Reset")).code == "StaffOnly", "Test shortcuts do not expose staff reset");
        controls.Dispatch(make("Reset"));
        check(controls.Mode == SessionControlMode.TeamTest, "Consecutive test visitors keep venue mode");
        // Cache eviction still cannot replay an applied mutation because its revision is old.
        for (int i = 0; i < 520; i++) dispatcher.Dispatch(command("Unknown"));
        check(dispatcher.Dispatch(skip).code == "StaleState", "Evicted mutation cannot execute again");
        // Re-create a handover without reusing a visitor's authority or accepted receipt.
        dispatcher.Dispatch(command("Reset"));
        var idleStart = command("Start");
        var delayedIdleStart = command("Start");
        bool replay;
        var startedOnWeb = dispatcher.DispatchWeb(idleStart, out replay);
        check(startedOnWeb.accepted && !replay, "Mobile Start creates a visitor");
        check(dispatcher.DispatchWeb(idleStart, out replay) == startedOnWeb && replay, "Mobile Start retry identifies the same visitor");
        var mobileSkip = command("Skip");
        var delayedReset = command("Reset");
        int beforeSkip = executions;
        check(dispatcher.DispatchWeb(mobileSkip, out replay).accepted, "Mobile Skip accepted");
        check(dispatcher.DispatchWeb(mobileSkip, out replay).accepted && replay && executions == beforeSkip + 1,
            "One mobile request skips exactly one stage");
        var changedRequest = command("Reset");
        changedRequest.commandId = mobileSkip.commandId;
        check(dispatcher.DispatchWeb(changedRequest, out replay).code == "CommandIdConflict", "An ID cannot be reused for a different action");
        check(dispatcher.DispatchWeb(command("Back"), out replay).code == "UnknownAction", "Back remains a desktop command");
        var mobileReset = command("Reset");
        check(dispatcher.DispatchWeb(mobileReset, out replay).accepted, "Mobile Reset accepted");
        check(dispatcher.DispatchWeb(mobileReset, out replay).accepted && replay, "Reset receipt can be retrieved in the same waiting period");
        check(dispatcher.DispatchWeb(idleStart, out replay).code == "StaleState", "Previous Start receipt cannot adopt another waiting period");
        check(dispatcher.DispatchWeb(delayedIdleStart, out replay).code == "StaleState", "Unsent Start from previous waiting period rejected");
        check(dispatcher.DispatchWeb(command("Start"), out replay).accepted, "Staff starts the next visitor from fresh state");
        var nextVisitor = state.SessionId;
        int nextRevision = state.Revision;
        foreach (var stale in new[] { mobileSkip, mobileReset, delayedReset, idleStart })
            check(dispatcher.DispatchWeb(stale, out replay).code == "StaleState", "Departed visitor cannot affect the next one");
        check(state.Revision == nextRevision && state.SessionId == nextVisitor, "Old tab left new visitor untouched");
        var missingRevision = command("Pause");
        missingRevision.expectedRevision = -1;
        check(dispatcher.DispatchWeb(missingRevision, out replay).code == "InvalidCommand", "Explicit state revision required");
        Console.WriteLine("PASS: " + count + " Experience command assertions");
    }
}
