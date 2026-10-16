using System;
public static class GazeTests
{
    static int assertions;
    static void Check(bool value,string message) { assertions++; if(!value)throw new Exception(message); }
    static bool Aim(GazeDwell gaze,int id,int frames=20) { bool ready=false; for(int i=0;i<frames;i++)ready=gaze.Step(id,.1f,1.8f); return ready; }
    public static void Run()
    {
        var gaze=new GazeDwell();
        Check(!gaze.Step(0,.1f,1.8f) && gaze.Progress==0,"No target cannot accumulate");
        Check(!Aim(gaze,1,10),"Short gaze does not activate");
        gaze.Step(0,.1f,1.8f);
        Check(!Aim(gaze,1,10),"Looking away cancels elapsed progress");
        Check(!Aim(gaze,2,10),"Switching target cancels previous progress");
        Check(Aim(gaze,2),"Continuous dwell becomes ready");
        Check(gaze.Claim(2,2),"Gaze can claim action");
        Check(!gaze.Claim(2,2.01),"Controller after gaze cannot double-submit");
        Check(!Aim(gaze,2,100),"Long gaze cannot repeat");
        gaze.Step(0,.1f,1.8f);
        Check(!Aim(gaze,2),"Brief jitter does not re-arm");
        Aim(gaze,0,3);
        Check(Aim(gaze,2),"Deliberate look-away re-arms target");
        Check(gaze.Claim(2,10),"Re-armed gaze can activate");
        gaze.Reset();
        Check(gaze.Claim(3,0),"Native input can win first");
        Check(!Aim(gaze,3),"Native input cancels pending gaze for same target");
        Check(!gaze.Claim(3,2),"Still-looking native retry suppressed");
        Check(!gaze.Claim(4,.1),"Mixed input global cooldown protects rapid target replacement");
        Aim(gaze,0,3);
        Check(gaze.Claim(4,2),"Other target permitted after cooldown");
        gaze.Reset();
        Check(!gaze.Step(5,10,1.8f),"A frame hitch cannot instantly select");
        Aim(gaze,5,8); gaze.CancelProgress();
        Check(!Aim(gaze,5,10),"Pause or revision change resets unfinished progress");
        gaze.Reset(); Check(gaze.Progress==0 && gaze.Claim(5,0),"New session clears previous input state");
        gaze.CancelProgress(); Check(!gaze.Claim(5,10),"Label change alone cannot re-trigger latched button");
        Console.WriteLine("PASS: "+assertions+" Experience gaze assertions");
    }
}
