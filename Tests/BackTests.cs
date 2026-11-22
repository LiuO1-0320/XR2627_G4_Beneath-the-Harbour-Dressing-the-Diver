using System;
public static class BackTests
{
    static void Check(bool value, string name) { if (!value) throw new Exception("Back: " + name); }
    static void Back(SessionState state)
    {
        Check(state.GoBack(target => { }), "Back accepted");
    }
    public static void Run()
    {
        var state = new SessionState();
        Check(!state.CanGoBack, "Idle rejects Back");
        state.Start();
        var visitor = state.SessionId;
        Check(!state.CanGoBack, "Briefing rejects Back");
        state.Skip(); state.Skip(); // Briefing, ACT1 and its Inside record are skipped.
        var firstAttempt = state.ActivityId;
        state.CompleteTask(firstAttempt, ExperienceStage.ScaleUp);
        state.Pause();
        int changes = 0;
        state.Changed += () => {
            Check(state.SessionId == visitor && state.Status != SessionStatus.Idle, "no visitor handover during Back");
            changes++;
        };
        int revision = state.Revision;
        Check(state.GoBack(target => {
            Check(target == ExperienceStage.ScaleUp && state.SessionId == visitor, "correct restart target");
            Check(!state.Skip() && !state.GoBack(s => {}), "reject reentrant control during cancellation");
        }), "paused Back accepted");
        Check(changes == 1 && state.Revision == revision + 1, "one revision for Back");
        Check(state.Stage == ExperienceStage.ScaleUp && state.Status == SessionStatus.Paused, "stay paused");
        Check(state.Result(ExperienceStage.Briefing) == StageResult.Skipped &&
              state.Result(ExperienceStage.TheCase) == StageResult.Skipped &&
              state.Result(ExperienceStage.Inside) == StageResult.Skipped, "preserve skipped stages");
        Check(state.Result(ExperienceStage.ScaleUp) == StageResult.Active &&
              state.Result(ExperienceStage.DressTheDiver) == StageResult.Pending, "restart only from target onwards");
        state.Resume();
        Check(!state.CompleteTask(firstAttempt, ExperienceStage.ScaleUp), "reject late task callback after Back");
        Check(state.CompleteTask(state.ActivityId, ExperienceStage.ScaleUp), "new task attempt can finish");
        state.CompleteTask(state.ActivityId, ExperienceStage.DressTheDiver);
        state.CompleteTask(state.ActivityId, ExperienceStage.Descent);
        state.ReportCleanupProgress(state.ActivityId, 2, 9);
        Back(state);
        Check(state.Stage == ExperienceStage.Descent && state.CleanupCollected == 0, "reset later cleanup progress");
        Check(state.Result(ExperienceStage.DressTheDiver) == StageResult.Completed, "keep earlier completion");
        state.CompleteTask(state.ActivityId, ExperienceStage.Descent);
        state.ReportCleanupProgress(state.ActivityId, 9, 9);
        Check(!state.ReportPlatformReturn(firstAttempt), "stale rope arrival rejected");
        Check(state.ReportPlatformReturn(state.ActivityId), "actual return accepted");
        state.ChooseFreeExploration(visitor);
        Back(state);
        Check(state.Stage == ExperienceStage.ExperienceComplete && state.HasReturnedAfterCleanup &&
              state.CleanupCompleted && state.CleanupCollected == 9 && state.CleanupTotal == 9, "keep evidence when returning to ending");
        Back(state);
        Check(state.Stage == ExperienceStage.SeabedCleanup && !state.CleanupResolved &&
              !state.HasReturnedAfterCleanup && state.CleanupCollected == 0, "reopen cleanup when it is the target");
        try { state.GoBack(s => { throw new InvalidOperationException("missing destination"); }); }
        catch (InvalidOperationException) { }
        Check(state.Status == SessionStatus.Paused && state.Stage == ExperienceStage.SeabedCleanup, "failed recovery preserves visit and progress");
        while (state.CanGoBack) Back(state);
        Check(state.Stage == ExperienceStage.Briefing && state.Result(ExperienceStage.Inside) == StageResult.Pending, "rewind to briefing");

        var handover = new SessionState();
        handover.Start(); var oldVisitor = handover.SessionId;
        handover.Skip(); Back(handover); handover.Reset(); handover.Start();
        Check(handover.SessionId != oldVisitor, "real Reset and Start begin another visit");
        Console.WriteLine("Back tests passed.");
    }
}
