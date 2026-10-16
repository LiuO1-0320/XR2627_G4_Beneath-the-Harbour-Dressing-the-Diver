using System;

public static class SessionStateTests
{
    static int assertions;
    static void Check(bool condition, string name)
    { assertions++; if (!condition) throw new Exception(name); }
    public static void Main()
    {
        Check(DiveShaftGeometry.Contains(0, 0), "Shaft center accepted");
        Check(DiveShaftGeometry.Contains(.65f, .45f), "Safe shaft boundary accepted");
        Check(!DiveShaftGeometry.Contains(.66f, 0), "Outside shaft side rejected");
        Check(!DiveShaftGeometry.Contains(0, .46f), "Outside shaft front rejected");
        Check(!DiveShaftGeometry.Contains(-1.5f, 0), "Land dock cannot initiate vertical travel");
        Check(!DiveShaftGeometry.Contains(float.NaN, 0), "Invalid position rejected");
        Check(DiveShaftGeometry.Contains(DiveShaftGeometry.AxisX, 0), "Travel axis inside safe shaft");
        Check(DiveShaftGeometry.AxisX + .35f < 1f, "Travel axis leaves body clearance from outer rim");
        Check(DiveShaftGeometry.CanInteract(-1.5f, 0), "Panel usable from supported land dock");
        Check(DiveShaftGeometry.CanInteract(2.2f, 1.6f), "Expanded interaction boundary accepted");
        Check(!DiveShaftGeometry.CanInteract(2.21f, 0), "Distant interaction rejected");
        Check(!DiveShaftGeometry.CanInteract(0, 1.61f), "Outside interaction rectangle rejected");
        Check(!DiveShaftGeometry.Contains(1.5f, 0) && DiveShaftGeometry.CanInteract(1.5f, 0), "Expanded activation never widens the safe travel shaft");
        Check(!DiveShaftGeometry.CanInteract(float.NaN, 0), "Invalid interaction position rejected");
        try { ResetTests.Run(); } catch (Exception error) { Console.WriteLine("RESET TEST FAILURE: " + error.Message); Environment.Exit(1); }
        GazeTests.Run();
        CommandTests.Run();
        AudioArrivalTests.Run();
        BriefingPracticeTests.Run();
        Check(SessionState.PreserveDiverEscort(ExperienceStage.DressTheDiver, ExperienceStage.Descent, StageResult.Completed), "Successful dress preserves NPC escort into ACT4");
        Check(SessionState.PreserveDiverEscort(ExperienceStage.Descent, ExperienceStage.SeabedCleanup, StageResult.Completed), "Successful descent preserves NPC arrival feedback");
        Check(!SessionState.PreserveDiverEscort(ExperienceStage.DressTheDiver, ExperienceStage.Descent, StageResult.Skipped), "Skipping dressing cleans up NPC instead of faking escort");
        Check(!SessionState.PreserveDiverEscort(ExperienceStage.Descent, ExperienceStage.SeabedCleanup, StageResult.Skipped), "Skipping descent cleans up escort");
        Check(!SessionState.PreserveDiverEscort(ExperienceStage.DressTheDiver, ExperienceStage.Briefing, StageResult.Completed), "Reset never preserves NPC escort");
        Check(!SessionState.PreserveDiverEscort(ExperienceStage.TheCase, ExperienceStage.ScaleUp, StageResult.Completed), "ACT1 cannot activate NPC escort");
        var state = new SessionState();
        Check(state.Status == SessionStatus.Idle && !state.ReportInteraction(state.SessionId, state.Stage, VisitorAction.ExploreHelmet), "Idle cannot advance");
        Check(!state.Pause() && !state.Skip(), "Idle guards");
        Guid first = Guid.Empty;
        for (int round = 0; round < 3; round++)
        {
            int events = 0;
            Action listener = () => events++;
            state.Changed += listener;
            Check(state.Start() && state.Stage == ExperienceStage.Briefing, "Start at briefing");
            Check(!state.Start() && events == 1, "Start idempotent");
            var id = state.SessionId;
            Check(id != Guid.Empty && id != first, "Independent session ID");
            Check(!state.ReportInteraction(id, ExperienceStage.Briefing, VisitorAction.InsideExplored), "Reject action unrelated to stage");
            Check(!state.ReportInteraction(Guid.NewGuid(), ExperienceStage.Briefing, VisitorAction.ExploreHelmet), "Reject foreign click");
            Check(!state.ReportInteraction(id, ExperienceStage.Inside, VisitorAction.InsideExplored), "Reject future stage click");
            Check(state.ReportInteraction(id, ExperienceStage.Briefing, VisitorAction.ExploreHelmet), "Enter helmet fitting");
            Check(!state.ReportInteraction(id, ExperienceStage.TheCase, VisitorAction.HelmetExamined), "Regression: releasing an examined helmet is not successful fitting");
            Check(state.Stage == ExperienceStage.TheCase && state.Result(ExperienceStage.TheCase) == StageResult.Active, "Failed fitting must remain active");
            Check(!state.CompleteTask(id, ExperienceStage.TheCase), "Generic task event cannot bypass fitting");
            Check(!state.ReportInteraction(first, ExperienceStage.TheCase, VisitorAction.HelmetEquipped), "Reject prior session fitting");
            state.Pause();
            Check(!state.ReportInteraction(id, ExperienceStage.TheCase, VisitorAction.HelmetEquipped), "Fitting cannot advance during pause");
            state.Resume();
            Check(state.ReportInteraction(id, ExperienceStage.TheCase, VisitorAction.HelmetEquipped) && state.Stage == ExperienceStage.ScaleUp, "Only real fitting success advances ACT1");
            Check(!state.ReportInteraction(id, ExperienceStage.TheCase, VisitorAction.HelmetEquipped), "Fitting success consumed only once");
            Check(!state.ReportInteraction(state.SessionId, state.Stage, VisitorAction.ExploreHelmet), "Inspection requires real event");
            Check(!state.CompleteTask(Guid.NewGuid(), state.Stage), "Reject foreign event");
            Check(!state.CompleteTask(id, ExperienceStage.DressTheDiver), "Reject wrong stage");
            Check(!state.ReportInteraction(id, ExperienceStage.ScaleUp, VisitorAction.HelmetExamined), "Inspection cannot use helmet handling alone");
            Check(state.CompleteTask(id, ExperienceStage.ScaleUp), "Inspection completes");
            Check(!state.ReportInteraction(id, ExperienceStage.TheCase, VisitorAction.HelmetExamined), "Ignore repeated prior interaction");
            Check(!state.CompleteTask(id, ExperienceStage.ScaleUp), "Duplicate event ignored");
            Check(state.Stage == ExperienceStage.DressTheDiver, "ACT2 success routes directly to ACT3");
            Check(!state.ReportInteraction(id, ExperienceStage.Inside, VisitorAction.InsideExplored), "Legacy Inside event cannot advance ACT3");
            Check(state.Result(ExperienceStage.Inside) == StageResult.Completed, "Inside shares actual ACT1 fitting result");
            Check(!state.ReportInteraction(state.SessionId, state.Stage, VisitorAction.ExploreHelmet), "Assembly cannot use ordinary clicks");
            Check(state.Pause(), "Pause");
            int count = events;
            Check(!state.Pause() && !state.ReportInteraction(state.SessionId, state.Stage, VisitorAction.ExploreHelmet) && !state.CompleteTask(id, state.Stage) && events == count, "Pause guards and no duplicate events");
            Check(state.Resume() && !state.Resume(), "Resume once");
            if (round == 1) Check(!state.CompleteTask(first, ExperienceStage.DressTheDiver), "Reject stale previous visitor event");
            Check(state.CompleteTask(id, ExperienceStage.DressTheDiver), "Assembly event completes");
            Check(state.Result(ExperienceStage.DressTheDiver) == StageResult.Completed, "Assembly result");
            Check(state.CompleteTask(id, ExperienceStage.Descent), "Descent event completes");
            Check(state.Stage == ExperienceStage.SeabedCleanup && state.Status == SessionStatus.Running, "Descent opens real cleanup task");
            Check(!state.ReportInteraction(id, ExperienceStage.SeabedCleanup, VisitorAction.ReflectOnViewports), "Obsolete reflection click rejected");
            Check(!state.CompleteTask(id, ExperienceStage.SeabedCleanup), "Generic completion cannot bypass collection progress");
            Check(!state.ReportPlatformReturn(id), "Early ascent cannot complete cleanup");
            Check(!state.ReportCleanupProgress(Guid.NewGuid(), 9, 9), "Old cleanup event rejected");
            Check(!state.ReportCleanupProgress(id, 10, 9), "Invalid count rejected");
            Check(state.ReportCleanupProgress(id, 0, 9), "Activity supplies actual total");
            Check(state.ReportCleanupProgress(id, 8, 9), "Partial cleanup tracked");
            Check(!state.ReportPlatformReturn(id), "Eight of nine is not complete");
            Check(!state.ReportCleanupProgress(id, 8, 9), "Duplicate progress ignored");
            state.Pause();
            Check(!state.ReportCleanupProgress(id, 9, 9), "Paused collection ignored");
            state.Resume();
            Check(state.ReportCleanupProgress(id, 9, 9) && state.CleanupCompleted, "All real litter completes cleanup");
            Check(state.Result(ExperienceStage.SeabedCleanup) == StageResult.Completed && state.Stage == ExperienceStage.SeabedCleanup, "Completed cleanup waits for actual landing");
            Check(!state.Skip() && !state.ChooseFreeExploration(id), "Completed cleanup still cannot skip landing");
            Check(!state.ReportCleanupProgress(id, 9, 9), "Completion emitted only once");
            Check(!state.ReportPlatformReturn(Guid.NewGuid()), "Old or foreign ascent rejected");
            state.Pause();
            Check(!state.ReportPlatformReturn(id), "Paused ascent cannot show ending");
            state.Resume();
            Check(state.ReportPlatformReturn(id) && state.Stage == ExperienceStage.ExperienceComplete, "First actual landing after cleanup opens final choice");
            Check(!state.ReportPlatformReturn(id), "Repeated surfacing cannot reopen ending");
            if (round == 1)
            {
                Check(state.ChooseFreeExploration(id) && state.Stage == ExperienceStage.FreeExploration, "Visitor can choose free exploration");
                Check(!state.ChooseFreeExploration(id), "Free exploration choice is idempotent");
                Check(!state.ReportPlatformReturn(id), "Another ascent in free mode never reopens ending");
                Check(!state.CompleteTask(id, ExperienceStage.Descent), "Free mode cannot rewrite guided results");
                state.Pause(); Check(!state.ChooseFreeExploration(id), "Pause cannot restart exploration"); state.Resume();
            }
            Check(!state.ExitExperience(Guid.NewGuid()), "Old visitor cannot exit new session");
            Check(state.ExitExperience(id) && state.Status == SessionStatus.Completed, "Visitor exit waits for staff");
            Check(!state.ExitExperience(id), "Duplicate exit does not publish again");
            Check(!state.Start() && !state.Skip() && !state.Resume(), "Completed waits for staff");
            first = id;
            Check(state.Reset() && state.Status == SessionStatus.Idle, "Reset");
            Check(state.CleanupCollected == 0 && state.CleanupTotal == 0 && !state.CleanupCompleted, "Reset cleanup progress");
            Check(!state.HasReturnedAfterCleanup, "Reset first surfacing latch");
            foreach (ExperienceStage stage in Enum.GetValues(typeof(ExperienceStage))) Check(state.Result(stage) == StageResult.Pending, "Reset every stage");
            state.Changed -= listener;
        }
        state.Start();
        for (int i = 0; i < 6; i++) state.Skip();
        Check(!state.ChooseFreeExploration(state.SessionId), "Skipped cleanup cannot open exploration underwater");
        Check(state.ReportPlatformReturn(state.SessionId), "Actual landing after skip opens ending");
        Check(state.ChooseFreeExploration(state.SessionId), "Staff recovery can open free exploration after landing");
        state.Pause();
        Check(state.ExitExperience(state.SessionId) && state.Status == SessionStatus.Completed, "Visitor can exit free exploration even while staff paused");
        state.Reset(); state.Start(); state.Pause();
        Check(state.Skip() && state.Status == SessionStatus.Paused, "Skip preserves pause");
        Check(state.Result(ExperienceStage.Briefing) == StageResult.Skipped, "Skipped is not completed");
        state.Reset(); state.Start();
        var pausedId = state.SessionId;
        state.Pause();
        Check(!state.ReportInteraction(pausedId, ExperienceStage.Briefing, VisitorAction.ExploreHelmet), "Ignore clicks during pause");
        state.Reset(); state.Start();
        Check(!state.ReportInteraction(pausedId, ExperienceStage.Briefing, VisitorAction.ExploreHelmet), "Ignore previous visitor click");
        state.Reset(); state.Start();
        state.Skip(); // Staff skips briefing.
        Check(state.Stage == ExperienceStage.TheCase, "Skip briefing reaches ACT1");
        state.Skip();
        Check(state.Stage == ExperienceStage.ScaleUp, "Skip ACT1 reaches ACT2");
        Check(state.Result(ExperienceStage.Inside) == StageResult.Skipped, "Skipping ACT1 does not invent Inside success");
        state.Pause();
        Check(state.Skip() && state.Stage == ExperienceStage.DressTheDiver, "Regression: skipping ACT2 reaches ACT3, never ACT1/Inside");
        Check(state.Status == SessionStatus.Paused, "ACT2 skip retains pause");
        Check(state.Result(ExperienceStage.ScaleUp) == StageResult.Skipped, "ACT2 skip remains distinct from success");
        state.Resume();
        Check(state.Skip() && state.Stage == ExperienceStage.Descent, "Skip ACT3 reaches ACT4");
        Check(state.Skip() && state.Stage == ExperienceStage.SeabedCleanup, "Skip ACT4 reaches cleanup");
        Check(state.Skip() && state.Stage == ExperienceStage.SeabedCleanup, "Skip cleanup waits for landing");
        Check(state.CleanupResolved && !state.CleanupCompleted, "Skipped cleanup never claims collection success");
        Check(!state.Skip() && !state.ChooseFreeExploration(state.SessionId) && !state.ExitExperience(state.SessionId), "Repeated skip and ending choices cannot bypass landing");
        Check(!state.ReportCleanupProgress(state.SessionId, 9, 9), "Late collection cannot overwrite skipped result");
        state.Pause();
        Check(!state.ReportPlatformReturn(state.SessionId), "Paused landing after skip is ignored");
        state.Resume();
        Check(!state.ReportPlatformReturn(Guid.NewGuid()), "Foreign landing after skip is ignored");
        Check(!state.HasReturnedAfterCleanup, "Staff skip does not fabricate landing");
        Check(state.ReportPlatformReturn(state.SessionId) && state.Stage == ExperienceStage.ExperienceComplete, "Landing after skip opens final choice");
        Check(state.Result(ExperienceStage.SeabedCleanup) == StageResult.Skipped, "Landing preserves skipped result");
        Check(state.Skip() && state.Status == SessionStatus.Completed, "Staff can end final choice safely");
        state.Reset(); state.Start();
        bool reentered = false;
        state.Changed += () => reentered |= state.Skip();
        Check(state.ReportInteraction(state.SessionId, ExperienceStage.Briefing, VisitorAction.ExploreHelmet) && !reentered && state.Stage == ExperienceStage.TheCase, "Transition callbacks cannot reenter");
        state.Reset();
        Console.WriteLine("PASS: " + assertions + " Experience session assertions");
    }
}
