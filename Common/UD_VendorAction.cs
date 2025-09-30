using ConsoleLib.Console;
using System.Collections.Generic;
using System.Text;

using Genkit;
using Qud.UI;

using XRL;
using XRL.UI;
using XRL.World;

using UD_Vendor_Actions.Harmony;

namespace UD_Vendor_Actions
{
    /// <summary>
    /// This class is analogous to the base game's <see cref="InventoryAction"/> and serves nearly the exact same function, extended to a given <see cref="GameObject"/> with whom the player is "engaged in trade".
    /// </summary>
    public class UD_VendorAction
    {
        public class Comparer : IComparer<UD_VendorAction>
        {
            public bool priorityFirst;

            public int Compare(UD_VendorAction a, UD_VendorAction b)
            {
                if (!priorityFirst)
                {
                    return SortCompare(a, b);
                }
                return PriorityCompare(a, b);
            }
        }

        /// <summary>The action currently being processed. Typically only assigned while <see cref="UD_VendorActionEvent"/> is being dispatched by the given action and handled.</summary>
        public static UD_VendorAction CurrentAction = null;

        /// <summary>Indicates whether this action has been processed once already in the case of a <see cref="Staggered"/> action.</summary>
        /// <remarks>While <see langword="private"/>, <see cref="UD_VendorActionEvent.Second"/> can be used during <see cref="IModEventHandler{UD_VendorActionEvent}.HandleEvent(UD_VendorActionEvent)"/> to check for which firing a given handling is occuring for.</remarks>
        private bool FirstProcessed = false;

        /// <summary>The name of the action, and the Key for the action's entry in <see cref="UD_GetVendorActionsEvent.Actions"/>.</summary>
        /// <remarks>This is also the means by which actions can be overridden during the collection event if there's functionality a prospective modder wants to perform instead.</remarks>
        public string Name;

        /// <summary>The hotkey for the action in the resultant menu.</summary>
        /// <remarks>Where possible, it'll be highlighted in the <see cref="Display"/> string; otherwise, it will be to the left in typical "[h] hotkey" style.</remarks>
        public char Key;

        /// <summary>How the action will be displayed in the resultant menu, with some caveats.</summary>
        /// <remarks>"Identify" with 'i' as the <see cref="Key"/> will display as "identify" due to the first instance of "I" being replaced with the hotkey, which is lower case, and highlighted.</remarks>
        public string Display;

        /// <summary>The command sent in conjunction with an <see cref="I_UD_VendorActionEvent{T}"/> for handling parts to check that they are responding to the correct action.</summary>
        public string Command;

        /// <summary>A substring of <see cref="Display"/> which can be optionally specified, and the resultant menu will try first to find an appropriate character to replace with the <see cref="Key"/>.</summary>
        /// <remarks>Tinkering Bytes uses this field for tinkering modifications:<br/>
        /// When getting vendor actions for data disks, if the disk is for an item modification, the action has "mod an item with tinkering" assigned to <see cref="Display"/>, and 'T' assigned to <see cref="Key"/>.<br/>
        /// This would show up in the menu as "mod an iTem with tinkering" if not for setting PreferToHighlight to "tinkering".<br/>
        /// Set so, the result is the menu instead displaying "mod an item with Tinkering", where the "T" in "Tinkering" is the highlighted hotkey.</remarks>
        public string PreferToHighlight;

        /// <summary>Used to determine which is the "selected by default" option in the resultant menu. Higher numbers supersede.</summary>
        public int Default;

        /// <summary>How high up the list an action should be, where higher numbers indicate a higher position in the list.</summary>
        /// <remarks>"[l]ook" is Priority 10.</remarks>
        public int Priority;

        /// <summary>A "simple" way of defining a drams cost that can be retrieved when an <see cref="I_UD_VendorActionEvent{T}"/> is fired.</summary>
        /// <remarks>This started as a way to quickly test passing arguments to the event, but removing it seemed pointless.</remarks>
        public int? DramsCost;

        /// <summary><see cref="FireOnVendor"/>, <see cref="FireOnItem"/> and <see cref="FireOnPlayer"/>, are used to determine which entity the action is supposed to <see cref="IModEventHandler{I_UD_VendorActionEvent{T}}.HandleEvent(I_UD_VendorActionEvent{T})"/> it.</summary>
        /// <remarks><see cref="Process"/> will always fire first on <see cref="FireOn"/> if assigned a non-<see langword="null"/> value, followed by, if their respective field is <see langword="true"/>, the vendor, the item, and the player, in that order.<br/><br/>
        /// If multiple of these fieldss are set <see langword="true"/>, then each indicated entity will get the opportunity to handle the event unless one of the prior handling entities returns false during their handling.</remarks>
        public bool FireOnVendor;

        /// <summary><see cref="FireOnVendor"/>, <see cref="FireOnItem"/> and <see cref="FireOnPlayer"/>, are used to determine which entity the action is supposed to <see cref="IModEventHandler{I_UD_VendorActionEvent{T}}.HandleEvent(I_UD_VendorActionEvent{T})"/> it.</summary>
        /// <remarks><see cref="Process"/> will always fire first on <see cref="FireOn"/> if assigned a non-<see langword="null"/> value, followed by, if their respective field is <see langword="true"/>, the vendor, the item, and the player, in that order.<br/><br/>
        /// If multiple of these fieldss are set <see langword="true"/>, then each indicated entity will get the opportunity to handle the event unless one of the prior handling entities returns false during their handling.</remarks>
        public bool FireOnItem;

        /// <summary><see cref="FireOnVendor"/>, <see cref="FireOnItem"/> and <see cref="FireOnPlayer"/>, are used to determine which entity the action is supposed to <see cref="IModEventHandler{I_UD_VendorActionEvent{T}}.HandleEvent(I_UD_VendorActionEvent{T})"/> it.</summary>
        /// <remarks><see cref="Process"/> will always fire first on <see cref="FireOn"/> if assigned a non-<see langword="null"/> value, followed by, if their respective field is <see langword="true"/>, the vendor, the item, and the player, in that order.<br/><br/>
        /// If multiple of these fieldss are set <see langword="true"/>, then each indicated entity will get the opportunity to handle the event unless one of the prior handling entities returns false during their handling.</remarks>
        public bool FireOnPlayer;

        /// <summary>An arbitrary entity that the action wants to <see cref="IModEventHandler{I_UD_VendorActionEvent{T}}.HandleEvent(I_UD_VendorActionEvent{T})"/> an <see cref="I_UD_VendorActionEvent{T}"/>. If assigned, this entity is always fired on first.</summary>
        /// <remarks>It needn't otherwise be part of the interaction at all, so steps should be taken to ensure that it's a valid target for handling the event (for example, not a cached object).<br/><br/>
        /// A use case for this is having whoever "owns" the given item handle <see cref="UD_VendorActionEvent"/> by assigning E.Item.InInventory to it during <see cref="UD_GetVendorActionsEvent"/>.</remarks>
        public GameObject FireOn;

        /// <summary>The main method patched to divert to this mod's implementation of vendor actions, <see cref="TradeLine.HandleVendorActions"/>, is <see langword="async"/> and calls an <see langword="await"/> method via <see cref="XRL.UI.Framework.APIDispatch.RunAndWaitAsync"/>.</summary>
        /// <remarks>This is unfamiliar territory for me, so I've tread carefully and tried to mimic it as much as possible. Some of the base game's handling of vendor actions occurs once the <see langword="await"/> has finished, so this field, set to <see langword="true"/>, indicates that the action wants to be processed/handled after the <see langword="await"/> has finished.<br/><br/>
        /// I believe that "doing an <see langword="await"/>" inside an already "doing" <see langword="await"/> is a great way to see your desktop, so I recommend setting this to <see langword="true"/> if your custom action requires handling that, itself, wants to <see langword="await"/>.<br/><br/>
        /// The base game's "Add to trade" action would require this field be set <see langword="true"/>, as it normally happens after <see cref="TradeLine.HandleVendorActions"/> has run.</remarks>
        public bool ProcessAfterAwait;

        /// <summary>Indicates that, for an action that requires a second firing of <see cref="UD_VendorActionEvent"/>, irrespective of the first firing occurring during <see cref="TradeLine_Patches.HandleVendorActions"/>'s <see langword="await"/> or after it, the second one should occur once <see langword="await"/> has finished.</summary>
        /// <remarks>Any action that fires its first time after will always fire its second time after too, if a second firing is flagged for.<br/><br/>
        /// See <see cref="ProcessAfterAwait"/> for more information on <see cref="TradeLine_Patches.HandleVendorActions"/>, its <see langword="await"/>, and how vendor actions interract with it.</remarks>
        public bool ProcessSecondAfterAwait;

        /// <summary>Indicates whether or not the active instance of <see cref="TradeScreen"/> should call <see cref="TradeScreen.ClearAndSetupTradeUI"/> after the action has been processed.</summary>
        /// <remarks>Actions that affect the contents of the trade UI ought to assign this <see langword="true"/>, so that each handling of an action gets a refreshed UI with any changes reflected.<br/><br/>
        /// The base game's "Identify" and "Repair" actions would require this field be set <see langword="true"/>.</remarks>
        public bool ClearAndSetUpTradeUI;

        /// <summary>Indicates whether or not an action should call <see cref="Process"/> twice as part of the action being resolved.</summary>
        /// <remarks>The main purpose of this field is to allow for the closure of the trade window "during" the "handling" of vendor actions, without the UI being manipulated while <see cref="IModEventHandler{UD_VendorActionEvent}.HandleEvent(UD_VendorActionEvent)"/> is resolving.<br/><br/>
        /// Tinkering Bytes requires this field be set <see langword="true"/> for UD_VendorDisassembly:<br/>
        /// The "invoice" for disassembly is calculated and presented to the player during <see cref="TradeLine_Patches.HandleVendorActions"/>'s <see langword="await"/>.<br/>
        /// - If they accept, the trade window is closed and disassembly begins as a "joint" <see cref="OngoingAction"/> between the player and the trader.<br/>
        /// - If they don't (or can't) accept the invoice, then closure of the trade window and the second firing are cancelled.</remarks>
        public bool Staggered;

        /// <summary>If <see cref="Staggered"/>, indicates that the trade window should be closed before <see cref="Process"/> is called the second time.</summary>
        /// <remarks>It is recommended (by LibrarianMage/Books, whose modding advice is invaluable) that the trade UI not be manipulated while any <see cref="MinEvent"/> are being resolved.<br/>
        /// It's my recommendation that this, or <see cref="CloseTradeAfterProcessing"/>, be the only means by which the trade UI is manipulated when dealing with vendor actions.</remarks>
        public bool CloseTradeBeforeProcessingSecond;

        /// <summary>Indicates that the trade window should be closed at the end of a given action being resolved.</summary>
        /// <remarks>If the chosen action has this field set to <see langword="true"/>, the trade window will be closed once the action is processed, whether successfully handled or not.<br/><br/>
        /// It is recommended (by LibrarianMage/Books, whose modding advice is invaluable) that the trade UI not be manipulated while any <see cref="MinEvent"/> are being resolved.<br/>
        /// It's my recommendation that this, or <see cref="CloseTradeAfterProcessing"/>, be the only means by which the trade UI is manipulated when dealing with vendor actions.</remarks>
        public bool CloseTradeAfterProcessing;

        /// <summary>
        /// Contains the logic for determining which entities should have <see cref="UD_VendorActionEvent"/> fired on them, setting up for staggered firing if flagged for, and telling <see cref="TradeLine_Patches.HandleVendorActions"/> whether the trade UI should be closed between instances of action processing or after the action has, overall, been resolved.
        /// </summary>
        /// <param name="TradeLine">The <see cref="TradeLine"/> instance for which the vendor actions are being resolved.</param>
        /// <param name="Vendor">The vendor with whom the player is currently "engaged in trade". This could be a merchant, or a companion, or even a container.</param>
        /// <param name="Item">The item for which the vendor actions are being collected.</param>
        /// <param name="Owner">This is typically the <paramref name="Item"/>'s <see cref="GameObject.InInventory"/>.</param>
        /// <param name="CloseTrade">Indicates whether or not <see cref="TradeScreen.Cancel"/> should be called once control leaves the method (which is after any events have been resolved).</param>
        /// <returns><see langword="true"/> if none of the handling entities returns <see langword="false"/> after their <see cref="IModEventHandler{UD_VendorActionEvent}.HandleEvent(UD_VendorActionEvent)"/>;<br/>
        /// <see langword="false"/> otherwise.</returns>
        public bool Process(TradeLine TradeLine, GameObject Vendor, GameObject Item, GameObject Owner, out bool CloseTrade)
        {
            bool blocked = false;
            CloseTrade = false;
            bool cancelSecond = false;
            if (Staggered || !FirstProcessed)
            {
                Event @event = Event.New("VendorCommandActivating", nameof(Vendor), Vendor, nameof(Item), Item, nameof(Owner), Owner);
                Owner?.FireEvent(@event);
                if (!blocked && GameObject.Validate(ref FireOn))
                {
                    FireOn.FireEvent(@event);
                    blocked = !UD_VendorActionEvent.Check(TradeLine, FireOn, Vendor, Item, Owner, Command, out CloseTrade, out cancelSecond, DramsCost, Staggered, FirstProcessed) || blocked;
                }
                if (!blocked && FireOnVendor)
                {
                    Vendor.FireEvent(@event);
                    blocked = !UD_VendorActionEvent.Check(TradeLine, Vendor, Vendor, Item, Owner, Command, out CloseTrade, out cancelSecond, DramsCost, Staggered, FirstProcessed) || blocked;
                }
                if (!blocked && FireOnItem)
                {
                    Item.FireEvent(@event);
                    blocked = !UD_VendorActionEvent.Check(TradeLine, Item, Vendor, Item, Owner, Command, out CloseTrade, out cancelSecond, DramsCost, Staggered, FirstProcessed) || blocked;
                }
                if (!blocked && FireOnPlayer)
                {
                    The.Player.FireEvent(@event);
                    blocked = !UD_VendorActionEvent.Check(TradeLine, The.Player, Vendor, Item, Owner, Command, out CloseTrade, out cancelSecond, DramsCost, Staggered, FirstProcessed) || blocked;
                }
                if (cancelSecond)
                {
                    Staggered = false;
                }
                if ((Staggered && !FirstProcessed && CloseTradeBeforeProcessingSecond)
                    || ((!Staggered || FirstProcessed) && CloseTradeAfterProcessing))
                {
                    CloseTrade = true;
                }
                FirstProcessed = true;
            }
            return !blocked;
        }

        /// <summary>This method shows a UI element with a processed list of vendor actions that can be selected from for handling by various different involved entities.</summary>
        /// <remarks>The code for this method was taken largely 1:1 from the decompiled base game's <see cref="Qud.API.EquipmentAPI.ShowInventoryActionMenu"/>, altered only slightly to account for differences between the two systems.</remarks>
        /// <param name="ActionTable">An uprocessed dictionary containing <see cref="Name"/> keyed vendor actions.</param>
        /// <param name="Item">The item for which the vendor actions are being listed and picked.</param>
        /// <param name="Intro">A short blurb appearing before the list of vendor actions.</param>
        /// <param name="Comparer">The <see cref="IComparer{UD_VendorAction}"/> by which the final list will be sorted.</param>
        /// <param name="MouseClick">Currently unimplemented. Passed true if the TradeLine showing the menu was clicked.</param>
        /// <returns>The selected <see cref="UD_VendorAction"/> if one was picked;<br/>
        /// <see langword="null"/> otherwise.</returns>
        public static UD_VendorAction ShowVendorActionMenu(
            Dictionary<string, UD_VendorAction> ActionTable, 
            GameObject Item = null, 
            string Intro = null, 
            IComparer<UD_VendorAction> Comparer = null, 
            bool MouseClick = false)
        {
            List<UD_VendorAction> actionsList = new();
            foreach ((string _, UD_VendorAction action) in ActionTable)
            {
                actionsList.Add(action);
            }
            actionsList.Sort(Comparer ??= new UD_VendorAction.Comparer(){ priorityFirst = true });

            Dictionary<char, UD_VendorAction> actionsByHotkey = new(16);

            List<UD_VendorAction> actionsWithoutHotkeys = null;
            var SB = Event.NewStringBuilder();
            foreach (UD_VendorAction vendorAction in actionsList)
            {
                if (vendorAction.Key != ' ' && !ControlManager.isKeyMapped(vendorAction.Key, new List<string> { "UINav", "Menus" }))
                {
                    if (actionsByHotkey.ContainsKey(vendorAction.Key))
                    {
                        actionsWithoutHotkeys ??= new();
                        actionsWithoutHotkeys.Add(vendorAction);
                    }
                    else
                    {
                        actionsByHotkey.Add(vendorAction.Key, vendorAction);
                        vendorAction.Display = ApplyHotkey(vendorAction.Display, vendorAction.Key, vendorAction.PreferToHighlight, ref SB);
                    }
                }
                else
                {
                    vendorAction.Key = ' ';
                }
            }
            if (actionsWithoutHotkeys != null)
            {
                SB ??= Event.NewStringBuilder();
                foreach (UD_VendorAction vendorAction in actionsWithoutHotkeys)
                {
                    char actionHotkey = char.ToUpper(vendorAction.Key);
                    if (actionHotkey != vendorAction.Key && !actionsByHotkey.ContainsKey(actionHotkey))
                    {
                        if (!ControlManager.isKeyMapped(actionHotkey, new List<string> { "UINav", "Menus" }))
                        {
                            vendorAction.Key = actionHotkey;
                            actionsByHotkey.Add(actionHotkey, vendorAction);
                            vendorAction.Display = ApplyHotkey(ColorUtility.StripFormatting(vendorAction.Display), actionHotkey, vendorAction.PreferToHighlight, ref SB);
                        }
                        continue;
                    }
                    string display = vendorAction.Display;
                    display = ColorUtility.StripFormatting(display);
                    bool foundDynamicKey = false;
                    SB.Clear();
                    int i = 0;
                    for (int length = display.Length; i < length; i++)
                    {
                        char dynamicHotkey = display[i];
                        if (!actionsByHotkey.ContainsKey(dynamicHotkey) && !ControlManager.isKeyMapped(dynamicHotkey, new List<string> { "UINav", "Menus" }))
                        {
                            vendorAction.Key = dynamicHotkey;
                            actionsByHotkey.Add(dynamicHotkey, vendorAction);
                            SB.Append("{{hotkey|").Append(dynamicHotkey).Append("}}")
                                .Append(display, i + 1, length - i - 1);
                            foundDynamicKey = true;
                            break;
                        }
                        SB.Append(dynamicHotkey);
                    }
                    if (!foundDynamicKey)
                    {
                        vendorAction.Key = ' ';
                    }
                    vendorAction.Display = SB.ToString();
                }
                actionsList.Sort(Comparer);
            }
            List<string> options = new();
            List<char> hotkeys = new();
            foreach (UD_VendorAction action in actionsList)
            {
                options.Add(action.Display);
                hotkeys.Add(action.Key);
            }

            int defaultSelected = 0;
            int currentDefault = int.MinValue;
            for (int i = 0; i < actionsList.Count; i++)
            {
                if (actionsList[i].Default > currentDefault)
                {
                    defaultSelected = i;
                    currentDefault = actionsList[i].Default;
                }
            }
            bool isConfused = The.Player.IsConfused;
            Location2D popupLocation = null;
            if (MouseClick)
            {
                // Would love to figure this out.
                // popupLocation = PopupMessage.LOCATION_AT_MOUSE_CURSOR;
            }
            string itemBaseDisplayName = Item?.Render?.DisplayName ?? Item.Blueprint;
            int pickedEntry = Popup.PickOption(
                Title: isConfused ? null : GetDisplayNameEvent.GetFor(Item, itemBaseDisplayName, Context: nameof(ShowVendorActionMenu)),
                Intro: Intro ?? (isConfused ? GetDisplayNameEvent.GetFor(Item, itemBaseDisplayName, Context: nameof(ShowVendorActionMenu)) : null),
                Options: options.ToArray(),
                Hotkeys: hotkeys.ToArray(),
                Context: isConfused ? null : Item,
                IntroIcon: isConfused ? null : Item.RenderForUI(),
                DefaultSelected: defaultSelected,
                AllowEscape: true,
                RespectOptionNewlines: true,
                CenterIntro: true,
                PopupLocation: popupLocation,
                PopupID: "VendorActionMenu:" + (Item?.IDIfAssigned ?? "(noid)"));

            if (pickedEntry < 0)
            {
                return null;
            }
            return actionsList[pickedEntry];
        }

        private static string ApplyHotkey(string Display, char Key, string Prefer, ref StringBuilder SB)
        {
            SB ??= Event.NewStringBuilder();
            SB.Clear();
            if (!Display.Contains("{{") && !Display.Contains("&"))
            {
                char hotkey = char.ToLower(Key);
                int preferHotkeyIndex = -1;
                int preferKeyIndex = -1;
                if (!string.IsNullOrEmpty(Prefer))
                {
                    preferKeyIndex = Display.IndexOf(Prefer);
                    if (preferKeyIndex != -1)
                    {
                        preferHotkeyIndex = Prefer.IndexOf(Key);
                        if (preferHotkeyIndex == -1 && hotkey != Key)
                        {
                            preferHotkeyIndex = Prefer.IndexOf(hotkey);
                        }
                    }
                }
                int hotkeyIndex = Display.IndexOf(Key);
                if (preferHotkeyIndex != -1)
                {
                    hotkeyIndex = preferHotkeyIndex + preferKeyIndex;
                }
                if (hotkeyIndex != -1)
                {
                    Display = SB.Append(Display, 0, hotkeyIndex).Append("{{hotkey|").Append(Key)
                        .Append("}}")
                        .Append(Display, hotkeyIndex + 1, Display.Length - hotkeyIndex - 1)
                        .ToString();
                }
            }
            return Display;
        }

        public static int SortCompare(UD_VendorAction a, UD_VendorAction b)
        {
            bool aKeyIsBlank = a.Key == ' ';
            bool bKeyIsBlank = b.Key == ' ';
            if (!aKeyIsBlank && !bKeyIsBlank)
            {
                int keyComparison = char.ToUpper(a.Key).CompareTo(char.ToUpper(b.Key));
                if (keyComparison != 0)
                {
                    return keyComparison;
                }
                if (a.Key != b.Key)
                {
                    return -a.Key.CompareTo(b.Key);
                }
                int defaultComparison = a.Default.CompareTo(b.Default);
                if (defaultComparison != 0)
                {
                    return -defaultComparison;
                }
            }
            else if (aKeyIsBlank || bKeyIsBlank)
            {
                return aKeyIsBlank.CompareTo(bKeyIsBlank);
            }
            return PriorityCompare(a, b);
        }

        public static int PriorityCompare(UD_VendorAction a, UD_VendorAction b)
        {
            int priorityComparison = a.Priority.CompareTo(b.Priority);
            if (priorityComparison != 0)
            {
                return -priorityComparison;
            }
            return -a.Display.CompareTo(b.Display);
        }

        /// <summary>Checks the passed <paramref name="Item"/> for being "display-only".</summary>
        /// <remarks>More information about "display-only" items can found on <see href="https://github.com/UnderDoug/UD_Vendor_Actions/wiki/Display‐only-items">this mod's GitHub</see></remarks>
        /// <param name="Item">The item being checked.</param>
        /// <returns><see langword="true"/> if the object is only for display;<br/>
        /// <see langword="false"/> otherwise.</returns>
        public static bool ItemIsTradeUIDisplayOnly(GameObject Item)
        {
            if (Item == null)
            {
                return false;
            }
            return Item.GetBlueprint().InheritsFrom("UD_TradeUI_DisplayItem")
                || Item.GetStringProperty("TradeUI_DisplayOnly", "No").EqualsNoCase("Yes")
                || Item.GetIntProperty("TradeUI_DisplayOnly", 0) > 0
                || (!Item.HasStringProperty("TradeUI_DisplayOnly") && !Item.HasIntProperty("TradeUI_DisplayOnly")
                    && Item.HasTag("TradeUI_DisplayOnly"));
        }
    }
}
