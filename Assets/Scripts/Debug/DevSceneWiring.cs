using UnityEngine;

namespace NWO
{
    /// <summary>
    /// Convenience wiring for a quick playable test:
    /// - Finds PlayerController2D in scene
    /// - Assigns CameraFollow2D target
    /// </summary>
    public sealed class DevSceneWiring : MonoBehaviour
    {
        [SerializeField] private CameraFollow2D cameraFollow;
        [SerializeField] private PlayerController2D player;

        private void Awake()
        {
            if (player == null) player = FindFirstObjectByType<PlayerController2D>();
            if (cameraFollow == null) cameraFollow = FindFirstObjectByType<CameraFollow2D>();

            if (player != null && cameraFollow != null)
            {
                cameraFollow.SetTarget(player.transform);
            }
        }
    }
}


