using System;
public static class BackTests
{
    static void Check(bool value, string name) { if (!value) throw new Exception("Back: " + name); }
    public static void Run()
    {
        var state = new SessionState();
        Check(!state.CanGoBack, "idle rejects back");
        state.Start(); Check(!state.CanGoBack, "briefing rejects back");
        state.ReportInteraction(state.SessionId, ExperienceStage.Briefing, VisitorAction.ExploreHelmet);
        state.ReportInteraction(state.SessionId, ExperienceStage.TheCase, VisitorAction.HelmetEquipped);
        state.CompleteTask(state.SessionId, ExperienceStage.ScaleUp);
        var oldId = state.SessionId; state.Pause();
        var restore = state.CaptureBackRestore(); state.Reset(); restore();
        Check(state.Stage == ExperienceStage.ScaleUp && state.Status == SessionStatus.Paused, "paused back targets ACT2");
        Check(state.Result(ExperienceStage.TheCase) == StageResult.Completed, "earlier success retained");
        Check(state.Result(ExperienceStage.DressTheDiver) == StageResult.Pending, "later task cleared");
        Check(state.SessionId != oldId, "new generation rejects old callbacks");
        state.Resume(); Check(!state.CompleteTask(oldId, ExperienceStage.ScaleUp), "old completion rejected");
        state.CompleteTask(state.SessionId, ExperienceStage.ScaleUp);
        state.CompleteTask(state.SessionId, ExperienceStage.DressTheDiver);
        state.CompleteTask(state.SessionId, ExperienceStage.Descent);
        state.ReportCleanupProgress(state.SessionId, 2, 9);
        restore = state.CaptureBackRestore(); state.Reset(); restore();
        Check(state.Stage == ExperienceStage.Descent && state.CleanupCollected == 0, "underwater back resets collection");
        Check(state.Result(ExperienceStage.DressTheDiver) == StageResult.Completed, "ACT3 result retained");
        while(state.CanGoBack) { restore = state.CaptureBackRestore(); state.Reset(); restore(); }
        Check(state.Stage == ExperienceStage.Briefing, "repeated back terminates at briefing");
        Console.WriteLine("Back tests passed.");
    }
}
