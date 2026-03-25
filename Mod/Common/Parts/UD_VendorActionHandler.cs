using System;
using XRL.UI;
using XRL.World.Parts.Skill;

using UD_Vendor_Actions;

using static XRL.World.Parts.Skill.Tinkering;
using Qud.UI;

namespace XRL.World.Parts
{
    /// <summary>
    /// Class designed to functionally restore the base game's vendor action behaviour that is otherwise skipped by the patches in this mod.
    /// </summary>
    /// <remarks>
    /// Largely, the base game's vendor action handler methods are called directly for their respective vendor actions, and the conditions by which they're offered are, for all intents and purposes, the same.<br/><br/>
    /// See <see cref="TradeUI.ShowVendorActions"/> for the base game's implementation to compare.
    /// </remarks>
    [AlwaysHandlesVendor_UD_VendorActions]
    [Serializable]
    public class UD_VendorActionHandler : IScribedPart, I_UD_VendorActionEventHandler
    {
        public const string COMMAND_LOOK = "CmdVendorLook";
        public const string COMMAND_ADD_TO_TRADE = "CmdTradeAdd";
        public const string COMMAND_IDENTIFY = "CmdVendorExamine";
        public const string COMMAND_REPAIR = "CmdVendorRepair";
        public const string COMMAND_RECHARGE = "CmdVendorRecharge";
        public const string COMMAND_READ = "CmdVendorRead";

        public UD_VendorActionHandler()
        {
        }

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register(UD_VendorActionEvent.ID, EventOrder.LATE, Serialize: true);
            base.Register(Object, Registrar);
        }
        public override bool WantEvent(int ID, int Cascade)
        {
            return base.WantEvent(ID, Cascade)
                || ID == UD_GetVendorActionsEvent.ID;
        }
        public virtual bool HandleEvent(UD_GetVendorActionsEvent E)
        {
            if (E.Vendor == ParentObject
                && ParentObject is GameObject vendor
                && E.Item is GameObject item)
            {
                int vendorIdentifyLevel = GetIdentifyLevel(vendor);
                bool itemUnderstood = item.Understood();
                Tinkering_Repair vendorRepairSkill = vendor.GetPart<Tinkering_Repair>();
                E.AddAction("Look", "look", COMMAND_LOOK, Key: 'l', Priority: 10);
                if (E.IncludeModernTradeOptions && !UD_VendorAction.ItemIsTradeUIDisplayOnly(item))
                {
                    E.AddAction("Add to trade", "add to trade", COMMAND_ADD_TO_TRADE, Key: 't', Priority: 9, ProcessAfterAwait: true);
                }
                if (vendorIdentifyLevel > 0 && !itemUnderstood)
                {
                    E.AddAction("Identify", "identify", COMMAND_IDENTIFY, Key: 'i', Priority: 8, ClearAndSetUpTradeUI: true);
                }
                if (vendorRepairSkill != null && IsRepairableEvent.Check(vendor, item, null, vendorRepairSkill))
                {
                    E.AddAction("Repair", "repair", COMMAND_REPAIR, Key: 'r', Priority: 7);
                }
                if (vendor.HasSkill(nameof(Tinkering_Tinker1)) 
                    && (itemUnderstood || vendorIdentifyLevel >= item.GetComplexity()) && item.NeedsRecharge())
                {
                    E.AddAction("Recharge", "recharge", COMMAND_RECHARGE, Key: 'c', Priority: 6, ClearAndSetUpTradeUI: true);
                }
                if (vendor.GetIntProperty("Librarian") != 0 
                    && item.HasInventoryActionWithCommand("Read") 
                    && item.InInventory == vendor)
                {
                    E.AddAction("Read", "read", COMMAND_RECHARGE, Key: 'b', Priority: 5);
                }
            }
            return base.HandleEvent(E);
        }
        public virtual bool HandleEvent(UD_VendorActionEvent E)
        {
            if (E.Vendor == ParentObject
                && ParentObject is GameObject vendor
                && E.Item is GameObject item
                && E.TradeLine is TradeLine tradeline)
            {
                if (E.Command == COMMAND_LOOK)
                {
                    TradeUI.DoVendorLook(item, vendor);
                    return true;
                }
                if (E.Command == COMMAND_IDENTIFY)
                {
                    TradeUI.DoVendorExamine(item, vendor);
                    return true;
                }
                if (E.Command == COMMAND_REPAIR)
                {
                    TradeUI.DoVendorRepair(item, vendor);
                    return true;
                }
                if (E.Command == COMMAND_RECHARGE)
                {
                    TradeUI.DoVendorRecharge(item, vendor);
                    return true;
                }
                if (E.Command == COMMAND_READ)
                {
                    TradeUI.DoVendorRead(item, vendor);
                    return true;
                }
                if (E.Command == COMMAND_ADD_TO_TRADE)
                {
                    tradeline.HandleTradeSome();
                    return true;
                }
            }
            return base.HandleEvent(E);
        }
    }
}
