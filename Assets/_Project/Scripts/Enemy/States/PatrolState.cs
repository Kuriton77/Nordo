using Nordo.Core;
using UnityEngine;

namespace Nordo.Enemy
{
    /// <summary>
    /// Calm patrol: walk the route (or idle at home) and listen. The moment suspicion crosses the
    /// investigate threshold — i.e. it has heard enough — it breaks off to investigate.
    /// </summary>
    public sealed class PatrolState : IListenerState
    {
        private readonly ListenerController _c;

        public PatrolState(ListenerController controller) => _c = controller;

        public ListenerStateId Id => ListenerStateId.Patrol;

        public void Enter()
        {
            _c.SetSpeed(_c.Config.PatrolSpeed);
            if (_c.Patrol != null && _c.Patrol.HasWaypoints)
            {
                _c.MoveTo(_c.Patrol.Current);
            }
            else
            {
                _c.MoveTo(_c.Blackboard.HomePosition);
            }
        }

        public void Tick(float deltaTime)
        {
            // Any sufficient suspicion pulls it off patrol.
            if (_c.Suspicion >= _c.Config.InvestigateThreshold)
            {
                _c.ChangeState(ListenerStateId.Investigate);
                return;
            }

            // Advance to the next waypoint once the current is reached.
            if (_c.ReachedDestination() && _c.Patrol != null && _c.Patrol.HasWaypoints)
            {
                _c.MoveTo(_c.Patrol.Advance());
            }
        }

        public void Exit()
        {
        }
    }
}
