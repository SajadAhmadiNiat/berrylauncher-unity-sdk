using BerrySdk;
using UnityEngine;

namespace BerryShooter
{
    public sealed class Bootstrap : MonoBehaviour
    {
        public string gameCode = "";
        public long gameNumber;

        private void Awake()
        {
            Application.runInBackground = true;
            Tr.Init("fa");
            gameObject.AddComponent<BerryRunner>().Configure(gameCode, gameNumber);
            gameObject.AddComponent<GameApp>();
        }
    }

    public sealed class BerryRunner : MonoBehaviour
    {
        private string code = "";
        private long number;

        public void Configure(string gameCode, long gameNumber)
        {
            code = gameCode ?? "";
            number = gameNumber;
        }

        private async void Start()
        {
            var options = new BerryOptions
            {
                GameId = BerryHub.GameId,
                GameCode = code,
                GameNumber = number,
                MockWhenStandalone = true,
                MockContext = MockContext(),
            };
            bool ok = await Berry.InitAsync(options);
            if (!ok) Debug.LogWarning("[BerryShooter] Berry could not start; the game runs without it.");
        }

        private void Update() { BerryMainThread.Pump(); }

        private void OnDestroy() { Berry.Shutdown(); }

        private void OnApplicationQuit() { Berry.Shutdown(); }

        private static BerryContext MockContext()
        {
            string id = Settings.MockId;
            string user = "player-" + id;
            string shown = Settings.PlayerName.Length > 0 ? Settings.PlayerName : user;
            var c = new BerryContext { GameId = BerryHub.GameId, GameName = "Berry Shooter", Language = "fa", LauncherVersion = "mock", SdkVersion = BerryClient.SdkVersion };
            c.Permissions.AddRange(new[] { "identity", "room", "players", "presence", "achievements", "scores", "token", "network", "lan", "friends", "profile" });
            c.User = new BerryUser { Username = user, DisplayName = shown, Tier = "gold", BerryCode = "BRY-" + id, UserId = long.Parse(id) };
            c.Profile = new BerryProfile { Badge = "purple", Club = "BRY", Online = true, Country = "IR", Plan = "gold", PlanUntilUtc = System.DateTime.UtcNow.AddDays(30), Language = "fa" };
            c.Network = new BerryNetwork { State = "running", Address = "10.77.0.2", Nat = "open" };
            c.Network.Peers.Add(new BerryPeer { Username = "ali", Address = "10.77.0.3", Phase = "direct", PingMs = 24 });
            c.Network.Peers.Add(new BerryPeer { Username = "sara", Address = "10.77.0.4", Phase = "relay", PingMs = 96 });
            c.Room = new BerryRoom { Id = "mock-room", Name = "Mock room", Code = "MOCK00", MaxPlayers = 8, Role = BerryRole.Host, Mode = BerryMode.VirtualLan, HostName = shown, Owner = shown, GameId = BerryHub.GameId, GameName = "Berry Shooter", Link = "https://berry.example/join/MOCK00", Status = "waiting", Visibility = "public", MyRole = "owner" };
            c.Players.Add(new BerryPlayer { Username = user, DisplayName = shown, IsHost = true, Role = "owner", Tier = "gold", Address = "10.77.0.2", Country = "IR", BerryCode = "BRY-" + id });
            c.Players.Add(new BerryPlayer { Username = "ali", DisplayName = "Ali", Tier = "silver", Address = "10.77.0.3", Country = "IR", Role = "admin", AdminLevel = 1, Ready = true, Stats = new BerryPlayerStats { NowMs = 24, AvgMs = 26, MinMs = 18, MaxMs = 44, JitterMs = 4, LossPercent = 0, Stable = true } });
            c.Players.Add(new BerryPlayer { Username = "sara", DisplayName = "Sara", Tier = "free", Address = "10.77.0.4", Country = "DE", Stats = new BerryPlayerStats { NowMs = 96, AvgMs = 90, MinMs = 70, MaxMs = 140, JitterMs = 12, LossPercent = 1.5, Stable = false } });
            return c;
        }
    }
}
