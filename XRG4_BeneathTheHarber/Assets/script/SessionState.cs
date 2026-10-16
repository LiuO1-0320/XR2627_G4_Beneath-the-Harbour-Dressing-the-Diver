using System;

public enum SessionStatus { Idle, Running, Paused, Completed }
public enum ExperienceStage { Briefing, TheCase, ScaleUp, Inside, DressTheDiver, Descent, SeabedCleanup, ExperienceComplete, FreeExploration }
public enum StageResult { Pending, Active, Completed, Skipped }
public enum VisitorAction { ExploreHelmet, HelmetExamined, InsideExplored, ReflectOnViewports, ReflectOnConnections, HelmetEquipped }

/// <summary>Pure state machine. No location, timer or UI can complete a task.</summary>
public sealed class SessionState
{
    public SessionStatus Status { get; private set; }
    public ExperienceStage Stage { get; private set; }
    public Guid SessionId { get; private set; }
    public int Revision { get; private set; }
    public int CleanupCollected { get; private set; }
    public int CleanupTotal { get; private set; }
    public bool CleanupCompleted => CleanupTotal > 0 && CleanupCollected == CleanupTotal;
    public bool CleanupResolved => Result(ExperienceStage.SeabedCleanup) == StageResult.Completed || Result(ExperienceStage.SeabedCleanup) == StageResult.Skipped;
    public bool HasReturnedAfterCleanup { get; private set; }
    readonly StageResult[] results = new StageResult[9];
    bool publishing;
    public event Action Changed;
    public StageResult Result(ExperienceStage stage) => results[(int)stage];
    public static bool IsTask(ExperienceStage stage) => stage == ExperienceStage.ScaleUp || stage == ExperienceStage.DressTheDiver || stage == ExperienceStage.Descent;
    public static bool PreserveDiverEscort(ExperienceStage from, ExperienceStage to, StageResult result)
    {
        return result == StageResult.Completed &&
            ((from == ExperienceStage.DressTheDiver && to == ExperienceStage.Descent) ||
             (from == ExperienceStage.Descent && to == ExperienceStage.SeabedCleanup));
    }
    public bool Start()
    {
        if (publishing || Status != SessionStatus.Idle) return false;
        SessionId = Guid.NewGuid(); Stage = ExperienceStage.Briefing;
        Status = SessionStatus.Running; results[0] = StageResult.Active;
        Publish(); return true;
    }
    public bool Pause() => SetStatus(SessionStatus.Running, SessionStatus.Paused);
    public bool Resume() => SetStatus(SessionStatus.Paused, SessionStatus.Running);
    bool SetStatus(SessionStatus from, SessionStatus to)
    {
        if (publishing || Status != from) return false;
        Status = to; Publish(); return true;
    }
    public bool ReportInteraction(Guid session, ExperienceStage stage, VisitorAction action)
    {
        if (publishing || session != SessionId || stage != Stage || Status != SessionStatus.Running) return false;
        bool matches = (stage == ExperienceStage.Briefing && action == VisitorAction.ExploreHelmet)
            || (stage == ExperienceStage.TheCase && action == VisitorAction.HelmetEquipped);
        if (!matches) return false;
        return Advance(StageResult.Completed);
    }
    // Only the real collection activity reports progress; arrival/ordinary clicks cannot complete it.
    public bool ReportCleanupProgress(Guid session, int collected, int total)
    {
        if (publishing || session != SessionId || Status != SessionStatus.Running || Stage != ExperienceStage.SeabedCleanup
            || CleanupResolved || total <= 0 || collected < 0 || collected > total
            || collected < CleanupCollected || (CleanupTotal > 0 && CleanupTotal != total)
            || (CleanupTotal == total && CleanupCollected == collected)) return false;
        CleanupCollected = collected; CleanupTotal = total;
        if (CleanupCompleted) results[(int)ExperienceStage.SeabedCleanup] = StageResult.Completed;
        Publish(); return true;
    }
    public bool ReportPlatformReturn(Guid session)
    {
        if (publishing || session != SessionId || Status != SessionStatus.Running || HasReturnedAfterCleanup
            || Stage != ExperienceStage.SeabedCleanup || !CleanupResolved) return false;
        HasReturnedAfterCleanup = true;
        return Advance(Result(ExperienceStage.SeabedCleanup));
    }
    public bool ChooseFreeExploration(Guid session)
    {
        return session == SessionId && Status == SessionStatus.Running && Stage == ExperienceStage.ExperienceComplete
            && Advance(StageResult.Completed);
    }
    public bool ExitExperience(Guid session)
    {
        if (publishing || session != SessionId || (Status != SessionStatus.Running && Status != SessionStatus.Paused)
            || (Stage != ExperienceStage.ExperienceComplete && Stage != ExperienceStage.FreeExploration)) return false;
        results[(int)Stage] = StageResult.Completed; Status = SessionStatus.Completed; Publish(); return true;
    }
    public bool CompleteTask(Guid session, ExperienceStage stage)
    {
        return session == SessionId && stage == Stage && IsTask(stage) &&
            Status == SessionStatus.Running && Advance(StageResult.Completed);
    }
    public bool Skip()
    {
        if (publishing || (Status != SessionStatus.Running && Status != SessionStatus.Paused)) return false;
        // Skipping collection resolves only the task, never the physical return to land.
        if (Stage == ExperienceStage.SeabedCleanup)
        {
            if (CleanupResolved) return false;
            results[(int)Stage] = StageResult.Skipped;
            Publish(); return true;
        }
        return Advance(StageResult.Skipped);
    }
    bool Advance(StageResult result)
    {
        if (publishing) return false;
        results[(int)Stage] = result;
        // Inside is now part of ACT1 fitting, not a second visit after ACT2.
        // Retain enum values/array slots for existing serialized scene references.
        if (Stage == ExperienceStage.TheCase) results[(int)ExperienceStage.Inside] = result;
        if (Stage == ExperienceStage.FreeExploration || (Stage == ExperienceStage.ExperienceComplete && result == StageResult.Skipped)) Status = SessionStatus.Completed;
        else
        {
            switch (Stage)
            {
                case ExperienceStage.Briefing: Stage = ExperienceStage.TheCase; break;
                case ExperienceStage.TheCase: Stage = ExperienceStage.ScaleUp; break;
                case ExperienceStage.ScaleUp: Stage = ExperienceStage.DressTheDiver; break;
                case ExperienceStage.DressTheDiver: Stage = ExperienceStage.Descent; break;
                case ExperienceStage.Descent: Stage = ExperienceStage.SeabedCleanup; break;
                case ExperienceStage.SeabedCleanup: Stage = ExperienceStage.ExperienceComplete; break;
                case ExperienceStage.ExperienceComplete: Stage = ExperienceStage.FreeExploration; break;
            }
            results[(int)Stage] = StageResult.Active;
        }
        Publish(); return true;
    }
    public bool Reset()
    {
        if (publishing) return false;
        Array.Clear(results, 0, results.Length);
        CleanupCollected = CleanupTotal = 0;
        HasReturnedAfterCleanup = false;
        SessionId = Guid.Empty; Stage = ExperienceStage.Briefing; Status = SessionStatus.Idle;
        Publish(); return true;
    }
    void Publish()
    {
        Revision++; publishing = true;
        try { Changed?.Invoke(); } finally { publishing = false; }
    }
}
