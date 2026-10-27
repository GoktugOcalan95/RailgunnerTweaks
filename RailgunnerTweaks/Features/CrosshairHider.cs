using RoR2.UI;
using System;
using UnityEngine;

namespace RailgunnerTweaks
{
    /// <summary>
    /// Hides decorative parts of Railgunner's crosshair and the default sniper's scope frame.
    /// Elements are hidden with a zero-opacity CanvasGroup, since CrosshairController
    /// toggles some of them every frame and would undo SetActive(false).
    /// </summary>
    internal class CrosshairHider : Feature
    {
        // Object names in the crosshair prefabs.
        private const string NormalCrosshairName = "RailgunnerCrosshair";
        private const string ScopeCrosshairName = "RailgunnerScopeCrosshair";            // both scopes
        private const string HeavyScopeCrosshairName = "RailgunnerScopeCrosshairHeavy";  // default sniper
        private const string FlavorReady = "Flavor, Ready";
        private const string FlavorUnknown = "Flavor, Unknown";
        private const string ScopeAvailable = "Available";
        private const string ScopeOutline = "Outline";

        public override string Name => "Crosshair hiding";

        protected override bool ShouldBeHooked =>
            PluginConfig.HideCrosshairFlavor.Value || PluginConfig.HideDefaultScopeFrame.Value;

        protected override void Hook()
        {
            On.RoR2.UI.CrosshairController.Awake += CrosshairController_Awake;
        }

        protected override void Unhook()
        {
            On.RoR2.UI.CrosshairController.Awake -= CrosshairController_Awake;
        }

        private static void CrosshairController_Awake(On.RoR2.UI.CrosshairController.orig_Awake orig, CrosshairController self)
        {
            orig(self);

            try
            {
                Apply(self.transform);
            }
            catch (Exception e)
            {
                // Errors are logged so they can't break the crosshair.
                Log.Error($"Failed to modify crosshair '{self.name}': {e}");
            }
        }

        private static void Apply(Transform crosshair)
        {
            // Spawned copies end in "(Clone)", so match the start of the name.
            string name = crosshair.name;
            bool isRailgunnerCrosshair = name.StartsWith(NormalCrosshairName) || name.StartsWith(ScopeCrosshairName);
            if (!isRailgunnerCrosshair) return;

            if (PluginConfig.HideCrosshairFlavor.Value)
            {
                // Includes inactive children, covering the copy nested inside the scope crosshairs.
                foreach (var child in crosshair.GetComponentsInChildren<Transform>(includeInactive: true))
                {
                    if (child.name == FlavorReady || child.name == FlavorUnknown)
                        Hide(child);
                }
            }

            if (PluginConfig.HideDefaultScopeFrame.Value && name.StartsWith(HeavyScopeCrosshairName))
            {
                // The Outline objects are the pink brackets and zoom text. The dot and reticle marks stay.
                var available = crosshair.Find(ScopeAvailable);
                if (available)
                {
                    foreach (Transform child in available)
                    {
                        if (child.name == ScopeOutline)
                            Hide(child);
                    }
                }
            }
        }

        private static void Hide(Transform target)
        {
            var group = target.GetComponent<CanvasGroup>();
            if (!group)
                group = target.gameObject.AddComponent<CanvasGroup>();

            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }
    }
}
