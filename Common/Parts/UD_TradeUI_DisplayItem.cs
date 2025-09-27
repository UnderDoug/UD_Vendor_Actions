using System;
using UD_Modding_Toolbox;
using UD_Vendor_Actions;

namespace XRL.World.Parts
{
    /// <summary>
    /// Prevents a "display-only" pseudo-item's existence persisting longer than the duration of the trade it was created for.
    /// </summary>
    /// <remarks>
    /// Pseudo-items by their nature aren't "real" items and shouldn't have the opportunity to be interacted with outside of the trade UI. They're an abstraction of either inforamtion relevant to trading, such as the contents of a creature's bit locker, or of a service that a trader could provide that doesn't involve an item, involves an item yet to be picked, or involves an item yet to exist.
    /// </remarks>
    [Serializable]
    public class UD_TradeUI_DisplayItem : IScribedPart, IModEventHandler<UD_EndTradeEvent>
    {
        public bool CeaseExistence(MinEvent FromEvent = null)
        {
            string label = $"{nameof(CeaseExistence)}({FromEvent?.GetType()?.Name ?? nameof(TurnTick)})";
            if (ParentObject != null)
            {
                Debug.CheckYeh(4, label, ParentObject.DebugName, Indent: Debug.LastIndent);
                ParentObject.Obliterate();
                return true;
            }
            Debug.CheckNah(2, label, ParentObject?.DebugName ?? Const.NULL, Indent: Debug.LastIndent);
            return false;
        }
        public override bool WantTurnTick()
        {
            return base.WantTurnTick()
                || true;
        }
        public override void TurnTick(long TimeTick, int Amount)
        {
            if (!CeaseExistence())
            {
                base.TurnTick(TimeTick, Amount);
            }
        }
        public override bool WantEvent(int ID, int Cascade)
        {
            return base.WantEvent(ID, Cascade)
                || ID == UD_EndTradeEvent.ID;
        }
        public virtual bool HandleEvent(UD_EndTradeEvent E)
        {
            CeaseExistence(E);
            return base.HandleEvent(E);
        }
    }
}
