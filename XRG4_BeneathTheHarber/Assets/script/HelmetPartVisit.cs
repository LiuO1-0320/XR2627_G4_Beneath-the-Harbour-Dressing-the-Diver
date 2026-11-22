/// <summary>Inspection progress for one removable exhibit part, shared by grab and gaze.</summary>
public sealed class HelmetPartVisit
{
    enum Position { InSocket, Loose, InPreview, ViewedInPreview }
    Position position;

    public bool Observed { get; private set; }
    public bool Detached => position != Position.InSocket;
    public bool ViewedInPreview => position == Position.ViewedInPreview;
    public bool Assisted => position == Position.InPreview || ViewedInPreview;
    public bool CanRefitWithGaze => ViewedInPreview;
    public bool Complete => Observed && !Detached;

    public void TakeByHand(bool recordVisit)
    {
        position = Position.Loose;
        Observed |= recordVisit;
    }

    public void TakeForInspection()
    {
        position = Position.InPreview;
    }

    public bool Observe(bool recordVisit)
    {
        if (!Assisted) return false;
        position = Position.ViewedInPreview;
        Observed |= recordVisit;
        return true;
    }

    public void ReturnToSocket()
    {
        position = Position.InSocket;
    }

    public void Reset()
    {
        ReturnToSocket();
        Observed = false;
    }
}
