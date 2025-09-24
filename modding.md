# Modding

So, you'd like to make a vendor do some custom behaviour in the trade UI?

Well, this is the mod library for you!

## Vendor Actions

A vendor action can be thought of like a service that a trader can perform on behalf of the player. They already exist in the base game, but they're relatively closed off from easy access (for now, there's a BitBucket issue requesting exposure), which is where this library comes in.

Things like tinkers repairing broken items and identifyign weird artifacts for the player are obvious examples, but so is Sheba Hagadias allowing the player to read donated books, and the simple ability to "look" at an item in the trade UI.

For this quick instructional, we'll be making an alternative to a tinker's ability to identify (examine) an object for the player.

### Making a Vendor Action Handling Part
An `IPart` or its derivative may implement either `I_UD_VendorActionEventHandler` or any of the individual `IModEventHandler<T>` interfaces to enable handling of `UD_GetVendorActionEvent` and `UD_VendorActionEvent`. We'll do it the the long way so I can put helpful comments in there.

Normal `WantEvent`ing and `HandleEvent`ing can proceed from here, keeping in mind that `HandleEvent` will not have a valid override if the event being handled is modded (all of the events provided by this library _are_, just to state it explicitly) and if the base class doesn't already implement it `virtual`ly (`IPart` definitely won't).

Below is what our part might look like.

`Our_VendorExamine.cs`
```cs
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
```
An exact copy of this file is available in this mod's `Examples` directory.

### Attaching Our Part
Merging this part into one or a couple of some base objects makes sense, so that as many "things" as possible are capable of performing this action if they otherwise meet the conditions we laid out.

Most objects don't have an `IdentifyLevel` greater than `0`, and, while the number of objects that _do_ jumps significantly by comparison when you consider only the subset that are _creatures_, it's still scarce even then.

Becasue of this, our part won't do much for most things it's attached to, but everything in Qud is in a constant state of flux and so it's concievable that a creature, or even an object, could develope a non-zero `IdentifyLevel`, and, thus, we should include it anyway.

Below is an example of merging our part into one of the base-most objects, `PhysicalObject`:

`PhysicalPhenomena.xml`
```xml
<?xml version="1.0" encoding="utf-8" ?>
<objects>
  
  <!-- Adds a handler for UD_VenderActionEvents that is added by this mod -->
  <object Name="PhysicalObject" Load="Merge">
    <part Name="Our_VendorExamine" />
  </object>
  
</objects>
```

Almost anything you could "physically" interact with will inherit from `PhysicalObject`. That includes the obvious `Creature`, but also includes the less obvious `Fungus` which does not inherit from `Creature`. Also captured by merging so early in the inheritance tree are any franken-objects (objects that have been animated), such as animated walls and doors. These latter objects would be missed if we merged only into `Creature`. Not to mention that a chest, a piece of furniture, is _traded with_ to store or retreive items.

### To Merge or Not To Merge

Due to having decorated our part with `[AlwaysHandlesVendor_UD_VendorActions]`, _anything_ the player manages to open the trade window with will have our part attached to it. By decorating the class this way, we've signalled to UD_Vendor_Actions (this library) that this is an important vendor action handling part and should _always_ be made available to offer and perform its vendor actions, including if our hypothetical mod, or UD_Vendor_Actions was added mid-save.

Considering the above, and the fact that our part doesn't need to do anything other than handle its action, we can wholly eschew the use of the XML in favour of having it added dynamically when `[AlwaysHandlesVendor_UD_VendorActions]` decorated classes are checked for and attached. 

While a trader performing an examination of an item is relatively niche, we want to ensure that _any_ trader that _becomes_ capable of doing so is able to. In this way, although it is a "rare" and "niche" action, it makes sense for it to _always_ be available to trade-capable objects. Attempting to add it only when a trader has the `Tinkering` skill means traders who get the skill after they're created miss out, as do non-tinker mutants with `Psychometry`. Not to mention the logistical nightmare of attempting to catch every possible way something might meet our condition (having a non-zero identify level) and trying to attach our part when all those things occur.

### Testing It Out

Technically speaking, all our hypothetical mod needs to work fully (other than having UD_Vendor_Actions and UD_Modding_Toolbox enabled) is the above `Our_VendorExamine.cs` file placed into a directory in our local mods folder. Check out the Caves of Qud wiki's page on [installing mods](https://wiki.cavesofqud.com/wiki/Modding:Installing_a_mod#Locate_Your_Mods_Folder) for more info on where to find it.

Once you've got our hypothetical mod "installed", fire up a run, whether new or an existing save, and go find a tinker to chat up. Wishing for `Bep` is a good way to quickly get a highly skilled tinker who's happy to trade.

## Display-Only Items

Say you've got some trade-pertinent information that a part you've added contains, and you want to be able to show that information inside the trade window. That's where a "display-only" item comes in.

Display-only items are mostly like other items with a couple of caveats, they:
- Will respond to `lmb` by opening the vendor action menu.
- Will respond to `rmb` by opening the vendor action menu.
- Will respond to `space`, while highlighted, by opening the vendor action menu.
- Will respond to `click`+`drag` by remaining at "0 included in trade".
- Will not increment the number "included in trade".
- Will not display a price on the right of their trade line.
- Will obliterate them selves when trade ends, or if a turn tick occurs while they exists.

Like a regular item, though, they'll show thier "look" when hovered over long enough, and can be the target of vendor actions.

### Making A Display-Only Item 

The first step to making a display-only item is to create a new blueprint that inherits from `UD_TradeUI_DisplayItem`. We're going to make an item that shows the player's current trade performance with the trader. 

Trade performance is the multiplier used to alter the buy/sell price on the basis of ego score and faction rep. When an item is in the trader's inventory, its value gets divided by this number. When it's in the player's inventory, it gets multiplied instead.

`Items.xml`
```xml
<?xml version="1.0" encoding="utf-8" ?>
<objects>
  
  <object Name="Our_TradePerformance_Display" Inherits="UD_TradeUI_DisplayItem">
  <part Name="Physics" Category="A+++ VIP Member Status" />
    <part Name="Render" DisplayName="Loyalty Card" TileColor="&amp;Y" DetailColor="W" Tile="Items/sw_credit_wedge.bmp" />
    <part Name="Description" Short="There's no higher virtue than fealty, except for savings savings savings!" />
    <part Name="Our_TradePerformance_DisplayPart" />
  </object>
  
</objects>
```

We've pre-attached a part we're yet to make that'll handle showing the display information we want to show.

There are two more main steps:

1. Write `Our_TradePerformance_DisplayPart` which will handle filling out the dynamic information we want (trade performance score)
2. Write a part for the trader that will create our new display-only item when we trade with them.

### Making Our Trade Performance Display Part

Trade performance can be retreived by calling `GetTradePerformanceEvent.GetFor(The.Player, Trader)` which should make this really straight forward.

We want to do two things with our display-only item. We want a summary of the information to show up in the display name for the item's TradeLine, and we want a little _more_ detail instead in the short description when out item is looked at.

One of the patches that [UnderDoug's Modding Toolbox](https://github.com/UnderDoug/UD_Modding_Toolbox/), a dependency of this mod, includes to `TradeLine.setData` makes use of the base game's `GetDisplayNameEvent` in place of `GameObject.DisplayName` to include a `Context` argument. This allows us to be more precise with which components of a display name will show up at different points.

Below is a simple part that will retreive the information we want and put it in the appropriate places:

`Our_TradePerformance_DisplayPart.cs`
```cs
using System;
using Qud.UI;

namespace XRL.World.Parts
{
    [Serializable]
    public class Our_TradePerformance_DisplayPart : IPart
    {
        public override bool CanGenerateStacked()
        {
            return false;
        }
        public override bool AllowStaticRegistration()
        {
            return true;
        }
        public override bool WantEvent(int ID, int Cascade)
        {
            return base.WantEvent(ID, Cascade)
                || ID == GetDisplayNameEvent.ID
                || ID == GetShortDescriptionEvent.ID;
        }
        public override bool HandleEvent(GetDisplayNameEvent E)
        {
            if (ParentObject.InInventory is GameObject vendor
                && E.Context == nameof(TradeLine))
            {
                double performance = GetTradePerformanceEvent.GetFor(The.Player, vendor);
                string membershipRating = Math.Round(performance * 100, 0).ToString().Color("W");
                E.AddTag($" - {membershipRating}".Color("y"));
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetShortDescriptionEvent E)
        {
            if (ParentObject.InInventory is GameObject vendor)
            {
                double performance = GetTradePerformanceEvent.GetFor(The.Player, vendor);
                string membershipRating = Math.Round(performance * 100, 0).ToString().Color("W");
                string membershipMessage = "=object.T's= {{Y|A+}}{{W|+}}{{M|+}} {{Y|VIP}} {{C|Member Status}} with =subject.t= is " + membershipRating;
                E.Postfix.AppendLine()
                    .Append(GameText.VariableReplace(membershipMessage, vendor, The.Player));

                if (vendor.IsPlayerLed())
                {
                    E.Postfix.AppendLine().AppendLine()
                        .Append("We're {{M|Besties!}}");
                }
            }
            return base.HandleEvent(E);
        }
    }
}
```