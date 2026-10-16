using System;

public static class AudioArrivalTests
{
    public static void Run()
    {
        int count = 0;
        Action<bool, string> check = (ok, message) => { count++; if (!ok) throw new Exception(message); };
        var gate = new NarrationArrivalGate();
        check(AudioPolicy.HarbourByDistance(14, 5, 14) == 0, "Museum endpoint uses configured 14m boundary");
        check(AudioPolicy.HarbourByDistance(5, 5, 14) == 1, "Harbour endpoint uses configured 5m boundary");
        check(AudioPolicy.HarbourByDistance(9.5f, 5, 14) == .5f, "Configured midpoint has equal mix");
        var outward = new float[37];
        for (int i = 0; i < outward.Length; i++)
            outward[i] = AudioPolicy.HarbourByDistance(5 + i * .25f, 5, 14);
        for (int i = outward.Length - 1; i >= 0; i--)
            check(outward[i] == AudioPolicy.HarbourByDistance(5 + i * .25f, 5, 14), "Return route uses identical spatial blend");
        check(AudioPolicy.HarbourByDistance(0, 2, 8) == 1, "Inside harbour is harbour ambience");
        check(AudioPolicy.HarbourByDistance(2, 2, 8) == 1, "Near shoreline is harbour ambience");
        check(AudioPolicy.HarbourByDistance(8, 2, 8) == 0, "Museum is museum music");
        check(AudioPolicy.HarbourByDistance(5, 2, 8) == .5f, "Boundary blends smoothly");
        check(AudioPolicy.HarbourByDistance(float.PositiveInfinity, 2, 8) == 0, "Missing water defaults to museum");
        check(AudioPolicy.HarbourByDistance(10, 8, 2) == 0, "Invalid radii remain bounded");
        float last = 0;
        for (int distance = 10; distance >= 0; distance--)
        {
            float next = AudioPolicy.HarbourByDistance(distance, 2, 8);
            check(next >= last && next <= 1, "Spatial blend is monotonic"); last = next;
        }
        var history = new RegionNarrationHistory();
        history.Begin("visitor-a");
        check(history.Request("visitor-a", ExperienceStage.TheCase, false, false, true), "First region visit auto plays");
        check(!history.Request("visitor-a", ExperienceStage.TheCase, false, false, true), "Repeated region entry does not auto replay");
        check(history.Request("visitor-a", ExperienceStage.ScaleUp, false, false, true), "Different region has independent first visit");
        check(!history.Request("visitor-a", ExperienceStage.ScaleUp, true, false, true), "Guided mode cannot manually replay");
        check(history.Request("visitor-a", ExperienceStage.ScaleUp, true, true, true), "Free exploration may replay a previously heard region");
        check(!history.Request("visitor-a", ExperienceStage.ScaleUp, true, true, false), "Pause blocks replay");
        check(history.Request("visitor-a", ExperienceStage.DressTheDiver, false, true, true), "First visit to skipped region auto plays in free exploration");
        history.Begin("visitor-a");
        check(!history.Request("visitor-a", ExperienceStage.TheCase, false, true, true), "Stage transition preserves visit history");
        history.Clear(); history.Begin("visitor-b");
        check(!history.Request("visitor-a", ExperienceStage.TheCase, true, true, true), "Previous visitor button cannot replay");
        check(history.Request("visitor-b", ExperienceStage.TheCase, false, false, true), "New visitor hears automatic region narration again");
        check(!AudioPolicy.Underwater(false), "NPC fitting / land phase cannot trigger water ambience");
        check(!AudioPolicy.Underwater(false), "Starting descent above water stays harbour");
        check(AudioPolicy.Underwater(true), "Surface crossing immediately selects underwater bed");
        check(AudioPolicy.Underwater(true), "Underwater bed is independent of splash and rope progress");
        check(!AudioPolicy.Underwater(false), "Land helmet cannot select underwater ambience");
        check(!AudioPolicy.Underwater(false), "Returning above surface restores harbour");
        check(!gate.CanPlay && !gate.Confirm("a", ExperienceStage.TheCase, true), "Idle rejects arrival");
        foreach (var stage in new[] { ExperienceStage.TheCase, ExperienceStage.ScaleUp, ExperienceStage.DressTheDiver })
        {
            gate.Begin("a", stage);
            check(!gate.CanPlay, "ACT1-3 wait for arrival");
            check(!gate.Confirm("old", stage, true) && !gate.CanPlay, "Old session cannot unlock");
            check(!gate.Confirm("a", ExperienceStage.Briefing, true) && !gate.CanPlay, "Wrong route cannot unlock");
            check(!gate.Confirm("a", stage, false) && !gate.CanPlay, "Pause does not accept arrival");
            check(gate.Confirm("a", stage, true) && gate.CanPlay, "Arrival after resume unlocks");
            check(!gate.Confirm("a", stage, true) && gate.CanPlay, "Duplicate arrival does not replay");
        }
        gate.Begin("a", ExperienceStage.TheCase);
        gate.Begin("a", ExperienceStage.ScaleUp);
        check(!gate.Confirm("a", ExperienceStage.TheCase, true) && !gate.CanPlay, "Skip rejects previous route arrival");
        gate.Clear();
        check(!gate.CanPlay && !gate.Confirm("a", ExperienceStage.ScaleUp, true), "Reset rejects pending arrival");
        gate.Begin("b", ExperienceStage.ScaleUp);
        check(!gate.Confirm("a", ExperienceStage.ScaleUp, true) && gate.Confirm("b", ExperienceStage.ScaleUp, true), "Second visitor has independent arrival");
        gate.Begin("b", ExperienceStage.Briefing);
        check(gate.CanPlay, "Briefing remains automatic");
        gate.Begin("b", ExperienceStage.Descent);
        check(gate.CanPlay, "Descent remains automatic");
        Console.WriteLine("PASS: " + count + " Experience audio arrival assertions");
    }
}
