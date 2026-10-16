/// <summary>Background selection depends on physical location, never task or helmet state.</summary>
public static class AudioPolicy
{
    public static float HarbourByDistance(float distance, float fullDistance, float fadeDistance)
    {
        float near = System.Math.Max(0, fullDistance);
        float far = System.Math.Max(near + .1f, fadeDistance);
        float t = System.Math.Max(0, System.Math.Min(1, (far - distance) / (far - near)));
        // Gentle endpoints, but a less steep middle than the previous quintic curve:
        // distant wind becomes audible earlier instead of concentrating the change near shore.
        return t * t * (3 - 2 * t);
    }
    public static bool Active(SessionStatus status) => status == SessionStatus.Running || status == SessionStatus.Paused;
    public static bool Underwater(bool headInWater) => headInWater;
    public static bool StageChanged(string previousSession, ExperienceStage previousStage, string session, ExperienceStage stage)
        => previousSession != session || previousStage != stage;
}

/// <summary>Arrival only unlocks narration, never task completion.</summary>
public sealed class NarrationArrivalGate
{
    string session;
    ExperienceStage stage;
    bool active;
    public bool CanPlay { get; private set; }
    public void Begin(string sessionId, ExperienceStage currentStage)
    {
        session = sessionId; stage = currentStage; active = true;
        CanPlay = stage != ExperienceStage.TheCase && stage != ExperienceStage.ScaleUp && stage != ExperienceStage.DressTheDiver;
    }
    public bool Confirm(string sessionId, ExperienceStage arrivedStage, bool running)
    {
        if (!active || CanPlay || !running || session != sessionId || stage != arrivedStage) return false;
        CanPlay = true; return true;
    }
    public void Clear() { active = CanPlay = false; session = null; }
}

public sealed class RegionNarrationHistory
{
    string session;
    readonly System.Collections.Generic.HashSet<ExperienceStage> visited = new System.Collections.Generic.HashSet<ExperienceStage>();
    public void Begin(string id) { if (session != id) { visited.Clear(); session = id; } }
    public bool Request(string id, ExperienceStage region, bool replay, bool freeExploration, bool running)
    {
        if (id != session || !running || (replay && !freeExploration)) return false;
        if (!replay && visited.Contains(region)) return false;
        visited.Add(region); return true;
    }
    public void Clear() { session = null; visited.Clear(); }
}
