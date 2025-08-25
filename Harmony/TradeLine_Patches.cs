using HarmonyLib;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using UnityEngine.EventSystems;

using Qud.UI;

using XRL;
using XRL.UI;
using XRL.UI.Framework;
using XRL.World;

namespace UD_Vendor_Actions.Harmony
{
    [HarmonyPatch]
    public static class TradeLine_Patches
    {
        public static bool MouseClick = false;

        public static bool Success = false;

        [HarmonyPatch(
            declaringType: typeof(TradeLine),
            methodName: nameof(TradeLine.setData),
            argumentTypes: new Type[] { typeof(FrameworkDataElement) },
            argumentVariations: new ArgumentType[] { ArgumentType.Normal })]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> setData_BlockDisplayOnly_Prefix(IEnumerable<CodeInstruction> instructions)
        {
            bool found = false;
            bool foundFormatting = false;

            MethodInfo tradeUI_formatPrice = AccessTools.Method(
                type: typeof(TradeUI), 
                name: nameof(TradeUI.FormatPrice), 
                parameters: new Type[] { typeof(double), typeof(float) });

            bool havePrice = false;

            bool haveBoxing = false;
            double price = 0.0;

            string methodName = $"{nameof(TradeLine_Patches)}.{nameof(TradeLine.setData)}";

            // string text2 = $"{TradeUI.GetValue(tradeLineData.go, tradeLineData.traderInventory):0.00}";
            // IL_0229: ldstr "{0:0.00}"
            // IL_022e: ldloc.0
            // IL_022f: ldfld class XRL.World.GameObject Qud.UI.TradeLineData::go
            // IL_0234: ldloc.0
            // IL_0235: ldfld bool Qud.UI.TradeLineData::traderInventory
            // IL_023a: newobj instance void valuetype [mscorlib] System.Nullable`1<bool>::.ctor(!0)
            // IL_023f: call float64 XRL.UI.TradeUI::GetValue(class XRL.World.GameObject, valuetype[mscorlib] System.Nullable`1<bool>)
            // IL_0244: box[mscorlib] System.Double
            // IL_0249: call string[mscorlib] System.String::Format(string, object)
            // IL_024e: stloc.3
            foreach (CodeInstruction instruction in instructions)
            {
                // instruction to allocate formatted price
                if (!found && haveBoxing && foundFormatting
                    && instruction.opcode == OpCodes.Stloc_3)
                {
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 1.0f);
                    yield return new CodeInstruction(OpCodes.Call, tradeUI_formatPrice);
                    found = true;
                }

                // instruction boxing price value
                if (!found && !haveBoxing && foundFormatting
                    && instruction.opcode == OpCodes.Box)
                {
                    haveBoxing = true;
                }

                // instruction with formatting text
                if (!found && !haveBoxing && !foundFormatting
                    && instruction.operand is string doubleFormatting
                    && doubleFormatting == @"{0:0.00}")
                {
                    foundFormatting = true;
                    continue;
                }
                yield return instruction;
            }
            if (!foundFormatting)
            {
                UnityEngine.Debug.Log($"Cannot find instruction with formatting text in {methodName}");
            }
            if (!haveBoxing)
            {
                UnityEngine.Debug.Log($"Cannot find instruction boxing price value in {methodName}");
            }
            if (!havePrice)
            {
                UnityEngine.Debug.Log($"Cannot find instruction with price value in {methodName}");
            }
            if (!found)
            {
                UnityEngine.Debug.Log($"Cannot find instruction to allocate formatted price in {methodName}");
            }
        }

        [HarmonyPatch(
            declaringType: typeof(TradeLine),
            methodName: nameof(TradeLine.OnPointerClick),
            argumentTypes: new Type[] { typeof(PointerEventData) },
            argumentVariations: new ArgumentType[] { ArgumentType.Normal })]
        [HarmonyPrefix]
        public static bool OnPointerClick_EventInstead_Prefix(ref TradeLine __instance, ref PointerEventData eventData)
        {
            if (__instance.context.IsActive() && !TradeLine.dragging)
            {
                if (eventData.button == PointerEventData.InputButton.Right 
                    || (eventData.button == PointerEventData.InputButton.Left && ItemIsTradeUIDisplayOnly(__instance.context.data.go)))
                {
                    eventData.button = PointerEventData.InputButton.Right;
                    MouseClick = true;
                }
                else
                {
                    MouseClick = false;
                }
            }
            return true;
        }

        [HarmonyPatch(
            declaringType: typeof(TradeLine),
            methodName: nameof(TradeLine.HandleVendorActions))]
        [HarmonyPrefix]
        public static bool HandleVendorActions_EventInstead_Prefix(ref TradeLine __instance)
        {
            HandleVendorActions(TradeScreen.Trader, __instance);

            MouseClick = false;
            bool success = Success;
            if (success)
            {
                // potentially do something here?
            }
            Success = false;
            return false; // skip the patched method. 
        }

        public static async void HandleVendorActions(GameObject Vendor, TradeLine TradeLine)
        {
            GameObject item = TradeLine.context.data.go;
            if (item != null)
            {
                Dictionary<string, VendorAction> actions = new();
                GetVendorActionsEvent.Send(TradeLine, Vendor, item, actions, true);
                
                VendorAction action = null;
                await APIDispatch.RunAndWaitAsync(delegate
                {
                    action = VendorAction.ShowVendorActionMenu(ActionTable: actions, Item: item, Intro: "Choose an action", MouseClick: MouseClick);
                    if (action != null && !action.WantsAsync)
                    {
                        Success = action.Process(TradeLine, Vendor, item, TradeLine.context.data.traderInventory ? Vendor : The.Player);
                    }
                });
                if (action != null && action.ClearAndSetUpTradeUI)
                {
                    TradeLine.screen.ClearAndSetupTradeUI();
                }
                TradeLine.screen.UpdateViewFromData();
                if (action != null && action.WantsAsync)
                {
                    Success = action.Process(TradeLine, Vendor, item, TradeLine.context.data.traderInventory ? Vendor : The.Player);
                }
            }
        }

        public static bool ItemIsTradeUIDisplayOnly(GameObject Item) => VendorAction.ItemIsTradeUIDisplayOnly(Item);
    }
}
