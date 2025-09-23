using System.Collections.Generic;
using Qud.UI;

using XRL;
using XRL.World;
using XRL.World.Parts;

using UD_Modding_Toolbox;

using UD_Vendor_Actions.Harmony;

namespace UD_Vendor_Actions
{
    /// <summary>
    /// This class is analogous to the base game's <see cref="GetInventoryActionsEvent"/> and serves nearly the exact same function, extended to traders (vendors).</summary>
    /// <remarks>Just like <see cref="GetInventoryActionsEvent"/> for inventory actions, this class is responsible for assigning values to any new <see cref="UD_VendorAction"/> entries being collected, and providing a somewhat curated list of them to <see cref="UD_VendorAction.ShowVendorActionMenu"/>.</remarks>
    [GameEvent(Cascade = CASCADE_NONE, Cache = Cache.Pool)]
    public class UD_GetVendorActionsEvent : I_UD_VendorActionEvent<UD_GetVendorActionsEvent>
    {
        /// <summary>The <see cref="UD_VendorAction.Name"/> keyed actions table into which vendor actions are collected and curated before being supplied to <see cref="UD_VendorAction.ShowVendorActionMenu"/>.</summary>
        public Dictionary<string, UD_VendorAction> Actions;

        /// <summary>Primarily used by <see cref="UD_VendorActionHandler"/> to include or exclude the "Add to trade" vendor action.</summary>
        /// <remarks>This field is set <see langword="true"/> when <see cref="Send"/> is called by the patched <see cref="TradeLine.HandleVendorActions"/> to mirror the base game's implementation doing so.<br/><br/>
        /// While currently redundant, its inclusion is to ensure it's easy to implement in the event that the base game's handling of vendor actions implements it.</remarks>
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

        /// <summary>
        /// Contains the logic for determining which sources of vendor actions should be given the opportunity to provide them, and calls <see cref="IModEventHandler{I_UD_VendorActionEvent{T}}.HandleEvent(I_UD_VendorActionEvent{T})"/> on those sources when applicable.
        /// </summary>
        /// <param name="TradeLine">The <see cref="TradeLine"/> instance from which this method was called.</param>
        /// <param name="Vendor">The vendor from whom vendor actions should be collected, if available.</param>
        /// <param name="Item">The item from which vendor actions should be collected, if available.</param>
        /// <param name="Actions">A pre-initialized actions table into which vendor actions can be collected.</param>
        /// <param name="IncludeModernTradeOptions">This is an ultimately unused parameter (always passed <see langword="true"/>).<br/><remarks>It is the condition by which the "Add to trade" vendor action is included, both base game and by this mod.</remarks></param>
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
            if (E.Actions.IsNullOrEmpty())
            {
                MetricsManager.LogPotentialModError(Utils.ThisMod, 
                    $"{nameof(UD_GetVendorActionsEvent)}.{nameof(Send)}(" +
                    $"{nameof(Vendor)}: {Vendor?.DebugName ?? Const.NULL}, " +
                    $"{nameof(Item)}: {Item?.DebugName ?? Const.NULL})" +
                    $"{nameof(Actions)} returned empty when \"Look\" should always be an option.\n" +
                    $"----{nameof(Vendor)}:\n" +
                    $"{Vendor?.GetBlueprint()?.BlueprintXML() ?? $"Failed to get {nameof(GameObjectBlueprint.BlueprintXML)}"}" +
                    $"----{nameof(Item)}:\n" +
                    $"{Item?.GetBlueprint()?.BlueprintXML() ?? $"Failed to get {nameof(GameObjectBlueprint.BlueprintXML)}"}");
                MetricsManager.LogModInfo(Utils.ThisMod, $"Please report the above error to {Utils.ThisMod?.Manifest?.Author?.Strip()} on the steam workshop.");
            }
        }

        /// <summary>
        /// The primary means by which a <see cref="UD_VendorAction"/> should be added to <see cref="Actions"/> that <see cref="UD_GetVendorActionsEvent"/> is designed to accumulate. It has some boilerplate code that ensures an action is configured correctly if certain arguments passed to it would conflict.
        /// </summary>
        /// <param name="Name">The name of the action, and the Key for the action's entry in <see cref="Actions"/>.</param>
        /// <param name="Display">How the action will be displayed in the resultant menu, with some caveats.</param>
        /// <param name="Command">The command sent in conjunction with an <see cref="I_UD_VendorActionEvent{T}"/> for handling parts to check that they are responding to the correct action.</param>
        /// <param name="PreferToHighlight">A substring of <see cref="UD_VendorAction.Display"/> which can be optionally specified, and the resultant menu will try first to find an appropriate character to replace with the <see cref="UD_VendorAction.Key"/>.</param>
        /// <param name="Key">The hotkey for the action in the resultant menu. Where possible, it'll be highlighted in the <see cref="UD_VendorAction.Display"/> string; otherwise, it will be to the left in typical "[h] hotkey" style.</param>
        /// <param name="Default">Used to determine which is the "selected by default" option in the resultant menu. Higher numbers supersede.</param>
        /// <param name="Priority">How high up the list an action should be, where higher numbers indicate a higher position in the list</param>
        /// <param name="DramsCost">A "simple" way of defining a drams cost that can be retrieved when an <see cref="I_UD_VendorActionEvent{T}"/> is fired.</param>
        /// <param name="FireOnVendor">Indicates whether to send the vendor action to the <see cref="I_UD_VendorActionEvent{T}.Vendor"/> for handling.</param>
        /// <param name="FireOnItem">Indicates whether to send the vendor action to the <see cref="I_UD_VendorActionEvent{T}.Item"/> for handling.</param>
        /// <param name="FireOnPlayer">Indicates whether to send the vendor action to the <see cref="The.Player"/> for handling.</param>
        /// <param name="FireOn">An arbitrary object to send the vendor action to for handling before any indicated objects get the opportunity to handle it.</param>
        /// <param name="Override">Indicates whether this vendor action should replace one of the same <paramref name="Name"/> if found in <see cref="Actions"/>.</param>
        /// <param name="ProcessAfterAwait">Indicates if the action should be processed once control has left <see cref="TradeLine_Patches.HandleVendorActions"/>'s <see langword="await"/> statement.</param>
        /// <param name="ProcessSecondAfterAwait">Indicates if the action's second processing, if <paramref name="Staggered"/>, should be processed once control has left <see cref="TradeLine_Patches.HandleVendorActions"/>'s <see langword="await"/> statement.</param>
        /// <param name="ClearAndSetUpTradeUI">Indicates whether or not the active instance of <see cref="TradeScreen"/> should call <see cref="TradeScreen.ClearAndSetupTradeUI"/> after the action has been processed.</param>
        /// <param name="Staggered">Indicates whether or not an action should call <see cref="UD_VendorAction.Process"/> twice as part of the action being resolved.</param>
        /// <param name="CloseTradeBeforeProcessingSecond">If <paramref name="Staggered"/>, indicates that the trade window should be closed before <see cref="UD_VendorAction.Process"/> is called the second time. See <see cref="UD_VendorAction.Staggered"/> for important information.</param>
        /// <param name="CloseTradeAfterProcessing">Indicates that the trade window should be closed at the end of <see cref="UD_VendorActionEvent"/> a given action being resolved. See <see cref="UD_VendorAction.Staggered"/> for important information.</param>
        /// <returns><see langword="true"/> if the action was added to <see cref="Actions"/>;<br/>
        /// <see langword="false"/> otherwise.</returns>
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
