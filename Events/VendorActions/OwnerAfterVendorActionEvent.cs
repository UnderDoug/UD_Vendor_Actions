using XRL.World;

namespace UD_Vendor_Actions
{
    [GameEvent(Cascade = CASCADE_NONE, Cache = Cache.Pool)]
    public class OwnerAfterVendorActionEvent : IVendorActionEvent<OwnerAfterVendorActionEvent>
    {
        public OwnerAfterVendorActionEvent()
        {
        }

        public override void Reset()
        {
            base.Reset();
        }
    }
}
