using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class HATrendTrader : Robot
    {
        int tradeVolume = 1000;
        string tradeLabel = "HATrendTrader: ";
    
        [Parameter("Stop Loss (pips)", DefaultValue = 50)]
        public int stopLossPips { get; set; }
    
        #region private fields
        private HeikenAshiDirection ha;
        
        private bool positionOpened = false;
        private int positionDirection = 0;
        
        #endregion

        #region cTrader events

        protected override void OnStart()
        {
            Positions.Opened += PositionsOnOpened;
            Positions.Closed += PositionsOnClosed;

            ha = Indicators.GetIndicator<HeikenAshiDirection>();
        }

        protected override void OnTick()
        {

        }

        protected override void OnBar()
        {
            //if (Trade.IsExecuting)
            //    return;
        
            if (ha.haDirection.Last(0) == 1 && positionDirection != 1)
            {
                foreach(var pos in Positions)
                    pos.Close();
                   
                //Print("Buy");                   
                ExecuteMarketOrder(TradeType.Buy, Symbol, tradeVolume, tradeLabel + Symbol);
            
            }
            else if (ha.haDirection.Last(0) == -1 && positionDirection != -1)
            {
                foreach(var pos in Positions)
                    pos.Close();
                    
                //Print("Sell");  
                ExecuteMarketOrder(TradeType.Sell, Symbol, tradeVolume, tradeLabel + Symbol);
            }
        }

        protected override void OnStop()
        {

        }

        #endregion

        #region event delegate methods

        private void PositionsOnOpened(PositionOpenedEventArgs args)
        {
            Print("Position opened {0}, {1}", args.Position.Label, args.Position.TradeType);
            positionOpened = true;
            
            args.Position.ModifyStopLossPips(stopLossPips);
            args.Position.ModifyTrailingStop(true);
            
            if (args.Position.TradeType == TradeType.Buy)
            {
                positionDirection = 1;
            }
            else
            {
                positionDirection = -1;
            }
        }

        private void PositionsOnClosed(PositionClosedEventArgs args)
        {
            Print("Position closed {0}, {1}", args.Position.Label, args.Position.TradeType);
            positionOpened = false;
            positionDirection = 0;
        }

        #endregion
    }
}
