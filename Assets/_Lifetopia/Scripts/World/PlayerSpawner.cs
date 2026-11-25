using Lifetopia.CameraSystem;
using Lifetopia.Player;
using UnityEngine;

namespace Lifetopia.World
{
    /// <summary>
    /// Places the player on the spawn point when the scene starts and snaps the camera there,
    /// so the first frame never shows the camera sliding in from elsewhere.
    /// Move the SpawnPoint object in the Scene view to change where the player starts.
    /// </summary>
    public sealed class PlayerSpawner : MonoBehaviour
    {
        [SerializeField] private PlayerMotor2D player;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private CameraFollow2D cameraFollow;

        private void Start()
        {
            if (player == null || spawnPoint == null)
            {
                Debug.LogWarning("[PlayerSpawner] Player or SpawnPoint not assigned.", this);
                return;
            }

            player.Teleport(spawnPoint.position);
            if (cameraFollow != null) cameraFollow.SnapToTarget();
        }

        private void OnDrawGizmos()
        {
            if (spawnPoint == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(spawnPoint.position, 0.4f);
        }
    }
}
