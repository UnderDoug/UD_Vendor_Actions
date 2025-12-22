using XRL.World;
using XRL.UI;

using UD_Vendor_Actions.Harmony;

namespace UD_Vendor_Actions
{
    /// <summary>
    /// A modded sibling event to <see cref="StartTradeEvent"/>, that is called by <see cref="TradeUI_Patches.ShowTradeScreen_SendEvent_Postfix"/>, a patch of <see cref="TradeUI.ShowTradeScreen"/>.
    /// </summary>
    /// <remarks>
    /// If you want to handle this event, ensure your handling part implements <see cref="IModEventHandler{StartTradeEvent}"/>, <see langword="where"/> T is <see cref="UD_EndTradeEvent"/>.
    /// </remarks>
    [GameEvent(Cascade = CASCADE_EQUIPMENT | CASCADE_INVENTORY | CASCADE_SLOTS, Cache = Cache.Singleton)]
    public class UD_EndTradeEvent : ModSingletonEvent<UD_EndTradeEvent>
    {
        public new static readonly int CascadeLevel = CASCADE_EQUIPMENT | CASCADE_INVENTORY | CASCADE_SLOTS;

        public static string RegisteredEventID => nameof(UD_EndTradeEvent);

        public GameObject Actor;

        public GameObject Trader;

        public UD_EndTradeEvent()
        {
        }

        public virtual string GetRegisteredEventID()
        {
            return RegisteredEventID;
        }

        public override int GetCascadeLevel()
        {
            return CascadeLevel;
        }

        public override void Reset()
        {
            base.Reset();
            Actor = null;
            Trader = null;
        }
        public static void Send(GameObject Actor, GameObject Trader)
        {
            Instance.Actor = Actor;
            Instance.Trader = Trader;

            Instance.Send(Actor);
            Instance.Send(Trader);

            Instance.Reset();
        }
        public void Send(GameObject Handler)
        {
            if (Handler != null
                && Handler.WantEvent(ID, CascadeLevel))
            {
                Handler.HandleEvent(Instance);
            }
        }
    }
}
