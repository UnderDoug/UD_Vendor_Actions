﻿using HarmonyLib;
using Qud.UI;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UD_Modding_Toolbox;
using UnityEngine.EventSystems;
using XRL;
using XRL.UI;
using XRL.UI.Framework;
using XRL.World;
using XRL.World.Parts;

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
            /*
            UnityEngine.Debug.LogError(
                $"{nameof(TradeUI_Patches)}." +
                $"{nameof(TradeUI.FormatPrice)}(" +
                $"{nameof(__result)}: {__result}, " +
                $"{nameof(Price)}: {Price})");
            */
            if (Price == -1)
            {
                __result = ""; // "{{K|\u2500 N/A \u2500}}";
                // UnityEngine.Debug.LogError($"    {nameof(__result)}: {__result}");
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
        [HarmonyPostfix]
        public static void ShowTradeScreen_SendEvent_Postfix(ref GameObject Trader)
        {
            EndTradeEvent.Send(The.Player, Trader);
        }

        [HarmonyPatch(
            declaringType: typeof(TradeUI),
            methodName: nameof(TradeUI.DoVendorRepair),
            argumentTypes: new Type[] { typeof(GameObject), typeof(GameObject) },
            argumentVariations: new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal })]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> DoVendorRepair_CorrectInvertedTradePerformance_Transpile(IEnumerable<CodeInstruction> Instructions, ILGenerator Generator)
        {
            bool doVomit = false;
            string patchMethodName = $"{nameof(TradeUI_Patches)}.{nameof(TradeUI.DoVendorRepair)}";
            int metricsCheckSteps = 0;

            CodeMatcher codeMatcher = new(Instructions, Generator);

            // int num = Math.Max(5 + (int)(GetValue(GO, false) / 25.0), 5) * GO.Count;
            CodeMatch[] match_Assign_Max_5_GetValue_xCount = new CodeMatch[]
            {
                // 5
                new(OpCodes.Ldc_I4_5),

                // GO, false
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldc_I4_0),
                new(OpCodes.Newobj, AccessTools.Constructor(typeof(bool?), new Type[]{ typeof(bool) })),

                // GetValue(GameObject, bool?)
                new(ins => ins.Calls(AccessTools.Method(typeof(TradeUI), nameof(TradeUI.GetValue), new Type[]{ typeof(GameObject), typeof(bool?) }))),
                
                // Below doesn't find the instructions.
                // / 25
                // new(OpCodes.Ldc_R8, 25),
                // new(OpCodes.Div),

                // (int)
                // new(OpCodes.Conv_I4),
                
                // + 5
                // new(OpCodes.Add),
                // new(OpCodes.Ldc_I4_5),

                // Math.Max(int, int)
                // new(ins => ins.Calls(AccessTools.Method(typeof(Math), nameof(Math.Max), new Type[]{ typeof(int), typeof(int) }))),

                // * GO.Count
                // new(OpCodes.Ldarg_0),
                // new(ins => ins.Calls(AccessTools.PropertyGetter(typeof(GameObject), nameof(GameObject.Count)))),
                // new(OpCodes.Mul),

                // int num = 
                // new(OpCodes.Stloc_S, 4),
            };

            // find end of:
            // int num = Math.Max(5 + (int)(GetValue(GO, false) / 25.0), 5) * GO.Count;
            // from the start
            if (codeMatcher.Start().MatchEndForward(match_Assign_Max_5_GetValue_xCount).IsInvalid)
            {
                MetricsManager.LogModError(ModManager.GetMod("UD_Tinkering_Bytes"), $"{patchMethodName}: ({metricsCheckSteps}) {nameof(CodeMatcher.MatchEndBackwards)} failed to find instructions {nameof(match_Assign_Max_5_GetValue_xCount)}");
                foreach (CodeMatch match in match_Assign_Max_5_GetValue_xCount)
                {
                    MetricsManager.LogModError(ModManager.GetMod("UD_Tinkering_Bytes"), $"    {match.name} {match.opcode}");
                }
                codeMatcher.Vomit(Generator, doVomit);
                return Instructions;
            }
            metricsCheckSteps++;

            // find start of:
            // GetValue(GO, false)
            // from the start
            if (codeMatcher.MatchStartBackwards(match_Assign_Max_5_GetValue_xCount[^2]).IsInvalid)
            {
                MetricsManager.LogModError(ModManager.GetMod("UD_Tinkering_Bytes"), $"{patchMethodName}: ({metricsCheckSteps}) {nameof(CodeMatcher.MatchEndBackwards)} failed to find instructions {nameof(match_Assign_Max_5_GetValue_xCount)}[^2]");
                foreach (CodeMatch match in match_Assign_Max_5_GetValue_xCount)
                {
                    MetricsManager.LogModError(ModManager.GetMod("UD_Tinkering_Bytes"), $"    {match.name} | {match.opcode} {match.operand}");
                }
                codeMatcher.Vomit(Generator, doVomit);
                return Instructions;
            }
            metricsCheckSteps++;

            // advance back to false in:
            // GetValue(GO, false)
            // from current position
            if (codeMatcher.Advance(-1).Instruction.opcode != OpCodes.Ldc_I4_0)
            {
                MetricsManager.LogModError(ModManager.GetMod("UD_Tinkering_Bytes"), $"{patchMethodName}: ({metricsCheckSteps}) {nameof(CodeMatcher.MatchEndBackwards)} failed to find instruction {nameof(OpCodes.Ldc_I4_0)} in {nameof(match_Assign_Max_5_GetValue_xCount)}");
                foreach (CodeMatch match in match_Assign_Max_5_GetValue_xCount)
                {
                    MetricsManager.LogModError(ModManager.GetMod("UD_Tinkering_Bytes"), $"    {match.name} {match.opcode}");
                }
                codeMatcher.Vomit(Generator, doVomit);
                return Instructions;
            }
            metricsCheckSteps++;

            codeMatcher.Instruction.opcode = OpCodes.Ldc_I4_1;

            MetricsManager.LogModInfo(ModManager.GetMod("UD_Tinkering_Bytes"), $"Successfully transpiled {patchMethodName}");
            return codeMatcher.Vomit(Generator, doVomit).InstructionEnumeration();
        }

        public static bool ItemIsTradeUIDisplayOnly(GameObject Item) => VendorAction.ItemIsTradeUIDisplayOnly(Item);
    }
}
