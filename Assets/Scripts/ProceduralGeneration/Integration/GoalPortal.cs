using UnityEngine;

namespace ProceduralGeneration.Integration
{
    /// <summary>
    /// Portal ở Goal room: player nhấn phím E để qua map tiếp theo.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class GoalPortal : MonoBehaviour
    {
        [Header("Interaction")]
        [SerializeField] private KeyCode confirmKey = KeyCode.E;
        [SerializeField] private bool requirePlayerInTrigger = true;

        private DungeonRunProgressionManager progressionManager;
        private bool playerInTrigger;

        public void Setup(DungeonRunProgressionManager manager)
        {
            progressionManager = manager;
        }

        private void Awake()
        {
            Collider2D col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        private void Update()
        {
            if (progressionManager == null)
                return;

            if (requirePlayerInTrigger && !playerInTrigger)
                return;

            if (Input.GetKeyDown(confirmKey))
            {
                bool advanced = progressionManager.TryAdvanceToNextMap();
                if (!advanced)
                    Debug.Log("[GoalPortal] Chưa thể qua map tiếp theo.");
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
                playerInTrigger = true;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
                playerInTrigger = false;
        }
    }
}
