using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using System.Collections;
using System.Collections.Generic;

namespace NWO
{
    /// <summary>
    /// Flash of Truth ability - unlocked after collecting all 3 Light Fragments
    /// Press Space to activate: burst of light for 5s, reveals traps, stuns enemies
    /// </summary>
    public class FlashOfTruth : MonoBehaviour
    {
        [Header("Flash Settings")]
        [SerializeField] private float flashDuration = 5f;
        [SerializeField] private float flashLightRadius = 50f; // Massive light burst
        [SerializeField] private float flashIntensity = 3f;
        [SerializeField] private Color flashColor = Color.white;
        
        [Header("Cooldown")]
        [SerializeField] private float cooldownTime = 15f;
        
        [Header("Effects")]
        [SerializeField] private float enemyStunDuration = 3f;
        [SerializeField] private Color trapRevealColor = new Color(1f, 0.3f, 0.3f, 0.8f);
        
        private bool isUnlocked = false;
        private bool isOnCooldown = false;
        private bool isFlashActive = false;
        private float cooldownTimer = 0f;
        
        private Light2D playerLight;
        private float originalRadius;
        private float originalIntensity;
        private Color originalColor;
        
        public bool IsUnlocked => isUnlocked;
        public bool IsOnCooldown => isOnCooldown;
        public float CooldownProgress => isOnCooldown ? (1f - cooldownTimer / cooldownTime) : 1f;
        public bool IsFlashActive => isFlashActive;
        
        private void Start()
        {
            // Get player light reference
            var lightingManager = DungeonLightingManager.Instance;
            if (lightingManager != null)
            {
                playerLight = lightingManager.GetPlayerLight();
            }
            
            // Subscribe to fragment collection
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnAllLightFragmentsCollected += OnAllFragmentsCollected;
                
                // Check if already collected
                if (GameManager.Instance.LightFragmentsCollected >= GameManager.Instance.TotalLightFragments)
                {
                    UnlockAbility();
                }
            }
        }
        
        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnAllLightFragmentsCollected -= OnAllFragmentsCollected;
            }
        }
        
        private void Update()
        {
            // Update cooldown timer
            if (isOnCooldown)
            {
                cooldownTimer -= Time.deltaTime;
                if (cooldownTimer <= 0f)
                {
                    isOnCooldown = false;
                    cooldownTimer = 0f;
                }
            }
            
            // Check for activation input (using new Input System)
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame && CanActivate())
            {
                ActivateFlash();
            }
        }
        
        private void OnAllFragmentsCollected()
        {
            UnlockAbility();
        }
        
        private void UnlockAbility()
        {
            isUnlocked = true;
        }
        
        private bool CanActivate()
        {
            return isUnlocked && !isOnCooldown && !isFlashActive;
        }
        
        private void ActivateFlash()
        {
            StartCoroutine(FlashSequence());
        }
        
        private IEnumerator FlashSequence()
        {
            isFlashActive = true;
            isOnCooldown = true;
            cooldownTimer = cooldownTime;
            
            // Store original light settings
            if (playerLight != null)
            {
                originalRadius = playerLight.pointLightOuterRadius;
                originalIntensity = playerLight.intensity;
                originalColor = playerLight.color;
                
                // Instant burst
                playerLight.pointLightOuterRadius = flashLightRadius;
                playerLight.pointLightInnerRadius = flashLightRadius * 0.6f;
                playerLight.intensity = flashIntensity;
                playerLight.color = flashColor;
            }
            
            // Reveal traps
            RevealTraps();
            
            // Stun enemies
            StunEnemies();
            
            // Activate light receivers (for mirror puzzles)
            ActivateLightReceivers();
            
            // Wait for flash duration
            yield return new WaitForSeconds(flashDuration);
            
            // Fade back to normal over 0.5s
            if (playerLight != null)
            {
                float fadeTime = 0.5f;
                float elapsed = 0f;
                
                while (elapsed < fadeTime)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / fadeTime;
                    
                    playerLight.pointLightOuterRadius = Mathf.Lerp(flashLightRadius, originalRadius, t);
                    playerLight.pointLightInnerRadius = Mathf.Lerp(flashLightRadius * 0.6f, originalRadius * 0.3f, t);
                    playerLight.intensity = Mathf.Lerp(flashIntensity, originalIntensity, t);
                    playerLight.color = Color.Lerp(flashColor, originalColor, t);
                    
                    yield return null;
                }
                
                playerLight.pointLightOuterRadius = originalRadius;
                playerLight.pointLightInnerRadius = originalRadius * 0.3f;
                playerLight.intensity = originalIntensity;
                playerLight.color = originalColor;
            }
            
            isFlashActive = false;
        }
        
        private void RevealTraps()
        {
            // Find all GameObjects with "Trap" in name or tag
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            
            foreach (var obj in allObjects)
            {
                // Check name, tag, or component type
                bool isTrap = obj.name.Contains("Trap") || 
                             obj.CompareTag("Trap") ||
                             obj.GetComponent<MonoBehaviour>()?.GetType().Name.Contains("Trap") == true;
                
                if (isTrap)
                {
                    StartCoroutine(HighlightObject(obj, flashDuration));
                }
            }
        }
        
        private IEnumerator HighlightObject(GameObject obj, float duration)
        {
            var renderers = obj.GetComponentsInChildren<SpriteRenderer>();
            if (renderers.Length == 0) yield break;
            
            // Store original colors
            var originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                originalColors[i] = renderers[i].color;
                renderers[i].color = trapRevealColor;
            }
            
            // Flash effect
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = 0.5f + 0.5f * Mathf.Sin(elapsed * 10f);
                
                foreach (var renderer in renderers)
                {
                    if (renderer != null)
                    {
                        var c = trapRevealColor;
                        c.a = alpha;
                        renderer.color = c;
                    }
                }
                
                yield return null;
            }
            
            // Restore original colors
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].color = originalColors[i];
                }
            }
        }
        
        private void StunEnemies()
        {
            // Find all GameObjects with "Enemy" in name or tag
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            
            foreach (var obj in allObjects)
            {
                // Check name, tag, or component type
                bool isEnemy = obj.name.Contains("Enemy") || 
                              obj.CompareTag("Enemy") ||
                              obj.GetComponent<MonoBehaviour>()?.GetType().Name.Contains("Enemy") == true;
                
                if (isEnemy)
                {
                    StartCoroutine(StunEnemy(obj));
                }
            }
        }
        
        private IEnumerator StunEnemy(GameObject enemy)
        {
            // Disable enemy AI/movement
            var enemyScript = enemy.GetComponent<MonoBehaviour>();
            if (enemyScript != null)
            {
                // Try to disable enemy behavior
                enemyScript.enabled = false;
            }
            
            // Visual feedback - flash white
            var renderers = enemy.GetComponentsInChildren<SpriteRenderer>();
            var originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                originalColors[i] = renderers[i].color;
            }
            
            float elapsed = 0f;
            while (elapsed < enemyStunDuration)
            {
                elapsed += Time.deltaTime;
                float flash = Mathf.PingPong(elapsed * 5f, 1f);
                
                foreach (var renderer in renderers)
                {
                    if (renderer != null)
                    {
                        renderer.color = Color.Lerp(originalColors[0], Color.white, flash * 0.5f);
                    }
                }
                
                yield return null;
            }
            
            // Restore
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].color = originalColors[i];
                }
            }
            
            if (enemyScript != null)
            {
                enemyScript.enabled = true;
            }
        }
        
        private void ActivateLightReceivers()
        {
            // Find all LightReceivers in flash range
            var receivers = FindObjectsByType<Puzzle.LightReceiver>(FindObjectsSortMode.None);
            
            foreach (var receiver in receivers)
            {
                float distance = Vector2.Distance(transform.position, receiver.transform.position);
                
                // If receiver is within flash radius, activate it for the flash duration
                if (distance <= flashLightRadius)
                {
                    StartCoroutine(TemporarilyActivateReceiver(receiver, flashDuration));
                }
            }
        }
        
        private IEnumerator TemporarilyActivateReceiver(Puzzle.LightReceiver receiver, float duration)
        {
            // Activate receiver
            receiver.ReceiveLight();
            
            // Keep it active for flash duration
            float elapsed = 0f;
            while (elapsed < duration)
            {
                receiver.ReceiveLight(); // Keep calling to maintain activation
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Let it deactivate naturally
            receiver.LoseLight();
        }
    }
}
