using HarmonyLib;

using System;
using System.Collections.Generic;

using Qud.UI;

using XRL;
using XRL.UI;
using XRL.UI.Framework;
using XRL.World;

using UnityEngine.EventSystems;

namespace UD_Vendor_Actions.Harmony
{
    [HarmonyPatch]
    public static class TradeScreen_Patches
    {
        [HarmonyPatch(
            declaringType: typeof(TradeScreen),
            methodName: nameof(TradeScreen.howManySelected),
            argumentTypes: new Type[] { typeof(GameObject) },
            argumentVariations: new ArgumentType[] { ArgumentType.Normal })]
        [HarmonyPostfix]
        public static void howManySelected_BlockDisplayOnly_Postfix(ref int __result, ref GameObject go)
        {
            if (ItemIsTradeUIDisplayOnly(go))
            {
                __result = 0;
            }
        }

        [HarmonyPatch(
            declaringType: typeof(TradeScreen),
            methodName: nameof(TradeScreen.setHowManySelected),
            argumentTypes: new Type[] { typeof(GameObject), typeof(int) },
            argumentVariations: new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal })]
        [HarmonyPrefix]
        public static bool setHowManySelected_BlockDisplayOnly_Prefix(ref GameObject go, ref int total)
        {
            if (ItemIsTradeUIDisplayOnly(go))
            {
                total = 0;
            }
            return true;
        }

        [HarmonyPatch(
            declaringType: typeof(TradeScreen),
            methodName: nameof(TradeScreen.incrementHowManySelected),
            argumentTypes: new Type[] { typeof(GameObject), typeof(int) },
            argumentVariations: new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal })]
        [HarmonyPrefix]
        public static bool incrementHowManySelected_BlockDisplayOnly_Prefix(ref GameObject go, ref int delta)
        {
            if (ItemIsTradeUIDisplayOnly(go))
            {
                delta = 0;
            }
            return true;
        }

        [HarmonyPatch(
            declaringType: typeof(TradeScreen),
            methodName: nameof(TradeScreen.HandleTradeSome),
            argumentTypes: new Type[] { typeof(TradeLine) },
            argumentVariations: new ArgumentType[] { ArgumentType.Normal })]
        [HarmonyPrefix]
        public static bool HandleTradeSome_BlockDisplayOnly_Prefix(ref TradeLine line)
        {
            if (ItemIsTradeUIDisplayOnly(line.context.data.go))
            {
                return false;
            }
            return true;
        }

        public static bool ItemIsTradeUIDisplayOnly(GameObject Item) => VendorAction.ItemIsTradeUIDisplayOnly(Item);
    }
}
