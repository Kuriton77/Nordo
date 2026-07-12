using Nordo.Core;
using Nordo.Core.Events;
using UnityEngine;

namespace Nordo.Enemy
{
    /// <summary>
    /// The strike. It stops, faces the player, plays the attack animation, and after a short wind-up
    /// lands the blow — raising <see cref="PlayerCaughtEvent"/> if the player is still in range. Then a
    /// cooldown, after which it re-chases (if the player is near/audible) or drops to a search.
    /// </summary>
    public sealed class AttackState : IListenerState
    {
        private readonly ListenerController _c;
        private float _strikeTime;
        private float _recoverTime;
        private bool _struck;

        public AttackState(ListenerController controller) => _c = controller;

        public ListenerStateId Id => ListenerStateId.Attack;

        public void Enter()
        {
            _c.StopMoving();
            _c.PlayAttackAnimation();
            _struck = false;
            _strikeTime = Time.time + _c.Config.AttackWindup;
            _recoverTime = _strikeTime + _c.Config.AttackCooldown;
        }

        public void Tick(float deltaTime)
        {
            // Track the player through the wind-up.
            if (_c.Player != null)
            {
                _c.FaceTowards(_c.Player.position, deltaTime);
            }

            // Land the blow once, at the end of the wind-up.
            if (!_struck && Time.time >= _strikeTime)
            {
                _struck = true;
                if (_c.DistanceToPlayer() <= _c.Config.AttackRange * 1.25f)
                {
                    EventBus<PlayerCaughtEvent>.Raise(new PlayerCaughtEvent(_c.transform.position));
                }
            }

            // After recovery, decide what to do next.
            if (Time.time >= _recoverTime)
            {
                bool playerNear = _c.DistanceToPlayer() <= _c.Config.HearingRange
                                  && _c.Blackboard.TimeSinceLastNoise <= _c.Config.LoseTargetTime;
                _c.ChangeState(playerNear ? ListenerStateId.Chase : ListenerStateId.Search);
            }
        }

        public void Exit()
        {
        }
    }
}
