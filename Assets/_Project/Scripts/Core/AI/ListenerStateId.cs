namespace Nordo.Core
{
    /// <summary>
    /// The behavioural states of The Listener, kept in Core so events and UI can refer to them without
    /// depending on the enemy assembly. The progression mirrors classic stealth AI, but every
    /// transition here is driven by <em>hearing</em>, never sight.
    /// </summary>
    public enum ListenerStateId
    {
        /// <summary>Walking a patrol route, calm, listening.</summary>
        Patrol = 0,

        /// <summary>Moving to the exact spot a sound came from.</summary>
        Investigate = 1,

        /// <summary>Sweeping the area around a lost sound, hunting for more.</summary>
        Search = 2,

        /// <summary>Actively pursuing a continuously-updated sound source.</summary>
        Chase = 3,

        /// <summary>Close enough to strike.</summary>
        Attack = 4,

        /// <summary>Heading back to the patrol route after giving up.</summary>
        Return = 5
    }
}
