using UnityEngine;

namespace Nordo.CameraFeel
{
    /// <summary>
    /// The additive contribution of a single camera effect for one frame: a local positional
    /// offset and a local rotational (Euler) offset. Effects return one of these; the
    /// <see cref="CameraRig"/> sums them and applies the total to the camera-effects transform.
    /// Using additive offsets (rather than having each effect write the transform directly)
    /// is what lets any number of effects compose cleanly — the essence of the modular stack.
    /// </summary>
    public readonly struct CameraEffectSample
    {
        /// <summary>Local-space position offset, in metres.</summary>
        public readonly Vector3 PositionOffset;

        /// <summary>Local-space rotation offset, in degrees (pitch, yaw, roll).</summary>
        public readonly Vector3 EulerOffset;

        public CameraEffectSample(Vector3 positionOffset, Vector3 eulerOffset)
        {
            PositionOffset = positionOffset;
            EulerOffset = eulerOffset;
        }

        /// <summary>A no-op sample that contributes nothing.</summary>
        public static CameraEffectSample Zero => new(Vector3.zero, Vector3.zero);

        /// <summary>Sums two samples component-wise so contributions accumulate.</summary>
        public static CameraEffectSample operator +(CameraEffectSample a, CameraEffectSample b)
        {
            return new CameraEffectSample(a.PositionOffset + b.PositionOffset, a.EulerOffset + b.EulerOffset);
        }
    }
}
