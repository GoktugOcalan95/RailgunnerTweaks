using BepInEx.Bootstrap;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using System.Runtime.CompilerServices;

namespace RailgunnerTweaks
{
    /// <summary>
    /// Adds the mod's settings to the in-game Risk of Options menu, if that mod is installed.
    /// Everything that touches Risk of Options lives in this class, and it is only called after
    /// checking IsInstalled, so the mod still loads fine for players without it.
    /// </summary>
    internal static class RiskOfOptionsCompat
    {
        public const string PluginGUID = "com.rune580.riskofoptions";

        public static bool IsInstalled => Chainloader.PluginInfos.ContainsKey(PluginGUID);

        // NoInlining keeps the runtime from loading Risk of Options before IsInstalled is checked.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Register()
        {
            ModSettingsManager.SetModDescription(
                "Quality-of-life tweaks for Railgunner: variable scope zoom, scoped sensitivity options, and optional removal of decorative HUD elements. " +
                "Every feature can be turned off, in which case the game behaves as vanilla.");

            ModSettingsManager.AddOption(new CheckBoxOption(PluginConfig.EnableVariableZoom));
            ModSettingsManager.AddOption(new CheckBoxOption(PluginConfig.InvertScroll));
            ModSettingsManager.AddOption(new StepSliderOption(PluginConfig.ZoomTransitionTime,
                new StepSliderConfig { min = 0f, max = PluginConfig.MaxTransitionTime, increment = 0.05f }));

            ModSettingsManager.AddOption(new CheckBoxOption(PluginConfig.ZeroMonitorMatch));
            ModSettingsManager.AddOption(new IntSliderOption(PluginConfig.ScopedSensitivityPercent,
                new IntSliderConfig { min = PluginConfig.MinScopedSensitivity, max = PluginConfig.MaxScopedSensitivity }));

            ModSettingsManager.AddOption(new CheckBoxOption(PluginConfig.HideCrosshairFlavor));
            ModSettingsManager.AddOption(new CheckBoxOption(PluginConfig.HideDefaultScopeFrame));

            AddLevels(PluginConfig.DefaultLevelEnabled, PluginConfig.DefaultLevel);
            AddLevels(PluginConfig.AlternateLevelEnabled, PluginConfig.AlternateLevel);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void AddLevels(BepInEx.Configuration.ConfigEntry<bool>[] enabled, BepInEx.Configuration.ConfigEntry<float>[] values)
        {
            for (int i = 0; i < enabled.Length; i++)
            {
                ModSettingsManager.AddOption(new CheckBoxOption(enabled[i]));
                ModSettingsManager.AddOption(new StepSliderOption(values[i],
                    new StepSliderConfig { min = PluginConfig.MinZoom, max = PluginConfig.MaxZoom, increment = PluginConfig.ZoomStep }));
            }
        }
    }
}
