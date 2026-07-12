using System;
using System.Collections.Generic;
using UnityEngine;
using Nordo.Core;
using Nordo.Core.Events;

namespace Nordo.Progression
{
    /// <summary>One stage of the objective chain: a stable id, a short title, and optional detail.</summary>
    [Serializable]
    public sealed class ObjectiveStep
    {
        [Tooltip("Stable id puzzle logic uses to complete this step.")]
        public string Id = "objective_id";

        [Tooltip("Player-facing one-line objective.")]
        public string Title = "New objective";

        [Tooltip("Optional detail shown in the logbook.")]
        [TextArea(1, 3)] public string Detail = string.Empty;

        public ObjectiveStep() { }
        public ObjectiveStep(string id, string title, string detail = "")
        {
            Id = id;
            Title = title;
            Detail = detail;
        }
    }

    /// <summary>
    /// Drives the multi-stage objective chain. Puzzle components call <see cref="CompleteObjective"/>
    /// with the current step's id to advance; it raises <see cref="ObjectiveChangedEvent"/> for the
    /// HUD/logbook and a completion message. Registered as <see cref="IObjectiveService"/>. Steps can be
    /// authored in the inspector or supplied at runtime by the level builder.
    /// </summary>
    [DefaultExecutionOrder(-60)]
    [DisallowMultipleComponent]
    public sealed class ObjectiveTracker : MonoBehaviour, IObjectiveService
    {
        [Tooltip("The ordered chain of objectives for this section.")]
        [SerializeField] private List<ObjectiveStep> _steps = new();

        private int _index;

        /// <summary>The full chain (for the logbook).</summary>
        public IReadOnlyList<ObjectiveStep> Steps => _steps;

        /// <summary>Index of the current step (equal to Steps.Count when complete).</summary>
        public int CurrentIndex => _index;

        /// <inheritdoc />
        public bool IsComplete => _index >= _steps.Count;

        /// <inheritdoc />
        public string CurrentObjectiveId => IsComplete ? string.Empty : _steps[_index].Id;

        /// <inheritdoc />
        public string CurrentObjectiveTitle => IsComplete ? string.Empty : _steps[_index].Title;

        private void Awake()
        {
            ServiceLocator.Register<IObjectiveService>(this);
        }

        private void Start()
        {
            RaiseChanged();
        }

        private void OnDestroy()
        {
            if (ServiceLocator.TryGet(out IObjectiveService current) && ReferenceEquals(current, this))
            {
                ServiceLocator.Unregister<IObjectiveService>();
            }
        }

        /// <summary>Replaces the objective chain at runtime (used by the level builder). Resets progress.</summary>
        public void SetObjectives(IEnumerable<ObjectiveStep> steps)
        {
            _steps = new List<ObjectiveStep>(steps);
            _index = 0;
            RaiseChanged();
        }

        /// <inheritdoc />
        public void CompleteObjective(string objectiveId)
        {
            // Only the current objective advances the chain — safe against double/out-of-order calls.
            if (IsComplete || _steps[_index].Id != objectiveId)
            {
                return;
            }

            EventBus<GameMessageEvent>.Raise(new GameMessageEvent($"Objective complete: {_steps[_index].Title}"));
            _index++;
            RaiseChanged();

            if (IsComplete)
            {
                EventBus<GameMessageEvent>.Raise(new GameMessageEvent("All objectives complete.", 5f));
            }
        }

        private void RaiseChanged()
        {
            EventBus<ObjectiveChangedEvent>.Raise(new ObjectiveChangedEvent(CurrentObjectiveId, CurrentObjectiveTitle, IsComplete));
        }
    }
}
