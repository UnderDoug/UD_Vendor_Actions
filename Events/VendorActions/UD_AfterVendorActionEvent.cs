using XRL.World;

namespace UD_Vendor_Actions
{
    [GameEvent(Cascade = CASCADE_NONE, Cache = Cache.Pool)]
    public class UD_AfterVendorActionEvent : I_UD_VendorActionEvent<UD_AfterVendorActionEvent>
    {
        public UD_AfterVendorActionEvent()
        {
        }

        public override void Reset()
        {
            base.Reset();
        }
    }
}
