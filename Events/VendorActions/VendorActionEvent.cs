using Qud.UI;
using XRL.World;

namespace UD_Vendor_Actions
{
    [GameEvent(Cascade = CASCADE_NONE, Cache = Cache.Pool)]
    public class VendorActionEvent : IVendorActionEvent<VendorActionEvent>
    {
        public bool Staggered;

        public bool Second;

        public bool CloseTradeRequested;

        public bool CancelSecondRequested;

        public VendorActionEvent()
        {
            Staggered = false;
            Second = false;
            CloseTradeRequested = false;
            CancelSecondRequested = false;
        }

        public override void Reset()
        {
            base.Reset();
            Staggered = false;
            Second = false;
            CloseTradeRequested = false;
            CancelSecondRequested = false;
        }

        public static bool Check(TradeLine TradeLine, GameObject Handler, GameObject Vendor, GameObject Item, GameObject Owner, string Command, out bool CloseTrade, out bool CancelSecond, int? DramsCost = null, bool Staggered = false, bool Second = false)
        {
            CloseTrade = false;
            CancelSecond = false;
            VendorActionEvent E = FromPool(TradeLine, Vendor, Item, Command, DramsCost);
            E.Staggered = Staggered;
            E.Second = Second;

            if (E != null && GameObject.Validate(ref Handler) && Handler.WantEvent(ID, CascadeLevel))
            {
                if (!Handler.HandleEvent(E))
                {
                    CloseTrade = E.IsCloseTradeRequested();
                    CancelSecond = E.IsCancelSecondRequested();
                    return false;
                }
                CancelSecond = E.IsCancelSecondRequested();
                CloseTrade = E.IsCloseTradeRequested();
                if (!Staggered || Second)
                {
                    AfterVendorActionEvent.SendAfter(Handler, E);
                    OwnerAfterVendorActionEvent.SendAfter(Owner, E);
                }
            }
            else
            {
                E?.Reset();
            }
            return true;
        }
        public void RequestTradeClose()
        {
            CloseTradeRequested = true;
        }
        public bool IsCloseTradeRequested()
        {
            return CloseTradeRequested;
        }
        public void RequestCancelSecond()
        {
            CancelSecondRequested = true;
        }
        public bool IsCancelSecondRequested()
        {
            return CancelSecondRequested;
        }

        public static implicit operator VendorActionEvent(AfterVendorActionEvent E)
        {
            return FromPool(E.TradeLine, E.Vendor, E.Item, E.Command, E.DramsCost);
        }
        public static implicit operator AfterVendorActionEvent(VendorActionEvent E)
        {
            return AfterVendorActionEvent.FromPool(E.TradeLine, E.Vendor, E.Item, E.Command, E.DramsCost);
        }
        public static implicit operator VendorActionEvent(OwnerAfterVendorActionEvent E)
        {
            return FromPool(E.TradeLine, E.Vendor, E.Item, E.Command, E.DramsCost);
        }
        public static implicit operator OwnerAfterVendorActionEvent(VendorActionEvent E)
        {
            return OwnerAfterVendorActionEvent.FromPool(E.TradeLine, E.Vendor, E.Item, E.Command, E.DramsCost);
        }
    }
}
