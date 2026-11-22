using System;

public static class HelmetPartVisitTests
{
    static int assertions;

    static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new Exception(message);
    }

    public static void Run()
    {
        var parts = new HelmetPartVisit[7];
        for (int i = 0; i < parts.Length; i++) parts[i] = new HelmetPartVisit();
        for (int i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            Check(!part.Observe(true) && !part.Observed, "Reading a fitted part cannot count as an inspection");
            part.TakeForInspection();
            Check(part.Detached && part.Assisted && !part.Observed && !part.CanRefitWithGaze, "Detaching alone cannot unlock refit");
            Check(part.Observe(true) && part.ViewedInPreview && part.CanRefitWithGaze, "Observing the detached part unlocks refit");
            part.Observe(true);
            Check(Array.FindAll(parts, p => p.Observed).Length == i + 1, "Repeated observation counts once");
            Check(!Array.TrueForAll(parts, p => p.Complete), "Even the final observed part must return to its socket");
            part.ReturnToSocket();
            Check(part.Complete && !part.Assisted && !part.CanRefitWithGaze, "Refit closes the current preview");
            Check(Array.TrueForAll(parts, p => p.Complete) == (i == parts.Length - 1), "Only seven finished parts satisfy the completion condition");
        }

        var last = parts[6];
        last.TakeForInspection();
        Check(last.Observed && !last.CanRefitWithGaze && !last.Complete, "Reopening a completed part requires a fresh preview");
        last.TakeByHand(true);
        Check(last.Detached && !last.Assisted && !last.Observe(true), "Hand takeover cancels gaze observation and refit");
        last.ReturnToSocket();
        Check(last.Complete, "A physical hand return satisfies the same completion condition");

        last.TakeByHand(true);
        last.TakeForInspection();
        Check(last.Assisted && !last.CanRefitWithGaze, "Gaze can take over a loose part but must inspect it again");
        last.Observe(true);
        last.Reset();
        Check(!last.Observed && !last.Detached && !last.Assisted && !last.CanRefitWithGaze, "Recovery clears credit and pending refit");
        Check(Array.FindAll(parts, p => p.Complete).Length == 6, "Recovering one loose part preserves other completed parts");

        last.TakeByHand(false);
        last.ReturnToSocket();
        Check(!last.Complete, "Free exploration hand input grants no task credit");
        last.TakeForInspection();
        Check(last.Observe(false) && last.CanRefitWithGaze && !last.Observed, "Free exploration permits observation and refit without task credit");
        last.ReturnToSocket();
        Check(!last.Complete, "Free exploration refit cannot complete the task");

        foreach (var part in parts) part.Reset();
        Check(Array.TrueForAll(parts, p => !p.Detached && !p.Observed), "A fresh visitor starts with seven fitted unobserved parts");
        for (int i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            if (i % 2 == 0)
            {
                part.TakeByHand(true);
                Check(part.Observed && !part.Complete && !part.Assisted, "Physical pickup grants observation while leaving the socket empty");
            }
            else
            {
                part.TakeForInspection();
                part.Observe(true);
            }
            part.ReturnToSocket();
        }
        Check(Array.TrueForAll(parts, p => p.Complete), "One journey can combine hand and gaze across all seven sockets");
        Console.WriteLine("PASS: " + assertions + " ACT2 helmet part assertions");
    }
}
