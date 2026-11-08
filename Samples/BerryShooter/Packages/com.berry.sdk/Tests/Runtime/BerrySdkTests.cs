using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace BerrySdk.Tests
{
    public class BerrySdkTests
    {
        [Test]
        public void Json_RoundTrip()
        {
            var d = new Dictionary<string, object> { { "a", 1 }, { "b", "x\"y\n" }, { "c", new List<object> { true, null, 2.5 } } };
            var back = (Dictionary<string, object>)BerryJson.Parse(BerryJson.Serialize(d));
            Assert.AreEqual(1.0, back["a"]);
            Assert.AreEqual("x\"y\n", back["b"]);
            Assert.AreEqual(3, ((List<object>)back["c"]).Count);
        }

        [Test]
        public void Json_RejectsGarbage()
        {
            object v;
            Assert.IsFalse(BerryJson.TryParse("{\"a\":", out v));
            Assert.IsFalse(BerryJson.TryParse("[1,2", out v));
            Assert.IsFalse(BerryJson.TryParse("{} x", out v));
            Assert.IsFalse(BerryJson.TryParse(new string('[', 100), out v));
        }

        [Test]
        public void Json_Unicode()
        {
            var o = (Dictionary<string, object>)BerryJson.Parse("{\"n\":\"\\u062a\\u0645\\u0634\\u06a9\"}");
            Assert.AreEqual("تمشک", o["n"]);
        }

        [Test]
        public void Context_ParsesOnlyWhatIsThere()
        {
            string json = "{\"game\":{\"id\":\"g\",\"name\":\"G\"},\"launcher\":{\"lang\":\"en\"},\"permissions\":[\"identity\",\"room\"],"
                + "\"identity\":{\"username\":\"ali\",\"displayName\":\"Ali\",\"tier\":\"gold\"},"
                + "\"room\":{\"id\":\"r1\",\"role\":\"guest\",\"mode\":\"vlan\",\"maxPlayers\":4,\"hostAddress\":\"10.77.0.2\"}}";
            var c = BerryContext.FromJson((Dictionary<string, object>)BerryJson.Parse(json));
            Assert.AreEqual("ali", c.User.Username);
            Assert.AreEqual(BerryRole.Guest, c.Room.Role);
            Assert.AreEqual(BerryMode.VirtualLan, c.Room.Mode);
            Assert.AreEqual("10.77.0.2", c.Room.HostAddress);
            Assert.IsTrue(c.Has("room"));
            Assert.IsFalse(c.Has("players"));
            Assert.AreEqual(0, c.Players.Count);
        }

        [Test]
        public void Session_FromEnvironment()
        {
            var env = new Dictionary<string, string> { { "BERRY_SDK_PORT", "5555" }, { "BERRY_SDK_TOKEN", "abc" }, { "BERRY_SDK_GAME", "g" } };
            var s = BerrySession.Discover("", k => env.ContainsKey(k) ? env[k] : null);
            Assert.AreEqual(5555, s.Port);
            Assert.AreEqual("abc", s.Token);
            Assert.IsNull(BerrySession.Discover("", k => null));
            env["BERRY_SDK_PORT"] = "99999";
            Assert.IsNull(BerrySession.Discover("", k => env.ContainsKey(k) ? env[k] : null));
        }

        [Test]
        public void Session_FromFile_OnlyForPlainIds()
        {
            string file = null;
            var s = BerrySession.Discover("my-game", k => k == "BERRY_CONFIG_DIR" ? "/cfg" : null, p => { file = p; return "{\"port\":4444,\"token\":\"t\"}"; });
            Assert.AreEqual(4444, s.Port);
            StringAssert.EndsWith("my-game.json", file);
            Assert.IsNull(BerrySession.Discover("../evil", k => null, p => "{\"port\":1,\"token\":\"t\"}"));
        }

        [Test]
        public void Context_Sdk12_ProfileAndNetwork()
        {
            var j = (Dictionary<string, object>)BerryJson.Parse("{\"sdk\":{\"protocol\":1,\"version\":\"1.2.0\"},\"profile\":{\"badge\":\"purple\",\"club\":\"ABC\",\"online\":true},\"network\":{\"state\":\"running\",\"address\":\"10.77.0.2\",\"peers\":[{\"username\":\"ali\",\"address\":\"10.77.0.3\",\"phase\":\"direct\",\"pingMs\":12}]}}");
            var c = BerryContext.FromJson(j);
            Assert.AreEqual("1.2.0", c.SdkVersion);
            Assert.AreEqual("purple", c.Profile.Badge);
            Assert.AreEqual("ABC", c.Profile.Club);
            Assert.IsTrue(c.Network.IsRunning);
            Assert.AreEqual("10.77.0.3", c.Network.Peers[0].Address);
            Assert.AreEqual(12, c.Network.Peers[0].PingMs);
        }

        [Test]
        public void RoomState_FromJson()
        {
            var j = (Dictionary<string, object>)BerryJson.Parse("{\"inRoom\":true,\"id\":\"r1\",\"code\":\"ABC234\",\"name\":\"Room\",\"maxPlayers\":4,\"host\":true}");
            var r = BerryRoomState.FromJson(j);
            Assert.IsTrue(r.InRoom);
            Assert.AreEqual("ABC234", r.Code);
            Assert.IsTrue(r.IsHost);
            Assert.IsFalse(BerryRoomState.FromJson(null).InRoom);
        }

        [Test]
        public void Util_TiersBadgesAndPing()
        {
            Assert.AreEqual(0, BerryUtil.TierRank("free"));
            Assert.AreEqual(4, BerryUtil.TierRank("Diamond"));
            Assert.IsTrue(BerryUtil.IsSubscriber("silver"));
            Assert.IsFalse(BerryUtil.IsSubscriber(null));
            Assert.AreEqual("#FACC15", BerryUtil.BadgeColorHex("gold"));
            Assert.AreEqual("[ABC] Ali", BerryUtil.NameWithClub("Ali", new BerryProfile { Club = "ABC" }));
            Assert.AreEqual(BerryPingQuality.Unknown, BerryUtil.PingQuality(-1));
            Assert.AreEqual(BerryPingQuality.Excellent, BerryUtil.PingQuality(25));
            Assert.AreEqual(BerryPingQuality.Poor, BerryUtil.PingQuality(400));
            Assert.AreEqual("--", BerryUtil.PingText(-1));
            var n = new BerryNetwork();
            n.Peers.Add(new BerryPeer { Username = "ali", PingMs = 30 });
            n.Peers.Add(new BerryPeer { Username = "sara", PingMs = 120 });
            Assert.AreEqual(30, BerryUtil.PeerPing(n, "ALI"));
            Assert.AreEqual(-1, BerryUtil.PeerPing(n, "nobody"));
            Assert.AreEqual(120, BerryUtil.WorstPing(n));
        }

        [Test]
        public void Util_AddressesAndRoomChecks()
        {
            Assert.IsTrue(BerryUtil.IsPrivateAddress("10.77.0.3"));
            Assert.IsTrue(BerryUtil.IsPrivateAddress("192.168.1.5"));
            Assert.IsFalse(BerryUtil.IsPrivateAddress("8.8.8.8"));
            Assert.IsFalse(BerryUtil.IsPrivateAddress("10.0.0"));
            Assert.IsNull(BerryUtil.ValidateRoom("My room", 8, null));
            Assert.AreEqual("bad_request", BerryUtil.ValidateRoom("ab", 8, null));
            Assert.AreEqual("bad_request", BerryUtil.ValidateRoom("My room", 1, null));
            Assert.AreEqual("bad_request", BerryUtil.ValidateRoom("My room", 8, "123"));
            var c = new BerryContext { Room = new BerryRoom { Role = BerryRole.Guest, HostAddress = "10.77.0.2" } };
            Assert.AreEqual("10.77.0.2", BerryUtil.HostAddressToConnect(c));
            c.Room.Role = BerryRole.Host;
            Assert.IsNull(BerryUtil.HostAddressToConnect(c));
            Assert.IsTrue(BerryUtil.IsInRoom(c));
            Assert.IsTrue(BerryUtil.ErrorText("denied", "fa").Length > 0);
        }

        [Test]
        public void Context_ExtendedFields()
        {
            string json = "{\"game\":{\"id\":\"g\",\"name\":\"G\"},\"launcher\":{\"version\":\"1.0.59\",\"lang\":\"en\",\"open\":true,\"loggedIn\":true,\"official\":true,\"uptimeSec\":120,\"build\":\"59\"},"
                + "\"sdk\":{\"version\":\"1.4.0\"},\"permissions\":[\"identity\",\"room\",\"players\",\"profile\"],"
                + "\"identity\":{\"username\":\"ali\",\"displayName\":\"<b>Ali</b>\",\"tier\":\"gold\",\"berryCode\":\"ABCDE-12345\",\"userId\":22},"
                + "\"profile\":{\"badge\":\"purple\",\"club\":\"BRY\",\"country\":\"IR\",\"plan\":\"gold\",\"planUntil\":4102444700000,\"staff\":false},"
                + "\"room\":{\"id\":\"r1\",\"name\":\"R\",\"code\":\"ABC234\",\"maxPlayers\":4,\"role\":\"guest\",\"mode\":\"vlan\",\"hostAddress\":\"10.77.0.2\",\"visibility\":\"public\",\"locked\":true,\"link\":\"https://x/join/ABC234\",\"myRole\":\"admin\",\"myAdminLevel\":2,\"tags\":[\"a\",\"b\"]},"
                + "\"players\":[{\"username\":\"ali\",\"displayName\":\"Ali\",\"host\":true,\"role\":\"owner\",\"country\":\"IR\",\"serverPingMs\":25,\"stats\":{\"avgMs\":23,\"jitterMs\":4,\"lossPct\":0,\"stable\":true}},{\"username\":\"sara\",\"role\":\"admin\",\"adminLevel\":2,\"adminPerms\":[\"kick\"]}]}";
            var c = BerryContext.FromJson((Dictionary<string, object>)BerryJson.Parse(json));
            Assert.IsTrue(c.Launcher.Open || c.Launcher.LoggedIn);
            Assert.IsTrue(c.Launcher.Official);
            Assert.AreEqual(120, c.Launcher.UptimeSec);
            Assert.AreEqual("bAli/b", c.User.DisplayName);
            Assert.AreEqual(22, c.User.UserId);
            Assert.AreEqual("IR", c.Profile.Country);
            Assert.IsTrue(c.Profile.DaysLeft > 100);
            Assert.AreEqual("public", c.Room.Visibility);
            Assert.IsTrue(c.Room.Locked);
            Assert.AreEqual(2, c.Room.MyAdminLevel);
            Assert.AreEqual(2, c.Room.Tags.Count);
            Assert.AreEqual(2, c.Players.Count);
            Assert.IsTrue(c.Players[0].IsOwner);
            Assert.AreEqual(4, c.Players[0].Stats.JitterMs);
            Assert.AreEqual(-1, c.Players[1].Stats.AvgMs);
            Assert.IsTrue(c.Players[1].IsAdmin);
            Assert.AreEqual("sara", c.Players[1].DisplayName);
            Assert.IsNotNull(c.FindPlayer("ali"));
        }

        [Test]
        public void Context_V14Fields()
        {
            string json = "{\"game\":{\"id\":\"g\",\"name\":\"G\",\"path\":\"D:/g\",\"installedVersion\":\"1.2\",\"source\":\"berry\",\"downloadedFromBerry\":true,\"defined\":true,\"free\":false,\"port\":27015,\"protocol\":\"both\"},"
                + "\"launcher\":{\"open\":true,\"theme\":\"light\",\"voice\":{\"joined\":true,\"channel\":\"room\"}},"
                + "\"permissions\":[\"identity\",\"room\",\"players\",\"profile\"],\"identity\":{\"username\":\"ali\"},"
                + "\"profile\":{\"staff\":true,\"hasProfile\":true,\"followers\":7},"
                + "\"room\":{\"id\":\"r\",\"role\":\"host\",\"maxPlayers\":4,\"isOpen\":true,\"inGame\":false,\"memberCount\":2,\"relay\":true,\"recommendedTransport\":\"tcp\",\"voice\":{\"active\":true,\"open\":true,\"count\":2,\"meIn\":true}},"
                + "\"players\":[{\"username\":\"ali\",\"isSelf\":true,\"ready\":true,\"connectedAt\":1700000000000,\"connection\":{\"method\":\"relay\",\"phase\":\"relay\",\"relay\":true,\"rttMs\":83}}]}";
            var c = BerryContext.FromJson((Dictionary<string, object>)BerryJson.Parse(json));
            Assert.IsTrue(c.Game.IsDownloadedFromBerry);
            Assert.IsTrue(c.Game.IsDefined);
            Assert.IsFalse(c.Game.IsFree);
            Assert.IsTrue(c.Game.UsesUdp && c.Game.UsesTcp);
            Assert.AreEqual(27015, c.Game.Port);
            Assert.IsFalse(c.Launcher.IsDark);
            Assert.IsTrue(c.Launcher.IsInVoice);
            Assert.IsTrue(c.Profile.IsAdmin);
            Assert.IsTrue(c.Profile.HasProfile);
            Assert.AreEqual(7, c.Profile.Followers);
            Assert.IsTrue(c.Room.IsOpen);
            Assert.IsTrue(c.Room.IsVoiceActive);
            Assert.IsTrue(c.Room.IsRelayed);
            Assert.AreEqual("tcp", c.Room.RecommendedTransport);
            Assert.IsTrue(c.Players[0].IsSelf && c.Players[0].IsReady && c.Players[0].IsRelayed);
            Assert.AreEqual(83, c.Players[0].Connection.RttMs);
            Assert.IsTrue(c.Players[0].ConnectedUtc > DateTime.MinValue);
        }

        [Test]
        public void Text_CleanRemovesControlAndTags()
        {
            Assert.AreEqual("abc", BerryText.Clean("a<b>c\u0001", 10));
            Assert.AreEqual("ab", BerryText.Clean("abcdef", 2));
            Assert.AreEqual("", BerryText.Clean(null, 5));
        }

        [Test]
        public void Probe_ReadsOnlyValidPorts()
        {
            System.Func<string, string> env = k => k == "BERRY_CONFIG_DIR" ? "cfg" : null;
            Assert.AreEqual(5555, BerryProbe.ReadPort(env, p => "{\"port\":5555,\"pid\":1}"));
            Assert.AreEqual(0, BerryProbe.ReadPort(env, p => "{\"port\":80}"));
            Assert.AreEqual(0, BerryProbe.ReadPort(env, p => "garbage"));
            Assert.AreEqual(0, BerryProbe.ReadPort(env, p => null));
        }

        [Test]
        public void Util_TransportAndTexts()
        {
            Assert.AreEqual(BerryTransport.Udp, BerryUtil.ChooseTransport(BerryTransport.Auto, null));
            Assert.AreEqual(BerryTransport.Tcp, BerryUtil.ChooseTransport(BerryTransport.Tcp, null));
            Assert.AreEqual(BerryTransport.Udp, BerryUtil.ChooseTransport(BerryTransport.Tcp, null, true, false));
            var c = new BerryContext { Network = new BerryNetwork() };
            c.Network.Peers.Add(new BerryPeer { Username = "x", Phase = "relay" });
            Assert.AreEqual(BerryTransport.Tcp, BerryUtil.ChooseTransport(BerryTransport.Auto, c));
            Assert.AreEqual("none", BerryUtil.ExpiryText(System.DateTime.MinValue));
            Assert.AreEqual("Owner", BerryUtil.RoleText("owner", 0));
            Assert.IsTrue(BerryUtil.LinkStateText(BerryLinkState.LauncherClosed, "fa").Length > 0);
            Assert.AreEqual("Free", BerryUtil.RoomTypeText(new BerryRoom { Free = true }));
        }
    }
}
