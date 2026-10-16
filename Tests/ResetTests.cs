using System;
using System.Collections.Generic;

public static class ResetTests
{
    static int assertions;
    static void Check(bool value,string message) { assertions++; if(!value)throw new Exception(message); }
    sealed class Module : IResettable
    {
        public int ResetOrder { get; set; }
        public Action prepare=()=>{}, restore=()=>{};
        public void PrepareReset()=>prepare(); public void RestoreReset()=>restore();
    }
    public static void Run()
    {
        var sequence=new ResetSequence();
        var events=new List<string>();
        var a=new Module { ResetOrder=0, prepare=()=>events.Add("stopA"), restore=()=>events.Add("restoreA") };
        var b=new Module { ResetOrder=10, prepare=()=>events.Add("stopB"), restore=()=>events.Add("restoreB") };
        var errors=sequence.Run(new[]{b,a,a},()=>events.Add("invalidate"),()=>events.Add("finish"));
        Check(errors.Count==0,"Successful reset reports success");
        Check(string.Join(",",events)=="invalidate,stopA,stopB,restoreA,restoreB,finish","Invalidate first, stop all before restore, deduplicate registration");
        Check(!sequence.IsRunning,"Busy flag cleared after reset");
        a.prepare=()=>throw new Exception("Broken stop"); b.restore=()=>throw new Exception("Broken restore");
        events.Clear();
        errors=sequence.Run(new[]{a,b},()=>events.Add("invalidate"),()=>events.Add("finish"));
        Check(errors.Count==2,"All module failures collected");
        Check(events.Contains("stopB") && events.Contains("restoreA") && events.Contains("finish"),"A faulty module cannot prevent remaining cleanup");
        Check(!sequence.IsRunning,"Faults do not leave resetting latch stuck");
        var reentrant=new Module { prepare=()=>sequence.Run(new IResettable[0],()=>{},()=>{}) };
        errors=sequence.Run(new[]{reentrant},()=>{},()=>{});
        Check(errors.Count==1 && !sequence.IsRunning,"Reentrant reset rejected and original reset finishes");
        errors=sequence.Run(new IResettable[0],()=>throw new Exception("Broken state callback"),()=>events.Add("finishAfterStateFault"));
        Check(errors.Count==1 && events.Contains("finishAfterStateFault"),"Scene cleanup still finalizes after state callback failure");

        foreach(ExperienceStage target in Enum.GetValues(typeof(ExperienceStage)))
        {
            if(target==ExperienceStage.Inside) continue; // Compatibility slot, not an independent runtime stage.
            foreach(bool paused in new[]{false,true})
            {
                var state=new SessionState(); state.Start();
                for(int i=0;state.Stage!=target && i<20;i++)
                {
                    if(state.Stage==ExperienceStage.SeabedCleanup) { state.Skip(); state.ReportPlatformReturn(state.SessionId); }
                    else if(state.Stage==ExperienceStage.ExperienceComplete) state.ChooseFreeExploration(state.SessionId);
                    else state.Skip();
                }
                Check(state.Stage==target,"Reach reset test stage "+target);
                if(paused) state.Pause();
                var old=state.SessionId;
                errors=sequence.Run(new IResettable[0],()=>state.Reset(),()=>{});
                Check(errors.Count==0 && state.Status==SessionStatus.Idle && state.Stage==ExperienceStage.Briefing && state.SessionId==Guid.Empty,"Reset every major stage, including pause");
                foreach(ExperienceStage stage in Enum.GetValues(typeof(ExperienceStage))) Check(state.Result(stage)==StageResult.Pending,"No previous result survives reset");
                state.Start(); Check(!state.CompleteTask(old,ExperienceStage.DressTheDiver),"Old completion cannot modify next visitor");
            }
        }
        Console.WriteLine("PASS: "+assertions+" Experience reset assertions");
    }
}
