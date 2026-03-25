using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using XRL;
using XRL.World;
using XRL.World.Parts;
using XRL.World.Tinkering;

using UD_Modding_Toolbox;

namespace UD_Vendor_Actions
{
    public static class Extensions
    {
        public static bool CanAfford(this GameObject Shopper, int DramsCost)
        {
            return !(Shopper.GetFreeDrams() < DramsCost);
        }
        public static bool CanAfford(this GameObject Shopper, double DramsCost)
        {
            return CanAfford(Shopper, (int)Math.Ceiling(DramsCost));
        }
    }
}
