/// <summary>Untimed practice. Cancelled grabs never count as successful release.</summary>
public sealed class BriefingPractice
{
    public enum Input { Controller, HeadGaze }

    public int Step { get; private set; }
    public Input? HeldBy { get; private set; }
    public bool Holding => HeldBy.HasValue;
    public bool Ready => Step == 3;
    public bool Select(int target)
    {
        if (Step > 1 || Step != target) return false;
        Step++; return true;
    }
    public bool Grab() => Grab(Input.Controller);

    public bool Grab(Input input)
    {
        if (Step != 2 || Holding) return false;
        HeldBy = input;
        return true;
    }

    public bool Release(bool cancelled) => Release(cancelled, Input.Controller);

    public bool Release(bool cancelled, Input input)
    {
        if (HeldBy != input) return false;
        CancelHold();
        if (cancelled) return false;
        Step = 3;
        return true;
    }

    public void CancelHold() { HeldBy = null; }

    public void Reset()
    {
        CancelHold();
        Step = 0;
    }
}
