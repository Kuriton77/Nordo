using UnityEngine;
using Nordo.Core;
using Nordo.Core.Events;
using Nordo.Interaction;

namespace Nordo.Progression
{
    /// <summary>
    /// A note, log page, or scrawled warning the player can read — the backbone of environmental
    /// storytelling and puzzle clues. Reading it raises <see cref="NoteReadEvent"/> so the HUD shows it
    /// and the logbook records it. Notes carry the lore of Vardø-9 and hint at what to do next.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ReadableNote : InteractableBase
    {
        [Tooltip("Stable id so a note is only logged once.")]
        [SerializeField] private string _noteId = "note";

        [SerializeField] private string _title = "Scrap of paper";

        [TextArea(3, 10)]
        [SerializeField] private string _body = string.Empty;

        [Tooltip("Optional objective id completed the first time this note is read (e.g. a tutorial beat).")]
        [SerializeField] private string _completesObjectiveId = string.Empty;

        private bool _read;

        /// <summary>Configures the note at runtime (used by the level builder).</summary>
        public void SetNote(string noteId, string title, string body, string completesObjectiveId = "")
        {
            _noteId = noteId;
            _title = title;
            _body = body;
            _completesObjectiveId = completesObjectiveId;
        }

        /// <inheritdoc />
        public override string GetPrompt(in InteractionContext context) => "Read";

        /// <inheritdoc />
        protected override void OnInteract(in InteractionContext context)
        {
            EventBus<NoteReadEvent>.Raise(new NoteReadEvent(_noteId, _title, _body));

            if (!_read && !string.IsNullOrEmpty(_completesObjectiveId)
                && ServiceLocator.TryGet(out IObjectiveService objectives) && objectives != null)
            {
                objectives.CompleteObjective(_completesObjectiveId);
            }

            _read = true;
        }
    }
}
