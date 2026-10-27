using BepInEx.Configuration;
using System;

namespace RailgunnerTweaks
{
    /// <summary>
    /// Base class for a toggleable feature. A feature's hooks are attached only while it is enabled,
    /// so a disabled feature leaves the game's code untouched, exactly as if the mod weren't installed.
    /// </summary>
    internal abstract class Feature
    {
        private bool isHooked;

        /// <summary>Name used in log messages.</summary>
        public abstract string Name { get; }

        /// <summary>Whether the current config wants this feature's hooks attached.</summary>
        protected abstract bool ShouldBeHooked { get; }

        protected abstract void Hook();
        protected abstract void Unhook();

        /// <summary>Called after any setting changes, for features that cache config values.</summary>
        public virtual void OnConfigChanged(ConfigEntryBase changedSetting) { }

        /// <summary>Attaches or detaches the hooks so they match the current config.</summary>
        public void Refresh()
        {
            bool wanted = ShouldBeHooked;
            if (wanted == isHooked) return;

            try
            {
                if (wanted) Hook();
                else Unhook();

                isHooked = wanted;
                Log.Info($"{Name}: {(wanted ? "enabled" : "disabled")}");
            }
            catch (Exception e)
            {
                Log.Error($"{Name}: failed to {(wanted ? "enable" : "disable")}: {e}");
            }
        }
    }
}
