using UnityEngine;

namespace ProceduralGeneration.Components
{
    /// <summary>
    /// Component đại diện cho một Door trong room
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class DoorController : MonoBehaviour
    {
        [Header("State")]
        [Tooltip("Cửa hiện tại có mở không?")]
        public bool isOpen = false;
        
        [Tooltip("Cửa có bị khóa không?")]
        public bool isLocked = false;
        
        [Header("Visual")]
        [Tooltip("GameObject hiển thị khi cửa đóng")]
        public GameObject closedVisual;
        
        [Tooltip("GameObject hiển thị khi cửa mở")]
        public GameObject openVisual;
        
        [Header("Collision")]
        [Tooltip("Collider sẽ disable khi cửa mở")]
        public Collider2D doorCollider;
        
        [Header("Animation")]
        [Tooltip("Animator cho animation mở/đóng cửa")]
        public Animator doorAnimator;
        
        [Header("Audio")]
        [Tooltip("Sound khi mở cửa")]
        public AudioClip openSound;
        
        [Tooltip("Sound khi đóng cửa")]
        public AudioClip closeSound;
        
        [Tooltip("Sound khi cửa bị khóa")]
        public AudioClip lockedSound;
        
        private AudioSource audioSource;
        
        private void Awake()
        {
            if (doorCollider == null)
                doorCollider = GetComponent<Collider2D>();
            
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
            
            UpdateVisuals();
        }
        
        /// <summary>
        /// Mở cửa
        /// </summary>
        public void OpenDoor()
        {
            if (isLocked)
            {
                PlaySound(lockedSound);
                Debug.Log("Door is locked!");
                return;
            }
            
            isOpen = true;
            UpdateVisuals();
            PlayAnimation("Open");
            PlaySound(openSound);
        }
        
        /// <summary>
        /// Đóng cửa
        /// </summary>
        public void CloseDoor()
        {
            isOpen = false;
            UpdateVisuals();
            PlayAnimation("Close");
            PlaySound(closeSound);
        }
        
        /// <summary>
        /// Toggle cửa
        /// </summary>
        public void ToggleDoor()
        {
            if (isOpen)
                CloseDoor();
            else
                OpenDoor();
        }
        
        /// <summary>
        /// Khóa cửa
        /// </summary>
        public void LockDoor()
        {
            isLocked = true;
            CloseDoor();
        }
        
        /// <summary>
        /// Mở khóa cửa
        /// </summary>
        public void UnlockDoor()
        {
            isLocked = false;
        }
        
        /// <summary>
        /// Update visuals dựa trên state
        /// </summary>
        private void UpdateVisuals()
        {
            if (closedVisual != null)
                closedVisual.SetActive(!isOpen);
            
            if (openVisual != null)
                openVisual.SetActive(isOpen);
            
            if (doorCollider != null)
                doorCollider.enabled = !isOpen;
        }
        
        /// <summary>
        /// Play animation
        /// </summary>
        private void PlayAnimation(string triggerName)
        {
            if (doorAnimator != null)
            {
                doorAnimator.SetTrigger(triggerName);
            }
        }
        
        /// <summary>
        /// Play sound
        /// </summary>
        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = isLocked ? Color.red : (isOpen ? Color.green : Color.yellow);
            Gizmos.DrawWireCube(transform.position, new Vector3(1f, 2f, 0.1f));
        }
    }
}
