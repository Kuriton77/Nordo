using TMPro;
using UnityEngine;
using Nordo.Core;
using Nordo.Core.Events;

namespace Nordo.Interaction
{
    /// <summary>
    /// Displays the interaction prompt. It listens for <see cref="InteractionPromptEvent"/>s and
    /// shows/hides a TextMeshPro label — the whole UI side of interaction, kept behind the event so
    /// gameplay never references UI. Swap this out (world-space, controller-glyph, localized) without
    /// touching a single line of interaction logic.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractionPromptPresenter : MonoBehaviour
    {
        [Tooltip("Root object toggled on/off with prompt visibility (e.g. a panel). If empty, this GameObject is used.")]
        [SerializeField] private GameObject _root;

        [Tooltip("Label that displays the prompt text.")]
        [SerializeField] private TMP_Text _label;

        [Tooltip("Optional format applied to the prompt, e.g. \"[E] {0}\". Use {0} for the verb.")]
        [SerializeField] private string _format = "[E] {0}";

        private void Awake()
        {
            if (_root == null)
            {
                _root = gameObject;
            }

            SetVisible(false);
        }

        private void OnEnable()
        {
            EventBus<InteractionPromptEvent>.Subscribe(OnPrompt);
        }

        private void OnDisable()
        {
            EventBus<InteractionPromptEvent>.Unsubscribe(OnPrompt);
        }

        private void OnPrompt(InteractionPromptEvent evt)
        {
            bool show = evt.Visible && !string.IsNullOrEmpty(evt.Text);
            if (show && _label != null)
            {
                _label.text = string.IsNullOrEmpty(_format) ? evt.Text : string.Format(_format, evt.Text);
            }

            SetVisible(show);
        }

        private void SetVisible(bool visible)
        {
            if (_root != null && _root.activeSelf != visible)
            {
                _root.SetActive(visible);
            }
        }
    }
}
