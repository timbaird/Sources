using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class Test : Robot
    {


        protected override void OnStart()
        {

            Positions.Opened += PositionsOnOpened;
            Positions.Closed += PositionsOnClosed;

            Print(1 / Symbol.PipValue);

        }

        protected override void OnTick()
        {

        }

        protected override void OnBar()
        {

        }

        protected override void OnStop()
        {

        }

        private void PositionsOnOpened(PositionOpenedEventArgs args)
        {

        }

        private void PositionsOnClosed(PositionClosedEventArgs args)
        {
            Print("Position closed {0}", args.Position.Label);
        }

        private void OpenPosition(TradeType pDirection, Symbol pSymbol, long pVolume, string pLabel)
        {
            ExecuteMarketOrder(pDirection, pSymbol, pVolume, pLabel);
        }
    }
}
