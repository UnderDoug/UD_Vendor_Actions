using System.Collections.Generic;

using Qud.UI;

using XRL;
using XRL.UI;
using XRL.World;
using XRL.World.Parts;
using XRL.World.Tinkering;

using UD_Modding_Toolbox;

namespace UD_Vendor_Actions
{
    // Start-up calls in order that they happen.

    [HasModSensitiveStaticCache]
    public static class UD_Vendor_Actions_ModBasedInitialiser
    {
        [ModSensitiveCacheInit]
        public static void AdditionalSetup()
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
    }

    [HasGameBasedStaticCache]
    public static class UD_Vendor_Actions_GameBasedInitialiser
    {
        [GameBasedCacheInit]
        public static void AdditionalSetup()
        {
            // Called once when world is first generated.

            // The.Game registered events should go here.
        }
    }

    [PlayerMutator]
    public class UD_Vendor_Actions_OnPlayerLoad : IPlayerMutator
    {
        public void mutate(GameObject player)
        {
            // Gets called once when the player is first generated
        }
    }

    [HasCallAfterGameLoaded]
    public class UD_Vendor_Actions_OnLoadGameHandler
    {
        [CallAfterGameLoaded]
        public static void OnLoadGameCallback()
        {
            // Gets called every time the game is loaded but not during generation
        }
    }
}