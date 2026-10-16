/// <summary>Untimed practice. Cancelled grabs never count as successful release.</summary>
public sealed class BriefingPractice
{
    public int Step { get; private set; }
    public bool Holding { get; private set; }
    public bool Ready => Step == 3;
    public bool Select(int target)
    {
        if (Step > 1 || Step != target) return false;
        Step++; return true;
    }
    public bool Grab()
    {
        if (Step != 2 || Holding) return false;
        Holding = true; return true;
    }
    public bool Release(bool cancelled)
    {
        bool completed = Step == 2 && Holding && !cancelled;
        Holding = false;
        if (completed) Step = 3;
        return completed;
    }
    public void Reset() { Step = 0; Holding = false; }
}
