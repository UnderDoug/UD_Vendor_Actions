using System;
using UD_Vendor_Actions;

namespace XRL.World.Parts
{
    [AlwaysHandlesVendor_UD_VendorActions] // Indicates that this part should be added via reflection to any vendor with whom the player attempts to engage in trade.
    [Serializable]
    public class Our_VendorLoyaltyCard : IPart
    {
        public override bool WantEvent(int ID, int Cascade)
        {
            return base.WantEvent(ID, Cascade)
                || ID == StartTradeEvent.ID;
        }
        public override bool HandleEvent(StartTradeEvent E)
        {
            if (E.Trader is GameObject vendor && vendor == ParentObject
                && (!vendor.HasPart<Container>() || vendor.HasPart<AnimatedObject>()) // vendor doesn't have Container part unless it's also an AnimatedObject
                && !vendor.HasPart<InteriorContainer>() // vendor doesn't have the InteriorContainer part.
                && vendor.GetInventory(go => go.Blueprint == "Our_TradePerformance_Display").IsNullOrEmpty()) // vendor doesn't already have our loyalty card object
            {
                // Do code...

                // Create an unmodified copy of the item, give it to the vendor.
                if (GameObject.CreateUnmodified("Our_TradePerformance_Display") is GameObject loyaltyCardObject)
                {
                    vendor.ReceiveObject(loyaltyCardObject);
                }
            }
            return base.HandleEvent(E);
        }
    }
}