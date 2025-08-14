This is a mod library. Its single purpose is to provide events for other mods to Want and Handle in service of extending the actions that vendors (via the trade window) can perform for or on behalf of the player.

The events added by this mod roughly mirror those found in the base game for `InventoryActions` and operate in a similar way. These events are fired and acted on inside a Harmony patch of `TradeLine.HandleVendorActions` and preventing the base method from running.

Like `InventoryActions`, `VendorActions` are collected and displayed in a list when the player interacts with items in the trade window. Once an action is selected, it's assessed inside `TradeLine.HandleVendorActions` and then processed according to a couple of factors.

A processed `VendorAction` sends its own event, `VendorActionEvent`, with a stored command, which can be handled by the Vendor, the Player, or the Item for whom the actions were collected.

## Events

### GetVendorActionsEvent
Does nearly exactly what `GetInventoryActionsEvent` does, but its `AddAction` method varies slightly.

The object that the event is fired on can be specified with a set of flags, as well as explicitly passed as an argument. The explicitly passed object will get the first opportunity to handle the event and doesn't even need to be otherwise involved in the interaction.

The initial call to `TradeLine.HandleVendorActions` utilises async, but not all actions (such as "add an item to trade") occur while waiting, insteading utilising async themselves. The flag `WantsAsync` can be used for any `VendorActions` that themselves need to happen _after_ the wait or make use of asych.

Some actions want `ClearAndSetUpTradeUI` called after they run. The eponimous flag can be used to have the patch run that function once it's been processed. 

### VendorActionEvent
Again, does nearly exactly what `InventoryActionEvent` does, just tweaked to handle an additional participant.

### AfterVendorActionEvent
Functionally identical to `AfterInventoryActionEvent`.

### OwnerAfterVendorActionEvent
This event differs from the base game in that the owner of the item isn't always the player. This event is fired on whichever is the owner of the object as determined by whether it's in the vendor's inventory or not.

## Other Classes

### IVendorActionEventHandler
Interface that can be implemented to contract a class as a handler for all the above events. It serves the same purpose as implementing `IModEventHandler<T>` for each of the events.

### UD_VendorActionHandler
Attached to the base Creature object, this `IPart` Wants and Handles `GetVendorActionsEvent` and `VendorActionEvent` to achieve the same behavior as the patched `TradeLine.HandleVendorActions`.

While this part is fairly simple in its execution, simply calling the methods that would have been called, it serves as a fairly good example of what it looks like to Handle the two events.

For a more in-depth look at some of the possibilities, check out the mod that necessitated this one: [Tinkering Bytes](https://github.com/UnderDoug/UD_Tinkering_Bytes). Tinkering Bytes adds vendor tinkering, disassembly, _and_ reverse engineering.