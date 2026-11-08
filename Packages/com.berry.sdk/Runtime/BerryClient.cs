using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BerrySdk
{
    public sealed class BerrySession
    {
        public int Port;
        public string Token = "";
        public string GameId = "";
        public string GameCode = "";
        public long GameNumber;

        public static BerrySession Discover(string gameIdHint, Func<string, string> getEnv = null, Func<string, string> readFile = null)
        {
            if (getEnv == null) getEnv = Environment.GetEnvironmentVariable;
            if (readFile == null) readFile = SafeRead;
            int port;
            string token = getEnv("BERRY_SDK_TOKEN");
            if (!string.IsNullOrEmpty(token) && int.TryParse(getEnv("BERRY_SDK_PORT"), out port) && port >= 1024 && port < 65536)
                return new BerrySession { Port = port, Token = token, GameId = getEnv("BERRY_SDK_GAME") ?? gameIdHint ?? "" };
            if (string.IsNullOrEmpty(gameIdHint) || !IsPlainId(gameIdHint)) return null;
            string text = readFile(Path.Combine(ConfigDir(getEnv), "sdk", gameIdHint + ".json"));
            if (string.IsNullOrEmpty(text)) return null;
            object parsed;
            if (!BerryJson.TryParse(text, out parsed)) return null;
            int p = (int)BerryJson.Num(parsed, "port");
            string t = BerryJson.Str(parsed, "token");
            if (p < 1024 || p >= 65536 || t.Length == 0) return null;
            return new BerrySession { Port = p, Token = t, GameId = gameIdHint };
        }

        private static bool IsPlainId(string s)
        {
            if (s.Length == 0 || s.Length > 64) return false;
            foreach (char c in s) if (!(char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.')) return false;
            return !s.Contains("..");
        }

        internal static string ConfigDir(Func<string, string> getEnv)
        {
            string over = getEnv("BERRY_CONFIG_DIR");
            if (!string.IsNullOrEmpty(over)) return over;
            string appData = getEnv("APPDATA");
            if (!string.IsNullOrEmpty(appData)) return Path.Combine(appData, "Berry");
            string xdg = getEnv("XDG_CONFIG_HOME");
            if (!string.IsNullOrEmpty(xdg)) return Path.Combine(xdg, "berry");
            string home = getEnv("HOME");
            return string.IsNullOrEmpty(home) ? "berry-config" : Path.Combine(home, ".config", "berry");
        }

        private static string SafeRead(string path)
        {
            try { return File.Exists(path) ? File.ReadAllText(path) : null; }
            catch (Exception) { return null; }
        }
    }

    public sealed class BerryClient : IDisposable
    {
        public const int Protocol = 1;
        public const string SdkVersion = "1.4.0";
        private const int MaxMessage = 64 * 1024;

        private ClientWebSocket ws;
        private readonly SemaphoreSlim sendLock = new SemaphoreSlim(1, 1);
        private readonly object gate = new object();
        private readonly Dictionary<int, TaskCompletionSource<Dictionary<string, object>>> pending = new Dictionary<int, TaskCompletionSource<Dictionary<string, object>>>();
        private CancellationTokenSource cts;
        private int nextId = 1;
        private bool disposed;

        public event Action<Dictionary<string, object>> ContextChanged;
        public event Action<string, object> Pushed;
        public event Action<string> Closed;

        public bool IsConnected { get { return ws != null && ws.State == WebSocketState.Open; } }

        public async Task<Dictionary<string, object>> ProbeAsync(int port, string gameHint, int timeoutMs = 1500)
        {
            if (port < 1024 || port >= 65536) throw new BerryException("bad_port");
            cts = new CancellationTokenSource();
            ws = new ClientWebSocket();
            using (var timeout = new CancellationTokenSource(timeoutMs))
            {
                await ws.ConnectAsync(new Uri("ws://127.0.0.1:" + port + "/"), timeout.Token).ConfigureAwait(false);
            }
            _ = Task.Run(ReceiveLoop);
            Dictionary<string, object> reply = await RequestAsync("probe", new Dictionary<string, object> { { "game", gameHint ?? "" } }, timeoutMs).ConfigureAwait(false);
            if (BerryJson.Str(reply, "type") != "probe.ok") throw new BerryException("refused");
            return BerryJson.Child(reply, "payload");
        }

        public async Task<Dictionary<string, object>> ConnectAsync(BerrySession session, int timeoutMs = 5000)
        {
            if (session == null) throw new BerryException("not_started_by_berry");
            cts = new CancellationTokenSource();
            ws = new ClientWebSocket();
            var uri = new Uri("ws://127.0.0.1:" + session.Port + "/");
            using (var timeout = new CancellationTokenSource(timeoutMs))
            {
                await ws.ConnectAsync(uri, timeout.Token).ConfigureAwait(false);
            }
            _ = Task.Run(ReceiveLoop);
            var hello = new Dictionary<string, object> { { "token", session.Token }, { "sdk", SdkVersion }, { "game", session.GameId } };
            if (!string.IsNullOrEmpty(session.GameCode)) { hello["gameCode"] = session.GameCode; hello["gameNumber"] = session.GameNumber; }
            Dictionary<string, object> reply = await RequestAsync("hello", hello, timeoutMs).ConfigureAwait(false);
            if (BerryJson.Str(reply, "type") != "hello.ok")
                throw new BerryException(BerryJson.Str(BerryJson.Child(reply, "payload"), "code", "refused"));
            return BerryJson.Child(reply, "payload");
        }

        public async Task<Dictionary<string, object>> RequestAsync(string type, Dictionary<string, object> payload, int timeoutMs = 15000)
        {
            int id;
            var tcs = new TaskCompletionSource<Dictionary<string, object>>();
            lock (gate) { id = nextId++; pending[id] = tcs; }
            try
            {
                await SendRaw(type, id, payload).ConfigureAwait(false);
                var done = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs)).ConfigureAwait(false);
                if (done != tcs.Task) throw new BerryException("timeout");
                return tcs.Task.Result;
            }
            finally
            {
                lock (gate) pending.Remove(id);
            }
        }

        public Task FireAsync(string type, Dictionary<string, object> payload)
        {
            int id;
            lock (gate) id = nextId++;
            return SendRaw(type, id, payload);
        }

        private async Task SendRaw(string type, int id, Dictionary<string, object> payload)
        {
            if (!IsConnected && type != "hello") throw new BerryException("not_connected");
            var msg = new Dictionary<string, object> { { "v", Protocol }, { "type", type }, { "id", id }, { "payload", payload ?? new Dictionary<string, object>() } };
            byte[] bytes = Encoding.UTF8.GetBytes(BerryJson.Serialize(msg));
            if (bytes.Length > MaxMessage) throw new BerryException("too_big");
            await sendLock.WaitAsync().ConfigureAwait(false);
            try { await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cts.Token).ConfigureAwait(false); }
            finally { sendLock.Release(); }
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[8192];
            string reason = "closed";
            try
            {
                while (ws.State == WebSocketState.Open && !cts.IsCancellationRequested)
                {
                    var ms = new MemoryStream();
                    WebSocketReceiveResult r;
                    do
                    {
                        r = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token).ConfigureAwait(false);
                        if (r.MessageType == WebSocketMessageType.Close) { reason = "closed"; goto finished; }
                        ms.Write(buffer, 0, r.Count);
                        if (ms.Length > MaxMessage) { reason = "too_big"; goto finished; }
                    } while (!r.EndOfMessage);
                    Handle(Encoding.UTF8.GetString(ms.ToArray()));
                }
            }
            catch (Exception) { reason = "connection_lost"; }
        finished:
            FailPending();
            var h = Closed;
            if (h != null && !disposed) h(reason);
        }

        private void Handle(string text)
        {
            object parsed;
            var msg = BerryJson.TryParse(text, out parsed) ? parsed as Dictionary<string, object> : null;
            if (msg == null) return;
            string type = BerryJson.Str(msg, "type");
            object idObj;
            TaskCompletionSource<Dictionary<string, object>> tcs = null;
            if (msg.TryGetValue("id", out idObj) && idObj is double)
            {
                lock (gate) pending.TryGetValue((int)(double)idObj, out tcs);
            }
            if (tcs != null) { tcs.TrySetResult(msg); return; }
            object payload;
            msg.TryGetValue("payload", out payload);
            try
            {
                if (type == "ctx") { var h = ContextChanged; if (h != null) h(payload as Dictionary<string, object>); }
                else if (type == "push")
                {
                    var h = Pushed;
                    var pd = payload as Dictionary<string, object>;
                    object data = null;
                    if (pd != null) pd.TryGetValue("data", out data);
                    if (h != null) h(BerryJson.Str(payload, "name"), data);
                }
                else if (type == "bye") { var h = Closed; if (h != null) h("bye:" + BerryJson.Str(payload, "reason")); }
            }
            catch (Exception) { }
        }

        private void FailPending()
        {
            lock (gate)
            {
                foreach (var kv in pending) kv.Value.TrySetException(new BerryException("connection_lost"));
                pending.Clear();
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            try { if (cts != null) cts.Cancel(); } catch (Exception) { }
            try { if (ws != null) ws.Dispose(); } catch (Exception) { }
            FailPending();
        }
    }
}
