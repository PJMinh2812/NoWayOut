using UnityEngine;

namespace NWO.Puzzle
{
    /// <summary>
    /// Door that opens when connected LightReceivers are activated
    /// Can require single or multiple receivers to be active
    /// </summary>
    public class MirrorPuzzleDoor : MonoBehaviour
    {
        [Header("Puzzle Requirements")]
        [SerializeField] private LightReceiver[] requiredReceivers; // All must be active to open
        [SerializeField] private bool requireAllReceivers = true; // If false, only need one
        
        [Header("Door Settings")]
        [SerializeField] private GameObject doorVisual;
        [SerializeField] private Collider2D doorCollider;
        [SerializeField] private float openSpeed = 2f;
        [SerializeField] private Vector3 openOffset = new Vector3(0, 3f, 0); // How far door moves when open
        
        [Header("Visual Effects")]
        [SerializeField] private Color closedColor = Color.white;
        [SerializeField] private Color openingColor = Color.green;
        [SerializeField] private ParticleSystem openEffects;
        [SerializeField] private AudioClip openSound;
        
        private bool isOpen = false;
        private Vector3 closedPosition;
        private Vector3 openPosition;
        private SpriteRenderer doorSprite;
        private AudioSource audioSource;
        
        private void Start()
        {
            if (doorVisual == null)
                doorVisual = gameObject;
            
            doorSprite = doorVisual.GetComponent<SpriteRenderer>();
            
            if (doorCollider == null)
                doorCollider = GetComponent<Collider2D>();
            
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
            
            closedPosition = doorVisual.transform.position;
            openPosition = closedPosition + openOffset;
            
            // Subscribe to receiver events
            foreach (var receiver in requiredReceivers)
            {
                if (receiver != null)
                {
                    receiver.OnActivated.AddListener(CheckPuzzleState);
                    receiver.OnDeactivated.AddListener(CheckPuzzleState);
                }
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            foreach (var receiver in requiredReceivers)
            {
                if (receiver != null)
                {
                    receiver.OnActivated.RemoveListener(CheckPuzzleState);
                    receiver.OnDeactivated.RemoveListener(CheckPuzzleState);
                }
            }
        }
        
        private void Update()
        {
            // Smoothly move door to target position
            Vector3 targetPosition = isOpen ? openPosition : closedPosition;
            doorVisual.transform.position = Vector3.Lerp(
                doorVisual.transform.position,
                targetPosition,
                Time.deltaTime * openSpeed
            );
            
            // Update visual feedback
            if (doorSprite != null)
            {
                float progress = Vector3.Distance(doorVisual.transform.position, closedPosition) /
                                Vector3.Distance(openPosition, closedPosition);
                doorSprite.color = Color.Lerp(closedColor, openingColor, progress);
            }
        }
        
        private void CheckPuzzleState()
        {
            bool shouldOpen = false;
            
            if (requireAllReceivers)
            {
                // All receivers must be activated
                shouldOpen = true;
                foreach (var receiver in requiredReceivers)
                {
                    if (receiver == null)
                        continue;
                    
                    // Check if receiver is activated (using reflection to access private field)
                    var field = receiver.GetType().GetField("isActivated", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    bool receiverActive = field != null && (bool)field.GetValue(receiver);
                    
                    if (!receiverActive)
                    {
                        shouldOpen = false;
                        break;
                    }
                }
            }
            else
            {
                // At least one receiver must be activated
                foreach (var receiver in requiredReceivers)
                {
                    if (receiver == null)
                        continue;
                    
                    var field = receiver.GetType().GetField("isActivated", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    bool receiverActive = field != null && (bool)field.GetValue(receiver);
                    
                    if (receiverActive)
                    {
                        shouldOpen = true;
                        break;
                    }
                }
            }
            
            if (shouldOpen && !isOpen)
            {
                OpenDoor();
            }
            else if (!shouldOpen && isOpen)
            {
                CloseDoor();
            }
        }
        
        private void OpenDoor()
        {
            isOpen = true;
            
            if (doorCollider != null)
            {
                doorCollider.enabled = false;
            }
            
            if (openEffects != null)
            {
                openEffects.Play();
            }
            
            if (openSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(openSound);
            }
            
            Debug.Log($"[MirrorPuzzleDoor] {gameObject.name} opened!");
        }
        
        private void CloseDoor()
        {
            isOpen = false;
            
            if (doorCollider != null)
            {
                doorCollider.enabled = true;
            }
            
            Debug.Log($"[MirrorPuzzleDoor] {gameObject.name} closed!");
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = isOpen ? Color.green : Color.red;
            Gizmos.DrawWireCube(transform.position, Vector3.one);
            
            // Draw open position
            Gizmos.color = Color.green;
            Vector3 openPos = Application.isPlaying ? openPosition : transform.position + openOffset;
            Gizmos.DrawWireCube(openPos, Vector3.one * 0.8f);
            Gizmos.DrawLine(transform.position, openPos);
        }
    }
}
