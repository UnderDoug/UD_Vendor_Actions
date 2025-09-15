using XRL.World;

namespace UD_Vendor_Actions
{
    [GameEvent(Cascade = CASCADE_INVENTORY, Cache = Cache.Pool)]
    public class UD_EndTradeEvent : ModSingletonEvent<UD_EndTradeEvent>
    {
        public new static readonly int CascadeLevel = CASCADE_INVENTORY;

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
            if (Actor.HandleEvent(Instance))
            {
                Trader.HandleEvent(Instance);
            }
            Instance.Reset();
        }
    }
}
