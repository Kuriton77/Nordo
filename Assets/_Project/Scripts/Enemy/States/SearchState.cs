using Nordo.Core;
using UnityEngine;

namespace Nordo.Enemy
{
    /// <summary>
    /// Sweep the area around a lost sound, moving between sampled navmesh points, hoping to hear
    /// something else. Any fresh noise pulls it back to investigate or straight into a chase; if the
    /// search times out, it gives up and returns to patrol. This is the window where a hidden, silent
    /// player is safest — and a single careless sound is fatal.
    /// </summary>
    public sealed class SearchState : IListenerState
    {
        private readonly ListenerController _c;
        private float _endTime;
        private float _lastHeardStamp;

        public SearchState(ListenerController controller) => _c = controller;

        public ListenerStateId Id => ListenerStateId.Search;

        public void Enter()
        {
            _c.SetSpeed(_c.Config.InvestigateSpeed * 0.9f);
            _endTime = Time.time + _c.Config.SearchDuration;
            _lastHeardStamp = _c.Blackboard.LastHeardTime;
            PickNewSearchPoint();
        }

        public void Tick(float deltaTime)
        {
            if (_c.Suspicion >= _c.Config.ChaseThreshold)
            {
                _c.ChangeState(ListenerStateId.Chase);
                return;
            }

            // A newer sound than when we started searching → investigate it.
            if (_c.Blackboard.LastHeardTime > _lastHeardStamp && _c.Suspicion >= _c.Config.InvestigateThreshold)
            {
                _c.ChangeState(ListenerStateId.Investigate);
                return;
            }

            if (Time.time >= _endTime)
            {
                _c.ChangeState(ListenerStateId.Return);
                return;
            }

            if (_c.ReachedDestination())
            {
                PickNewSearchPoint();
            }
        }

        public void Exit()
        {
        }

        private void PickNewSearchPoint()
        {
            if (_c.TrySampleAround(_c.Blackboard.LastHeardPosition, _c.Config.SearchRadius, out Vector3 point))
            {
                _c.MoveTo(point);
            }
        }
    }
}
