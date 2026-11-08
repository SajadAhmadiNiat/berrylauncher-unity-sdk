using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace BerryShooter
{
    public interface ILink : IDisposable
    {
        bool TryReceive(out string text, out string peer);
        void Send(string text, string peer);
        long BytesIn { get; }
        long BytesOut { get; }
        string Kind { get; }
    }

    public sealed class UdpLink : ILink
    {
        private UdpClient udp;
        private readonly Dictionary<string, IPEndPoint> known = new Dictionary<string, IPEndPoint>();
        private readonly IPEndPoint fixedTarget;
        public int Port { get; private set; }
        public long BytesIn { get; private set; }
        public long BytesOut { get; private set; }
        public string Kind { get { return "UDP"; } }

        private UdpLink(UdpClient c, IPEndPoint target)
        {
            udp = c;
            fixedTarget = target;
            Port = ((IPEndPoint)c.Client.LocalEndPoint).Port;
        }

        public static UdpLink Bind(int port) { return Make(new IPEndPoint(IPAddress.Any, port), null); }

        public static UdpLink Open(IPEndPoint target) { return Make(new IPEndPoint(IPAddress.Any, 0), target); }

        private static UdpLink Make(IPEndPoint local, IPEndPoint target)
        {
            var c = new UdpClient(local);
            c.Client.Blocking = false;
            try { c.Client.IOControl(-1744830452, new byte[] { 0 }, null); }
            catch (Exception) { }
            return new UdpLink(c, target);
        }

        public void Send(string text, string peer)
        {
            if (udp == null) return;
            IPEndPoint to = fixedTarget;
            if (to == null && (peer == null || !known.TryGetValue(peer, out to))) return;
            try
            {
                byte[] b = Encoding.UTF8.GetBytes(text);
                if (b.Length > Proto.MaxPacket) return;
                udp.Send(b, b.Length, to);
                BytesOut += b.Length;
            }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }
        }

        public bool TryReceive(out string text, out string peer)
        {
            text = null;
            peer = null;
            if (udp == null) return false;
            var from = new IPEndPoint(IPAddress.Any, 0);
            for (int guard = 0; guard < 8; guard++)
            {
                try
                {
                    if (udp.Available <= 0) return false;
                    byte[] b = udp.Receive(ref from);
                    BytesIn += b.Length;
                    if (b.Length == 0 || b.Length > Proto.MaxPacket) continue;
                    if (fixedTarget != null)
                    {
                        if (!from.Equals(fixedTarget)) continue;
                        peer = "host";
                    }
                    else
                    {
                        peer = "u:" + from;
                        if (!known.ContainsKey(peer))
                        {
                            if (known.Count > 256) known.Clear();
                            known[peer] = new IPEndPoint(from.Address, from.Port);
                        }
                    }
                    text = Encoding.UTF8.GetString(b);
                    return true;
                }
                catch (SocketException) { }
                catch (ObjectDisposedException) { return false; }
            }
            return false;
        }

        public void Dispose()
        {
            try { if (udp != null) udp.Close(); }
            catch (Exception) { }
            udp = null;
        }
    }

    internal sealed class TcpConn
    {
        public TcpClient Client;
        public string Key;
        public bool Dead;
        private readonly byte[] buf = new byte[4096];
        private readonly StringBuilder acc = new StringBuilder();
        private readonly Decoder decoder = Encoding.UTF8.GetDecoder();
        private readonly char[] chars = new char[4096];

        public TcpConn(TcpClient c, string key)
        {
            Client = c;
            Key = key;
            c.NoDelay = true;
            c.SendTimeout = 30;
        }

        public long Pump(Queue<string> lines)
        {
            long got = 0;
            try
            {
                NetworkStream s = Client.GetStream();
                int rounds = 0;
                while (rounds++ < 8 && s.DataAvailable)
                {
                    int n = s.Read(buf, 0, buf.Length);
                    if (n <= 0) { Dead = true; break; }
                    got += n;
                    int cn = decoder.GetChars(buf, 0, n, chars, 0);
                    for (int i = 0; i < cn; i++)
                    {
                        char ch = chars[i];
                        if (ch == '\n')
                        {
                            if (acc.Length > 0 && lines.Count < 512) lines.Enqueue(acc.ToString());
                            acc.Length = 0;
                        }
                        else if (acc.Length < Proto.MaxPacket) acc.Append(ch);
                        else { Dead = true; break; }
                    }
                }
                if (!Dead && Client.Client.Poll(0, SelectMode.SelectRead) && Client.Client.Available == 0) Dead = true;
            }
            catch (Exception) { Dead = true; }
            return got;
        }

        public long Write(string text)
        {
            if (Dead) return 0;
            try
            {
                byte[] b = Encoding.UTF8.GetBytes(text + "\n");
                Client.GetStream().Write(b, 0, b.Length);
                return b.Length;
            }
            catch (Exception) { Dead = true; return 0; }
        }

        public void Close()
        {
            try { Client.Close(); }
            catch (Exception) { }
        }
    }

    public sealed class TcpHostLink : ILink
    {
        private TcpListener listener;
        private readonly List<TcpConn> conns = new List<TcpConn>();
        private readonly Queue<KeyValuePair<string, string>> inbox = new Queue<KeyValuePair<string, string>>();
        private readonly int maxConns;
        public long BytesIn { get; private set; }
        public long BytesOut { get; private set; }
        public string Kind { get { return "TCP"; } }

        public TcpHostLink(int port, int maxConns)
        {
            this.maxConns = maxConns;
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start(8);
        }

        public bool TryReceive(out string text, out string peer)
        {
            text = null;
            peer = null;
            if (listener == null) return false;
            try
            {
                int guard = 0;
                while (guard++ < 4 && listener.Pending())
                {
                    TcpClient c = listener.AcceptTcpClient();
                    if (conns.Count >= maxConns) { c.Close(); continue; }
                    conns.Add(new TcpConn(c, "t:" + c.Client.RemoteEndPoint));
                }
            }
            catch (Exception) { }
            var lines = new Queue<string>();
            for (int i = conns.Count - 1; i >= 0; i--)
            {
                TcpConn c = conns[i];
                lines.Clear();
                BytesIn += c.Pump(lines);
                while (lines.Count > 0) inbox.Enqueue(new KeyValuePair<string, string>(c.Key, lines.Dequeue()));
                if (c.Dead)
                {
                    inbox.Enqueue(new KeyValuePair<string, string>(c.Key, "L"));
                    c.Close();
                    conns.RemoveAt(i);
                }
            }
            if (inbox.Count == 0) return false;
            var kv = inbox.Dequeue();
            peer = kv.Key;
            text = kv.Value;
            return true;
        }

        public void Send(string text, string peer)
        {
            if (peer == null) return;
            foreach (TcpConn c in conns)
                if (c.Key == peer) { BytesOut += c.Write(text); return; }
        }

        public void Dispose()
        {
            try { if (listener != null) listener.Stop(); }
            catch (Exception) { }
            listener = null;
            foreach (TcpConn c in conns) c.Close();
            conns.Clear();
        }
    }

    public sealed class TcpClientLink : ILink
    {
        private TcpClient client;
        private TcpConn conn;
        private IAsyncResult connecting;
        private readonly Queue<string> lines = new Queue<string>();
        public bool Failed { get; private set; }
        public bool Connected { get { return conn != null && !conn.Dead; } }
        public long BytesIn { get; private set; }
        public long BytesOut { get; private set; }
        public string Kind { get { return "TCP"; } }

        public TcpClientLink(IPAddress address, int port)
        {
            client = new TcpClient(AddressFamily.InterNetwork);
            try { connecting = client.BeginConnect(address, port, null, null); }
            catch (Exception) { Failed = true; }
        }

        private void Poll()
        {
            if (conn != null || Failed || connecting == null) return;
            if (!connecting.IsCompleted) return;
            try
            {
                client.EndConnect(connecting);
                conn = new TcpConn(client, "host");
            }
            catch (Exception) { Failed = true; }
        }

        public bool TryReceive(out string text, out string peer)
        {
            text = null;
            peer = "host";
            Poll();
            if (conn == null) return false;
            BytesIn += conn.Pump(lines);
            if (conn.Dead && lines.Count == 0) { Failed = true; return false; }
            if (lines.Count == 0) return false;
            text = lines.Dequeue();
            return true;
        }

        public void Send(string text, string peer)
        {
            Poll();
            if (conn != null) BytesOut += conn.Write(text);
        }

        public void Dispose()
        {
            if (conn != null) conn.Close();
            else
            {
                try { if (client != null) client.Close(); }
                catch (Exception) { }
            }
            conn = null;
            client = null;
        }
    }

    public sealed class MultiLink : ILink
    {
        private readonly List<ILink> links = new List<ILink>();
        public long BytesIn { get { long n = 0; foreach (ILink l in links) n += l.BytesIn; return n; } }
        public long BytesOut { get { long n = 0; foreach (ILink l in links) n += l.BytesOut; return n; } }
        public string Kind
        {
            get
            {
                var parts = new List<string>();
                foreach (ILink l in links) parts.Add(l.Kind);
                return string.Join("+", parts.ToArray());
            }
        }

        public MultiLink(params ILink[] parts) { foreach (ILink l in parts) if (l != null) links.Add(l); }

        public int Count { get { return links.Count; } }

        public bool TryReceive(out string text, out string peer)
        {
            foreach (ILink l in links) if (l.TryReceive(out text, out peer)) return true;
            text = null;
            peer = null;
            return false;
        }

        public void Send(string text, string peer)
        {
            if (peer == null) return;
            foreach (ILink l in links)
            {
                if (peer.StartsWith("u:") && l is UdpLink) { l.Send(text, peer); return; }
                if (peer.StartsWith("t:") && l is TcpHostLink) { l.Send(text, peer); return; }
            }
        }

        public void Dispose()
        {
            foreach (ILink l in links) l.Dispose();
            links.Clear();
        }
    }

    public sealed class ClientLink : ILink
    {
        private UdpLink udp;
        private TcpClientLink tcp;
        private readonly IPAddress address;
        private readonly int port;
        private readonly int mode;
        private readonly float tcpDelay;
        private float started = -1f;
        private string lastKind = "";
        public bool Locked { get; private set; }
        public long BytesIn { get { return (udp != null ? udp.BytesIn : 0) + (tcp != null ? tcp.BytesIn : 0) + lockedIn; } }
        public long BytesOut { get { return (udp != null ? udp.BytesOut : 0) + (tcp != null ? tcp.BytesOut : 0) + lockedOut; } }
        public string Kind { get { return Locked ? lastKind : "AUTO"; } }
        public bool TcpFailed { get { return tcp != null && tcp.Failed; } }
        private long lockedIn, lockedOut;
        private bool switched;

        public ClientLink(IPAddress address, int port, int mode, bool preferTcp)
        {
            this.address = address;
            this.port = port;
            this.mode = mode;
            tcpDelay = 2.4f;
            bool startTcp = mode == 2 || (mode == 0 && preferTcp);
            if (startTcp) tcp = new TcpClientLink(address, port);
            else udp = UdpLink.Open(new IPEndPoint(address, port));
        }

        public void Update(float now)
        {
            if (started < 0f) started = now;
            if (Locked || mode != 0 || switched) return;
            bool failed = tcp != null && tcp.Failed;
            if (now - started < tcpDelay && !failed) return;
            switched = true;
            if (tcp != null)
            {
                tcp.Dispose();
                tcp = null;
                udp = UdpLink.Open(new IPEndPoint(address, port));
            }
            else
            {
                udp.Dispose();
                udp = null;
                tcp = new TcpClientLink(address, port);
            }
        }

        public bool TryReceive(out string text, out string peer)
        {
            text = null;
            peer = "host";
            if (udp != null && udp.TryReceive(out text, out peer)) { if (!Locked) lastKind = "UDP"; return true; }
            if (tcp != null && tcp.TryReceive(out text, out peer)) { if (!Locked) lastKind = "TCP"; return true; }
            return false;
        }

        public void Send(string text, string peer)
        {
            if (Locked)
            {
                if (lastKind == "UDP" && udp != null) udp.Send(text, "host");
                else if (lastKind == "TCP" && tcp != null) tcp.Send(text, "host");
                return;
            }
            if (udp != null) udp.Send(text, "host");
            if (tcp != null) tcp.Send(text, "host");
        }

        public void Lock()
        {
            if (Locked || lastKind.Length == 0) return;
            Locked = true;
            if (lastKind == "UDP" && tcp != null) { lockedIn += tcp.BytesIn; lockedOut += tcp.BytesOut; tcp.Dispose(); tcp = null; }
            if (lastKind == "TCP" && udp != null) { lockedIn += udp.BytesIn; lockedOut += udp.BytesOut; udp.Dispose(); udp = null; }
        }

        public void Dispose()
        {
            if (udp != null) udp.Dispose();
            if (tcp != null) tcp.Dispose();
            udp = null;
            tcp = null;
        }
    }
}
