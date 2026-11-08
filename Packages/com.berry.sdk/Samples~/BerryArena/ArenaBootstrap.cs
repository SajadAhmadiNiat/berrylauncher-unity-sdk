using UnityEngine;

namespace BerrySdk.Samples
{
    public sealed class ArenaBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            var b = gameObject.AddComponent<BerryBehaviour>();
            b.gameId = "berry-arena";
            b.mockWhenStandalone = true;
            gameObject.AddComponent<ArenaGame>();
        }
    }
}
