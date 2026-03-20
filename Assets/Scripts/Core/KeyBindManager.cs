using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

namespace NWO
{
    /// <summary>
    /// Central keybind manager. All game scripts read key bindings from here
    /// instead of hard-coding Keyboard.current.xxxKey.
    /// Persists bindings to PlayerPrefs. Singleton — auto-created by GameManager.
    /// </summary>
    public sealed class KeyBindManager : MonoBehaviour
    {
        public static KeyBindManager Instance { get; private set; }

        // ── Action names ──────────────────────────────────────
        public const string ACT_MOVE_UP    = "MoveUp";
        public const string ACT_MOVE_DOWN  = "MoveDown";
        public const string ACT_MOVE_LEFT  = "MoveLeft";
        public const string ACT_MOVE_RIGHT = "MoveRight";
        public const string ACT_DASH       = "Dash";
        public const string ACT_ATTACK     = "Attack";
        public const string ACT_SPELL1     = "Spell1";
        public const string ACT_SPELL2     = "Spell2";
        public const string ACT_SPELL3     = "Spell3";
        public const string ACT_SPELL0     = "SpellIdle";
        public const string ACT_INTERACT   = "Interact";

        /// <summary>Fired when any keybind changes.</summary>
        public event Action OnBindingsChanged;

        // ── Storage ───────────────────────────────────────────
        private const string PREFS_KEY = "NWO_KeyBinds";

        // Primary binding per action
        private readonly Dictionary<string, Key> _bindings = new();
        // Default values
        private static readonly Dictionary<string, Key> _defaults = new()
        {
            { ACT_MOVE_UP,    Key.W },
            { ACT_MOVE_DOWN,  Key.S },
            { ACT_MOVE_LEFT,  Key.A },
            { ACT_MOVE_RIGHT, Key.D },
            { ACT_DASH,       Key.LeftShift },
            { ACT_ATTACK,     Key.Q },
            { ACT_SPELL1,     Key.Digit1 },
            { ACT_SPELL2,     Key.Digit2 },
            { ACT_SPELL3,     Key.Digit3 },
            { ACT_SPELL0,     Key.Digit0 },
            { ACT_INTERACT,   Key.E },
        };

        // ── Lifecycle ─────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadBindings();
        }

        // ── Public API ────────────────────────────────────────

        /// <summary>Get the Key currently bound to an action.</summary>
        public Key GetKey(string action)
        {
            return _bindings.TryGetValue(action, out var k) ? k : Key.None;
        }

        /// <summary>Is the key for this action currently held?</summary>
        public bool IsPressed(string action)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return false;
            var key = GetKey(action);
            return key != Key.None && keyboard[key].isPressed;
        }

        /// <summary>Was the key for this action pressed this frame?</summary>
        public bool WasPressedThisFrame(string action)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return false;
            var key = GetKey(action);
            return key != Key.None && keyboard[key].wasPressedThisFrame;
        }

        /// <summary>Set a new key for an action. Saves and notifies.</summary>
        public void SetKey(string action, Key newKey)
        {
            _bindings[action] = newKey;
            SaveBindings();
            OnBindingsChanged?.Invoke();
        }

        /// <summary>Reset all bindings to defaults.</summary>
        public void ResetToDefaults()
        {
            _bindings.Clear();
            foreach (var kv in _defaults) _bindings[kv.Key] = kv.Value;
            SaveBindings();
            OnBindingsChanged?.Invoke();
        }

        /// <summary>Get the display name for a key.</summary>
        public static string GetKeyDisplayName(Key key)
        {
            return key switch
            {
                Key.LeftShift  => "L-Shift",
                Key.RightShift => "R-Shift",
                Key.LeftCtrl   => "L-Ctrl",
                Key.RightCtrl  => "R-Ctrl",
                Key.LeftAlt    => "L-Alt",
                Key.RightAlt   => "R-Alt",
                Key.Space      => "Space",
                Key.Digit0     => "0",
                Key.Digit1     => "1",
                Key.Digit2     => "2",
                Key.Digit3     => "3",
                Key.Digit4     => "4",
                Key.Digit5     => "5",
                Key.Digit6     => "6",
                Key.Digit7     => "7",
                Key.Digit8     => "8",
                Key.Digit9     => "9",
                Key.None       => "-",
                _              => key.ToString()
            };
        }

        /// <summary>Get the default key for an action.</summary>
        public static Key GetDefault(string action)
        {
            return _defaults.TryGetValue(action, out var k) ? k : Key.None;
        }

        /// <summary>Get all rebindable action names.</summary>
        public static IEnumerable<string> AllActions => _defaults.Keys;

        /// <summary>Human-readable label for action.</summary>
        public static string GetActionLabel(string action)
        {
            return action switch
            {
                ACT_MOVE_UP    => "MOVE UP",
                ACT_MOVE_DOWN  => "MOVE DOWN",
                ACT_MOVE_LEFT  => "MOVE LEFT",
                ACT_MOVE_RIGHT => "MOVE RIGHT",
                ACT_DASH       => "DASH",
                ACT_ATTACK     => "ATTACK",
                ACT_SPELL1     => "SPELL 1",
                ACT_SPELL2     => "SPELL 2",
                ACT_SPELL3     => "SPELL 3",
                ACT_SPELL0     => "SPELL IDLE",
                ACT_INTERACT   => "INTERACT",
                _              => action.ToUpperInvariant()
            };
        }

        // ── Persistence ───────────────────────────────────────

        private void LoadBindings()
        {
            // Start with defaults
            foreach (var kv in _defaults) _bindings[kv.Key] = kv.Value;

            var json = PlayerPrefs.GetString(PREFS_KEY, "");
            if (string.IsNullOrWhiteSpace(json)) return;

            try
            {
                var data = JsonUtility.FromJson<BindingsData>(json);
                if (data?.actions != null)
                {
                    foreach (var entry in data.actions)
                    {
                        if (_defaults.ContainsKey(entry.action) && Enum.TryParse<Key>(entry.key, out var k))
                            _bindings[entry.action] = k;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[KeyBindManager] Failed to load bindings: {ex.Message}");
            }
        }

        private void SaveBindings()
        {
            var data = new BindingsData();
            data.actions = new List<BindingEntry>();
            foreach (var kv in _bindings)
                data.actions.Add(new BindingEntry { action = kv.Key, key = kv.Value.ToString() });

            PlayerPrefs.SetString(PREFS_KEY, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        [Serializable]
        private class BindingsData
        {
            public List<BindingEntry> actions;
        }

        [Serializable]
        private class BindingEntry
        {
            public string action;
            public string key;
        }
    }
}
