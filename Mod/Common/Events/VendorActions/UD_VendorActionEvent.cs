using Qud.UI;
using XRL;
using XRL.World;
using UD_Vendor_Actions.Harmony;

namespace UD_Vendor_Actions
{
    /// <summary>
    /// A modded analogue to the base game's <see cref="InventoryActionEvent"/> and serves nearly the exact same function, extended to traders (vendors).
    /// </summary>
    /// <remarks>This class is responsible for providing the opportunity for various <see cref="IPart"/>s to perform custom behaviour in response to a selected <see cref="UD_VendorAction"/>.<br/><br/>
    /// A class can implement <see cref="I_UD_VendorActionEventHandler"/> to enable handling of the entire <see cref="I_UD_VendorActionEvent{T}"/> family of modded events.</remarks>
    [GameEvent(Cascade = CASCADE_NONE, Cache = Cache.Pool)]
    public class UD_VendorActionEvent : I_UD_VendorActionEvent<UD_VendorActionEvent>
    {
        /// <summary>Indicates that this event will be sent twice.</summary>
        public bool Staggered;

        /// <summary>Indicates whether this is the second time the event has been sent.</summary>
        public bool Second;

        /// <summary>Indicates that the trade window should be closed after the conclusion of <see cref="Check"/>.</summary>
        /// <remarks><see cref="RequestTradeClose"/> can be used to set this field.</remarks>
        public bool CloseTradeRequested;

        /// <summary>If <see cref="Staggered"/> and not <see cref="Second"/>, indicates that the event is no longer needed a second time.</summary>
        /// <remarks><see cref="RequestCancelSecond"/> can be used to set this field.</remarks>
        public bool CancelSecondRequested;

        public UD_VendorActionEvent()
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

        /// <summary>
        /// Calls <see cref="IModEventHandler{I_UD_VendorActionEvent{T}}.HandleEvent(I_UD_VendorActionEvent{T})"/> on the supplied <paramref name="Handler"/>, and <see langword="out"/>s for <see cref="UD_VendorAction.Process"/> whether the trade window should be closed and whether, if <see cref="Staggered"/>, the second call should be cancelled.
        /// </summary>
        /// <remarks>
        /// This method is also responsible for calling <see cref="I_UD_VendorActionEvent{T}.SendAfter"/> for <see cref="UD_AfterVendorActionEvent"/>, and <see cref="UD_OwnerAfterVendorActionEvent"/>, if handling is successful.
        /// </remarks>
        /// <param name="TradeLine">The <see cref="TradeLine"/> instance from which the method was called.</param>
        /// <param name="Handler">The entity that will call <see cref="IModEventHandler{I_UD_VendorActionEvent{T}}.HandleEvent(I_UD_VendorActionEvent{T})"/>, handling the event if it <see cref="GameObject.WantEvent"/>.<br/>Typically one of the <paramref name="Vendor"/>, the <paramref name="Item"/>, or <see cref="The.Player"/>, but potentially anything, given <see cref="UD_VendorAction.FireOn"/>, if supplied, is always the first <paramref name="Handler"/>.</param>
        /// <param name="Vendor">The vendor with whom the player is currently "engaged in trade". This could be a merchant, or a companion, or even a container.</param>
        /// <param name="Item">The item for which the vendor actions are being collected.</param>
        /// <param name="Owner">This is typically the <paramref name="Item"/>'s <see cref="GameObject.InInventory"/>.</param>
        /// <param name="Command">An identifier for the action being sent for handling.</param>
        /// <param name="CloseTrade">Indicates that the trade window should be closed after the conclusion of this method.</param>
        /// <param name="CancelSecond">If <see cref="Staggered"/> and not <see cref="Second"/>, indicates that the event is no longer needed a second time.</param>
        /// <param name="DramsCost">A "simple" way of defining a drams cost that can be retrieved when this event is being handled. Largely unused.</param>
        /// <param name="Staggered">Indicates that this event will be sent twice.</param>
        /// <param name="Second">Indicates whether this is the second time the event has been sent.</param>
        /// <returns><see langword="true"/> if <paramref name="Handler"/> successfully <see cref="GameObject.Validate(ref GameObject)"/>, <see cref="GameObject.WantEvent"/>, and <see cref="IModEventHandler{I_UD_VendorActionEvent{T}}.HandleEvent(I_UD_VendorActionEvent{T})"/> returns <see langword="true"/>;<br/>
        /// <see langword="false"/> otherwise.</returns>
        public static bool Check(
            TradeLine TradeLine,
            GameObject Handler,
            GameObject Vendor,
            GameObject Item,
            GameObject Owner,
            string Command,
            out bool CloseTrade,
            out bool CancelSecond,
            int? DramsCost = null,
            bool Staggered = false,
            bool Second = false)
        {
            CloseTrade = false;
            CancelSecond = false;
            UD_VendorActionEvent E = FromPool(TradeLine, Vendor, Item, Command, DramsCost);
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
                    UD_AfterVendorActionEvent.SendAfter(Handler, E);
                    UD_OwnerAfterVendorActionEvent.SendAfter(Owner, E);
                }
            }
            else
            {
                E?.Reset();
            }
            return true;
        }

        /// <summary></summary>
        public void RequestTradeClose()
        {
            CloseTradeRequested = true;
        }

        /// <summary></summary>
        /// <returns></returns>
        public bool IsCloseTradeRequested()
        {
            return CloseTradeRequested;
        }

        /// <summary></summary>
        public void RequestCancelSecond()
        {
            CancelSecondRequested = true;
        }

        /// <summary></summary>
        /// <returns></returns>
        public bool IsCancelSecondRequested()
        {
            return CancelSecondRequested;
        }

        public static implicit operator UD_VendorActionEvent(UD_AfterVendorActionEvent E)
        {
            return FromPool(E.TradeLine, E.Vendor, E.Item, E.Command, E.DramsCost);
        }
        public static implicit operator UD_AfterVendorActionEvent(UD_VendorActionEvent E)
        {
            return UD_AfterVendorActionEvent.FromPool(E.TradeLine, E.Vendor, E.Item, E.Command, E.DramsCost);
        }
        public static implicit operator UD_VendorActionEvent(UD_OwnerAfterVendorActionEvent E)
        {
            return FromPool(E.TradeLine, E.Vendor, E.Item, E.Command, E.DramsCost);
        }
        public static implicit operator UD_OwnerAfterVendorActionEvent(UD_VendorActionEvent E)
        {
            return UD_OwnerAfterVendorActionEvent.FromPool(E.TradeLine, E.Vendor, E.Item, E.Command, E.DramsCost);
        }
    }
}
