using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;
 
namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class Spread : Robot
    {
 
 
        int pos = 0;
        int neg = 0;
 
        protected override void OnTick()
        {
            if (Symbol.Spread > 0)
            {
                Print("Spread = " + Math.Round((Symbol.Spread / Symbol.PipSize), 2));
                Print(Symbol.Bid + " " + Symbol.Ask);
                //Print("Spread = " + Symbol.Spread);
                //Print("Pipsize = " + Symbol.PipSize);
                pos++;
            }
            if (Symbol.Spread < 0)
            {
                Print("Spread = " + Math.Round((Symbol.Spread / Symbol.PipSize), 2));
                //Print("Spread = " + Symbol.Spread);
                //Print("Pipsize = " + Symbol.PipSize);
                neg++;
            }
        }
 
        protected override void OnStop()
        {
            Print("Pos: " + pos + ", Neg: " + neg);
        }
    }
}