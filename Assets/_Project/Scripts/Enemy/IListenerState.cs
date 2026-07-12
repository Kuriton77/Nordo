using Nordo.Core;

namespace Nordo.Enemy
{
    /// <summary>
    /// One behavioural state of The Listener's finite state machine. States are plain objects (not
    /// components) constructed once with a reference to their <see cref="ListenerController"/>; the
    /// controller ticks the current one. This keeps each behaviour isolated and testable, and adding a
    /// state is just a new class — the Open/Closed principle applied to AI behaviour.
    /// </summary>
    public interface IListenerState
    {
        /// <summary>Which state this is (also used for animation/audio/debug).</summary>
        ListenerStateId Id { get; }

        /// <summary>Called once when the state becomes active.</summary>
        void Enter();

        /// <summary>Called every frame while active.</summary>
        void Tick(float deltaTime);

        /// <summary>Called once when the state is left.</summary>
        void Exit();
    }
}
