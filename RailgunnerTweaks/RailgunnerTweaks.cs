using BepInEx;

namespace RailgunnerTweaks
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    // Soft dependency: load after Risk of Options if it's installed, but don't require it.
    [BepInDependency(RiskOfOptionsCompat.PluginGUID, BepInDependency.DependencyFlags.SoftDependency)]
    public class RailgunnerTweaksPlugin : BaseUnityPlugin
    {
        public const string PluginAuthor = "Bishey";
        public const string PluginName = "RailgunnerTweaks";
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginVersion = "1.0.0";

        private Feature[] features;

        public void Awake()
        {
            Log.Init(Logger);

            PluginConfig.Bind(Config);

            features = new Feature[]
            {
                new CrosshairHider(),
                new VariableZoom(),
                new ScopedSensitivity(),
            };

            // Attach the hooks of every feature that starts enabled.
            foreach (var feature in features)
                feature.Refresh();

            // Fires whenever any setting changes, e.g. from the Risk of Options menu.
            Config.SettingChanged += (_, args) =>
            {
                foreach (var feature in features)
                {
                    feature.Refresh();
                    feature.OnConfigChanged(args.ChangedSetting);
                }
            };

            if (RiskOfOptionsCompat.IsInstalled)
                RiskOfOptionsCompat.Register();

            Log.Info("RailgunnerTweaks loaded.");
        }
    }
}
