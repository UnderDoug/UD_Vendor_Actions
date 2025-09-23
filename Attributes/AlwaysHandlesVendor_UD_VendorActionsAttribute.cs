using System;
using XRL.World;
using XRL.World.Parts;

namespace UD_Vendor_Actions
{
    /// <summary>
    /// An <see cref="IPart"/> designed for Vendors that should always be available to offer one or more <see cref="UD_VendorAction"/> to <see cref="UD_GetVendorActionsEvent"/>, including if the mod is enabled mid-save.<br/><br/>
    /// A good example of one such part is <see cref="UD_VendorActionHandler"/>, which was designed to be attached to the base "Creature" blueprint.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class AlwaysHandlesVendor_UD_VendorActionsAttribute : Attribute
    {
    }
}
