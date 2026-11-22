using System;
public static class BriefingPracticeTests
{
    public static void Run()
    {
        int count=0;
        Action<bool,string> check=(ok,message)=>{count++;if(!ok)throw new Exception(message);};
        var p=new BriefingPractice();
        for(int round=0;round<3;round++)
        {
            check(!p.Ready && p.Step==0,"New visitor starts from first practice");
            check(!p.Grab() && !p.Release(false),"Cannot bypass selection practice");
            check(!p.Select(1),"Wrong target cannot advance");
            check(p.Select(0) && !p.Select(0),"Duplicate clicks do not advance twice");
            check(p.Select(1) && !p.Ready,"Two selections do not complete grab practice");
            check(!p.Release(false),"Release without pickup cannot complete");
            check(p.Grab() && !p.Grab(),"Pickup accepted once");
            check(!p.Release(true) && !p.Ready && !p.Holding,"Pause/reset cancellation is not success");
            check(p.Grab() && p.Release(false) && p.Ready,"Actual pickup and release completes practice");
            check(!p.Release(false) && !p.Grab(),"Success does not repeat");
            p.Reset();
        }
        const BriefingPractice.Input gaze = BriefingPractice.Input.HeadGaze;
        check(!p.Grab(gaze) && !p.Release(false, gaze), "Gaze cannot start at the welcome step");
        check(p.Select(0) && !p.Grab(gaze), "The left button alone cannot unlock the ball");
        check(p.Select(1) && !p.Release(false, gaze), "Gaze release requires a matching pickup");
        check(p.Grab() && p.HeldBy == BriefingPractice.Input.Controller, "Grip pickup owns the ball");
        check(!p.Grab(gaze) && !p.Release(false, gaze) && !p.Release(true, gaze) && p.Holding,
            "Gaze cannot take over, release, or cancel a hand-held ball");
        check(!p.Release(true) && p.Step == 2, "A cancelled grip allows another attempt");
        check(p.Grab(gaze) && p.HeldBy == gaze && !p.Ready, "Gaze pickup alone cannot finish");
        check(!p.Grab() && !p.Grab(gaze), "A held gaze ball cannot be picked up again");
        check(!p.Release(false) && !p.Release(true) && p.HeldBy == gaze,
            "A stale controller exit cannot release or cancel a gaze hold");
        check(!p.Release(true, gaze) && !p.Holding && p.Step == 2, "Cancelled gaze releases no credit");
        check(p.Grab(gaze), "Gaze may retry after cancellation");
        p.CancelHold();
        check(!p.Release(false, gaze) && p.Step == 2, "Suspension preserves button progress and cancels ownership");
        check(p.Grab(gaze) && p.Release(false, gaze) && p.Ready && !p.Holding,
            "Only deliberate release of the gaze-held ball finishes practice");
        check(!p.Release(false, gaze) && !p.Grab(gaze), "Gaze success is consumed once");
        p.Reset();
        check(p.Select(0) && p.Select(1) && p.Grab(gaze), "Another visitor can pick up the ball");
        p.Reset();
        check(!p.Holding && !p.Ready && !p.Release(false, gaze) && p.Step == 0,
            "Reset while holding drops ownership without completing the next visitor's practice");
        Console.WriteLine("PASS: "+count+" Experience briefing practice assertions");
    }
}
