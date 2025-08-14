using XRL.World;

namespace UD_Vendor_Actions
{
    [GameEvent(Cascade = CASCADE_NONE, Cache = Cache.Pool)]
    public class AfterVendorActionEvent : IVendorActionEvent<AfterVendorActionEvent>
    {
        public AfterVendorActionEvent()
        {
        }

        public override void Reset()
        {
            base.Reset();
        }
    }
}
