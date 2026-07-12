using Nordo.Core;
using UnityEngine;

namespace Nordo.Enemy
{
    /// <summary>
    /// Full pursuit. It runs to the continuously-updated last-heard position — so as long as the
    /// player keeps making noise, it keeps closing. Get within strike range and it attacks; but the
    /// instant the player goes silent for long enough (the config's lose-target window), it loses the
    /// trail and drops to a search. This is the beating heart of the "noise is lethal" fantasy.
    /// </summary>
    public sealed class ChaseState : IListenerState
    {
        private readonly ListenerController _c;
        private float _repathStamp;

        public ChaseState(ListenerController controller) => _c = controller;

        public ListenerStateId Id => ListenerStateId.Chase;

        public void Enter()
        {
            _c.SetSpeed(_c.Config.ChaseSpeed);
            _repathStamp = -1f;
            Repath();
        }

        public void Tick(float deltaTime)
        {
            // Close enough to strike?
            if (_c.DistanceToPlayer() <= _c.Config.AttackRange)
            {
                _c.ChangeState(ListenerStateId.Attack);
                return;
            }

            // Lost the trail: player has been silent past the lose-target window.
            if (_c.Blackboard.TimeSinceLastNoise > _c.Config.LoseTargetTime)
            {
                _c.ChangeState(ListenerStateId.Search);
                return;
            }

            // Keep charging the freshest heard position.
            if (_c.Blackboard.LastHeardTime > _repathStamp)
            {
                Repath();
            }
        }

        public void Exit()
        {
        }

        private void Repath()
        {
            _repathStamp = _c.Blackboard.LastHeardTime;
            _c.MoveTo(_c.Blackboard.LastHeardPosition);
        }
    }
}
