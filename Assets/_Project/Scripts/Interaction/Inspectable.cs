using UnityEngine;

namespace Nordo.Interaction
{
    /// <summary>
    /// A prop the player can hold up and examine (a note, a keepsake, a mysterious device). Interacting
    /// hands it to the player's <see cref="InspectionController"/>, which lifts it into view and lets
    /// the player rotate it. Carries the presentation data (distance, scale, optional caption).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Inspectable : InteractableBase
    {
        [Header("Inspection")]
        [Tooltip("Distance in front of the camera to hold the object while inspecting.")]
        [Range(0.2f, 1.5f)] [SerializeField] private float _inspectDistance = 0.5f;

        [Tooltip("Uniform scale multiplier applied while inspecting (to enlarge small props).")]
        [Range(0.2f, 5f)] [SerializeField] private float _inspectScale = 1f;

        [Tooltip("Optional caption shown while inspecting (e.g. transcribed note text).")]
        [TextArea(1, 4)]
        [SerializeField] private string _caption = string.Empty;

        /// <summary>Distance in front of the camera to hold the object.</summary>
        public float InspectDistance => _inspectDistance;

        /// <summary>Scale multiplier applied during inspection.</summary>
        public float InspectScale => _inspectScale;

        /// <summary>Optional caption to display during inspection.</summary>
        public string Caption => _caption;

        /// <inheritdoc />
        public override string GetPrompt(in InteractionContext context) => "Inspect";

        /// <inheritdoc />
        protected override void OnInteract(in InteractionContext context)
        {
            if (context.InteractorRoot == null)
            {
                return;
            }

            InspectionController controller = context.InteractorRoot.GetComponentInChildren<InspectionController>();
            if (controller != null)
            {
                controller.BeginInspect(this);
            }
        }
    }
}
