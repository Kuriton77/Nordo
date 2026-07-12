using System;
using UnityEngine;
using Nordo.Level;

namespace Nordo.Boot
{
    /// <summary>
    /// The single component in the startup scene. On play it builds the player rig (via
    /// <see cref="PlayerRigFactory"/>) and then the first Vardø-9 section (via
    /// <see cref="StationEntranceBuilder"/>), which finds the freshly-built, "Player"-tagged rig and
    /// assembles the level around it. The result: open the project, press Play, and the game runs — no
    /// manual scene reconstruction required.
    /// <para>
    /// The player build is guarded so that, even if it fails, the section still assembles and the
    /// project remains valid and open.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Tooltip("Build the player rig in code. Turn off if you provide your own 'Player'-tagged rig in the scene.")]
        [SerializeField] private bool _buildPlayerRig = true;

        [Tooltip("Where the player rig is first placed (the section repositions it to its own spawn).")]
        [SerializeField] private Vector3 _initialSpawn = new Vector3(0f, 1f, -9f);

        private void Start()
        {
            if (_buildPlayerRig && GameObject.FindGameObjectWithTag("Player") == null)
            {
                try
                {
                    PlayerRigFactory.Build(_initialSpawn);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[GameBootstrap] Player rig build failed: {e}");
                }
            }

            // Build the section next; its own Start (one frame later) finds the player that now exists.
            new GameObject("Vardo9_Builder").AddComponent<StationEntranceBuilder>();
        }
    }
}
