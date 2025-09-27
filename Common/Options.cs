using System.Collections.Generic;
using XRL;

namespace UD_Vendor_Actions
{
    [HasModSensitiveStaticCache]
    [HasOptionFlagUpdate(Prefix = "Option_UD_Vendor_Actions_")]
    public static class Options
    {
        public static bool doDebug = true;
        public static Dictionary<string, bool> classDoDebug = new()
        {
            // General
            { nameof(Utils), true },
            { nameof(Extensions), true },
        };

        public static bool getClassDoDebug(string Class)
        {
            if (classDoDebug.ContainsKey(Class))
            {
                return classDoDebug[Class];
            }
            return doDebug;
        }

        // Debug Settings
        [OptionFlag] public static bool DebugEnableExampleFiles;
    }
}
