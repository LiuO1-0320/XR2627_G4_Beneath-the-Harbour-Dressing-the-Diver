using System;
using System.Collections.Generic;

/// <summary>Implement both phases. Preparation must cancel callbacks/selections before any restore.</summary>
public interface IResettable
{
    int ResetOrder { get; }
    void PrepareReset();
    void RestoreReset();
}

/// <summary>Independent failures cannot prevent the remaining modules from being restored.</summary>
public sealed class ResetSequence
{
    public bool IsRunning { get; private set; }
    public List<string> Run(IEnumerable<IResettable> modules, Action invalidateSession, Action finish)
    {
        if (IsRunning) throw new InvalidOperationException("Reset is already running.");
        IsRunning=true;
        var failures=new List<string>();
        try
        {
            var ordered=new List<IResettable>();
            foreach(var module in modules) if(module!=null && !ordered.Contains(module)) ordered.Add(module);
            ordered.Sort((a,b)=>a.ResetOrder.CompareTo(b.ResetOrder));
            Attempt("Session",invalidateSession,failures);
            foreach(var module in ordered) Attempt(module.GetType().Name+".Prepare",module.PrepareReset,failures);
            foreach(var module in ordered) Attempt(module.GetType().Name+".Restore",module.RestoreReset,failures);
            Attempt("Finalize",finish,failures);
        }
        finally { IsRunning=false; }
        return failures;
    }
    static void Attempt(string name,Action action,List<string> failures)
    { try { action(); } catch(Exception error) { failures.Add(name+": "+error.Message); } }
}
