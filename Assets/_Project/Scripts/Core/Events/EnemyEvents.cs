using UnityEngine;

namespace Nordo.Core.Events
{
    /// <summary>
    /// Raised when The Listener reaches and strikes the player. Deliberately carries no damage — the
    /// level/game decides what "caught" means (restart, wound, game-over), keeping the enemy decoupled
    /// from any health/flow system.
    /// </summary>
    public readonly struct PlayerCaughtEvent : IGameEvent
    {
        /// <summary>Where the strike happened.</summary>
        public readonly Vector3 Position;

        public PlayerCaughtEvent(Vector3 position)
        {
            Position = position;
        }
    }

    /// <summary>
    /// Raised whenever The Listener changes behavioural state. HUD, audio stingers and music can react
    /// (e.g. swell tension on entering Chase) without polling the enemy.
    /// </summary>
    public readonly struct ListenerStateChangedEvent : IGameEvent
    {
        /// <summary>The state just entered.</summary>
        public readonly ListenerStateId State;

        /// <summary>The state just left.</summary>
        public readonly ListenerStateId PreviousState;

        public ListenerStateChangedEvent(ListenerStateId state, ListenerStateId previousState)
        {
            State = state;
            PreviousState = previousState;
        }
    }
}
