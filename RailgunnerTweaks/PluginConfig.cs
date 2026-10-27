using BepInEx.Configuration;
using System.Globalization;

namespace RailgunnerTweaks
{
    /// <summary>All of the mod's settings, stored in BepInEx/config/Bishey.RailgunnerTweaks.cfg.</summary>
    internal static class PluginConfig
    {
        // Each section is a tab in the Risk of Options menu.
        public const string GeneralSection = "General";
        public const string DefaultZoomSection = "Zoom - Default";
        public const string AlternateZoomSection = "Zoom - Alternate";

        public const float MinZoom = 1f;
        public const float MaxZoom = 10f;
        public const float ZoomStep = 0.1f;
        public const float MaxTransitionTime = 1f;
        public const int MinScopedSensitivity = 1;
        public const int MaxScopedSensitivity = 100;

        // Change 2: crosshair
        public static ConfigEntry<bool> HideCrosshairFlavor;
        public static ConfigEntry<bool> HideDefaultScopeFrame;

        // Change 1: variable zoom
        public static ConfigEntry<bool> EnableVariableZoom;
        public static ConfigEntry<bool> InvertScroll;
        public static ConfigEntry<float> ZoomTransitionTime;

        // Change 3: scoped sensitivity
        public static ConfigEntry<bool> ZeroMonitorMatch;
        public static ConfigEntry<int> ScopedSensitivityPercent;

        // Index 0 = level 1, index 1 = level 2, index 2 = level 3.
        public static readonly ConfigEntry<bool>[] DefaultLevelEnabled = new ConfigEntry<bool>[3];
        public static readonly ConfigEntry<float>[] DefaultLevel = new ConfigEntry<float>[3];
        public static readonly ConfigEntry<bool>[] AlternateLevelEnabled = new ConfigEntry<bool>[3];
        public static readonly ConfigEntry<float>[] AlternateLevel = new ConfigEntry<float>[3];

        public static void Bind(ConfigFile config)
        {
            HideCrosshairFlavor = config.Bind(GeneralSection, "HideCrosshairFlavor", true,
                "Hides the decorative yellow frame and status text around Railgunner's crosshair. " +
                "Takes effect on your next scope-in.");

            HideDefaultScopeFrame = config.Bind(GeneralSection, "HideDefaultScopeFrame", true,
                "Hides the pink frame and zoom text inside the default sniper's scope. " +
                "Takes effect on your next scope-in.");

            EnableVariableZoom = config.Bind(GeneralSection, "EnableVariableZoom", true,
                "Lets you change the scope's zoom with the mouse wheel.");

            InvertScroll = config.Bind(GeneralSection, "InvertScroll", false,
                "When off, scrolling up zooms in. When on, scrolling down zooms in.");

            ZoomTransitionTime = config.Bind(GeneralSection, "ZoomTransitionTime", 0.1f,
                new ConfigDescription("How long a zoom change takes, in seconds. 0 makes it instant.",
                    new AcceptableValueRange<float>(0f, MaxTransitionTime)));

            ZeroMonitorMatch = config.Bind(GeneralSection, "ZeroMonitorMatch", true,
                "While scoped, scales mouse sensitivity with 0% monitor match (focal length scaling), " +
                "so aiming feels consistent at every zoom level.");

            ScopedSensitivityPercent = config.Bind(GeneralSection, "ScopedSensitivityPercent", 100,
                new ConfigDescription(
                    "Mouse sensitivity while scoped, as a percentage, applied on top of the scaling above. " +
                    "100 means same sensitivity scoped or unscoped.",
                    new AcceptableValueRange<int>(MinScopedSensitivity, MaxScopedSensitivity)));

            BindLevels(config, DefaultZoomSection, "Default", "default sniper", 3.6f,
                new[] { 2f, 4f, 8f }, DefaultLevelEnabled, DefaultLevel);

            BindLevels(config, AlternateZoomSection, "Alternate", "alternate sniper", 2.2f,
                new[] { 2f, 3f, 5f }, AlternateLevelEnabled, AlternateLevel);
        }

        private static void BindLevels(ConfigFile config, string section, string keyPrefix, string weaponName,
            float vanillaZoom, float[] defaults, ConfigEntry<bool>[] enabled, ConfigEntry<float>[] values)
        {
            string vanilla = Format(vanillaZoom);

            for (int i = 0; i < 3; i++)
            {
                int level = i + 1;

                enabled[i] = config.Bind(section, $"{keyPrefix}Level{level}Enabled", true,
                    $"Whether zoom level {level} is available for the {weaponName}. If every level is disabled, this scope uses vanilla zoom.");

                values[i] = config.Bind(section, $"{keyPrefix}Level{level}", defaults[i],
                    new ConfigDescription(
                        $"Level {level} zoom amount for {weaponName}. Vanilla zoom is {vanilla}x at 60 FOV, mod default is {Format(defaults[i])}x.",
                        new AcceptableValueRange<float>(MinZoom, MaxZoom)));
            }
        }

        // Always use "." as the decimal separator, whatever the player's system language.
        private static string Format(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
