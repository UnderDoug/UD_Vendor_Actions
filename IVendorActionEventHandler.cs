using XRL.World;

namespace UD_Vendor_Actions
{
    public interface IVendorActionEventHandler
        : IModEventHandler<GetVendorActionsEvent>
        , IModEventHandler<VendorActionEvent>
        , IModEventHandler<AfterVendorActionEvent>
        , IModEventHandler<OwnerAfterVendorActionEvent>
    {
    }
}
