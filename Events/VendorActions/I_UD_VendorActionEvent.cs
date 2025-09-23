using Qud.UI;
using XRL.World;

namespace UD_Vendor_Actions
{
    /// <summary>
    /// A base <see cref="ModPooledEvent{T}"/> for a family of <see cref="UD_VendorAction"/>-focused events to inherit common members from.
    /// </summary>
    /// <typeparam name="T">A class inheriting from <see cref="I_UD_VendorActionEvent{T}"/>, with a default parameterless constructor.</typeparam>
    [GameEvent(Base = true, Cascade = CASCADE_NONE, Cache = Cache.Pool)]
    public abstract class I_UD_VendorActionEvent<T> : ModPooledEvent<T>
        where T : I_UD_VendorActionEvent<T>, new()
    {
        public new static readonly int CascadeLevel = CASCADE_NONE;

        public static string RegisteredEventID => typeof(T).Name;

        /// <summary>The entity with whom the player is or was "engaged in trade".</summary>
        public GameObject Vendor;

        /// <summary>The object around which a given <see cref="UD_VendorAction"/> is centred.</summary>
        public GameObject Item;

        /// <summary>See <see cref="UD_VendorAction.Command"/>: <inheritdoc cref="UD_VendorAction.Command"/>.</summary>
        public string Command;

        /// <summary>See <see cref="UD_VendorAction.DramsCost"/>: <inheritdoc cref="UD_VendorAction.DramsCost"/>.</summary>
        public int? DramsCost;

        /// <summary>The <see cref="Qud.UI.TradeLine"/> instance from which the event was fired.</summary>
        public TradeLine TradeLine;

        public I_UD_VendorActionEvent()
        {
        }

        public virtual string GetRegisteredEventID()
        {
            return RegisteredEventID;
        }

        public override void Reset()
        {
            base.Reset();
            Vendor = null;
            Item = null;
            Command = null;
            DramsCost = null;
            TradeLine = null;
        }

        public static T FromPool(TradeLine TradeLine, GameObject Vendor, GameObject Item, string Command = null, int? DramsCost = null)
        {
            if (GameObject.Validate(ref Vendor))
            {
                T E = FromPool();
                E.TradeLine = TradeLine;
                E.Vendor = Vendor;
                E.Item = Item;
                E.Command = Command;
                E.DramsCost = DramsCost;
                return E;
            }
            return null;
        }

        public static T FromPool(T Source)
        {
            if (GameObject.Validate(ref Source.Vendor))
            {
                T E = FromPool();
                E.TradeLine = Source.TradeLine;
                E.Vendor = Source.Vendor;
                E.Item = Source.Item;
                E.Command = Source.Command;
                E.DramsCost = Source.DramsCost;
                return E;
            }
            return null;
        }

        /// <summary>Handles sending the "after" subset of events in this event family.<br/><br/>
        /// Forms part of the near 1:1 copy this code is of the decompiled base game's <see cref="InventoryActionEvent"/> family of <see cref="MinEvent"/>.</summary>
        /// <param name="Object">The object that will call <see cref="IModEventHandler{I_UD_VendorActionEvent{T}}.HandleEvent(I_UD_VendorActionEvent{T})"/>.</param>
        /// <param name="Source">The parent <see cref="I_UD_VendorActionEvent{T}"/> that called this method.</param>
        public static void SendAfter(GameObject Object, T Source)
        {
            if (GameObject.Validate(ref Object) && Object.WantEvent(ID, CascadeLevel))
            {
                T E = FromPool(Source);
                if (E != null)
                {
                    Object.HandleEvent(E);
                    Source.ProcessChildEvent(E);
                    Source.TradeLine = E.TradeLine;
                    Source.Vendor = E.Vendor;
                    Source.Item = E.Item;
                    Source.Command = E.Command;
                    Source.DramsCost = E.DramsCost;
                }
            }
        }
    }
}
