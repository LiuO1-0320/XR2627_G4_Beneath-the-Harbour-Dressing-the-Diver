using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;

/// <summary>Local IPv4 staff page with optional paired control access.</summary>
public sealed class StaffMonitorServer : IDisposable
{
    public const int DefaultPort = 8765;
    const int MaxClients = 8, MaxHeaderBytes = 8192, MaxCommandBytes = 4096, TimeoutMs = 2500;
    const int MaxConcurrentCommands = 3, MaxCommandsPerSecond = 12;
    static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
    readonly Stopwatch commandWindow = new Stopwatch();
    int activeCommands, commandsThisWindow;
    readonly object gate = new object();
    readonly HashSet<TcpClient> clients = new HashSet<TcpClient>();
    TcpListener listener;
    volatile byte[] latestView;
    public string[] Urls { get; private set; } = new string[0];

    public bool TryStart(int port, string html, out string error)
    {
        return TryStart(port, html, null, null, out error);
    }

    public bool TryStart(int port, string html, string controlKey, Func<string, string> handleCommand, out string error)
    {
        error = null;
        if (handleCommand != null && string.IsNullOrWhiteSpace(controlKey))
        {
            error = "Staff controls require a pairing key.";
            return false;
        }
        if (port < 1 || port > 65535 || string.IsNullOrEmpty(html))
        {
            error = "Invalid port or missing staff monitor page.";
            return false;
        }

        var page = Encoding.UTF8.GetBytes(html);
        lock (gate)
        {
            if (listener != null) { error = "Staff monitor is already running."; return false; }
            var host = new TcpListener(IPAddress.Any, port);
            try
            {
                host.ExclusiveAddressUse = true;
                host.Start(MaxClients);
                Urls = LocalUrls(port);
                listener = host;
                new Thread(() => AcceptClients(host, page, controlKey, handleCommand)) { IsBackground = true, Name = "Staff monitor HTTP" }.Start();
                return true;
            }
            catch (Exception e)
            {
                host.Stop();
                listener = null;
                Urls = new string[0];
                error = "Could not open staff monitor on port " + port + ". " + e.Message;
                return false;
            }
        }
    }

    // Unity serializes one complete view on its main thread; readers see either publication in full.
    public void Publish(string json) { latestView = Encoding.UTF8.GetBytes(json); }

    static bool LocalAddress(IPAddress ip)
    {
        if (ip.AddressFamily != AddressFamily.InterNetwork) return false;
        var b = ip.GetAddressBytes();
        return IPAddress.IsLoopback(ip) || b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
            || (b[0] == 192 && b[1] == 168) || (b[0] == 169 && b[1] == 254);
    }

    static string[] LocalUrls(int port)
    {
        var urls = new List<string> { "http://127.0.0.1:" + port + "/" };
        try
        {
            foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (adapter.OperationalStatus != OperationalStatus.Up) continue;
                foreach (var unicast in adapter.GetIPProperties().UnicastAddresses)
                {
                    var ip = unicast.Address;
                    if (IPAddress.IsLoopback(ip) || !LocalAddress(ip)) continue;
                    string url = "http://" + ip + ":" + port + "/";
                    if (!urls.Contains(url)) urls.Add(url);
                }
            }
        }
        catch (NetworkInformationException) { /* Loopback remains accessible during adapter changes. */ }
        return urls.ToArray();
    }

    void AcceptClients(TcpListener host, byte[] page, string controlKey, Func<string, string> handleCommand)
    {
        try
        {
            while (true)
            {
                var client = host.AcceptTcpClient();
                lock (gate)
                {
                    if (host != listener) { client.Close(); return; }
                    if (clients.Count >= MaxClients)
                    {
                        client.Close();
                        continue;
                    }
                    clients.Add(client);
                    ThreadPool.QueueUserWorkItem(_ => Serve(client, host, page, controlKey, handleCommand));
                }
            }
        }
        catch (SocketException) { /* Stopping the listener interrupts Accept. */ }
        catch (InvalidOperationException) { /* Stop may precede the next Accept. */ }
    }

    void Serve(TcpClient client, TcpListener host, byte[] page, string controlKey, Func<string, string> handleCommand)
    {
        try
        {
            var socket = client.Client;
            if (socket == null) return;
            var peer = socket.RemoteEndPoint as IPEndPoint;
            var local = socket.LocalEndPoint as IPEndPoint;
            if (peer == null || local == null || !LocalAddress(peer.Address) || !LocalAddress(local.Address)) return;
            using (var stream = client.GetStream())
            {
                stream.WriteTimeout = TimeoutMs;
                var timer = Stopwatch.StartNew();
                byte[] prefix;
                var request = ReadHeaders(stream, timer, out prefix);
                if (request == null) return;
                var lines = request.Split(new[] { "\r\n" }, StringSplitOptions.None);
                var first = lines[0].Split(' ');
                var headers = ParseHeaders(lines);
                if (headers == null || first.Length != 3 || (first[2] != "HTTP/1.1" && first[2] != "HTTP/1.0"))
                { Reply(stream, "400 Bad Request"); return; }
                string path = first[1].Split('?')[0];
                byte[] commandBody = null;
                if (first[0] == "POST" && path == "/api/command")
                {
                    // Windows may reset the socket if it closes with an unread body, hiding even a 401/403.
                    commandBody = ReadCommandBody(stream, headers, prefix, timer);
                    if (commandBody == null) return;
                }
                if (!ValidHost(Header(headers, "Host"), local)) { Reply(stream, "403 Forbidden"); return; }

                if (path == "/api/command" && handleCommand != null)
                {
                    if (first[0] != "POST") Reply(stream, "405 Method Not Allowed", extraHeader: "Allow: POST\r\n");
                    else ServeCommand(stream, host, headers, commandBody, controlKey, handleCommand);
                }
                else if (first[0] != "GET") Reply(stream, "405 Method Not Allowed", extraHeader: "Allow: GET\r\n");
                else if (path == "/" || path == "/index.html") Reply(stream, "200 OK", "text/html", page);
                else if (path == "/api/view")
                {
                    var data = latestView;
                    if (data == null) Reply(stream, "503 Service Unavailable");
                    else Reply(stream, "200 OK", "application/json", data);
                }
                else Reply(stream, "404 Not Found");
            }
        }
        catch (IOException) { /* Browsers can disconnect or exceed the read/write deadline. */ }
        catch (SocketException) { }
        catch (InvalidOperationException) { /* Client closed during shutdown, before GetStream. */ }
        finally
        {
            lock (gate) clients.Remove(client);
            client.Close();
        }
    }

    static Dictionary<string, string> ParseHeaders(string[] lines)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 1; i < lines.Length; i++)
        {
            int colon = lines[i].IndexOf(':');
            if (colon <= 0) return null;
            string name = lines[i].Substring(0, colon);
            if (headers.ContainsKey(name)) return null;
            headers.Add(name, lines[i].Substring(colon + 1).Trim());
        }
        return headers;
    }

    static string Header(Dictionary<string, string> headers, string key)
    {
        return headers.TryGetValue(key, out var value) ? value : null;
    }

    static bool ValidHost(string host, IPEndPoint local)
    {
        string suffix = local.Port == 80 ? "" : ":" + local.Port;
        return string.Equals(host, local.Address + suffix, StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "localhost" + suffix, StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, Environment.MachineName + suffix, StringComparison.OrdinalIgnoreCase);
    }

    static string ReadHeaders(NetworkStream stream, Stopwatch timer, out byte[] prefix)
    {
        prefix = null;
        var bytes = new byte[MaxHeaderBytes];
        int used = 0;
        while (used < bytes.Length)
        {
            int remaining = TimeoutMs - (int)timer.ElapsedMilliseconds;
            if (remaining <= 0) return null;
            stream.ReadTimeout = remaining;
            int start = Math.Max(0, used - 3);
            int read = stream.Read(bytes, used, bytes.Length - used);
            if (read == 0) return null;
            used += read;
            for (int i = start; i <= used - 4; i++)
                if (bytes[i] == 13 && bytes[i + 1] == 10 && bytes[i + 2] == 13 && bytes[i + 3] == 10)
                {
                    // The first TCP read can contain headers and some or all of the JSON body.
                    prefix = new byte[used - i - 4];
                    Buffer.BlockCopy(bytes, i + 4, prefix, 0, prefix.Length);
                    return Encoding.ASCII.GetString(bytes, 0, i);
                }
        }
        return null;
    }

    static byte[] ReadCommandBody(NetworkStream stream, Dictionary<string, string> headers, byte[] prefix, Stopwatch timer)
    {
        if (headers.ContainsKey("Transfer-Encoding")) { Reply(stream, "400 Bad Request"); return null; }
        int length;
        if (!int.TryParse(Header(headers, "Content-Length"), NumberStyles.None, CultureInfo.InvariantCulture, out length))
        { Reply(stream, "411 Length Required"); return null; }
        if (length > MaxCommandBytes) { Reply(stream, "413 Payload Too Large"); return null; }
        if (length == 0 || prefix.Length > length) { Reply(stream, "400 Bad Request"); return null; }
        var body = new byte[length];
        Buffer.BlockCopy(prefix, 0, body, 0, prefix.Length);
        for (int used = prefix.Length; used < length;)
        {
            int remaining = TimeoutMs - (int)timer.ElapsedMilliseconds;
            if (remaining <= 0) return null;
            stream.ReadTimeout = remaining;
            int read = stream.Read(body, used, length - used);
            if (read == 0) return null;
            used += read;
        }
        return body;
    }

    void ServeCommand(NetworkStream stream, TcpListener host, Dictionary<string, string> headers, byte[] body,
        string key, Func<string, string> handleCommand)
    {
        if (!string.Equals(Header(headers, "Origin"), "http://" + Header(headers, "Host"), StringComparison.OrdinalIgnoreCase))
        { Reply(stream, "403 Forbidden"); return; }
        if (!Authorized(Header(headers, "Authorization"), key))
        { Reply(stream, "401 Unauthorized"); return; }
        var contentType = (Header(headers, "Content-Type") ?? "").Split(';')[0].Trim();
        if (!contentType.Equals("application/json", StringComparison.OrdinalIgnoreCase))
        { Reply(stream, "415 Unsupported Media Type"); return; }
        string json;
        try { json = StrictUtf8.GetString(body); }
        catch (DecoderFallbackException) { Reply(stream, "400 Bad Request"); return; }
        if (!TryReserveCommand(host)) { Reply(stream, "429 Too Many Requests"); return; }
        string result;
        try { result = handleCommand(json); }
        catch (Exception) { result = null; }
        finally { lock (gate) activeCommands--; }
        if (result == null) Reply(stream, "503 Service Unavailable");
        else Reply(stream, "200 OK", "application/json", Encoding.UTF8.GetBytes(result));
    }

    static bool Authorized(string authorization, string key)
    {
        const string scheme = "Bearer ";
        if (authorization == null || authorization.Length != key.Length + scheme.Length ||
            !authorization.StartsWith(scheme, StringComparison.OrdinalIgnoreCase)) return false;
        int different = 0;
        for (int i = 0; i < key.Length; i++) different |= key[i] ^ authorization[i + scheme.Length];
        return different == 0;
    }

    bool TryReserveCommand(TcpListener host)
    {
        lock (gate)
        {
            if (listener != host || activeCommands >= MaxConcurrentCommands) return false;
            if (!commandWindow.IsRunning || commandWindow.ElapsedMilliseconds >= 1000)
            { commandWindow.Restart(); commandsThisWindow = 0; }
            if (commandsThisWindow >= MaxCommandsPerSecond) return false;
            commandsThisWindow++;
            activeCommands++;
            return true;
        }
    }

    static void Reply(NetworkStream stream, string status, string type = "text/plain", byte[] body = null,
        string extraHeader = "")
    {
        var headers = Encoding.ASCII.GetBytes("HTTP/1.1 " + status + "\r\nContent-Type: " + type +
            "; charset=utf-8\r\nContent-Length: " + (body == null ? 0 : body.Length) +
            "\r\nCache-Control: no-store\r\nConnection: close\r\nX-Content-Type-Options: nosniff\r\nX-Frame-Options: DENY\r\nReferrer-Policy: no-referrer\r\n" + extraHeader + "\r\n");
        stream.Write(headers, 0, headers.Length);
        if (body != null) stream.Write(body, 0, body.Length);
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (listener != null) listener.Stop();
            listener = null;
            foreach (var client in clients) client.Close();
            clients.Clear();
            latestView = null;
            Urls = new string[0];
        }
    }
}
