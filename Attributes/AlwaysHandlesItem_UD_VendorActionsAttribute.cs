using System;
using XRL.World;

namespace UD_Vendor_Actions
{
    /// <summary>
    /// An <see cref="IPart"/> designed for Items that should always be available to offer one or more <see cref="UD_VendorAction"/> to <see cref="UD_GetVendorActionsEvent"/>, including if the mod is enabled mid-save.<br/><br/>
    /// There are currently no good examples, however if this part would be attached to the base "Item" blueprint then it should be decorated with this attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class AlwaysHandlesItem_UD_VendorActionsAttribute : Attribute
    {
    }
}
