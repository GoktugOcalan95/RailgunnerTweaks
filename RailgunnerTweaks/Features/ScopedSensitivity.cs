#pragma warning disable Publicizer001

using HarmonyLib;
using RoR2;
using RoR2.CameraModes;
using System.Reflection;
using UnityEngine;

namespace RailgunnerTweaks
{
    /// <summary>
    /// Change 3: scoped mouse sensitivity for Railgunner.
    ///
    /// Vanilla multiplies look input by renderedFov / baseFov. While scoped, this feature can replace
    /// that with 0% monitor match (tangent ratio against the rendered unscoped FOV) and apply a
    /// percentage. Unscoped sensitivity is never changed.
    ///
    /// Uses a Harmony postfix, since CollectLookInput has in/out parameters
    /// that are awkward to hook with MonoMod.
    /// </summary>
    internal class ScopedSensitivity : Feature
    {
        private const string RailgunnerBodyName = "RailgunnerBody";

        private static readonly MethodInfo CollectLookInput =
            AccessTools.Method(typeof(CameraModeBase), nameof(CameraModeBase.CollectLookInput));
        private static readonly MethodInfo PostfixMethod =
            AccessTools.Method(typeof(ScopedSensitivity), nameof(CollectLookInput_Postfix));

        private Harmony harmony;

        public override string Name => "Scoped sensitivity";

        protected override bool ShouldBeHooked =>
            PluginConfig.ZeroMonitorMatch.Value || PluginConfig.ScopedSensitivityPercent.Value != 100;

        protected override void Hook()
        {
            harmony ??= new Harmony(RailgunnerTweaksPlugin.PluginGUID + ".ScopedSensitivity");
            harmony.Patch(CollectLookInput, postfix: new HarmonyMethod(PostfixMethod));
        }

        protected override void Unhook()
        {
            harmony?.Unpatch(CollectLookInput, PostfixMethod);
        }

        private static void CollectLookInput_Postfix(ref CameraModeBase.CameraModeContext context, ref CameraModeBase.CollectLookInputResult result)
        {
            var targetParams = context.targetInfo.targetParams;
            if (!targetParams || !targetParams.name.StartsWith(RailgunnerBodyName)) return;

            float renderedFov = context.cameraInfo.previousCameraState.fov;
            float paramsFov = targetParams.currentCameraParamsData.fov.value;
            float unscopedFov = CameraFov.GetUnscopedFov(targetParams);

            // Scoped means a camera override has narrowed the FOV below the body's normal FOV.
            if (paramsFov < 1f || renderedFov < 1f || paramsFov >= unscopedFov - 0.01f) return;

            float factor = PluginConfig.ScopedSensitivityPercent.Value / 100f;

            if (PluginConfig.ZeroMonitorMatch.Value)
            {
                // Rendered FOV = camera data FOV * sprint multiplier, so the same ratio gives
                // the rendered unscoped FOV without measuring the multiplier separately.
                float renderedUnscoped = renderedFov * unscopedFov / paramsFov;

                // Vanilla scale is renderedFov / baseFov. The target is the unscoped scale
                // (renderedUnscoped / baseFov) times the 0% monitor match ratio, so baseFov cancels out.
                float tanRatio = Mathf.Tan(renderedFov * 0.5f * Mathf.Deg2Rad) / Mathf.Tan(renderedUnscoped * 0.5f * Mathf.Deg2Rad);
                factor *= renderedUnscoped / renderedFov * tanRatio;
            }

            result.lookInput *= factor;
        }
    }
}
