namespace Nordo.CameraFeel
{
    /// <summary>
    /// Contract for a single, composable camera effect (head-bob, sway, landing impact, tilt…).
    /// <para>
    /// Each effect has exactly one responsibility and reports its per-frame contribution as an
    /// additive <see cref="CameraEffectSample"/>. The <see cref="CameraRig"/> discovers all
    /// effects on its GameObject and sums their samples, so new effects can be added simply by
    /// attaching another component — no rig code changes. This is the Open/Closed principle in
    /// practice and the "fully modular and expandable" requirement made concrete.
    /// </para>
    /// </summary>
    public interface ICameraEffect
    {
        /// <summary>
        /// When false, the rig skips this effect entirely (and lets its contribution decay to
        /// zero via smoothing). Lets effects be toggled at runtime — e.g. an accessibility
        /// option that disables head-bob — without being removed.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Computes this effect's additive offset for the current frame.
        /// </summary>
        /// <param name="deltaTime">Frame time to advance internal phases/springs with.</param>
        /// <param name="state">Immutable snapshot of player + aim state this frame.</param>
        CameraEffectSample Evaluate(float deltaTime, in CameraRigState state);
    }
}
