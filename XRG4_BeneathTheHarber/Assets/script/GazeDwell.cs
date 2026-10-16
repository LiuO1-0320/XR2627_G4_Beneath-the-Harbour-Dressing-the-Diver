using System;

/// <summary>Pure dwell and mixed-input arbitration. A completed target requires a real look-away.</summary>
public sealed class GazeDwell
{
    int candidate, latched;
    float elapsed, away;
    double nextSubmit;
    public float Progress { get; private set; }
    public bool Step(int target, float dt, float duration)
    {
        dt = Math.Max(0, Math.Min(dt, .1f)); // A hitch must not complete a dwell.
        if (target != latched) { away += dt; if (away >= .25f) latched = 0; }
        else away = 0;
        if (target != candidate) { candidate = target; elapsed = 0; }
        if (target == 0 || target == latched) { elapsed = 0; Progress = 0; return false; }
        elapsed += dt; Progress = Math.Min(1, elapsed / Math.Max(.5f, duration));
        return Progress >= 1;
    }
    public bool Claim(int target, double now)
    {
        if (target == 0 || target == latched || now < nextSubmit) return false;
        latched = target; away = elapsed = 0; Progress = 0; nextSubmit = now + .35;
        return true;
    }
    public void CancelProgress() { candidate = 0; elapsed = 0; Progress = 0; }
    public void Reset() { candidate = latched = 0; elapsed = away = 0; Progress = 0; nextSubmit = 0; }
}
