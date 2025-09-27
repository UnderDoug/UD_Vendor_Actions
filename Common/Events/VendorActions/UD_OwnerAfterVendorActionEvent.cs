using XRL.World;

namespace UD_Vendor_Actions
{
    /// <summary>
    /// Signals that a <see cref="UD_VendorActionEvent"/> has successfully resolved, sent to the Owner supplied to <see cref="UD_VendorActionEvent.Check"/>.
    /// </summary>
    /// <remarks>
    /// A class can implement <see cref="I_UD_VendorActionEventHandler"/> to enable handling of the entire <see cref="I_UD_VendorActionEvent{T}"/> family of modded events.
    /// </remarks>
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
