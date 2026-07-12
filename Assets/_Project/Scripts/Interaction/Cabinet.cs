using System;
using UnityEngine;

namespace Nordo.Interaction
{
    /// <summary>
    /// A cabinet with one or more hinged doors that all open together (e.g. a double-door cabinet
    /// whose leaves swing apart). Each door rotates about a shared axis by its own angle — set two
    /// doors to +90° / −90° for a symmetric pair. Everything else is inherited from
    /// <see cref="OpenableBase"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Cabinet : OpenableBase
    {
        /// <summary>One hinged leaf of the cabinet.</summary>
        [Serializable]
        private struct Leaf
        {
            [Tooltip("The door transform to rotate.")]
            public Transform Door;

            [Tooltip("Angle in degrees this leaf swings when open (sign sets the direction).")]
            [Range(-170f, 170f)] public float OpenAngle;
        }

        [Header("Cabinet")]
        [Tooltip("Shared local axis all leaves rotate about (usually up).")]
        [SerializeField] private Vector3 _hingeAxis = Vector3.up;

        [SerializeField] private Leaf[] _leaves = new Leaf[0];

        private Quaternion[] _closedRotations;

        protected override void CacheClosedState()
        {
            _closedRotations = new Quaternion[_leaves.Length];
            for (int i = 0; i < _leaves.Length; i++)
            {
                if (_leaves[i].Door != null)
                {
                    _closedRotations[i] = _leaves[i].Door.localRotation;
                }
            }
        }

        protected override void ApplyOpenAmount(float amount)
        {
            for (int i = 0; i < _leaves.Length; i++)
            {
                Transform door = _leaves[i].Door;
                if (door == null)
                {
                    continue;
                }

                door.localRotation = _closedRotations[i] * Quaternion.AngleAxis(_leaves[i].OpenAngle * amount, _hingeAxis);
            }
        }
    }
}
