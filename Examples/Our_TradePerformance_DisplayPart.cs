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
                && E.Context == nameof(TradeLine)) // This context is provided by a patch to TradeLine in UD_Modding_Toolbox.
            {
                double performance = GetTradePerformanceEvent.GetFor(The.Player, vendor);
                string membershipRating = Math.Round(performance * 100, 0).ToString().Color("W");
                E.AddTag($"- {membershipRating}".Color("y"));
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

                /*
                From Faction Rep
                Faction ifExists = XRL.World.Factions.GetIfExists(E.Trader.GetPrimaryFaction());
		        if (ifExists != null)
		        {
			        E.LinearAdjustment += The.Game.PlayerReputation.GetTradePerformance(ifExists);
		        }

                From Ego:
                if (flag && Actor.WantEvent(PooledEvent<GetTradePerformanceEvent>.ID, CascadeLevel))
	            {
		            GetTradePerformanceEvent getTradePerformanceEvent = FromPool(Actor, Trader, num, num2, num3);
		            if (!Actor.HandleEvent(getTradePerformanceEvent))
		            {
			            flag = false;
		            }
		            num2 = getTradePerformanceEvent.LinearAdjustment;
		            num3 = getTradePerformanceEvent.FactorAdjustment;
	            }
                */

                if (vendor.IsPlayerLed()) // Special extra info if the trader in a companion of the player.
                {
                    E.Postfix.AppendLine().AppendLine()
                        .Append("We're {{M|Besties!}}");
                }
            }
            return base.HandleEvent(E);
        }
    }
}