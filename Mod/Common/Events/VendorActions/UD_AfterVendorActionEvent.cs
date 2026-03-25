using XRL.World;

namespace UD_Vendor_Actions
{
    /// <summary>
    /// Signals that a <see cref="UD_VendorActionEvent"/> has successfully resolved.
    /// </summary>
    /// <remarks>
    /// A class can implement <see cref="I_UD_VendorActionEventHandler"/> to enable handling of the entire <see cref="I_UD_VendorActionEvent{T}"/> family of modded events.
    /// </remarks>
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
