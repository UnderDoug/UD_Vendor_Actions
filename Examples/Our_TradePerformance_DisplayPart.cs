using System;
using Qud.UI;

namespace XRL.World.Parts
{
    [Serializable]
    public class Our_TradePerformance_DisplayPart : IPart
    {
        // This will make it obvious if we've accidentally created too many or if they're not being obliterated when they're supposed to.
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
                && The.Player is GameObject player)
            {
                // Do code...
                // Your Bep Loyalty Card - Rating: 35
                
                E.AddAdjective(GameText.VariableReplace("=object.T's= {{Y|=subject.name=}}", vendor, player));

                if (E.Context == nameof(TradeLine)) // This context is provided by a patch to TradeLine in UD_Modding_Toolbox.
                {
                    // This will apear in the item's trade line display name, but nowhere else.
                    double performance = GetTradePerformanceEvent.GetFor(The.Player, vendor);
                    string membershipRating = Math.Round(performance * 100, 0).ToString().Color("W");
                    E.AddTag($"- Rating: {membershipRating}".Color("y"));
                }
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(GetShortDescriptionEvent E)
        {
            if (ParentObject.InInventory is GameObject vendor
                && The.Player is GameObject player)
            {
                // Do code...
                // This is a lot of work, ultimately, but hopefully not too hard to follow.
                double performance = GetTradePerformanceEvent.GetFor(The.Player, vendor);
                string membershipRating = Math.Round(performance * 100, 0).ToString().Color("W");
                string membershipMessage = "=object.Name's= {{Y|A+}}{{W|+}}{{M|+}} {{Y|VIP}} {{C|Member Status}} with =subject.Name= is " + membershipRating + "!";
                E.Postfix.AppendLine()
                    .Append(GameText.VariableReplace(membershipMessage, vendor, player));

                // We're getting the bonus from the player's faction rep.
                double baseFactionPerformance = 0;
                double factionPerformance = 0;
                string factionRepMessage = "{{K|\u2022}} {{W|Faction}} rep: "; // \u2022 is •
                Faction vendorFaction = Factions.GetIfExists(vendor.GetPrimaryFaction());
		        if (vendorFaction != null)
		        {
                    baseFactionPerformance = The.Game.PlayerReputation.GetTradePerformance(vendorFaction);
                    factionPerformance = baseFactionPerformance;
                    factionRepMessage = "{{K|\u2022}} Rep with {{W|" + vendorFaction.DisplayName + "}}: "; // \u2022 is •
                }
                factionPerformance = Math.Round(factionPerformance * 0.07 * 100, 0);
                factionRepMessage += factionPerformance ;

                // We're extracting the bonus the player gets from ego (snake oiler increases this by 2, as though ego were 4 higher.
                double egoPerformance = player.StatMod("Ego");
                string egoModMessage = "{{K|\u2022}} {{M|Ego}}: ";
                if (player.WantEvent(GetTradePerformanceEvent.ID, GetTradePerformanceEvent.CascadeLevel))
	            {
		            GetTradePerformanceEvent e = GetTradePerformanceEvent.FromPool(player, vendor, (int)egoPerformance, 0, 1);
		            if (player.HandleEvent(e))
		            {
                        egoPerformance += e.LinearAdjustment - baseFactionPerformance;
                    }
                }
                egoPerformance = Math.Round(egoPerformance * 0.07 * 100, 0);
                egoModMessage += egoPerformance;

                E.Postfix.AppendLine().AppendLine()
                    .Append("Rating includes bonuses:");

                E.Postfix.AppendLine()
                    .Append(egoModMessage);

                E.Postfix.AppendLine()
                    .Append(factionRepMessage);

                // Special extra info if the trader is a companion of the player.
                if (vendor.IsPlayerLed())
                {
                    E.Postfix.AppendLine().AppendLine()
                        .Append("We're {{M|Besties}}!");
                }
            }
            return base.HandleEvent(E);
        }
    }
}