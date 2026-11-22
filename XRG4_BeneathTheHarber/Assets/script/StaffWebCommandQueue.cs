using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

/// <summary>Bridges HTTP workers to the Unity thread for the lifetime of one staff listener.</summary>
internal sealed class StaffWebCommandQueue
{
    sealed class Request
    {
        public string json;
        public readonly Stopwatch age = Stopwatch.StartNew();
        public readonly TaskCompletionSource<string> receipt = new TaskCompletionSource<string>();
        public int claimed;
    }

    const int TimeoutMs = 4000;
    readonly object gate = new object();
    readonly Queue<Request> waiting = new Queue<Request>();
    bool closed;

    // A null receipt means the caller must refresh, never replay automatically.
    public string Submit(string json)
    {
        var request = new Request { json = json };
        lock (gate)
        {
            if (closed || waiting.Count >= 8) return null;
            waiting.Enqueue(request);
        }
        if (request.receipt.Task.Wait(TimeoutMs)) return request.receipt.Task.Result;
        // Cancel commands that have not started, even when the editor is paused.
        Interlocked.CompareExchange(ref request.claimed, 1, 0);
        return null;
    }

    public void Drain(Func<string, string> execute)
    {
        for (int i = 0; i < 8; i++)
        {
            Request request;
            lock (gate)
            {
                if (closed || waiting.Count == 0) return;
                request = waiting.Dequeue();
            }
            string reply = null;
            try
            {
                if (request.age.ElapsedMilliseconds < TimeoutMs &&
                    Interlocked.CompareExchange(ref request.claimed, 1, 0) == 0)
                    reply = execute(request.json);
            }
            finally { request.receipt.TrySetResult(reply); }
        }
    }

    public void Close()
    {
        lock (gate)
        {
            closed = true;
            while (waiting.Count > 0) waiting.Dequeue().receipt.TrySetResult(null);
        }
    }
}
