using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class EmptySampleBot : Robot
    {
        #region parameters

        [Parameter("DebugMode", DefaultValue = false)]
        public bool pDebug { get; set; }

        [Parameter(DefaultValue = 0.0)]
        public double Parameter { get; set; }

        [Parameter()]
        public DataSeries Source { get; set; }

        #endregion

        #region private fields



        #endregion

        #region indicators

        // money management indictator
        private AverageTrueRange i_atr;

        // entry confirmation indicator


        // volume indicator
        private OnBalanceVolume i_obv;

        // other filter indicator 1


        // other filter indicator 2


        // exit indicator

/*
            private HeikenAshiDirection i_ha;
            */


        #endregion

        #region cTrader events

                protected override void OnStart()
        {
            // Instantiate Indicators
            i_atr = Indicators.AverageTrueRange(14, MovingAverageType.Exponential);
            i_obv = Indicators.OnBalanceVolume(Source);
            //i_ha = Indicators.GetIndicator<HeikenAshiDirection>();


            Positions.Opened += PositionsOnOpened;
            Positions.Closed += PositionsOnClosed;

        }

        protected override void OnTick()
        {
            // Put your per tick logic here
            DebugOutput("Tick Logic Empty");
        }

        protected override void OnBar()
        {
            // Put your per bar logic here
            DebugOutput("Bar Logic Empty");
        }

        protected override void OnStop()
        {
            // Put your deinitialization logic here
            DebugOutput("OnStop Logic Empty");


            // only executed at end of backtest run
            // closes any open trades at end of backtest
            if (IsBacktesting)
            {
                foreach (var pos in Positions)
                    pos.Close();
            }

        }

        #endregion

        #region event delegate methods

        private void PositionsOnOpened(PositionOpenedEventArgs args)
        {
            Print("Position opened {0}", args.Position.Label);
        }

        private void PositionsOnClosed(PositionClosedEventArgs args)
        {
            Print("Position closed {0}", args.Position.Label);
        }

        #endregion

        #region market interaction methods

        private void OpenPosition(TradeType pDirection, Symbol pSymbol, long pVolume, string pLabel)
        {
            ExecuteMarketOrder(pDirection, pSymbol, pVolume, pLabel);
            DebugOutput("OpenPosition : Method Executed");
        }

        #endregion

        #region utility methods

        private void DebugOutput(string pOutput)
        {
            if (pDebug)
            {
                Print(pOutput);
            }
        }

        #endregion
    }
}
