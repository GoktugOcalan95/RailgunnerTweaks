#pragma warning disable Publicizer001

using BepInEx.Configuration;
using EntityStates.Railgunner.Scope;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace RailgunnerTweaks
{
    /// <summary>
    /// Change 1: mouse-wheel zoom levels for both of Railgunner's scopes.
    ///
    /// Each scope state (wind-up, active, wind-down) adds a camera override in StartScopeParamsOverride.
    /// The hook takes over that override's FOV and keeps it at the selected zoom level until the game
    /// discards it. Levels are true magnification relative to the live unscoped view, including any
    /// sprint FOV multiplier. While fully scoped in, the mouse wheel steps between levels.
    /// </summary>
    internal class VariableZoom : Feature
    {
        private enum Scope { Default = 0, Alternate = 1 }

        /// <summary>A scope camera override whose FOV is being controlled.</summary>
        private class TrackedOverride
        {
            public CameraTargetParams.CameraParamsOverride cameraOverride;
            public CameraTargetParams targetParams;
            public Scope scope;
            public float originalFov;
        }

        /// <summary>Zoom state for one scope: level list, remembered level and easing.</summary>
        private class ScopeZoom
        {
            public List<float> levels;   // sorted, de-duplicated enabled levels; null = rebuild
            public int index = -1;       // position in levels; -1 = starting level

            private float easeFrom, easeTo;
            private float easeStartTime = float.NegativeInfinity;

            public bool HasValue => easeTo > 0f;

            /// <summary>Current magnification, including any zoom change in progress.</summary>
            public float Current
            {
                get
                {
                    float duration = PluginConfig.ZoomTransitionTime.Value;
                    float elapsed = Time.time - easeStartTime;
                    if (duration <= 0f || elapsed >= duration) return easeTo;

                    float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                    // Eased in log space, so equal zoom ratios take equal time.
                    return Mathf.Exp(Mathf.Lerp(Mathf.Log(easeFrom), Mathf.Log(easeTo), t));
                }
            }

            public void SnapTo(float zoom)
            {
                easeFrom = easeTo = zoom;
                easeStartTime = float.NegativeInfinity;
            }

            public void EaseTo(float zoom)
            {
                easeFrom = HasValue ? Current : zoom;
                easeTo = zoom;
                easeStartTime = Time.time;
            }
        }

        /// <summary>Runs the per-frame work. Exists only while the feature is enabled.</summary>
        private class Driver : MonoBehaviour
        {
            public VariableZoom owner;
            private void Update() => owner.Tick();
        }

        private const string RailgunnerBodyName = "RailgunnerBody";

        private readonly ScopeZoom[] zooms = { new ScopeZoom(), new ScopeZoom() };
        private readonly List<TrackedOverride> tracked = new List<TrackedOverride>();

        private BaseActive activeState;      // the fully scoped-in state, if any
        private Scope activeScope;
        private float sprintFovMultiplier = 1f;
        private GameObject driverObject;

        public override string Name => "Variable zoom";

        protected override bool ShouldBeHooked => PluginConfig.EnableVariableZoom.Value;

        protected override void Hook()
        {
            On.EntityStates.Railgunner.Scope.BaseScopeState.StartScopeParamsOverride += BaseScopeState_StartScopeParamsOverride;
            On.EntityStates.Railgunner.Scope.BaseActive.OnExit += BaseActive_OnExit;

            driverObject = new GameObject("RailgunnerTweaks.VariableZoom");
            Object.DontDestroyOnLoad(driverObject);
            driverObject.hideFlags = HideFlags.HideAndDontSave;
            driverObject.AddComponent<Driver>().owner = this;
        }

        protected override void Unhook()
        {
            On.EntityStates.Railgunner.Scope.BaseScopeState.StartScopeParamsOverride -= BaseScopeState_StartScopeParamsOverride;
            On.EntityStates.Railgunner.Scope.BaseActive.OnExit -= BaseActive_OnExit;

            // Restore vanilla FOV on any overrides still active when the feature is turned off.
            foreach (var t in tracked)
            {
                if (t.cameraOverride != null)
                    t.cameraOverride.cameraParamsData.fov.value = t.originalFov;
            }
            tracked.Clear();
            activeState = null;

            if (driverObject) Object.Destroy(driverObject);
            driverObject = null;
        }

        public override void OnConfigChanged(ConfigEntryBase changedSetting)
        {
            // A change to a scope's level settings resets that scope to its starting level.
            if (Contains(PluginConfig.DefaultLevelEnabled, changedSetting) || Contains(PluginConfig.DefaultLevel, changedSetting))
                ResetScope(Scope.Default);
            else if (Contains(PluginConfig.AlternateLevelEnabled, changedSetting) || Contains(PluginConfig.AlternateLevel, changedSetting))
                ResetScope(Scope.Alternate);
        }

        // ---------- Hooks ----------

        private void BaseScopeState_StartScopeParamsOverride(
            On.EntityStates.Railgunner.Scope.BaseScopeState.orig_StartScopeParamsOverride orig,
            BaseScopeState self, float transitionDuration)
        {
            bool alreadyHadOverride = self.cameraParamsOverrideHandle.isValid;
            orig(self, transitionDuration);
            if (alreadyHadOverride) return; // no new override was added

            // Local player only.
            if (!self.isAuthority || !self.characterBody || !self.characterBody.isPlayerControlled) return;

            Scope? scope = GetScope(self);
            var handle = self.cameraParamsOverrideHandle;
            if (scope == null || !handle.isValid || !self.cameraTargetParams) return;

            var levels = GetLevels(scope.Value);
            if (levels.Count == 0) return; // all levels disabled: scope stays vanilla

            var zoom = zooms[(int)scope.Value];

            // A new scope-in starts on the remembered level without easing.
            if (self is BaseWindUp || !zoom.HasValue)
                zoom.SnapTo(levels[GetIndex(zoom, levels)]);

            if (self is BaseActive active)
            {
                activeState = active;
                activeScope = scope.Value;
            }

            var cameraOverride = handle.target;
            tracked.Add(new TrackedOverride
            {
                cameraOverride = cameraOverride,
                targetParams = self.cameraTargetParams,
                scope = scope.Value,
                originalFov = cameraOverride.cameraParamsData.fov.value,
            });

            cameraOverride.cameraParamsData.fov.value = CalculateOverrideFov(self.cameraTargetParams, zoom.Current);
        }

        private void BaseActive_OnExit(On.EntityStates.Railgunner.Scope.BaseActive.orig_OnExit orig, BaseActive self)
        {
            orig(self);
            if (activeState == self) activeState = null;
        }

        // ---------- Per frame ----------

        private void Tick()
        {
            UpdateSprintFovMultiplier();
            HandleScroll();
            UpdateTrackedOverrides();
        }

        private void HandleScroll()
        {
            if (activeState == null) return;
            if (!activeState.outer)
            {
                activeState = null;
                return;
            }

            float delta = Input.mouseScrollDelta.y;
            if (delta == 0f) return;

            int step = delta > 0f ? 1 : -1;
            if (PluginConfig.InvertScroll.Value) step = -step;

            var zoom = zooms[(int)activeScope];
            var levels = GetLevels(activeScope);
            if (levels.Count < 2) return;

            int current = GetIndex(zoom, levels);
            int next = Mathf.Clamp(current + step, 0, levels.Count - 1);
            if (next == current) return;

            zoom.index = next;
            zoom.EaseTo(levels[next]);
        }

        private void UpdateTrackedOverrides()
        {
            float now = Time.time;
            for (int i = tracked.Count - 1; i >= 0; i--)
            {
                var t = tracked[i];
                if (!t.targetParams || t.cameraOverride == null || t.cameraOverride.exitEndTime <= now)
                {
                    tracked.RemoveAt(i); // override finished and discarded by the game
                    continue;
                }

                var levels = GetLevels(t.scope);
                t.cameraOverride.cameraParamsData.fov.value = levels.Count == 0
                    ? t.originalFov // all levels disabled mid-scope: vanilla FOV
                    : CalculateOverrideFov(t.targetParams, zooms[(int)t.scope].Current);
            }
        }

        /// <summary>
        /// The game multiplies the final FOV by a sprint multiplier.
        /// It isn't exposed, so it is measured as rendered FOV divided by
        /// camera data FOV, smoothed to hide one-frame timing differences.
        /// </summary>
        private void UpdateSprintFovMultiplier()
        {
            foreach (var rig in CameraRigController.readOnlyInstancesList)
            {
                var body = rig ? rig.targetBody : null;
                if (!body || !rig.sceneCam || !body.name.StartsWith(RailgunnerBodyName)) continue;

                var targetParams = body.GetComponent<CameraTargetParams>();
                if (!targetParams) continue;

                float paramsFov = targetParams.currentCameraParamsData.fov.value;
                if (paramsFov < 1f) continue;

                float measured = Mathf.Clamp(rig.sceneCam.fieldOfView / paramsFov, 0.5f, 3f);
                float blend = 1f - Mathf.Exp(-Time.deltaTime / 0.05f);
                sprintFovMultiplier = Mathf.Lerp(sprintFovMultiplier, measured, blend);
                return;
            }
        }

        // ---------- Math ----------

        /// <summary>
        /// Override FOV that renders the given true magnification of the rendered unscoped view.
        /// Magnification is the ratio of the tangents of the half-angles.
        /// </summary>
        private float CalculateOverrideFov(CameraTargetParams targetParams, float magnification)
        {
            magnification = Mathf.Max(magnification, 1f);
            float multiplier = sprintFovMultiplier;

            float renderedUnscoped = CameraFov.GetUnscopedFov(targetParams) * multiplier;
            float halfAngle = renderedUnscoped * 0.5f * Mathf.Deg2Rad;
            float renderedScoped = 2f * Mathf.Atan(Mathf.Tan(halfAngle) / magnification) * Mathf.Rad2Deg;

            // The game applies the sprint multiplier to this value, so it is divided out here.
            return renderedScoped / multiplier;
        }

        // ---------- Levels ----------

        private List<float> GetLevels(Scope scope)
        {
            var zoom = zooms[(int)scope];
            if (zoom.levels == null)
            {
                zoom.levels = scope == Scope.Default
                    ? BuildLevels(PluginConfig.DefaultLevelEnabled, PluginConfig.DefaultLevel)
                    : BuildLevels(PluginConfig.AlternateLevelEnabled, PluginConfig.AlternateLevel);
            }
            return zoom.levels;
        }

        /// <summary>Enabled levels, sorted from least to most zoom, with duplicates merged.</summary>
        private static List<float> BuildLevels(ConfigEntry<bool>[] enabled, ConfigEntry<float>[] values)
        {
            var levels = new List<float>();
            for (int i = 0; i < enabled.Length; i++)
            {
                if (!enabled[i].Value) continue;

                float value = Mathf.Clamp(values[i].Value, PluginConfig.MinZoom, PluginConfig.MaxZoom);
                value = Mathf.Round(value * 100f) / 100f; // removes float noise from the sliders

                if (!levels.Exists(existing => Mathf.Abs(existing - value) < 0.001f))
                    levels.Add(value);
            }
            levels.Sort();
            return levels;
        }

        /// <summary>Remembered level, or the starting level: middle of three, higher of two, or the only one.</summary>
        private static int GetIndex(ScopeZoom zoom, List<float> levels)
        {
            if (zoom.index < 0 || zoom.index >= levels.Count)
                zoom.index = levels.Count / 2;
            return zoom.index;
        }

        private void ResetScope(Scope scope)
        {
            var zoom = zooms[(int)scope];
            zoom.levels = null;
            zoom.index = -1;

            // If this scope is in use, ease to the new starting level.
            var levels = GetLevels(scope);
            if (levels.Count > 0 && tracked.Exists(t => t.scope == scope))
                zoom.EaseTo(levels[GetIndex(zoom, levels)]);
        }

        // ---------- Helpers ----------

        private static Scope? GetScope(BaseScopeState state)
        {
            if (state is WindUpScopeHeavy || state is ActiveScopeHeavy || state is WindDownScopeHeavy) return Scope.Default;
            if (state is WindUpScopeLight || state is ActiveScopeLight || state is WindDownScopeLight) return Scope.Alternate;
            return null;
        }

        private static bool Contains<T>(T[] entries, ConfigEntryBase setting) where T : ConfigEntryBase
        {
            foreach (var entry in entries)
                if (ReferenceEquals(entry, setting)) return true;
            return false;
        }
    }
}
