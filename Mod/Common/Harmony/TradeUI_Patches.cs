using HarmonyLib;

using System;
using System.Collections.Generic;

using XRL;
using XRL.UI;
using XRL.World;

namespace UD_Vendor_Actions.Harmony
{
    [HarmonyPatch]
    public static class TradeUI_Patches
    {
        [HarmonyPatch(
            declaringType: typeof(TradeUI),
            methodName: nameof(TradeUI.GetNumberSelected),
            argumentTypes: new Type[] { typeof(GameObject) },
            argumentVariations: new ArgumentType[] { ArgumentType.Normal })]
        [HarmonyPostfix]
        public static void GetNumberSelected_BlockDisplayOnly_Postfix(ref int __result, ref GameObject obj)
        {
            if (ItemIsTradeUIDisplayOnly(obj))
            {
                __result = -999;
            }
        }

        [HarmonyPatch(
            declaringType: typeof(TradeUI),
            methodName: nameof(TradeUI.SetNumberSelected),
            argumentTypes: new Type[] { typeof(GameObject), typeof(int) },
            argumentVariations: new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal })]
        [HarmonyPrefix]
        public static bool SetNumberSelected_BlockDisplayOnly_Prefix(ref GameObject obj, ref int amount)
        {
            if (ItemIsTradeUIDisplayOnly(obj))
            {
                amount = 0;
            }
            return true;
        }

        [HarmonyPatch(
            declaringType: typeof(TradeUI),
            methodName: nameof(TradeUI.GetValue),
            argumentTypes: new Type[] { typeof(GameObject), typeof(bool?) },
            argumentVariations: new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal })]
        [HarmonyPostfix]
        public static void GetValue_BlockDisplayOnly_Postfix(ref double __result, ref GameObject obj)
        {
            if (ItemIsTradeUIDisplayOnly(obj))
            {
                __result = -1;
            }
        }

        [HarmonyPatch(
            declaringType: typeof(TradeUI),
            methodName: nameof(TradeUI.FormatPrice),
            argumentTypes: new Type[] { typeof(double), typeof(float) },
            argumentVariations: new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal })]
        [HarmonyPrefix]
        public static bool FindInTradeList_BlockDisplayOnly_Prefix(ref string __result, ref double Price)
        {
            if (Price == -1)
            {
                __result = "";
                return false;
            }
            return true;
        }

        [HarmonyPatch(
            declaringType: typeof(TradeUI),
            methodName: nameof(TradeUI.FindInTradeList),
            argumentTypes: new Type[] { typeof(List<TradeEntry>), typeof(GameObject) },
            argumentVariations: new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal })]
        [HarmonyPostfix]
        public static void FindInTradeList_BlockDisplayOnly_Postfix(ref int __result, ref GameObject obj)
        {
            if (ItemIsTradeUIDisplayOnly(obj))
            {
                __result = -1;
            }
        }

        [HarmonyPatch(
            declaringType: typeof(TradeUI),
            methodName: nameof(TradeUI.ShowTradeScreen),
            argumentTypes: new Type[] { typeof(GameObject), typeof(float), typeof(TradeUI.TradeScreenMode) },
            argumentVariations: new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal })]
        [HarmonyPrefix]
        public static bool ShowTradeScreen_GetVendorActionVendorHandlers_Prefix(ref GameObject Trader)
        {
            Utils.ApplyVendorActionHandlerPartsFromAttribute(Trader, typeof(AlwaysHandlesVendor_UD_VendorActionsAttribute));
            return true;
        }

        [HarmonyPatch(
            declaringType: typeof(TradeUI),
            methodName: nameof(TradeUI.ShowTradeScreen),
            argumentTypes: new Type[] { typeof(GameObject), typeof(float), typeof(TradeUI.TradeScreenMode) },
            argumentVariations: new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal })]
        [HarmonyPostfix]
        public static void ShowTradeScreen_SendEvent_Postfix(ref GameObject Trader)
        {
            UD_EndTradeEvent.Send(The.Player, Trader);
        }

        public static bool ItemIsTradeUIDisplayOnly(GameObject Item) => UD_VendorAction.ItemIsTradeUIDisplayOnly(Item);
    }
}
