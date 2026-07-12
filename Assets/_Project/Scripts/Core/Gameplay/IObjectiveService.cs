namespace Nordo.Core
{
    /// <summary>
    /// The objective/progression tracker's public surface. Puzzle components advance the chain by
    /// calling <see cref="CompleteObjective"/> with a known id; UI reads the current objective. Kept in
    /// Core so any system can drive or display progression without depending on the concrete tracker.
    /// </summary>
    public interface IObjectiveService
    {
        /// <summary>Id of the current objective (empty when complete).</summary>
        string CurrentObjectiveId { get; }

        /// <summary>Player-facing title of the current objective.</summary>
        string CurrentObjectiveTitle { get; }

        /// <summary>Whether the whole chain is finished.</summary>
        bool IsComplete { get; }

        /// <summary>
        /// Marks the given objective complete and advances to the next. No-op if the id is not the
        /// current objective (so double-calls and out-of-order triggers are safe).
        /// </summary>
        void CompleteObjective(string objectiveId);
    }
}
