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
            bool complete = false;
            bool foundFormatting = false;
            int lineCounter = 0;

            MethodInfo tradeUI_formatPrice = AccessTools.Method(
                type: typeof(TradeUI), 
                name: nameof(TradeUI.FormatPrice), 
                parameters: new Type[] { typeof(double), typeof(float) });

            bool haveBoxing = false;

            string methodDebugName = $"{nameof(TradeLine_Patches)}.{nameof(TradeLine.setData)}";
            UnityEngine.Debug.Log($"{methodDebugName}");

            // string text2 = $"{TradeUI.GetValue(tradeLineData.go, tradeLineData.traderInventory):0.00}";
            //      change to "" V
            // IL_0229: ldstr "{0:0.00}"
            // IL_022e: ldloc.0
            // IL_022f: ldfld class XRL.World.GameObject Qud.UI.TradeLineData::go
            // IL_0234: ldloc.0
            // IL_0235: ldfld bool Qud.UI.TradeLineData::traderInventory
            // IL_023a: newobj instance void valuetype [mscorlib] System.Nullable`1<bool>::.ctor(!0)
            // IL_023f: call float64 XRL.UI.TradeUI::GetValue(class XRL.World.GameObject, valuetype[mscorlib] System.Nullable`1<bool>)
            //      locate V
            // IL_0244: box[mscorlib] System.Double
            //      skip V
            // IL_0249: call string[mscorlib] System.String::Format(string, object)
            //      add Ldc_R4, 1.0f
            //      add Call TradeUI.FormatPrice
            // IL_024e: stloc.3

            string opCode = "";
            string operand = "";
            /*
            foreach (CodeInstruction instruction in instructions)
            {
                // instruction to allocate formatted price
                if (!complete && haveBoxing && foundFormatting
                    && instruction.opcode == OpCodes.Stloc_3)
                {
                    UnityEngine.Debug.Log($"    ----[{4}");
                    CodeInstruction putFloatOnStack = new(OpCodes.Ldc_R4, 1.0f);
                    opCode = putFloatOnStack?.opcode.ToString();
                    operand = putFloatOnStack?.operand?.ToString();
                    UnityEngine.Debug.Log($"    {lineCounter++}: {opCode} {operand}");
                    yield return putFloatOnStack;

                    UnityEngine.Debug.Log($"    ----[{5}");
                    CodeInstruction callTradeUI_FormatPrice = CodeInstruction.Call(typeof(TradeUI), nameof(TradeUI.FormatPrice), new Type[] { typeof(System.Double), typeof(System.Single) });
                    opCode = callTradeUI_FormatPrice?.opcode.ToString();
                    operand = callTradeUI_FormatPrice?.operand?.ToString();
                    UnityEngine.Debug.Log($"    {lineCounter++}: {opCode} {operand}");
                    yield return callTradeUI_FormatPrice;

                    UnityEngine.Debug.Log($"    ----[{6}");
                    complete = true;
                }

                // instruction string format
                if (!complete && haveBoxing && foundFormatting
                    && instruction.opcode == OpCodes.Call
                    && instruction.operand is MethodInfo stringFormat
                    && stringFormat == AccessTools.Method(typeof(string), nameof(string.Format), new Type[] { typeof(System.String), typeof(System.Object) }))
                {
                    UnityEngine.Debug.Log($"    ----[{3} - Skip {instruction.opcode} {instruction.operand}");
                    continue;
                }

                // instruction boxing price value
                if (!complete && !haveBoxing && foundFormatting
                    && instruction.opcode == OpCodes.Box)
                {
                    UnityEngine.Debug.Log($"    ----[{2} - Skip {instruction.opcode} {instruction.operand}");
                    haveBoxing = true;
                }

                // instruction with formatting text
                if (!complete && !haveBoxing && !foundFormatting
                    && instruction.operand is string doubleFormatting
                    && doubleFormatting == @"{0:0.00}")
                {
                    UnityEngine.Debug.Log($"    ----[{1}"); //  - Skip {instruction.opcode} {instruction.operand}
                    foundFormatting = true;
                    instruction.operand = "";
                    // continue;
                }
                opCode = instruction?.opcode.ToString();
                operand = instruction?.operand?.ToString();
                UnityEngine.Debug.Log($"    {lineCounter++}: {opCode} {operand}");
                yield return instruction;
            }
            if (!foundFormatting)
            {
                UnityEngine.Debug.Log($"Cannot find instruction with formatting text in {methodDebugName}");
            }
            if (!haveBoxing)
            {
                UnityEngine.Debug.Log($"Cannot find instruction boxing price value in {methodDebugName}");
            }
            if (!complete)
            {
                UnityEngine.Debug.Log($"Cannot find instruction to allocate formatted price in {methodDebugName}");
            }
            */

            // string text2 = $"{TradeUI.GetValue(tradeLineData.go, tradeLineData.traderInventory):0.00}";
            //      change to "" V
            // IL_0229: ldstr "{0:0.00}"
            // IL_022e: ldloc.0
            // IL_022f: ldfld class XRL.World.GameObject Qud.UI.TradeLineData::go
            // IL_0234: ldloc.0
            // IL_0235: ldfld bool Qud.UI.TradeLineData::traderInventory
            // IL_023a: newobj instance void valuetype [mscorlib] System.Nullable`1<bool>::.ctor(!0)
            // IL_023f: call float64 XRL.UI.TradeUI::GetValue(class XRL.World.GameObject, valuetype[mscorlib] System.Nullable`1<bool>)
            //      locate V
            // IL_0244: box[mscorlib] System.Double
            //      skip V
            // IL_0249: call string[mscorlib] System.String::Format(string, object)
            //      add Ldc_R4, 1.0f
            //      add Call TradeUI.FormatPrice
            // IL_024e: stloc.3

            List<CodeInstruction> codes = new(instructions);
            bool haveRange = false;
            int startIndex = -1;
            int endIndex = -1;

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
                    UnityEngine.Debug.Log($"    ----{nameof(endIndex)}: {endIndex}");
                }

                if (startIndex < 0
                    && endIndex < 0
                    && codes[i]?.operand is string doubleFormatting
                    && doubleFormatting == @"{0:0.00}")
                {
                    startIndex = i;
                    UnityEngine.Debug.Log($"    ----{nameof(startIndex)}: {startIndex}");
                }

                UnityEngine.Debug.Log($"    {i}: {opCode} {operand}");

                if (startIndex > -1 && endIndex > -1)
                {
                    haveRange = true;
                    break;
                }
            }
            UnityEngine.Debug.Log($"{nameof(haveRange)}: {haveRange}");
            if (haveRange)
            {
                List<CodeInstruction> instructionsToInsert = new()
                {
                    new(OpCodes.Ldloc_0),
                    new(OpCodes.Ldfld, AccessTools.Field(typeof(TradeLineData), nameof(TradeLineData.go))),
                    new(OpCodes.Ldloc_0),
                    new(OpCodes.Ldfld, AccessTools.Field(typeof(TradeLineData), nameof(TradeLineData.traderInventory))),
                    new(OpCodes.Newobj, typeof(Nullable<bool>)),
                    CodeInstruction.Call(typeof(TradeUI), nameof(TradeUI.GetValue), new Type[] { typeof(GameObject), typeof(Nullable<bool>) }),
                    new(OpCodes.Ldsfld, AccessTools.Field(typeof(TradeScreen), nameof(TradeScreen.CostMultiple))),
                    CodeInstruction.Call(typeof(TradeUI), nameof(TradeUI.FormatPrice), new Type[] { typeof(double), typeof(float) }),
                };

                UnityEngine.Debug.Log($"{nameof(instructionsToInsert)}:");
                int insertCounter = 0;
                foreach (CodeInstruction instruction in instructionsToInsert)
                {
                    opCode = instruction?.opcode.ToString();
                    operand = instruction?.operand?.ToString();
                    UnityEngine.Debug.Log($"    {insertCounter++}: {opCode} {operand}");
                }
                codes.RemoveRange(startIndex, (endIndex - startIndex) + 1);

                UnityEngine.Debug.Log($"{nameof(codes)}.{nameof(codes.RemoveRange)}:");
                int removedCounter = 0;
                foreach (CodeInstruction instruction in codes)
                {
                    opCode = instruction?.opcode.ToString();
                    operand = instruction?.operand?.ToString();
                    UnityEngine.Debug.Log($"    {removedCounter++}: {opCode} {operand}");
                }

                UnityEngine.Debug.Log($"{nameof(startIndex)}:");
                opCode = codes[startIndex]?.opcode.ToString();
                operand = codes[startIndex]?.operand?.ToString();
                UnityEngine.Debug.Log($"    {startIndex}: {opCode} {operand}");
                codes.InsertRange(startIndex , instructionsToInsert);

                UnityEngine.Debug.Log($"{nameof(codes)}.{nameof(codes.RemoveRange)}:");
                int insertedCounter = 0;
                foreach (CodeInstruction instruction in codes)
                {
                    opCode = instruction?.opcode.ToString();
                    operand = instruction?.operand?.ToString();
                    UnityEngine.Debug.Log($"    {insertedCounter++}: {opCode} {operand}");
                }
            }

            return codes;
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
