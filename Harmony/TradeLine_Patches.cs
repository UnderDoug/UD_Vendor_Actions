using HarmonyLib;

using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Qud.UI;

using XRL;
using XRL.UI;
using XRL.UI.Framework;
using XRL.World;

using UnityEngine.EventSystems;

using UD_Modding_Toolbox;

using static UD_Vendor_Actions.Utils;

using static UD_Vendor_Actions.UD_VendorAction;
using XRL.World.Parts;

namespace UD_Vendor_Actions.Harmony
{
    [HarmonyPatch]
    public static class TradeLine_Patches
    {
        public static bool MouseClick = false;

        public static bool Success = false;

        public static bool NeedsFallbackToBase = false;

        public static bool CloseTrade = false;

        private static UD_VendorAction _CurrentAction = null;

        [HarmonyPatch(
            declaringType: typeof(TradeLine),
            methodName: nameof(TradeLine.setData),
            argumentTypes: new Type[] { typeof(FrameworkDataElement) },
            argumentVariations: new ArgumentType[] { ArgumentType.Normal })]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> setData_UseFormatPrice_Transpile(IEnumerable<CodeInstruction> Instructions)
        {
            string patchMethodName = $"{nameof(TradeLine_Patches)}.{nameof(TradeLine.setData)}";

            // string text2 = $"{TradeUI.GetValue(tradeLineData.go, tradeLineData.traderInventory):0.00}";
            //      Start Below
            // IL_0229: ldstr "{0:0.00}"
            // IL_022e: ldloc.0
            // IL_022f: ldfld class XRL.World.GameObject Qud.UI.TradeLineData::go
            // IL_0234: ldloc.0
            // IL_0235: ldfld bool Qud.UI.TradeLineData::traderInventory
            // IL_023a: newobj instance void valuetype [mscorlib] System.Nullable`1<bool>::.ctor(!0)
            // IL_023f: call float64 XRL.UI.TradeUI::GetValue(class XRL.World.GameObject, valuetype[mscorlib] System.Nullable`1<bool>)
            // IL_0244: box[mscorlib] System.Double
            // IL_0249: call string[mscorlib] System.String::Format(string, object)
            //      End Above
            // IL_024e: stloc.3

            // Courtesy Books (mostly)

            CodeMatcher codeMatcher = new(Instructions);

            codeMatcher.MatchStartForward(
                new CodeMatch[1]
                {
                    new(OpCodes.Stloc_3),
                });

            if (codeMatcher.IsInvalid)
            {
                MetricsManager.LogModError(ThisMod, $"{patchMethodName}: {nameof(CodeMatcher.MatchStartForward)} failed to find instruction {OpCodes.Stloc_3}");
                return Instructions;
            }

            int endIndex = codeMatcher.Advance(-1).Pos;

            codeMatcher.MatchStartBackwards(
                new CodeMatch[1]
                {
                    new(OpCodes.Ldstr, "{0:0.00}"),
                });

            if (codeMatcher.IsInvalid)
            {
                MetricsManager.LogModError(ThisMod, $"{patchMethodName}: {nameof(CodeMatcher.MatchStartBackwards)} failed to find instruction {OpCodes.Ldstr} {"{0:0.00}".Quote()}");
                return Instructions;
            }

            int startIndex = codeMatcher.Pos;

            codeMatcher.RemoveInstructionsInRange(startIndex, endIndex)
                .Insert(
                    new CodeInstruction[]
                    {
                        // TradeUI.FormatPrice(TradeUI.GetValue(data.go, data.traderInverntory), TradeScreen.CostMultiple)
                        new(OpCodes.Ldloc_0), // can be CodeInstruction.LoadLocal(0) in the future
                        CodeInstruction.LoadField(typeof(TradeLineData), nameof(TradeLineData.go)),
                        new(OpCodes.Ldloc_0),
                        CodeInstruction.LoadField(typeof(TradeLineData), nameof(TradeLineData.traderInventory)),
                        new(OpCodes.Newobj, AccessTools.Constructor(typeof(bool?), new Type[] { typeof(bool) })),
                        CodeInstruction.Call(typeof(TradeUI), nameof(TradeUI.GetValue), new Type[] { typeof(GameObject), typeof(bool?) }),
                        CodeInstruction.LoadField(typeof(TradeScreen), nameof(TradeScreen.CostMultiple)),
                        CodeInstruction.Call(typeof(TradeUI), nameof(TradeUI.FormatPrice), new Type[] { typeof(double), typeof(float) }),
                    }
                );

            MetricsManager.LogModInfo(ThisMod, $"Successfully transpiled {patchMethodName}");

            return codeMatcher.InstructionEnumeration();
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
            Success = false;
            NeedsFallbackToBase = false;
            CloseTrade = false;
            CurrentAction = null;
            _CurrentAction = null;
            HandleVendorActions(TradeScreen.Trader, __instance);

            MouseClick = false;
            if (Success)
            {
                // potentially do something here?
            }
            bool runPatchedMethod = NeedsFallbackToBase;
            Success = false;
            NeedsFallbackToBase = false;
            CloseTrade = false;
            CurrentAction = null;
            _CurrentAction = null;
            return runPatchedMethod; // skip the patched method.
        }

        public static async void HandleVendorActions(GameObject Vendor, TradeLine TradeLine)
        {
            int indent = Debug.LastIndent;
            bool doDebug = true;
            string methodName = 
                $"{nameof(TradeLine_Patches)}." +
                $"{nameof(HandleVendorActions)}";
            string methodArgs = "(" +
                $"{nameof(Vendor)}: {Vendor?.DebugName}, " +
                $"{nameof(TradeLine)})";

            GameObject item = TradeLine.context.data.go;
            bool traderInventory = TradeLine.context.data.traderInventory;
            GameObject owner = traderInventory ? Vendor : The.Player;

            bool processAfterAwait = false;
            bool processSecondAfterAwait = false;
            bool clearAndSetUpTradeUI = false;
            bool staggered = false;
            bool closeTradeBeforeProcessingSecond = false;

            bool tradeClosed = false;

            if (item != null)
            {
                Debug.Entry(4, $"{methodName}{methodArgs} for {nameof(item)}: {item?.DebugName}",
                    Indent: indent + 1, Toggle: doDebug);

                ApplyVendorActionHandlerPartsFromAttribute(item, typeof(AlwaysHandlesItem_UD_VendorActionsAttribute));
                if (!Vendor.HasPart<UD_VendorActionHandler>())
                {
                    MetricsManager.LogPotentialModError(ThisMod, 
                        $"{Vendor?.DebugName ?? Const.NULL} missing {nameof(UD_VendorActionHandler)}." +
                        $" This may be caused by enabling this mod for an already existing save." +
                        $" If no {nameof(UD_VendorAction)}s show up please report this error to " +
                        $"{ThisMod?.Manifest?.Author?.Strip()} on the steam workshop.");
                    // Vendor.RequirePart<UD_VendorActionHandler>();
                }

                Dictionary<string, UD_VendorAction> actions = new();
                Debug.Entry(4, $"Sending {nameof(UD_GetVendorActionsEvent)}", Indent: indent + 2, Toggle: doDebug);
                UD_GetVendorActionsEvent.Send(TradeLine, Vendor, item, actions, true);

                NeedsFallbackToBase = actions.IsNullOrEmpty();
                if (NeedsFallbackToBase)
                {
                    return;
                }

                Debug.Entry(4, $"awaiting {nameof(APIDispatch)}.{nameof(APIDispatch.RunAndWaitAsync)}",
                    Indent: indent + 2, Toggle: doDebug);
                await APIDispatch.RunAndWaitAsync(delegate
                {
                    Debug.Entry(4, $"{nameof(CurrentAction.ShowVendorActionMenu)}",
                        Indent: indent + 2, Toggle: doDebug);
                    CurrentAction = ShowVendorActionMenu(
                        ActionTable: actions,
                        Item: item,
                        Intro: "Choose an action",
                        MouseClick: MouseClick);

                    _CurrentAction = CurrentAction;

                    if (CurrentAction != null)
                    {
                        processAfterAwait = CurrentAction.ProcessAfterAwait;
                        Debug.LoopItem(4, $"{nameof(processAfterAwait)}", $"{processAfterAwait}", 
                            Good: processAfterAwait, Indent: indent + 3, Toggle: doDebug);

                        processSecondAfterAwait = CurrentAction.ProcessSecondAfterAwait;
                        Debug.LoopItem(4, $"{nameof(processSecondAfterAwait)}", $"{processSecondAfterAwait}", 
                            Good: processSecondAfterAwait, Indent: indent + 3, Toggle: doDebug);

                        clearAndSetUpTradeUI = CurrentAction.ClearAndSetUpTradeUI;
                        Debug.LoopItem(4, $"{nameof(clearAndSetUpTradeUI)}", $"{clearAndSetUpTradeUI}", 
                            Good: clearAndSetUpTradeUI, Indent: indent + 3, Toggle: doDebug);

                        staggered = CurrentAction.Staggered;
                        Debug.LoopItem(4, $"{nameof(staggered)}", $"{staggered}", 
                            Good: staggered, Indent: indent + 3, Toggle: doDebug);

                        closeTradeBeforeProcessingSecond = CurrentAction.CloseTradeBeforeProcessingSecond;
                        Debug.LoopItem(4, $"{nameof(closeTradeBeforeProcessingSecond)}", $"{closeTradeBeforeProcessingSecond}", 
                            Good: closeTradeBeforeProcessingSecond, Indent: indent + 3, Toggle: doDebug);
                    }

                    if (CurrentAction != null && !processAfterAwait)
                    {
                        Debug.Entry(4, $"[process during await]", Indent: indent + 2, Toggle: doDebug);
                        Debug.Entry(4, $"{nameof(CurrentAction)}: {CurrentAction.Name}, {nameof(CurrentAction.Process)}", 
                            Indent: indent + 3, Toggle: doDebug);
                        Success = CurrentAction.Process(TradeLine, Vendor, item, owner, out CloseTrade);

                        if (!processSecondAfterAwait)
                        {
                            if (CurrentAction != null && staggered && !closeTradeBeforeProcessingSecond)
                            {
                                Debug.Entry(4, $"[process second during await, before trade UI closed]", Indent: indent + 3, Toggle: doDebug);
                                Debug.Entry(4, $"{nameof(CurrentAction)}: {CurrentAction.Name}, {nameof(CurrentAction.Process)}",
                                    Indent: indent + 4, Toggle: doDebug);
                                Success = CurrentAction.Process(TradeLine, Vendor, item, owner, out CloseTrade);
                            }
                            if (!tradeClosed && CloseTrade) // && !(processSecondAfterAwait && closeTradeBeforeProcessingSecond))
                            {
                                Debug.Entry(4, $"Closing trade UI...", Indent: indent + 3, Toggle: doDebug);
                                TradeLine?.screen?.Cancel();
                                ConversationUI.Escape();
                                tradeClosed = true;
                            }
                            Debug.LoopItem(4, $"{nameof(CloseTrade)}", $"{CloseTrade}", Good: CloseTrade, Indent: indent + 3, Toggle: doDebug);
                            if (CurrentAction != null && staggered && closeTradeBeforeProcessingSecond)
                            {
                                Debug.Entry(4, $"[process second during await, after trade UI closed]", Indent: indent + 3, Toggle: doDebug);
                                Debug.Entry(4, $"{nameof(CurrentAction)}: {CurrentAction.Name}, {nameof(CurrentAction.Process)}",
                                    Indent: indent + 4, Toggle: doDebug);
                                Success = CurrentAction.Process(TradeLine, Vendor, item, owner, out CloseTrade);
                            }
                        }
                    }
                });
                Debug.Entry(4, $"finished {nameof(APIDispatch)}.{nameof(APIDispatch.RunAndWaitAsync)}",
                    Indent: indent + 2, Toggle: doDebug);

                // this ensures the current action can't influence the contents of the trade UI between firings of the action
                CurrentAction = null;

                if (clearAndSetUpTradeUI && !CloseTrade)
                {
                    TradeLine?.screen?.ClearAndSetupTradeUI();
                }

                if (!tradeClosed && !CloseTrade)
                {
                    Debug.Entry(4, $"Calling {nameof(TradeLine.screen.UpdateViewFromData)}", Indent: indent + 2, Toggle: doDebug);
                    TradeLine?.screen?.UpdateViewFromData();
                }

                // obviously, we need the action again to keep going
                CurrentAction = _CurrentAction;

                if (CurrentAction != null && (processAfterAwait || processSecondAfterAwait))
                {
                    if (CurrentAction != null && !staggered)
                    {
                        Debug.Entry(4, $"[process after await]", Indent: indent + 2, Toggle: doDebug);
                        Debug.Entry(4, $"{nameof(CurrentAction)}: {CurrentAction.Name}, {nameof(CurrentAction.Process)}",
                            Indent: indent + 3, Toggle: doDebug);
                        Success = CurrentAction.Process(TradeLine, Vendor, item, owner, out CloseTrade);
                    }

                    if (CurrentAction != null && staggered && !closeTradeBeforeProcessingSecond)
                    {
                        Debug.Entry(4, $"[process second after await, before trade UI closed]", Indent: indent + 2, Toggle: doDebug);
                        Debug.Entry(4, $"{nameof(CurrentAction)}: {CurrentAction.Name}, {nameof(CurrentAction.Process)}",
                            Indent: indent + 3, Toggle: doDebug);
                        Success = CurrentAction.Process(TradeLine, Vendor, item, owner, out CloseTrade);
                    }
                    if (!tradeClosed && CloseTrade)
                    {
                        Debug.Entry(4, $"Closing trade UI...", Indent: indent + 2, Toggle: doDebug);
                        TradeLine?.screen?.Cancel();
                        ConversationUI.Escape();
                        tradeClosed = true;
                    }
                    Debug.LoopItem(4, $"{nameof(CloseTrade)}", $"{CloseTrade}", Good: CloseTrade, Indent: indent + 3, Toggle: doDebug);
                    if (CurrentAction != null && staggered && closeTradeBeforeProcessingSecond)
                    {
                        Debug.Entry(4, $"[process second after await, after trade UI closed]", Indent: indent + 2, Toggle: doDebug);
                        Debug.Entry(4, $"{nameof(CurrentAction)}: {CurrentAction.Name}, {nameof(CurrentAction.Process)}",
                            Indent: indent + 3, Toggle: doDebug);
                        Success = CurrentAction.Process(TradeLine, Vendor, item, owner, out CloseTrade);
                    }
                }
            }
            Debug.LastIndent = indent;
        }

        public static bool ItemIsTradeUIDisplayOnly(GameObject Item) => UD_VendorAction.ItemIsTradeUIDisplayOnly(Item);
    }
}
