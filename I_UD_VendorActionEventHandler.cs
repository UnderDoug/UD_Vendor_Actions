using XRL.World;

namespace UD_Vendor_Actions
{
    public interface I_UD_VendorActionEventHandler
        : IModEventHandler<UD_GetVendorActionsEvent>
        , IModEventHandler<UD_VendorActionEvent>
        , IModEventHandler<UD_AfterVendorActionEvent>
        , IModEventHandler<UD_OwnerAfterVendorActionEvent>
    {
    }
}
