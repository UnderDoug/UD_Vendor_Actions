using System;

namespace UD_Vendor_Actions
{
    /// <summary>
    /// An IPart for Items to handle UD_VendorActions that should always be available to handle them, including if the mod is enabled mid-save.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class AlwaysHandlesItem_UD_VendorActionsAttribute : Attribute
    {
    }
}
