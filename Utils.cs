using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using UD_Modding_Toolbox;
using XRL;
using XRL.World;

namespace UD_Vendor_Actions
{
    public static class Utils
    {
        private static bool doDebug => true;
        private static bool getDoDebug(string MethodName)
        {
            if (MethodName == nameof(ApplyVendorActionHandlerPartsFromAttribute))
                return true;

            return doDebug;
        }

        public static ModInfo ThisMod => ModManager.GetMod("UD_Vendor_Actions");

        public static void ApplyVendorActionHandlerPartsFromAttribute(GameObject Object, Type Attribute)
        {
            if (Object  == null || Attribute == null)
            {
                MetricsManager.LogModError(ThisMod, $"{nameof(Utils)}.{nameof(ApplyVendorActionHandlerPartsFromAttribute)} passed null value.");
                return;
            }

            int indent = Debug.LastIndent;
            bool doDebug = getDoDebug(nameof(ApplyVendorActionHandlerPartsFromAttribute));
            Debug.Entry(4, $"Applying {Attribute?.Name} parts to {nameof(Object)}: {Object?.DebugName ?? Const.NULL}",
                Indent: indent + 1, Toggle: doDebug);
            if (ModManager.GetTypesWithAttribute(Attribute) is List<Type> handle_UD_VendorActionTypes
                && !handle_UD_VendorActionTypes.IsNullOrEmpty())
            {
                foreach (Type handle_UD_VendorAction in ModManager.GetTypesWithAttribute(Attribute))
                {
                    if (Object.HasPart(handle_UD_VendorAction))
                    {
                        Debug.CheckNah(4, $"{handle_UD_VendorAction.Name} (already attached)", Indent: indent + 2, Toggle: doDebug);
                        continue;
                    }
                    if (Activator.CreateInstance(handle_UD_VendorAction) is IPart handle_UD_VendorActionPart)
                    {
                        Debug.CheckYeh(4, $"{handle_UD_VendorAction.Name}", Indent: indent + 2, Toggle: doDebug);
                        Object.AddPart(handle_UD_VendorActionPart);
                    }
                    else
                    {
                        Debug.CheckNah(4, $"{handle_UD_VendorAction.Name} (not an {nameof(IPart)})", Indent: indent + 2, Toggle: doDebug);
                        MetricsManager.LogPotentialModError(ModManager.GetMod(handle_UD_VendorAction.Assembly),
                            $"{handle_UD_VendorAction.GetType()} is decorated with {Attribute.Name} attribute but is not an IPart derivative.");
                    }
                }
            }
            else
            {
                Debug.LoopItem(4, $"None to apply.",
                        Indent: indent + 2, Toggle: doDebug);
            }
            Debug.LastIndent = indent;
        }
    }
}
