using System;
using UnityEngine;
using Nordo.Core;
using Nordo.Core.Events;
using Nordo.Interaction;
using Nordo.Noise;

namespace Nordo.Progression
{
    /// <summary>
    /// The generator. Requires its prerequisite <see cref="FuseBox"/> to be powered; starting it kicks
    /// the attached <see cref="MachineNoiseEmitter"/> to life (a very loud beacon — the price of
    /// progress) and raises <see cref="Started"/>, which the level wires to restore power and advance
    /// the objective. This is the puzzle's climax and its tightest link to The Listener: the moment you
    /// restore power, you are at your loudest.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Generator : InteractableBase
    {
        [Tooltip("Fuse box that must be powered before the generator can start.")]
        [SerializeField] private FuseBox _prerequisite;

        [Tooltip("Machine emitter started with the generator (loud hum + start spike).")]
        [SerializeField] private MachineNoiseEmitter _machine;

        [SerializeField] private AudioClip _startClip;

        private bool _running;

        /// <summary>Whether the generator is running.</summary>
        public bool IsRunning => _running;

        /// <summary>Raised once when the generator starts.</summary>
        public event Action Started;

        /// <summary>Runtime wiring for the level builder.</summary>
        public void Configure(FuseBox prerequisite, MachineNoiseEmitter machine)
        {
            _prerequisite = prerequisite;
            _machine = machine;
        }

        /// <inheritdoc />
        public override string GetPrompt(in InteractionContext context)
        {
            if (_running)
            {
                return "Generator running";
            }

            return _prerequisite != null && !_prerequisite.IsPowered ? "Generator (no power to it)" : "Start generator";
        }

        /// <inheritdoc />
        protected override void OnInteract(in InteractionContext context)
        {
            if (_running)
            {
                return;
            }

            if (_prerequisite != null && !_prerequisite.IsPowered)
            {
                EventBus<GameMessageEvent>.Raise(new GameMessageEvent("Nothing. The fuse box has no charge — find a fuse."));
                return;
            }

            _running = true;

            if (_machine != null)
            {
                _machine.StartMachine();
            }

            if (_startClip != null)
            {
                AudioSource.PlayClipAtPoint(_startClip, transform.position);
            }

            EventBus<GameMessageEvent>.Raise(new GameMessageEvent("The generator coughs, then ROARS to life. Everything can hear it now.", 5f));
            Started?.Invoke();
        }
    }
}
