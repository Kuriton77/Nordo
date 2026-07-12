using UnityEngine;

namespace Nordo.Enemy
{
    /// <summary>
    /// An ordered set of waypoints The Listener patrols between when calm. Attach to an empty object
    /// and parent (or assign) the point transforms; the enemy cycles through them. If a route is empty
    /// the enemy simply idles at its home position, so this is entirely optional.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PatrolRoute : MonoBehaviour
    {
        [Tooltip("Waypoints, visited in order and looped. If empty, child transforms are used.")]
        [SerializeField] private Transform[] _waypoints;

        private int _index;

        private void Awake()
        {
            // Fall back to child transforms so a route can be built just by parenting points.
            if (_waypoints == null || _waypoints.Length == 0)
            {
                int count = transform.childCount;
                _waypoints = new Transform[count];
                for (int i = 0; i < count; i++)
                {
                    _waypoints[i] = transform.GetChild(i);
                }
            }
        }

        /// <summary>Whether the route has any usable waypoints.</summary>
        public bool HasWaypoints => _waypoints != null && _waypoints.Length > 0;

        /// <summary>The current waypoint position (or a fallback if empty).</summary>
        public Vector3 Current => HasWaypoints && _waypoints[_index] != null ? _waypoints[_index].position : transform.position;

        /// <summary>Advances to and returns the next waypoint position.</summary>
        public Vector3 Advance()
        {
            if (!HasWaypoints)
            {
                return transform.position;
            }

            _index = (_index + 1) % _waypoints.Length;
            return Current;
        }

        /// <summary>Selects (and returns) the waypoint nearest a position, e.g. when returning to patrol.</summary>
        public Vector3 SelectNearest(Vector3 position)
        {
            if (!HasWaypoints)
            {
                return transform.position;
            }

            int best = 0;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < _waypoints.Length; i++)
            {
                if (_waypoints[i] == null)
                {
                    continue;
                }

                float sqr = (_waypoints[i].position - position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = i;
                }
            }

            _index = best;
            return Current;
        }

        /// <summary>Assigns waypoints at runtime (used by the vertical-slice builder).</summary>
        public void SetWaypoints(Transform[] waypoints)
        {
            _waypoints = waypoints;
            _index = 0;
        }
    }
}
