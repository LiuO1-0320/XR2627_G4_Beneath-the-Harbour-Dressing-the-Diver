using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

// Run with Tests/run-staff-monitor-http-tests.ps1. All traffic stays on loopback.
static class StaffMonitorHttpTests
{
    static int port, checks;
    static readonly string pairing = Guid.NewGuid().ToString("N");
    static string Authority { get { return "127.0.0.1:" + port; } }
    static string Headers { get { return "Origin: http://" + Authority + "\r\nAuthorization: Bearer " + pairing + "\r\nContent-Type: application/json; charset=utf-8\r\n"; } }

    static void Check(bool pass, string name)
    {
        if (!pass) throw new Exception(name);
        checks++;
        Console.WriteLine("PASS " + name);
    }

    static string Request(string method, string path, string headers = "", byte[] body = null, bool split = false, string authority = null)
    {
        using (var client = new TcpClient())
        {
            client.Connect(IPAddress.Loopback, port);
            using (var stream = client.GetStream())
            {
                stream.ReadTimeout = stream.WriteTimeout = 5000;
                string text = method + " " + path + " HTTP/1.1\r\nHost: " + (authority ?? Authority) + "\r\n" + headers;
                if (body != null) text += "Content-Length: " + body.Length + "\r\n";
                var header = Encoding.ASCII.GetBytes(text + "\r\n");
                int first = body == null || split ? 0 : body.Length;
                var packet = new byte[header.Length + first];
                Buffer.BlockCopy(header, 0, packet, 0, header.Length);
                if (first > 0) Buffer.BlockCopy(body, 0, packet, header.Length, first);
                stream.Write(packet, 0, packet.Length);
                if (split)
                {
                    if (client.Client.Poll(120000, SelectMode.SelectRead))
                        throw new IOException("Server replied or closed before the declared POST body arrived.");
                    stream.Write(body, first, body.Length - first);
                }
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                    return reader.ReadToEnd();
            }
        }
    }

    static string Post(string headers, string json) { return Request("POST", "/api/command", headers, Encoding.UTF8.GetBytes(json)); }
    static bool Status(string reply, int code) { return reply.StartsWith("HTTP/1.1 " + code + " ", StringComparison.Ordinal); }

    static Thread Submit(StaffWebCommandQueue queue, string message, Action<string> received)
    {
        var worker = new Thread(() => received(queue.Submit(message))) { IsBackground = true };
        worker.Start();
        // Wait until the HTTP worker is blocked on its receipt, after enqueueing the message.
        Check(SpinWait.SpinUntil(() => (worker.ThreadState & ThreadState.WaitSleepJoin) != 0, 2000),
            "worker waiting for a Unity-thread receipt");
        return worker;
    }

    static void CheckQueue()
    {
        var queue = new StaffWebCommandQueue();
        int calls = 0;
        string receipt = "pending";
        var worker = Submit(queue, "Pause", reply => receipt = reply);
        Func<string, string> run = request => { calls++; return "done:" + request; };
        queue.Drain(run);
        queue.Drain(run);
        Check(worker.Join(2000) && receipt == "done:Pause" && calls == 1, "command acknowledged once across repeated drains");

        receipt = "pending";
        worker = Submit(queue, "Skip", reply => receipt = reply);
        Thread.Sleep(4200); // Exceed the real four-second worker deadline without draining.
        Check(worker.Join(2000) && receipt == null, "stalled Unity thread gives worker a null receipt");
        queue.Drain(run);
        Check(calls == 1, "resuming after timeout never applies expired Skip");

        receipt = "pending";
        worker = Submit(queue, "Reset", reply => receipt = reply);
        queue.Close();
        Check(worker.Join(2000) && receipt == null, "close releases pending worker with no receipt");
        queue.Drain(run);
        Check(calls == 1 && queue.Submit("Start") == null, "closed queue discards Reset and rejects future commands");
    }

    public static int Main()
    {
        try { Run(); return 0; }
        catch (Exception ex) { Console.Error.WriteLine(ex.GetType().Name + ": " + ex.Message); return 1; }
    }

    static void Run()
    {
        var free = new TcpListener(IPAddress.Loopback, 0);
        free.Start();
        port = ((IPEndPoint)free.LocalEndpoint).Port;
        free.Stop();
        const string json = "{\"note\":\"观众已暂停\"}";
        const string receipt = "{\"accepted\":true}";
        const string rejected = "{\"accepted\":false,\"reason\":\"stale-session\"}";
        int invocations = 0;
        using (var server = new StaffMonitorServer())
        {
            string error;
            Check(server.TryStart(port, "staff page", pairing, request =>
            {
                Interlocked.Increment(ref invocations);
                if (request == json) return receipt;
                if (request == "{}") return rejected;
                if (request == "{\"timeout\":true}") return null;
                throw new InvalidOperationException();
            }, out error), "start paired listener");
            server.Publish("{\"state\":\"Idle\"}");
            var view = Request("GET", "/api/view");
            Check(Status(view, 200) && view.EndsWith("{\"state\":\"Idle\"}") && view.Contains("Cache-Control: no-store")
                && !view.Contains(pairing), "public read-only live view without pairing secret");
            Check(Request("GET", "/").EndsWith("staff page"), "page still available");
            Check(Status(Post(Headers.Replace("Origin: http://" + Authority + "\r\n", ""), "{}"), 403), "absent Origin rejected");
            Check(Status(Post(Headers.Replace("http://" + Authority, "http://example.invalid"), "{}"), 403), "foreign Origin rejected");
            Check(Status(Post(Headers.Replace("Authorization: Bearer " + pairing + "\r\n", ""), "{}"), 401), "unpaired device rejected");
            Check(Status(Post(Headers.Replace(pairing, new string('x', pairing.Length)), "{}"), 401), "wrong pairing rejected");
            Check(Status(Post(Headers.Replace("application/json", "text/plain"), "{}"), 415), "media type enforced");
            Check(Status(Request("POST", "/api/command", Headers), 411), "length required");
            Check(Status(Request("POST", "/api/command", Headers, new byte[] { 0xC3, 0x28 }), 400), "invalid UTF-8 rejected");
            Check(Status(Post(Headers, new string('a', 4097)), 413), "size limit enforced");
            Check(Status(Request("GET", "/api/command"), 405), "GET cannot submit a command");
            var bytes = Encoding.UTF8.GetBytes(json);
            Check(Status(Request("POST", "/api/command", Headers.Replace(pairing, "unpaired"), bytes, true), 401),
                "headers before body still deliver unpaired status");
            Check(Status(Request("POST", "/api/command", Headers.Replace("http://" + Authority, "http://other.invalid"), bytes, true), 403),
                "headers before body still deliver foreign Origin status");
            Check(Status(Request("POST", "/api/command", Headers, bytes, true, "other.invalid"), 403),
                "headers before body still deliver foreign Host status");
            Check(Volatile.Read(ref invocations) == 0, "rejected requests never reach command dispatcher");
            var first = Request("POST", "/api/command", Headers, Encoding.UTF8.GetBytes(json), true);
            Check(Status(first, 200) && first.EndsWith(receipt) && Volatile.Read(ref invocations) == 1, "fragmented UTF-8 command dispatched exactly once");
            Check(Post(Headers, "{}").EndsWith(rejected), "session rejection receipt preserved");
            Check(Status(Post(Headers, "{\"timeout\":true}"), 503), "dispatcher timeout returns unavailable");
            Check(Status(Post(Headers, "{\"error\":true}"), 503), "dispatcher error returns unavailable");
            Check(Status(Request("GET", "/api/view"), 200), "monitor still serves after dispatcher error");
        }
        using (var burst = new StaffMonitorServer())
        using (var entered = new CountdownEvent(3))
        using (var release = new ManualResetEvent(false))
        {
            string error;
            Check(burst.TryStart(port, "staff page", pairing, value =>
            {
                if (value == "{\"hold\":true}")
                {
                    entered.Signal();
                    if (!release.WaitOne(3000)) return null;
                }
                return receipt;
            }, out error), "port reusable after dispose");
            burst.Publish("{}");
            var waiters = new Thread[3];
            var replies = new string[3];
            for (int i = 0; i < waiters.Length; i++)
            {
                int index = i;
                waiters[i] = new Thread(() =>
                {
                    try { replies[index] = Post(Headers, "{\"hold\":true}"); }
                    catch (IOException) { replies[index] = ""; }
                });
                waiters[i].Start();
            }
            try
            {
                Check(entered.Wait(2500), "three in-flight requests admitted");
                Check(Status(Post(Headers, "{}"), 429), "excess commands receive back pressure");
                Check(Status(Request("GET", "/api/view"), 200), "readable view during command contention");
            }
            finally { release.Set(); foreach (var worker in waiters) worker.Join(4000); }
            foreach (var reply in replies) Check(reply != null && Status(reply, 200), "in-flight receipt delivered");
        }
        using (var readOnly = new StaffMonitorServer())
        {
            string error;
            Check(readOnly.TryStart(port, "staff page", out error), "read-only listener restarts on same port");
            Check(Status(Post(Headers, "{}"), 405), "read-only overload disables commands");
        }
        CheckQueue();
        Console.WriteLine(checks + " checks passed.");
    }
}
