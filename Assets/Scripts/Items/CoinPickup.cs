using UnityEngine;
using System.Collections.Generic;

namespace NWO
{
    /// <summary>
    /// World-space coin pickup. Spawns from enemy death, animates with coin sprites,
    /// magnetizes toward player, and adds coin on collect.
    /// Uses object pooling (like DamagePopup) - no prefab needed.
    /// Coin sprites are loaded from Assets/Art/Boss/coin_anim_f0..f3.
    /// </summary>
    public sealed class CoinPickup : MonoBehaviour
    {
        // === Pool ===
        private static readonly Queue<CoinPickup> _pool = new();
        private static bool _poolInitialized;
        private const int POOL_SIZE = 30;
        private static Sprite _fallbackSprite;
        private static Sprite[] _coinFrames;

        // === Settings ===
        private const float PICKUP_RADIUS = 1.2f;
        private const float MAGNET_RADIUS = 3.5f;
        private const float MAGNET_SPEED = 14f;
        private const float LIFETIME = 20f;
        private const float POP_FORCE = 2.5f;
        private const float GRAVITY = 9f;
        private const float BOB_AMPLITUDE = 0.06f;
        private const float BOB_FREQUENCY = 2.5f;
        private const float SPIN_SPEED = 7f;
        private const float FRAME_RATE = 8f;

        // === Instance state ===
        private SpriteRenderer _sr;
        private Transform _playerTransform;
        private Vector3 _velocity;
        private float _elapsed;
        private float _bobBaseY;
        private bool _grounded;
        private bool _collected;
        private int _coinValue;
        private float _frameTimer;
        private int _frameIndex;

        // === Audio ===
        private static AudioClip _coinSound;
        private static AudioSource _sharedAudioSource;

        // -------------------------------------------------------
        // Pool management
        // -------------------------------------------------------

        public static void WarmPool()
        {
            if (_poolInitialized) return;
            _poolInitialized = true;

            _fallbackSprite = CreateGoldCoinSprite();

            for (int i = 0; i < POOL_SIZE; i++)
            {
                var inst = CreateInstance();
                inst.gameObject.SetActive(false);
                _pool.Enqueue(inst);
            }
        }

        /// <summary>Set coin animation frames (call from CoinManager).</summary>
        public static void SetCoinFrames(Sprite[] frames)
        {
            if (frames != null && frames.Length > 0)
                _coinFrames = frames;
        }

        /// <summary>Set coin pickup sound clip (call from CoinManager).</summary>
        public static void SetCoinSound(AudioClip clip)
        {
            _coinSound = clip;
        }

        private static CoinPickup CreateInstance()
        {
            var go = new GameObject("CoinPickup");
            var pickup = go.AddComponent<CoinPickup>();
            pickup._sr = go.AddComponent<SpriteRenderer>();
            pickup._sr.sprite = GetCurrentFrame(0);
            pickup._sr.sortingOrder = 50;
            DontDestroyOnLoad(go);
            return pickup;
        }

        private static Sprite GetCurrentFrame(int index)
        {
            if (_coinFrames != null && _coinFrames.Length > 0)
                return _coinFrames[index % _coinFrames.Length];
            return _fallbackSprite;
        }

        /// <summary>Spawn coin(s) at world position.</summary>
        public static void Spawn(Vector3 position, int value = 1)
        {
            if (!_poolInitialized) WarmPool();

            CoinPickup inst;
            if (_pool.Count > 0)
            {
                inst = _pool.Dequeue();
                inst.gameObject.SetActive(true);
            }
            else
            {
                inst = CreateInstance();
            }

            inst.Setup(position, value);
        }

        /// <summary>Spawn multiple coins spread out from position.</summary>
        public static void SpawnMultiple(Vector3 position, int count, int valueEach = 1)
        {
            for (int i = 0; i < count; i++)
                Spawn(position, valueEach);
        }

        // -------------------------------------------------------
        // Instance logic
        // -------------------------------------------------------

        private void Setup(Vector3 position, int value)
        {
            transform.position = position;
            _coinValue = value;
            _elapsed = 0f;
            _grounded = false;
            _collected = false;
            _frameTimer = 0f;
            _frameIndex = 0;

            // Random pop arc
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float force = Random.Range(POP_FORCE * 0.4f, POP_FORCE);
            _velocity = new Vector3(
                Mathf.Cos(angle) * force,
                Random.Range(2.5f, 4f),
                0f
            );

            transform.localScale = Vector3.one;
            _sr.sprite = GetCurrentFrame(0);
            _sr.color = Color.white;

            // Cache player
            if (_playerTransform == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) _playerTransform = player.transform;
            }
        }

        private void Update()
        {
            if (_collected) return;

            float dt = Time.deltaTime;
            _elapsed += dt;

            // Auto-despawn after lifetime
            if (_elapsed > LIFETIME)
            {
                ReturnToPool();
                return;
            }

            // Blink warning (last 3s)
            if (_elapsed > LIFETIME - 3f)
            {
                float blink = Mathf.PingPong(_elapsed * 5f, 1f);
                _sr.color = new Color(1f, 1f, 1f, blink > 0.5f ? 1f : 0.3f);
            }

            // --- Physics: pop arc then land ---
            if (!_grounded)
            {
                _velocity.y -= GRAVITY * dt;
                transform.position += _velocity * dt;

                if (_velocity.y < -1.5f && _elapsed > 0.12f)
                {
                    _grounded = true;
                    _bobBaseY = transform.position.y;
                    _velocity = Vector3.zero;
                }
            }
            else
            {
                // Gentle bob
                float bob = Mathf.Sin((_elapsed) * BOB_FREQUENCY * Mathf.PI * 2f) * BOB_AMPLITUDE;
                var pos = transform.position;
                pos.y = _bobBaseY + bob;
                transform.position = pos;
            }

            // --- Sprite animation ---
            if (_coinFrames != null && _coinFrames.Length > 1)
            {
                _frameTimer += dt;
                float interval = 1f / FRAME_RATE;
                if (_frameTimer >= interval)
                {
                    _frameTimer -= interval;
                    _frameIndex = (_frameIndex + 1) % _coinFrames.Length;
                    _sr.sprite = _coinFrames[_frameIndex];
                }
            }
            else
            {
                // Fallback: spin via scale.x oscillation
                float spin = Mathf.Cos(_elapsed * SPIN_SPEED);
                var s = transform.localScale;
                s.x = Mathf.Abs(spin) * 0.9f + 0.1f;
                transform.localScale = s;
            }

            // --- Magnet + pickup ---
            if (_playerTransform != null)
            {
                float dist = Vector2.Distance(transform.position, _playerTransform.position);

                if (dist <= PICKUP_RADIUS)
                {
                    Collect();
                    return;
                }

                if (dist <= MAGNET_RADIUS)
                {
                    Vector3 dir = (_playerTransform.position - transform.position).normalized;
                    float t = 1f - (dist / MAGNET_RADIUS);
                    transform.position += dir * (MAGNET_SPEED * t * t * dt);
                }
            }
        }

        private void Collect()
        {
            _collected = true;

            if (CoinManager.Instance != null)
                CoinManager.Instance.AddCoins(_coinValue);

            PlayCoinSound();
            ReturnToPool();
        }

        private void ReturnToPool()
        {
            gameObject.SetActive(false);
            _sr.color = Color.white;
            transform.localScale = Vector3.one;
            _pool.Enqueue(this);
        }

        // -------------------------------------------------------
        // Audio
        // -------------------------------------------------------

        private static void PlayCoinSound()
        {
            if (_coinSound == null) return;

            if (_sharedAudioSource == null)
            {
                var go = new GameObject("CoinAudio");
                _sharedAudioSource = go.AddComponent<AudioSource>();
                _sharedAudioSource.playOnAwake = false;
                _sharedAudioSource.spatialBlend = 0f;
                _sharedAudioSource.volume = 0.45f;
                DontDestroyOnLoad(go);
            }

            _sharedAudioSource.PlayOneShot(_coinSound);
        }

        // -------------------------------------------------------
        // Fallback gold coin sprite (generated)
        // -------------------------------------------------------

        private static Sprite CreateGoldCoinSprite()
        {
            const int size = 16;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Color gold = new(1f, 0.84f, 0.1f, 1f);
            Color dark = new(0.78f, 0.6f, 0.05f, 1f);
            Color light = new(1f, 0.95f, 0.5f, 1f);
            float center = (size - 1) / 2f;
            float radius = size / 2f - 1f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (d <= radius - 2f)
                    {
                        float hl = Mathf.Clamp01(1f - new Vector2(x - center + 2, y - center - 2).magnitude / radius);
                        tex.SetPixel(x, y, Color.Lerp(gold, light, hl * 0.5f));
                    }
                    else if (d <= radius)
                        tex.SetPixel(x, y, dark);
                    else
                        tex.SetPixel(x, y, Color.clear);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        }
    }
}
