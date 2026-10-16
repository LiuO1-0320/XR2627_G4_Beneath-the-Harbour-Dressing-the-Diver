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
        Console.WriteLine("PASS: "+count+" Experience briefing practice assertions");
    }
}
