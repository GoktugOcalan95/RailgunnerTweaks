using RoR2;

namespace RailgunnerTweaks
{
    /// <summary>Camera FOV helpers shared by the zoom and sensitivity features.</summary>
    internal static class CameraFov
    {
        /// <summary>
        /// The body's normal FOV, before camera overrides and the sprint multiplier.
        /// Computed the same way as CameraTargetParams.CalcParams.
        /// </summary>
        public static float GetUnscopedFov(CameraTargetParams targetParams)
        {
            var data = CharacterCameraParamsData.basic;
            if (targetParams && targetParams.cameraParams)
            {
                var bodyData = targetParams.cameraParams.data;
                CharacterCameraParamsData.Blend(in bodyData, ref data, 1f);
            }
            return data.fov.value;
        }
    }
}
