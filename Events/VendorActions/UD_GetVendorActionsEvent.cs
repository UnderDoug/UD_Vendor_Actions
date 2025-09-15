using System.Collections.Generic;
using Qud.UI;
using XRL;
using XRL.World;

namespace UD_Vendor_Actions
{
    [GameEvent(Cascade = CASCADE_NONE, Cache = Cache.Pool)]
    public class UD_GetVendorActionsEvent : I_UD_VendorActionEvent<UD_GetVendorActionsEvent>
    {
        public Dictionary<string, UD_VendorAction> Actions;

        public bool IncludeModernTradeOptions;

        public UD_GetVendorActionsEvent()
        {
        }

        public override void Reset()
        {
            base.Reset();
            Actions = null;
        }

        public static UD_GetVendorActionsEvent FromPool(TradeLine TradeLine, GameObject Vendor, GameObject Item, Dictionary<string, UD_VendorAction> Actions = null, bool IncludeModernTradeOptions = false)
        {
            UD_GetVendorActionsEvent E = FromPool(TradeLine, Vendor, Item, null, null);
            if (E != null)
            {
                E.Actions = Actions ?? new();
                E.IncludeModernTradeOptions = IncludeModernTradeOptions;
            }
            return E;
        }

        public static void Send(TradeLine TradeLine, GameObject Vendor, GameObject Item, Dictionary<string, UD_VendorAction> Actions, bool IncludeModernTradeOptions = false)
        {
            UD_GetVendorActionsEvent E = FromPool(TradeLine, Vendor, Item, Actions, IncludeModernTradeOptions);

            bool validVendor = GameObject.Validate(ref Vendor);
            bool validItem = GameObject.Validate(ref Item);

            bool vendorWants = validVendor && Vendor.WantEvent(ID, CascadeLevel);
            bool itemWants = validItem && Item.WantEvent(ID, CascadeLevel);
            bool playerWants = The.Player.WantEvent(ID, CascadeLevel);

            bool anyWants = vendorWants || itemWants || playerWants;

            if (E != null && validVendor && validItem)
            {
                bool blocked = false;

                if (anyWants)
                {
                    if (!blocked && vendorWants)
                    {
                        blocked = !Vendor.HandleEvent(E);
                    }
                    if (!blocked && itemWants)
                    {
                        blocked = !Item.HandleEvent(E);
                    }
                    if (!blocked && playerWants)
                    {
                        blocked = !The.Player.HandleEvent(E);
                    }
                }
            }
        }
        public bool AddAction(string Name, string Display = null, string Command = null, string PreferToHighlight = null, char Key = ' ', int Default = 0, int Priority = 0, int? DramsCost = null, bool FireOnVendor = true, bool FireOnItem = false, bool FireOnPlayer = false, GameObject FireOn = null, bool Override = false, bool ProcessAfterAwait = false, bool ProcessSecondAfterAwait = false, bool ClearAndSetUpTradeUI = false, bool Staggered = false, bool CloseTradeBeforeProcessingSecond = false, bool CloseTradeAfterProcessing = false)
        {
            Actions ??= new();

            if (!Override && Actions.ContainsKey(Name))
            {
                return false;
            }
            UD_VendorAction vendorAction = new()
            {
                Name = Name,
                Key = Key,
                Display = Display,
                Command = Command,
                PreferToHighlight = PreferToHighlight,
                Default = Default,
                Priority = Priority,
                DramsCost = DramsCost,
                FireOnVendor = FireOnVendor,
                FireOnItem = FireOnItem,
                FireOnPlayer = FireOnPlayer,
                FireOn = FireOn,
                ProcessAfterAwait = ProcessAfterAwait,
                ProcessSecondAfterAwait = ProcessAfterAwait || ProcessSecondAfterAwait,
                ClearAndSetUpTradeUI = ClearAndSetUpTradeUI,
                Staggered = Staggered,
            };
            vendorAction.CloseTradeBeforeProcessingSecond = vendorAction.Staggered && CloseTradeBeforeProcessingSecond;
            vendorAction.CloseTradeAfterProcessing = !vendorAction.CloseTradeBeforeProcessingSecond && CloseTradeAfterProcessing;
            Actions[Name] = vendorAction;
            return true;
        }
    }
}
