using System;
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
            Debug.Entry(4, $"Applying {Attribute?.GetType()?.Name} parts to {nameof(Object)}: {Object?.DebugName ?? Const.NULL}",
                Indent: indent + 2, Toggle: doDebug);
            foreach (Type handle_UD_VendorAction in ModManager.GetTypesWithAttribute(Attribute))
            {
                Debug.LoopItem(4, $"{nameof(handle_UD_VendorAction)}: {handle_UD_VendorAction.GetType().Name}",
                    Indent: indent + 2, Toggle: doDebug);
                if (Object.HasPart(handle_UD_VendorAction))
                {
                    continue;
                }
                if (Activator.CreateInstance(handle_UD_VendorAction) is IPart handle_UD_VendorActionPart)
                {
                    Object.AddPart(handle_UD_VendorActionPart);
                }
                else
                {
                    MetricsManager.LogPotentialModError(ModManager.GetMod(handle_UD_VendorAction.Assembly),
                        $"{handle_UD_VendorAction.GetType()} is decorated with {Attribute.GetType().Name} attribute but is not an IPart derivative.");
                }
            }
            Debug.LastIndent = indent;
        }
    }
}
