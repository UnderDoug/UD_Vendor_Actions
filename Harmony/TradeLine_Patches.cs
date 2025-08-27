using HarmonyLib;
using Qud.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine.EventSystems;
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
        public static IEnumerable<CodeInstruction> setData_UseFormatPrice_Transpile(IEnumerable<CodeInstruction> instructions)
        {
            string patchMethodName = $"{nameof(TradeLine_Patches)}.{nameof(TradeLine.setData)}";
            string opCode = "";
            string operand = "";

            List<CodeInstruction> codes = new(instructions);
            bool haveRange = false;
            int startIndex = -1;
            int endIndex = -1;
            CodeInstruction newNullableBool = null;

            // string text2 = $"{TradeUI.GetValue(tradeLineData.go, tradeLineData.traderInventory):0.00}";
            //      Start Below
            // IL_0229: ldstr "{0:0.00}"
            // IL_022e: ldloc.0
            // IL_022f: ldfld class XRL.World.GameObject Qud.UI.TradeLineData::go
            // IL_0234: ldloc.0
            // IL_0235: ldfld bool Qud.UI.TradeLineData::traderInventory
            //      Save this instruction.
            // IL_023a: newobj instance void valuetype [mscorlib] System.Nullable`1<bool>::.ctor(!0)
            // IL_023f: call float64 XRL.UI.TradeUI::GetValue(class XRL.World.GameObject, valuetype[mscorlib] System.Nullable`1<bool>)
            // IL_0244: box[mscorlib] System.Double
            // IL_0249: call string[mscorlib] System.String::Format(string, object)
            //      End Above
            // IL_024e: stloc.3
            for (int i = 0; i < codes.Count; i++)
            {
                opCode = codes[i]?.opcode.ToString();
                operand = codes[i]?.operand?.ToString();

                if (startIndex > -1
                    && endIndex < 0
                    && codes[i]?.opcode == OpCodes.Call
                    && codes[i]?.operand is MethodInfo stringFormat
                    && stringFormat == AccessTools.Method(typeof(string), nameof(string.Format), new Type[] { typeof(string), typeof(object) }))
                {
                    endIndex = i;
                }

                if (startIndex > -1
                    && endIndex < 0
                    && codes[i]?.opcode == OpCodes.Newobj)
                {
                    newNullableBool = codes[i];
                }

                if (startIndex < 0
                    && endIndex < 0
                    && codes[i]?.operand is string doubleFormatting
                    && doubleFormatting == @"{0:0.00}")
                {
                    startIndex = i;
                }

                if (startIndex > -1 && endIndex > -1)
                {
                    haveRange = true;
                    break;
                }
            }
            if (haveRange)
            {
                List<CodeInstruction> instructionsToInsert = new()
                {
                    new(OpCodes.Ldloc_0),
                    new(OpCodes.Ldfld, AccessTools.Field(typeof(TradeLineData), nameof(TradeLineData.go))),
                    new(OpCodes.Ldloc_0),
                    new(OpCodes.Ldfld, AccessTools.Field(typeof(TradeLineData), nameof(TradeLineData.traderInventory))),
                    newNullableBool,
                    CodeInstruction.Call(typeof(TradeUI), nameof(TradeUI.GetValue), new Type[] { typeof(GameObject), typeof(bool?) }),
                    new(OpCodes.Ldsfld, AccessTools.Field(typeof(TradeScreen), nameof(TradeScreen.CostMultiple))),
                    CodeInstruction.Call(typeof(TradeUI), nameof(TradeUI.FormatPrice), new Type[] { typeof(double), typeof(float) }),
                };
                codes.RemoveRange(startIndex, endIndex - startIndex + 1);
                codes.InsertRange(startIndex , instructionsToInsert);
                MetricsManager.LogModInfo(ModManager.GetMod(), $"Successfully transpiled {patchMethodName}");
            }
            else
            {
                MetricsManager.LogModError(ModManager.GetMod(), $"Failed to transpile {patchMethodName}");
            }
            return codes.AsEnumerable();
        }
        [HarmonyPatch(
            declaringType: typeof(TradeLine),
            methodName: nameof(TradeLine.setData),
            argumentTypes: new Type[] { typeof(FrameworkDataElement) },
            argumentVariations: new ArgumentType[] { ArgumentType.Normal })]
        [HarmonyPostfix]
        public static void setData_ExcludeCurrencySymbolIfDisplayOnly_Postfix(ref TradeLine __instance, ref FrameworkDataElement data)
        {
            if (data is TradeLineData tradeLineData && tradeLineData.type != TradeLineDataType.Category)
            {
                double price = TradeUI.GetValue(tradeLineData?.go, tradeLineData?.traderInventory);
                if (price < 0 || ItemIsTradeUIDisplayOnly(tradeLineData.go))
                {
                    __instance?.rightFloatText?.SetText(TradeUI.FormatPrice(price, TradeUI.costMultiple));
                }
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
