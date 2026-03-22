using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace ProceduralGeneration.Integration
{
    /// <summary>
    /// Portal ở Goal room: player nhấn phím E để qua map tiếp theo.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class GoalPortal : MonoBehaviour
    {
        [Header("Interaction")]
        [SerializeField] private Key confirmKey = Key.E;
        [SerializeField] private bool requirePlayerInTrigger = true;

        [Header("Auto Wiring")]
        [SerializeField] private bool autoFindProgressionManager = true;

        [Header("Debug")]
        [SerializeField] private bool verboseLogs = false;

        private DungeonRunProgressionManager progressionManager;
        private bool playerInTrigger;
        private bool hasWarnedMissingManager;
        private Transform triggerPlayerTransform;
        private readonly HashSet<int> playerBodyIdsInTrigger = new HashSet<int>();

        public void Setup(DungeonRunProgressionManager manager)
        {
            progressionManager = manager;
        }

        private void Awake()
        {
            Collider2D col = GetComponent<Collider2D>();
            col.isTrigger = true;

            // Existing prefabs/scenes can deserialize this as None after field type migration.
            if (confirmKey == Key.None)
            {
                confirmKey = Key.E;
                if (verboseLogs)
                    Debug.Log("[GoalPortal] confirmKey dang la None, da fallback ve Key.E");
            }

            if (autoFindProgressionManager && progressionManager == null)
                TryResolveProgressionManager();
        }

        private void Start()
    {
            if (progressionManager == null)
            {
                TryResolveProgressionManager();
                if (progressionManager == null)
                {
                    Debug.LogWarning("[GoalPortal] Chua tim thay DungeonRunProgressionManager. Portal se khong the qua map.");
                    hasWarnedMissingManager = true;
                }
            }
        }

        private void Update()
        {
            if (progressionManager == null)
            {
                if (autoFindProgressionManager)
                    TryResolveProgressionManager();

                if (progressionManager == null)
                {
                    if (!hasWarnedMissingManager)
                    {
                        Debug.LogWarning("[GoalPortal] Khong co progression manager. Hay gan portalPrefab trong DungeonRunProgressionManager hoac dat manager trong scene.");
                        hasWarnedMissingManager = true;
                    }
                    return;
                }
            }

            if (hasWarnedMissingManager)
                hasWarnedMissingManager = false;

            if (WasConfirmPressedThisFrame())
            {
                if (requirePlayerInTrigger && !playerInTrigger)
                {
                    if (verboseLogs)
                        Debug.Log("[GoalPortal] Da nhan phim xac nhan, nhung player chua nam trong trigger.");
                    return;
                }

                if (verboseLogs)
                    Debug.Log("[GoalPortal] Nhan phim xac nhan, dang thu chuyen map...");

                Vector3 spawnAnchor = triggerPlayerTransform != null
                    ? new Vector3(triggerPlayerTransform.position.x, triggerPlayerTransform.position.y, 0f)
                    : new Vector3(transform.position.x, transform.position.y, 0f);

                if (verboseLogs)
                    Debug.Log($"[GoalPortal] Using spawn anchor {spawnAnchor}");

                bool advanced = progressionManager.TryAdvanceToNextMap(spawnAnchor);
                if (!advanced && verboseLogs)
                    Debug.Log("[GoalPortal] Chua the qua map tiep theo (map co the chua hoan thanh). ");
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsPlayerCollider(other))
            {
                int bodyId = GetPlayerBodyId(other);
                playerBodyIdsInTrigger.Add(bodyId);
                playerInTrigger = playerBodyIdsInTrigger.Count > 0;
                triggerPlayerTransform = ResolvePlayerTransform(other);

                if (verboseLogs)
                    Debug.Log($"[GoalPortal] Player da vao trigger portal. overlap={playerBodyIdsInTrigger.Count}");
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (IsPlayerCollider(other))
            {
                int bodyId = GetPlayerBodyId(other);
                playerBodyIdsInTrigger.Add(bodyId);
                playerInTrigger = playerBodyIdsInTrigger.Count > 0;
                triggerPlayerTransform = ResolvePlayerTransform(other);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (IsPlayerCollider(other))
            {
                int bodyId = GetPlayerBodyId(other);
                playerBodyIdsInTrigger.Remove(bodyId);
                playerInTrigger = playerBodyIdsInTrigger.Count > 0;
                if (!playerInTrigger)
                    triggerPlayerTransform = null;

                if (verboseLogs)
                    Debug.Log($"[GoalPortal] Player da ra khoi trigger portal. overlap={playerBodyIdsInTrigger.Count}");
            }
        }

        private void OnDisable()
        {
            playerBodyIdsInTrigger.Clear();
            playerInTrigger = false;
        }

        private static bool IsPlayerCollider(Collider2D other)
        {
            if (other.CompareTag("Player")) return true;

            Rigidbody2D attachedRb = other.attachedRigidbody;
            return attachedRb != null && attachedRb.CompareTag("Player");
        }

        private static int GetPlayerBodyId(Collider2D other)
        {
            Rigidbody2D attachedRb = other.attachedRigidbody;
            return attachedRb != null ? attachedRb.GetInstanceID() : other.GetInstanceID();
        }

        private static Transform ResolvePlayerTransform(Collider2D other)
        {
            Rigidbody2D attachedRb = other.attachedRigidbody;
            if (attachedRb != null)
                return attachedRb.transform;

            return other.transform;
        }

        private bool WasConfirmPressedThisFrame()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return false;

            if (confirmKey != Key.None && keyboard[confirmKey].wasPressedThisFrame)
                return true;

            // Fallback for migrated data or unexpected key binding values.
            return keyboard.eKey.wasPressedThisFrame
                   || keyboard.enterKey.wasPressedThisFrame
                   || keyboard.numpadEnterKey.wasPressedThisFrame;
        }

        private void TryResolveProgressionManager()
        {
            progressionManager = FindFirstObjectByType<DungeonRunProgressionManager>();
            if (progressionManager != null && verboseLogs)
                Debug.Log("[GoalPortal] Da auto-tim thay DungeonRunProgressionManager.");
        }
    }
}
