using BepInEx.Configuration;
using UnityEngine;

namespace RecipeHelper
{
    // All config entries, bound from the plugin's config file.
    internal sealed class PluginConfig
    {
        public readonly ConfigEntry<bool> Enabled;
        public readonly ConfigEntry<int> MaxChainSteps;

        public readonly ConfigEntry<bool> DebugLogging;
        public readonly ConfigEntry<bool> DebugAlwaysShow;
        public readonly ConfigEntry<KeyboardShortcut> DumpKey;

        public PluginConfig(ConfigFile config)
        {
            Enabled = config.Bind("Sidebar", "Enabled", true,
                "Show the list of craftable items beside the Wandering CHEF's crafting dialog.");
            MaxChainSteps = config.Bind("Sidebar", "MaxChainSteps", 3,
                new ConfigDescription("The most crafts in a row to look ahead, counting crafts whose results are used as ingredients in later ones.",
                    new AcceptableValueRange<int>(1, 5)));

            DebugLogging = config.Bind("Debug", "DebugLogging", false,
                "Enable the dump key, which logs the recipe catalog size and the plan for your inventory.");
            DebugAlwaysShow = config.Bind("Debug", "DebugAlwaysShow", false,
                "List every recipe regardless of your inventory, for testing the layout.");
            DumpKey = config.Bind("Debug", "DumpKey", new KeyboardShortcut(KeyCode.F8),
                "With DebugLogging on, press to log the recipe catalog size and the plan for your inventory.");
        }
    }
}
