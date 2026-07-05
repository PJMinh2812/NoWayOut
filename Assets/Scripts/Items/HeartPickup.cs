using UnityEngine;
using System.Collections.Generic;

namespace NWO
{
    /// <summary>
    /// World-space heart pickup. Spawns from enemy/boss death,
    /// magnetizes toward player, and heals player on collect.
    /// Uses object pooling - no prefab needed.
    /// </summary>
    public sealed class HeartPickup : MonoBehaviour
    {
        private static readonly Queue<HeartPickup> _pool = new();
        private static bool _poolInitialized;
        private const int POOL_SIZE = 20;
        private static Sprite _fallbackSprite;
        private static Sprite[] _heartFrames;

        private const float PICKUP_RADIUS = 1.2f;
        private const float MAGNET_RADIUS = 3.5f;
        private const float MAGNET_SPEED = 12f;
        private const float LIFETIME = 20f;
        private const float POP_FORCE = 2.4f;
        private const float GRAVITY = 9f;
        private const float BOB_AMPLITUDE = 0.06f;
        private const float BOB_FREQUENCY = 2.5f;
        private const float SPIN_SPEED = 7f;
        private const float FRAME_RATE = 8f;

        private SpriteRenderer _sr;
        private Transform _playerTransform;
        private Vector3 _velocity;
        private float _elapsed;
        private float _bobBaseY;
        private bool _grounded;
        private bool _collected;
        private int _healAmount;
        private float _frameTimer;
        private int _frameIndex;

        private static Transform _cachedPlayerTransform;
        private static PlayerHealth2D _cachedPlayerHealth;

        private static AudioClip _pickupSound;
        private static AudioSource _sharedAudioSource;

        public static void WarmPool()
        {
            if (_poolInitialized) return;
            _poolInitialized = true;

            _fallbackSprite = CreateHeartSprite();

            for (int i = 0; i < POOL_SIZE; i++)
            {
                var inst = CreateInstance();
                inst.gameObject.SetActive(false);
                _pool.Enqueue(inst);
            }
        }

        public static void SetHeartFrames(Sprite[] frames)
        {
            if (frames != null && frames.Length > 0)
                _heartFrames = frames;
        }

        public static void SetHeartSound(AudioClip clip)
        {
            _pickupSound = clip;
        }

        private static HeartPickup CreateInstance()
        {
            var go = new GameObject("HeartPickup");
            var pickup = go.AddComponent<HeartPickup>();
            pickup._sr = go.AddComponent<SpriteRenderer>();
            pickup._sr.sprite = GetCurrentFrame(0);
            pickup._sr.sortingOrder = 50;
            DontDestroyOnLoad(go);
            return pickup;
        }

        private static Sprite GetCurrentFrame(int index)
        {
            if (_heartFrames != null && _heartFrames.Length > 0)
                return _heartFrames[index % _heartFrames.Length];
            return _fallbackSprite;
        }

        public static void Spawn(Vector3 position, int healAmount = 10)
        {
            if (!_poolInitialized) WarmPool();

            HeartPickup inst;
            if (_pool.Count > 0)
            {
                inst = _pool.Dequeue();
                inst.gameObject.SetActive(true);
            }
            else
            {
                inst = CreateInstance();
            }

            inst.Setup(position, healAmount);
        }

        public static void SpawnMultiple(Vector3 position, int count, int healAmountEach = 10)
        {
            for (int i = 0; i < count; i++)
                Spawn(position, healAmountEach);
        }

        private void Setup(Vector3 position, int healAmount)
        {
            transform.position = position;
            _healAmount = Mathf.Max(1, healAmount);
            _elapsed = 0f;
            _grounded = false;
            _collected = false;
            _frameTimer = 0f;
            _frameIndex = 0;

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

            RefreshPlayerCache();
            _playerTransform = _cachedPlayerTransform;
        }

        private static void RefreshPlayerCache()
        {
            if (_cachedPlayerTransform == null || _cachedPlayerHealth == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    _cachedPlayerTransform = player.transform;
                    _cachedPlayerHealth = player.GetComponent<PlayerHealth2D>();
                }
            }
        }

        private void Update()
        {
            if (_collected) return;

            float dt = Time.deltaTime;
            _elapsed += dt;

            if (_elapsed > LIFETIME)
            {
                ReturnToPool();
                return;
            }

            if (_elapsed > LIFETIME - 3f)
            {
                float blink = Mathf.PingPong(_elapsed * 5f, 1f);
                _sr.color = new Color(1f, 1f, 1f, blink > 0.5f ? 1f : 0.3f);
            }

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
                float bob = Mathf.Sin((_elapsed) * BOB_FREQUENCY * Mathf.PI * 2f) * BOB_AMPLITUDE;
                var pos = transform.position;
                pos.y = _bobBaseY + bob;
                transform.position = pos;
            }

            if (_heartFrames != null && _heartFrames.Length > 1)
            {
                _frameTimer += dt;
                float interval = 1f / FRAME_RATE;
                if (_frameTimer >= interval)
                {
                    _frameTimer -= interval;
                    _frameIndex = (_frameIndex + 1) % _heartFrames.Length;
                    _sr.sprite = _heartFrames[_frameIndex];
                }
            }
            else
            {
                float spin = Mathf.Cos(_elapsed * SPIN_SPEED);
                var s = transform.localScale;
                s.x = Mathf.Abs(spin) * 0.9f + 0.1f;
                transform.localScale = s;
            }

            if (_playerTransform == null)
            {
                RefreshPlayerCache();
                _playerTransform = _cachedPlayerTransform;
            }

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

            if (_cachedPlayerHealth == null)
                RefreshPlayerCache();

            if (_cachedPlayerHealth != null && !_cachedPlayerHealth.IsDead)
                _cachedPlayerHealth.Heal(_healAmount);

            PlayPickupSound();
            ReturnToPool();
        }

        private void ReturnToPool()
        {
            gameObject.SetActive(false);
            _sr.color = Color.white;
            transform.localScale = Vector3.one;
            _pool.Enqueue(this);
        }

        private static void PlayPickupSound()
        {
            if (_pickupSound == null) return;

            if (_sharedAudioSource == null)
            {
                var go = new GameObject("HeartAudio");
                _sharedAudioSource = go.AddComponent<AudioSource>();
                _sharedAudioSource.playOnAwake = false;
                _sharedAudioSource.spatialBlend = 0f;
                _sharedAudioSource.volume = 0.45f;
                DontDestroyOnLoad(go);
            }

            _sharedAudioSource.PlayOneShot(_pickupSound);
        }

        private static Sprite CreateHeartSprite()
        {
            const int size = 16;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Color red = new(0.95f, 0.15f, 0.2f, 1f);
            Color dark = new(0.62f, 0.05f, 0.1f, 1f);
            Color light = new(1f, 0.52f, 0.56f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x / (size - 1f)) * 2f - 1f;
                    float ny = (y / (size - 1f)) * 2f - 1f;

                    float a = nx * nx + ny * ny - 0.3f;
                    float heart = a * a * a - nx * nx * ny * ny * ny;

                    if (heart <= 0f)
                    {
                        float hl = Mathf.Clamp01(1f - (new Vector2(nx + 0.22f, ny - 0.2f).magnitude));
                        tex.SetPixel(x, y, Color.Lerp(red, light, hl * 0.4f));
                    }
                    else if (heart <= 0.02f)
                    {
                        tex.SetPixel(x, y, dark);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        }
    }
}
