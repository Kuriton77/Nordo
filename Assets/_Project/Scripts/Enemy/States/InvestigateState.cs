using Nordo.Core;
using UnityEngine;

namespace Nordo.Enemy
{
    /// <summary>
    /// Move to the exact spot the last sound came from. If it keeps hearing things on the way (or a
    /// loud one), suspicion climbs into a chase. On arrival it pauses and "listens" for a moment; if
    /// nothing more comes, it downgrades to a search of the area, or gives up and returns.
    /// </summary>
    public sealed class InvestigateState : IListenerState
    {
        private readonly ListenerController _c;
        private float _lookTimer;
        private bool _arrived;
        private float _targetStamp;

        public InvestigateState(ListenerController controller) => _c = controller;

        public ListenerStateId Id => ListenerStateId.Investigate;

        public void Enter()
        {
            _c.SetSpeed(_c.Config.InvestigateSpeed);
            _arrived = false;
            _lookTimer = 0f;
            GoToLastHeard();
        }

        public void Tick(float deltaTime)
        {
            // Escalate to a chase if suspicion is now high.
            if (_c.Suspicion >= _c.Config.ChaseThreshold)
            {
                _c.ChangeState(ListenerStateId.Chase);
                return;
            }

            // If a newer sound arrived, re-path to it.
            if (_c.Blackboard.LastHeardTime > _targetStamp)
            {
                _arrived = false;
                GoToLastHeard();
            }

            if (!_arrived)
            {
                if (_c.ReachedDestination())
                {
                    _arrived = true;
                    _lookTimer = _c.Config.InvestigateLookTime;
                    _c.StopMoving();
                }

                return;
            }

            // Arrived: look around for a beat, facing the last known spot.
            _c.FaceTowards(_c.Blackboard.LastHeardPosition, deltaTime);
            _lookTimer -= deltaTime;
            if (_lookTimer <= 0f)
            {
                // Still uneasy → sweep the area; otherwise head home.
                _c.ChangeState(_c.Suspicion >= _c.Config.InvestigateThreshold * 0.5f
                    ? ListenerStateId.Search
                    : ListenerStateId.Return);
            }
        }

        public void Exit()
        {
        }

        private void GoToLastHeard()
        {
            _targetStamp = _c.Blackboard.LastHeardTime;
            _c.MoveTo(_c.Blackboard.LastHeardPosition);
        }
    }
}
