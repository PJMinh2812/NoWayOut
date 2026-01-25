using System;

namespace GloomCraft.Dungeon
{
    /// <summary>
    /// Central event hub for dungeon-related events.
    /// Enables loose coupling between dungeon systems.
    /// </summary>
    public static class DungeonEvents
    {
        /// <summary>
        /// Fired when dungeon data generation is complete (before rendering).
        /// </summary>
        public static event Action<DungeonGenerator2D.Result> OnDungeonGenerated;
        
        /// <summary>
        /// Fired when dungeon rendering is complete.
        /// </summary>
        public static event Action<DungeonMap> OnDungeonRendered;
        
        /// <summary>
        /// Fired when all entities (enemies, furniture) are spawned.
        /// </summary>
        public static event Action OnEntitiesSpawned;
        
        /// <summary>
        /// Fired when player is positioned at start point.
        /// </summary>
        public static event Action<UnityEngine.Vector3> OnPlayerSpawned;
        
        /// <summary>
        /// Fired when the entire map initialization is complete and ready to play.
        /// </summary>
        public static event Action OnMapReady;
        
        // Internal invoke methods
        internal static void RaiseDungeonGenerated(DungeonGenerator2D.Result result) 
            => OnDungeonGenerated?.Invoke(result);
        
        internal static void RaiseDungeonRendered(DungeonMap map) 
            => OnDungeonRendered?.Invoke(map);
        
        internal static void RaiseEntitiesSpawned() 
            => OnEntitiesSpawned?.Invoke();
        
        internal static void RaisePlayerSpawned(UnityEngine.Vector3 position) 
            => OnPlayerSpawned?.Invoke(position);
        
        internal static void RaiseMapReady() 
            => OnMapReady?.Invoke();
        
        /// <summary>
        /// Clear all event subscriptions. Call when changing scenes.
        /// </summary>
        public static void ClearAll()
        {
            OnDungeonGenerated = null;
            OnDungeonRendered = null;
            OnEntitiesSpawned = null;
            OnPlayerSpawned = null;
            OnMapReady = null;
        }
    }
}
