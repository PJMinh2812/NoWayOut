using UnityEngine;

namespace GloomCraft
{
    /// <summary>
    /// Minimal bootstrap similar to microStudio's global.init/update.
    /// Attach this to an empty GameObject in your starting Unity scene.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private GameStateMachine stateMachine;

        private void Awake()
        {
            if (stateMachine == null)
            {
                stateMachine = FindFirstObjectByType<GameStateMachine>();
            }
        }

        private void Start()
        {
            if (stateMachine == null)
            {
                Debug.LogError("[GloomCraft] Missing GameStateMachine in scene.");
                enabled = false;
                return;
            }

            stateMachine.SetState(GameState.Introduction);
        }
    }
}


