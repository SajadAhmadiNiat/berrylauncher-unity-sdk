using UnityEngine;

namespace BerrySdk
{
    [DisallowMultipleComponent]
    public sealed class BerryBehaviour : MonoBehaviour
    {
        [Tooltip("The id from your berry-game.json")]
        public string gameId = "";
        [Tooltip("Games of the Berry developer platform: the game code from your dashboard (like BG-ABCD-EFGH). Leave empty for other games.")]
        public string gameCode = "";
        [Tooltip("Games of the Berry developer platform: the 6-digit game number from your dashboard.")]
        public long gameNumber;
        [Tooltip("Use fake data when the game is not started by the Berry launcher (editor / testing).")]
        public bool mockWhenStandalone = true;
        [Header("Mock data (editor only)")]
        public string mockUsername = "mock_player";
        public bool mockIsHost = true;

        private static BerryBehaviour instance;

        private async void Start()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            var opts = new BerryOptions { GameId = gameId, GameCode = gameCode, GameNumber = gameNumber, MockWhenStandalone = mockWhenStandalone, MockName = mockUsername, MockIsHost = mockIsHost };
            await Berry.InitAsync(opts);
        }

        private void Update() { BerryMainThread.Pump(); }
        private void OnDestroy() { if (instance == this) { Berry.Shutdown(); instance = null; } }
        private void OnApplicationQuit() { Berry.Shutdown(); }
    }
}
