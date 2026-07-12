namespace Nordo.Core.Events
{
    /// <summary>
    /// Fired whenever anything makes a noise. Carries a <see cref="NoiseStimulus"/> so emitters stay
    /// completely decoupled from the hearing system: they just raise this, and the
    /// <c>NoiseSystem</c> service (a subscriber) handles propagation. This is why doors, thrown
    /// props and machines need no reference to the AI at all.
    /// </summary>
    public readonly struct NoiseEvent : IGameEvent
    {
        /// <summary>The sound that was produced.</summary>
        public readonly NoiseStimulus Stimulus;

        public NoiseEvent(NoiseStimulus stimulus)
        {
            Stimulus = stimulus;
        }
    }
}
