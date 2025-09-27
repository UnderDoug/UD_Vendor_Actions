using System;
using XRL.UI;
using static XRL.World.Parts.Skill.Tinkering;
using UD_Vendor_Actions;

namespace XRL.World.Parts
{
    [AlwaysHandlesVendor_UD_VendorActions] // Indicates that this part should be added via reflection to any vendor with whom the player attempts to engage in trade.
    [Serializable]
    public class Our_VendorExamine
        : IPart
        , IModEventHandler<UD_GetVendorActionsEvent> // Provides dispatch for UD_GetVendorActionsEvent
        , IModEventHandler<UD_VendorActionEvent>     // Provides dispatch for UD_VendorActionEvent
    {
        // Putting the command in a const makes it easy to update across the part/mod in the event it conflicts somewhere.
        public const string COMMAND_IDENTIFY = "Cmd_My_VendorExamine";

        // Typical WantEvent override for relevant events.
        public override bool WantEvent(int ID, int Cascade)
        {
            return base.WantEvent(ID, Cascade)
                || ID == UD_GetVendorActionsEvent.ID
                || ID == UD_VendorActionEvent.ID;
        }

        // No HandleEvent with UD_GetVendorActionsEvent parameter to override.
        // virtual isn't required, but lets a hypothetical derived type override it.
        public virtual bool HandleEvent(UD_GetVendorActionsEvent E)
        {
            if (E.Vendor != null && ParentObject == E.Vendor)
            {
                // Check how competent the vendor is at IDing items, and if the player doesn't understand the item
                if (GetIdentifyLevel(E.Vendor) > 0 && !E.Item.Understood())
                {
                    E.AddAction(
                        Name: "Identify",               // The action's name, used as the key in the actions table dictionary
                        Display: "identify",            // how the action will look in the vendor action menu
                        Command: COMMAND_IDENTIFY,      // the command to check for when handling UD_VendorActionEvent
                        Key: 'i',                       // the hotkey for this action in the menu. Gets highlighted in Display.
                        Priority: 9,                    // how high up the vendor action menu the action appears, higher number = higher up
                        Override: true,                 // tells UD_GetVendorActionsEvent to replace with this action any action with the same name
                        ClearAndSetUpTradeUI: true);    // tells the trade UI to refresh itself after this action is processed
                }
            }
            return base.HandleEvent(E);
        }

        // No HandleEvent with UD_VendorActionEvent parameter to override.
        // virtual isn't required, but lets a hypothetical derived type override it.
        public virtual bool HandleEvent(UD_VendorActionEvent E)
        {
            if (E.Vendor != null && ParentObject == E.Vendor)
            {
                // Check if this event is for the action we want to perform.
                if (E.Command == COMMAND_IDENTIFY)
                {
                    // Do some code...
                    string notAffordMessage = "=subject.T= won't check out =object.t's= bauble for less than {{C|5 drams}} of fresh water.";
                    string yesNoMessage = "=subject.T= will check out =object.t's= bauble for {{C|5 drams}} of fresh water.";
                    string identifiedMessage = "=subject.T= says:\n\n\"Yep, that's =object.a==object.refname=\"";
                    if (The.Player.GetFreeDrams() < 5)
                    {
                        Popup.Show(GameText.VariableReplace(notAffordMessage, E.Vendor, The.Player));
                        return false; // This effectively cancels the action. Nothing else will get to handle this event/action.
                    }
                    if (Popup.ShowYesNo(GameText.VariableReplace(yesNoMessage, E.Vendor, The.Player)) == DialogResult.Yes)
                    {
                        The.Player.UseDrams(5);
                        E.Vendor.GiveDrams(5);
                        Popup.Show(GameText.VariableReplace(identifiedMessage, E.Vendor, E.Item));
                        return true; // Typically the same return value as base.HandleEvent(E), but prevents the base class from performing any further handling.
                                     // If our action specified multiple "FireOn"s, such as the item itself, they'd also get the opportunity to handle this event/action.
                    }
                }
            }
            return base.HandleEvent(E);
        }
    }
}