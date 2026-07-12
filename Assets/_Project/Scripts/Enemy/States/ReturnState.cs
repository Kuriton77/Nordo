using Nordo.Core;
using UnityEngine;

namespace Nordo.Enemy
{
    /// <summary>
    /// Give up and walk back to the patrol route (or home). If something is heard on the way, it
    /// snaps back to investigating; otherwise, on arrival, it resumes a calm patrol.
    /// </summary>
    public sealed class ReturnState : IListenerState
    {
        private readonly ListenerController _c;

        public ReturnState(ListenerController controller) => _c = controller;

        public ListenerStateId Id => ListenerStateId.Return;

        public void Enter()
        {
            _c.SetSpeed(_c.Config.ReturnSpeed);

            Vector3 destination = _c.Patrol != null && _c.Patrol.HasWaypoints
                ? _c.Patrol.SelectNearest(_c.transform.position)
                : _c.Blackboard.HomePosition;

            _c.MoveTo(destination);
        }

        public void Tick(float deltaTime)
        {
            if (_c.Suspicion >= _c.Config.InvestigateThreshold)
            {
                _c.ChangeState(ListenerStateId.Investigate);
                return;
            }

            if (_c.ReachedDestination())
            {
                _c.ChangeState(ListenerStateId.Patrol);
            }
        }

        public void Exit()
        {
        }
    }
}
