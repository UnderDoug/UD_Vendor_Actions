using System.Collections.Generic;
using System;

using Qud.UI;

using XRL;
using XRL.UI;
using XRL.World;
using XRL.World.Parts;
using XRL.World.Tinkering;

using Version = XRL.Version;

using UD_Modding_Toolbox;

namespace UD_Vendor_Actions
{
    [HasModSensitiveStaticCache]
    [HasGameBasedStaticCache]
    [HasCallAfterGameLoaded]
    public static class Startup
    {
        private static string ThisModGameStatePrefix => Utils.ThisMod.ID + "::";

        public static bool SaveStartedWithVendorActions
        {
            get => (The.Game?.GetBooleanGameState(ThisModGameStatePrefix + nameof(GameBasedCacheInit))).GetValueOrDefault();
            private set => The.Game?.SetBooleanGameState(ThisModGameStatePrefix + nameof(GameBasedCacheInit), value);
        }

        public static Version? LastModVersionSaved
        {
            get => The.Game?.GetObjectGameState(ThisModGameStatePrefix + nameof(Version)) as Version?;
            private set => The.Game?.SetObjectGameState(ThisModGameStatePrefix + nameof(Version), value);
        }

        /// <summary>
        /// Set manually between versions to issue a single warning for existing saves 
        /// </summary>
        public static bool NeedVersionMismatchWarning => false;

        [GameBasedStaticCache]
        [ModSensitiveStaticCache]
        public static bool ModVersionWarningIssued = false;

        // Start-up calls in order that they happen.

        [ModSensitiveCacheInit]
        public static void ModSensitiveCacheInit()
        {
            // Called at game startup and whenever mod configuration changes

            if (GameObjectFactory.Factory.GetBlueprintIfExists("UD_PhysicsCategoryIcons") is GameObjectBlueprint UD_PhysicsCategoryIcons)
            {
                foreach ((string name, string value) in UD_PhysicsCategoryIcons.Tags)
                {
                    if (!name.IsNullOrEmpty() && !value.IsNullOrEmpty() && !FilterBarCategoryButton.categoryImageMap.IsNullOrEmpty())
                    {
                        if (!FilterBarCategoryButton.categoryImageMap.ContainsKey(name))
                        {
                            FilterBarCategoryButton.categoryImageMap.Add(name, value);
                        }
                        else
                        {
                            FilterBarCategoryButton.categoryImageMap[name] = value;
                        }
                    }
                }
            }
        }

        [GameBasedCacheInit]
        public static void GameBasedCacheInit()
        {
            // Called once when world is first generated.

            // The.Game registered events should go here.

            SaveStartedWithVendorActions = true;
            LastModVersionSaved = Utils.ThisMod.Manifest.Version;
        }

        // [PlayerMutator]

        // The.Player.FireEvent("GameRestored");
		// AfterGameLoadedEvent.Send(Return);  // Return is the game.

        [CallAfterGameLoaded]
        public static void OnLoadGameCallback()
        {
            // Gets called every time the game is loaded (from a save), but not during generation, after Reader has finished
            if (Options.EnableWarningsForBigJumpsInModVersion && Utils.ThisMod.Manifest.Version is Version newestVersion)
            {
                if (LastModVersionSaved is not Version savedVersion
                    || newestVersion.Minor > savedVersion.Minor
                    || newestVersion.Major > savedVersion.Major)
                {
                    if (NeedVersionMismatchWarning && !ModVersionWarningIssued)
                    {
                        ModManifest thisModManifest = Utils.ThisMod.Manifest;
                        savedVersion = LastModVersionSaved.GetValueOrDefault();
                        Popup.Show(thisModManifest.Title + " version mismatch:\n\n" +
                            "The version of " + thisModManifest.Title.Strip() + " used by this save is " +
                            "{{C|v" + savedVersion + "}} while the one currently enabled is {{C|v" + newestVersion + "}}." +
                            "\n\nSee this mod's {{C|\"Change Notes\"}} on the {{b|steam workshop}} for information on its backwards compatibility." +
                            "\n\nTo revert this save to its pre-migration state use {{hotkey|alt + F4}} to exit the game without saving " +
                            "(this should work in most circumstances)." +
                            "\n\nThere is an option to turn off these warnings.");
                        ModVersionWarningIssued = true;
                    }
                    else
                    {
                        ModVersionWarningIssued = false;
                    }
                }
                LastModVersionSaved = Utils.ThisMod.Manifest.Version;
            }
        }
    }

    // [ModSensitiveCacheInit]

    // [GameBasedCacheInit]

    [PlayerMutator]
    public class UD_Vendor_Actions_OnPlayerLoad : IPlayerMutator
    {
        public void mutate(GameObject player)
        {
            // Gets called once when the player is first generated
        }
    }

    // [CallAfterGameLoaded]
}