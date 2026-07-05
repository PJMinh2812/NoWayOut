using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using NWO;

namespace ProceduralGeneration.Integration
{
    /// <summary>
    /// Goal chest in map x-5. Player presses E to open and claim run progression.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class GoalChest : MonoBehaviour
    {
        [Header("Interaction")]
        [SerializeField] private Key confirmKey = Key.E;
        [SerializeField] private bool requirePlayerInTrigger = true;

        [Header("Animation")]
        [Tooltip("Animator bool parameter used to switch closed/open state (Door-style)")]
        [SerializeField] private string openBoolParameter = "isOpen";
        [SerializeField] private Animator chestAnimator;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip openSfx;

        [Header("Auto Wiring")]
        [SerializeField] private bool autoFindProgressionManager = true;

        [Header("Debug")]
        [SerializeField] private bool verboseLogs = false;

        private DungeonRunProgressionManager progressionManager;
        private bool playerInTrigger;
        private bool isOpened;
        private readonly HashSet<int> playerBodies = new HashSet<int>();

        public void Setup(DungeonRunProgressionManager manager)
        {
            progressionManager = manager;
            SyncOpenedStateFromProgression();
        }

        private void Awake()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col == null)
            {
                col = gameObject.AddComponent<BoxCollider2D>();
                if (verboseLogs)
                    Debug.Log($"[GoalChest] No collider found on '{name}', added BoxCollider2D.");
            }
            col.isTrigger = true;

            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
                rb.isKinematic = true;
                if (verboseLogs)
                    Debug.Log($"[GoalChest] No Rigidbody2D found on '{name}', added kinematic Rigidbody2D.");
            }

            if (chestAnimator == null)
                chestAnimator = GetComponent<Animator>();

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if (confirmKey == Key.None)
                confirmKey = Key.E;

            if (autoFindProgressionManager && progressionManager == null)
                progressionManager = FindFirstObjectByType<DungeonRunProgressionManager>();

            SyncOpenedStateFromProgression();

            if (verboseLogs)
            {
                Debug.Log($"[GoalChest] Awake on '{name}'. requirePlayerInTrigger={requirePlayerInTrigger}, key={confirmKey}, hasAnimator={(chestAnimator != null)}, hasCollider={(col != null)}");
            }
        }

        private void Update()
        {
            if (isOpened)
                return;

            if (progressionManager == null && autoFindProgressionManager)
                progressionManager = FindFirstObjectByType<DungeonRunProgressionManager>();

            if (!WasConfirmPressedThisFrame())
                return;

            if (requirePlayerInTrigger && !playerInTrigger)
            {
                if (verboseLogs)
                    Debug.Log($"[GoalChest] Key pressed but playerInTrigger=false on '{name}'.");
                return;
            }

            if (verboseLogs)
                Debug.Log($"[GoalChest] Key pressed, trying to open '{name}'.");

            TryOpenChest();
        }

        private void TryOpenChest()
        {
            if (progressionManager == null)
            {
                Debug.LogWarning($"[GoalChest] Missing DungeonRunProgressionManager on '{name}'.");
                return;
            }

            bool opened = progressionManager.TryOpenGoalChest();
            if (!opened)
            {
                if (verboseLogs)
                {
                    Debug.Log($"[GoalChest] TryOpenGoalChest returned false. round={progressionManager.CurrentRound}, map={progressionManager.CurrentMap}, openedCount={progressionManager.OpenedGoalChestCount}/{progressionManager.TotalGoalChests}");
                }
                SyncOpenedStateFromProgression();
                return;
            }

            isOpened = true;
            ApplyVisualState();
            PlayOpenSfx();

            if (verboseLogs)
                Debug.Log("[GoalChest] Chest opened.");
        }

        private void SyncOpenedStateFromProgression()
        {
            if (progressionManager == null)
            {
                ApplyVisualState();
                return;
            }

            isOpened = progressionManager.IsCurrentRoundGoalChestOpened();
            ApplyVisualState();

            if (verboseLogs)
            {
                Debug.Log($"[GoalChest] Sync state on '{name}': isOpened={isOpened}, round={progressionManager.CurrentRound}, map={progressionManager.CurrentMap}");
            }
        }

        private void ApplyVisualState()
        {
            if (chestAnimator != null && !string.IsNullOrWhiteSpace(openBoolParameter))
                chestAnimator.SetBool(openBoolParameter, isOpened);
        }

        private void PlayOpenSfx()
        {
            if (audioSource != null && openSfx != null)
                audioSource.PlayOneShot(openSfx);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayerCollider(other))
                return;

            playerBodies.Add(GetPlayerBodyId(other));
            playerInTrigger = playerBodies.Count > 0;

            if (verboseLogs)
                Debug.Log($"[GoalChest] Player entered trigger '{name}'. overlap={playerBodies.Count}, collider='{other.name}'");
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!IsPlayerCollider(other))
                return;

            playerBodies.Add(GetPlayerBodyId(other));
            playerInTrigger = playerBodies.Count > 0;

            if (verboseLogs)
                Debug.Log($"[GoalChest] Player stay in trigger '{name}'. overlap={playerBodies.Count}, collider='{other.name}'");
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsPlayerCollider(other))
                return;

            playerBodies.Remove(GetPlayerBodyId(other));
            playerInTrigger = playerBodies.Count > 0;

            if (verboseLogs)
                Debug.Log($"[GoalChest] Player exited trigger '{name}'. overlap={playerBodies.Count}, collider='{other.name}'");
        }

        private void OnDisable()
        {
            playerBodies.Clear();
            playerInTrigger = false;
        }

        private bool WasConfirmPressedThisFrame()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return false;

            if (confirmKey != Key.None && keyboard[confirmKey].wasPressedThisFrame)
                return true;

            return keyboard.eKey.wasPressedThisFrame;
        }

        private static bool IsPlayerCollider(Collider2D other)
        {
            if (other.CompareTag("Player"))
                return true;

            Rigidbody2D attachedRb = other.attachedRigidbody;
            if (attachedRb != null && attachedRb.CompareTag("Player"))
                return true;

            if (other.transform.root != null && other.transform.root.CompareTag("Player"))
                return true;

            if (other.GetComponentInParent<PlayerController2D>() != null)
                return true;

            if (other.GetComponentInParent<PlayerHealth2D>() != null)
                return true;

            return false;
        }

        private static int GetPlayerBodyId(Collider2D other)
        {
            Rigidbody2D attachedRb = other.attachedRigidbody;
            return attachedRb != null ? attachedRb.GetInstanceID() : other.GetInstanceID();
        }
    }
}
