using XRL.World;

namespace UD_Vendor_Actions
{
    /// <summary>
    /// Contracts a class as capable of handling the following modded events:
    /// <list type="bullet">
    /// <item><see cref="UD_GetVendorActionsEvent"/></item>
    /// <item><see cref="UD_VendorActionEvent"/></item>
    /// <item><see cref="UD_AfterVendorActionEvent"/></item>
    /// <item><see cref="UD_OwnerAfterVendorActionEvent"/></item>
    /// </list>
    /// </summary>
    public interface I_UD_VendorActionEventHandler
        : IModEventHandler<UD_GetVendorActionsEvent>
        , IModEventHandler<UD_VendorActionEvent>
        , IModEventHandler<UD_AfterVendorActionEvent>
        , IModEventHandler<UD_OwnerAfterVendorActionEvent>
    {
    }
}
