using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace NWO.Puzzle
{
    /// <summary>
    /// Mirror that reflects light from player's lantern to activate switches
    /// Reflects light at 45° or 90° angles based on rotation
    /// </summary>
    public class LightMirror : MonoBehaviour
    {
        [Header("Mirror Settings")]
        [SerializeField] private float maxReflectionDistance = 15f;
        [SerializeField] private float detectionRadius = 8f; // Range to detect player light
        [SerializeField] private LayerMask obstacleLayer; // Walls that block light
        
        [Header("Visual")]
        [SerializeField] private LineRenderer beamRenderer;
        [SerializeField] private Color beamColor = new Color(1f, 1f, 0.5f, 0.8f);
        [SerializeField] private float beamWidth = 0.2f;
        [SerializeField] private SpriteRenderer mirrorSprite;
        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        
        private Light2D playerLight;
        private Transform playerTransform;
        private bool isActivated = false;
        private Vector2 reflectionEndPoint;
        private LightReceiver hitReceiver;
        
        private void Start()
        {
            SetupBeamRenderer();
            FindPlayerLight();
            
            if (mirrorSprite == null)
                mirrorSprite = GetComponent<SpriteRenderer>();
        }
        
        private void Update()
        {
            CheckPlayerLightInRange();
            
            if (isActivated)
            {
                CalculateReflection();
                UpdateBeamVisualization();
            }
            else
            {
                HideBeam();
            }
            
            UpdateMirrorAppearance();
        }
        
        private void SetupBeamRenderer()
        {
            if (beamRenderer == null)
            {
                GameObject beamObj = new GameObject("LightBeam");
                beamObj.transform.SetParent(transform);
                beamObj.transform.localPosition = Vector3.zero;
                
                beamRenderer = beamObj.AddComponent<LineRenderer>();
                beamRenderer.startWidth = beamWidth;
                beamRenderer.endWidth = beamWidth;
                beamRenderer.material = new Material(Shader.Find("Sprites/Default"));
                beamRenderer.startColor = beamColor;
                beamRenderer.endColor = beamColor;
                beamRenderer.positionCount = 2;
                beamRenderer.sortingOrder = 5;
            }
        }
        
        private void FindPlayerLight()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                playerLight = player.GetComponentInChildren<Light2D>();
            }
        }
        
        private void CheckPlayerLightInRange()
        {
            if (playerLight == null || playerTransform == null)
            {
                FindPlayerLight();
                isActivated = false;
                return;
            }
            
            // Check if player is in range
            float distance = Vector2.Distance(transform.position, playerTransform.position);
            
            // Mirror activates when player is close enough and light is on
            if (distance <= detectionRadius && playerLight.intensity > 0.1f)
            {
                // Check if there's line of sight to player
                Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;
                RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToPlayer, distance, obstacleLayer);
                
                isActivated = (hit.collider == null); // Activated if no obstacle
            }
            else
            {
                isActivated = false;
            }
        }
        
        private void CalculateReflection()
        {
            // Get direction from player to mirror
            Vector2 incomingDirection = (transform.position - playerTransform.position).normalized;
            
            // Calculate reflection based on mirror's rotation
            float mirrorAngleRad = transform.eulerAngles.z * Mathf.Deg2Rad;
            Vector2 mirrorNormal = new Vector2(-Mathf.Sin(mirrorAngleRad), Mathf.Cos(mirrorAngleRad));
            
            // Reflect the incoming direction
            Vector2 reflectedDirection = Vector2.Reflect(incomingDirection, mirrorNormal);
            
            // Cast ray in reflected direction
            RaycastHit2D hit = Physics2D.Raycast(transform.position, reflectedDirection, maxReflectionDistance, obstacleLayer);
            
            if (hit.collider != null)
            {
                reflectionEndPoint = hit.point;
                
                // Check if hit a light receiver
                hitReceiver = hit.collider.GetComponent<LightReceiver>();
                if (hitReceiver != null)
                {
                    hitReceiver.ReceiveLight();
                }
            }
            else
            {
                reflectionEndPoint = (Vector2)transform.position + reflectedDirection * maxReflectionDistance;
                hitReceiver = null;
            }
        }
        
        private void UpdateBeamVisualization()
        {
            if (beamRenderer != null)
            {
                beamRenderer.enabled = true;
                beamRenderer.SetPosition(0, transform.position);
                beamRenderer.SetPosition(1, reflectionEndPoint);
                
                // Pulse effect
                float pulse = 0.6f + 0.4f * Mathf.Sin(Time.time * 3f);
                Color currentColor = beamColor;
                currentColor.a = beamColor.a * pulse;
                beamRenderer.startColor = currentColor;
                beamRenderer.endColor = currentColor;
            }
        }
        
        private void HideBeam()
        {
            if (beamRenderer != null)
            {
                beamRenderer.enabled = false;
            }
            
            // Deactivate receiver if we were hitting one
            if (hitReceiver != null)
            {
                hitReceiver.LoseLight();
                hitReceiver = null;
            }
        }
        
        private void UpdateMirrorAppearance()
        {
            if (mirrorSprite != null)
            {
                mirrorSprite.color = isActivated ? activeColor : inactiveColor;
            }
        }
        
        private void OnDrawGizmos()
        {
            // Draw detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
            
            // Draw mirror normal direction
            float angle = transform.eulerAngles.z * Mathf.Deg2Rad;
            Vector2 normal = new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle));
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + normal * 2f);
        }
    }
}
