using XRL.World;

namespace UD_Vendor_Actions
{
    [GameEvent(Cascade = CASCADE_NONE, Cache = Cache.Pool)]
    public class UD_OwnerAfterVendorActionEvent : I_UD_VendorActionEvent<UD_OwnerAfterVendorActionEvent>
    {
        public UD_OwnerAfterVendorActionEvent()
        {
        }

        public override void Reset()
        {
            base.Reset();
        }
    }
}
