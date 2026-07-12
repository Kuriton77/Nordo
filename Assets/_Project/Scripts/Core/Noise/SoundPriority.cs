namespace Nordo.Core
{
    /// <summary>
    /// Relative importance of a noise, used by the AI to arbitrate between competing stimuli
    /// (a distant drip vs. a slammed door). Higher values interrupt lower-priority behaviour.
    /// Kept in Core so emitters and the (future) enemy hearing system share one scale.
    /// </summary>
    public enum SoundPriority
    {
        /// <summary>Background texture — rarely worth reacting to (a settling creak).</summary>
        Ambient = 0,

        /// <summary>Quiet, deliberate sounds (a crouched footstep, a drawer easing open).</summary>
        Minor = 1,

        /// <summary>Clearly audible activity (normal footsteps, a cabinet opening).</summary>
        Notable = 2,

        /// <summary>Loud, sudden events that demand investigation (a sprint, a thrown object, a slam).</summary>
        Alarming = 3
    }

    /// <summary>The kind of thing that produced a noise. Lets listeners weight sources differently.</summary>
    public enum NoiseSourceKind
    {
        Generic = 0,
        Footstep = 1,
        Impact = 2,
        Door = 3,
        Object = 4,
        Voice = 5,
        Machine = 6
    }
}
