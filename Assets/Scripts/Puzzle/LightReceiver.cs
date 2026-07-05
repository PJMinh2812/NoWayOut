using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;

namespace NWO.Puzzle
{
    /// <summary>
    /// Light-activated switch that triggers doors, traps, or other mechanisms
    /// Activates when receiving reflected light from mirrors or direct light from Flash of Truth
    /// </summary>
    public class LightReceiver : MonoBehaviour
    {
        [Header("Receiver Settings")]
        [SerializeField] private bool requiresContinuousLight = true; // Stays active only while receiving light
        [SerializeField] private bool oneTimeActivation = false; // Can only be activated once
        [SerializeField] private float activationDelay = 0.5f; // Time to fully activate
        
        [Header("Visual Feedback")]
        [SerializeField] private SpriteRenderer receiverSprite;
        [SerializeField] private Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        [SerializeField] private Color activeColor = new Color(1f, 1f, 0.3f, 1f);
        [SerializeField] private Light2D indicatorLight;
        [SerializeField] private ParticleSystem activationParticles;
        
        [Header("Audio")]
        [SerializeField] private AudioClip activationSound;
        [SerializeField] private AudioClip deactivationSound;
        
        [Header("Events")]
        public UnityEvent OnActivated;
        public UnityEvent OnDeactivated;
        
        private bool isReceivingLight = false;
        private bool isActivated = false;
        private bool hasBeenActivated = false; // For one-time activation
        private float activationTimer = 0f;
        private AudioSource audioSource;
        
        private void Start()
        {
            if (receiverSprite == null)
                receiverSprite = GetComponent<SpriteRenderer>();
            
            if (indicatorLight == null)
                indicatorLight = GetComponentInChildren<Light2D>();
            
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
            
            UpdateVisuals();
        }
        
        private void Update()
        {
            if (oneTimeActivation && hasBeenActivated)
                return;
            
            // Handle activation timer
            if (isReceivingLight)
            {
                activationTimer += Time.deltaTime;
                
                if (activationTimer >= activationDelay && !isActivated)
                {
                    Activate();
                }
            }
            else if (requiresContinuousLight)
            {
                activationTimer -= Time.deltaTime * 2f; // Deactivate faster than activate
                
                if (activationTimer <= 0f && isActivated)
                {
                    Deactivate();
                }
            }
            
            activationTimer = Mathf.Clamp(activationTimer, 0f, activationDelay);
            
            UpdateVisuals();
        }
        
        /// <summary>
        /// Called by LightMirror or other light sources when light hits this receiver
        /// </summary>
        public void ReceiveLight()
        {
            isReceivingLight = true;
        }
        
        /// <summary>
        /// Called when light is no longer hitting this receiver
        /// </summary>
        public void LoseLight()
        {
            isReceivingLight = false;
        }
        
        private void Activate()
        {
            if (isActivated || (oneTimeActivation && hasBeenActivated))
                return;
            
            isActivated = true;
            hasBeenActivated = true;
            
            OnActivated?.Invoke();
            
            if (activationSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(activationSound);
            }
            
            if (activationParticles != null)
            {
                activationParticles.Play();
            }
            
            Debug.Log($"[LightReceiver] {gameObject.name} activated!");
        }
        
        private void Deactivate()
        {
            if (!isActivated || (oneTimeActivation && hasBeenActivated))
                return;
            
            isActivated = false;
            
            OnDeactivated?.Invoke();
            
            if (deactivationSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(deactivationSound);
            }
            
            Debug.Log($"[LightReceiver] {gameObject.name} deactivated!");
        }
        
        private void UpdateVisuals()
        {
            // Calculate lerp value based on activation progress
            float progress = activationTimer / activationDelay;
            
            // Update sprite color
            if (receiverSprite != null)
            {
                receiverSprite.color = Color.Lerp(inactiveColor, activeColor, progress);
            }
            
            // Update indicator light
            if (indicatorLight != null)
            {
                indicatorLight.intensity = Mathf.Lerp(0.2f, 1.5f, progress);
                indicatorLight.pointLightOuterRadius = Mathf.Lerp(2f, 5f, progress);
            }
        }
        
        /// <summary>
        /// Manual activation from other scripts (e.g., for testing or alternative triggers)
        /// </summary>
        public void ForceActivate()
        {
            activationTimer = activationDelay;
            Activate();
        }
        
        /// <summary>
        /// Manual deactivation
        /// </summary>
        public void ForceDeactivate()
        {
            if (!oneTimeActivation)
            {
                activationTimer = 0f;
                Deactivate();
            }
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = isActivated ? Color.yellow : Color.gray;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            
            if (isActivated)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, transform.position + Vector3.up * 1.5f);
            }
        }
    }
}
