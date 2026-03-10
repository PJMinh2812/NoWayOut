using System;
using UnityEngine;

namespace NWO
{
    public enum GameState
    {
        Introduction,
        MainMenu,
        Instructions,
        Game,
        Paused,
        GameOver
    }

    /// <summary>
    /// Unity-side equivalent of microStudio global.SceneManager.
    /// This is deliberately minimal scaffolding: it only logs state changes for now.
    /// </summary>
    public sealed class GameStateMachine : MonoBehaviour
    {
        [field: SerializeField] public GameState CurrentState { get; private set; }

        public event Action<GameState> OnStateChanged;

        public void SetState(GameState next)
        {
            if (CurrentState == next) return;
            CurrentState = next;
            Debug.Log($"[NWO] State -> {next}");
            OnStateChanged?.Invoke(next);
        }
    }
}


