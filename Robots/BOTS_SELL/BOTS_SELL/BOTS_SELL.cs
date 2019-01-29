using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class BOTS_SELL : Robot
    {
        #region parameters
        [Parameter("% Acct Risk per Trade", DefaultValue = 0.5)]
        public double pPercAcctRisk { get; set; }

        [Parameter("Initial Stop Loss ATR Multipler", DefaultValue = 1.5)]
        public double pInitialSLATRMultipler { get; set; }

        [Parameter("Minimum Stop Loss (pips)", DefaultValue = 5)]
        public double pMinimumSLPips { get; set; }

        [Parameter("Scale Out Take Profit ATR Multiplier", DefaultValue = 1.5)]
        public double pScaleOutATRMultipler { get; set; }

        [Parameter("Minimum Scale Out Take Profit (pips)", DefaultValue = 5)]
        public double pMinimumScaleOutTPPips { get; set; }

        [Parameter("Start Trailing Stop ATR Multiplier", DefaultValue = 3)]
        public double pStartTrailingATRMultipler { get; set; }

        [Parameter("Distance to Trail ATR Multipler", DefaultValue = 1.5)]
        public double pTrailingDistanceATRMultipler { get; set; }

        #endregion

        #region private fields
// EDIT FOR OTHER DIRECTION    Buy or Sell
        private TradeType vDirection = TradeType.Sell;

        private string vLabel;

        private double vInitialSLPips;
        private double vScaleOutTPPips;
        private double vScaleOutTPPrice;
        private double vStartTrailingPips;
        private double vStartTrailingPrice;
        private double vTrailingDistancePips;
        private long vVolume;
        private Position vPos;
        private bool vAlreadyScaledOut = false;
        private bool vClosedByExitIndicator = false;

        #endregion

        #region indicators

        // money management indictator
        private AverageTrueRange i_atr;

        // exit indicator
        private HeikenAshiDirectionExitIndicator i_ha;

        #endregion

        #region cTrader events

        protected override void OnStart()
        {

            if (vDirection == TradeType.Buy)
            {
                vLabel = "BOTS_Buy_" + Symbol.Code;
            }
            else
            {
                vLabel = "BOTS_Sell_" + Symbol.Code;
            }

            // Instantiate Indicators
            i_atr = Indicators.AverageTrueRange(MarketSeries, 14, MovingAverageType.Exponential);
            i_ha = Indicators.GetIndicator<HeikenAshiDirectionExitIndicator>(TimeFrame);

            Positions.Opened += PositionsOnOpened;
            Positions.Closed += PositionsOnClosed;

            CalculateTradeParameters();

            // check if a position is already open
            var posList = Positions.FindAll(vLabel);

            if (posList.Length == 0)
            {
                var vResult = ExecuteMarketOrder(vDirection, Symbol, vVolume, vLabel);
                vPos = vResult.Position;
                vPos.ModifyStopLossPips(vInitialSLPips);
            }
            else
            {
                vPos = posList[0];

                if (vDirection == TradeType.Buy && vPos.StopLoss > vPos.EntryPrice || vDirection == TradeType.Sell && vPos.StopLoss < vPos.EntryPrice)
                {
                    vAlreadyScaledOut = true;
                }
                // hasn't scaled out yet
                else
                {
                    vScaleOutTPPrice = Convert.ToDouble(vPos.TakeProfit);
                    double? d = null;
                    vPos.ModifyTakeProfitPrice(d);
                }
            }

            if (vDirection == TradeType.Buy)
            {
                vScaleOutTPPrice = vPos.EntryPrice + (vScaleOutTPPips / 10000);
                vStartTrailingPrice = vPos.EntryPrice + (vStartTrailingPips / 10000);
            }
            else
            {
                vScaleOutTPPrice = vPos.EntryPrice - (vScaleOutTPPips / 10000);
                vStartTrailingPrice = vPos.EntryPrice - (vStartTrailingPips / 10000);
            }

            if (vDirection == TradeType.Buy && vStartTrailingPrice < vScaleOutTPPrice || vDirection == TradeType.Sell && vStartTrailingPrice > vScaleOutTPPrice)

                vStartTrailingPrice = vScaleOutTPPrice;

        }

        protected override void OnTick()
        {
            // if this still needs to be scaled out
            if (!vAlreadyScaledOut)
            {
                if (vDirection == TradeType.Buy && Symbol.Bid > vScaleOutTPPrice || vDirection == TradeType.Sell && Symbol.Ask < vScaleOutTPPrice)
                {
                    var vNewVolume = vPos.VolumeInUnits / 2;
                    vNewVolume = vNewVolume - (vNewVolume % 1000);

                    if (vNewVolume > 0)
                    {
                        vPos.ModifyVolume(vNewVolume);
                        vPos.ModifyStopLossPips(-1);

                        Print(vLabel + " SCALED OUT at : " + Symbol.Bid);
                    }
                    else
                    {
                        vPos.Close();
                    }

                    vAlreadyScaledOut = true;
                }
            }
            // has already, or doesn't need scaling out, but does need to start trailing
            else if (!vPos.HasTrailingStop)
            {

// EDIT FOR OTHER DIRECTION     Bid >  for buy  Ask < for sell

                if (vDirection == TradeType.Buy && Symbol.Bid > vStartTrailingPrice || vDirection == TradeType.Sell && Symbol.Ask < vStartTrailingPrice)
                {
                    vPos.ModifyStopLossPips(vTrailingDistancePips - vStartTrailingPips);
                    vPos.ModifyTrailingStop(true);
                }
            }
        }
        // otherwise is already trailing nothing more to do


        protected override void OnBar()
        {
            Print(vLabel + " On Bar HA Result : " + i_ha.haDirection.Last(1));

            //if (vDirection == TradeType.Buy && i_ha.haDirection.LastValue == -1 || vDirection == TradeType.Sell && i_ha.haDirection.LastValue == 1)
            if (vAlreadyScaledOut && (vDirection == TradeType.Buy && i_ha.haDirection.Last(1) == -1 || vDirection == TradeType.Sell && i_ha.haDirection.Last(1) == 1))
            {
                vPos.Close();
                vClosedByExitIndicator = true;
            }
        }

        protected override void OnStop()
        {
            // Put your deinitialization logic here


            // only executed at end of backtest run
            // closes any open trades at end of backtest
            if (IsBacktesting)
            {
                foreach (var pos in Positions)
                    pos.Close();
            }
            else
            {
                if (!vAlreadyScaledOut)
                {
                    vPos.ModifyTakeProfitPrice(vScaleOutTPPrice);
                }
            }

        }


        #endregion

        #region event delegate methods

        private void PositionsOnOpened(PositionOpenedEventArgs args)
        {
            Print(vLabel + " Position opened {0}", args.Position.Label);
            vPos = args.Position;
        }

        private void PositionsOnClosed(PositionClosedEventArgs args)
        {
            if (args.Position.Label == vLabel)
            {
                Print(vLabel + " Position closed {0}", args.Position.Label);

                if (vClosedByExitIndicator)
                {
                    Print(vLabel + " Postion Closed From Exit Indicator");
                }
                else
                {
                    Print(vLabel + " Postion Closed From Stop Loss");
                }
                Stop();
            }
        }

        #endregion

        #region market interaction methods


        #endregion

        #region utility methods

        private void CalculateTradeParameters()
        {


            var maxRiskAmount = Account.Balance * (pPercAcctRisk / 100);
            var atr = Math.Round((i_atr.Result.LastValue * 10000), 0);
            vInitialSLPips = Convert.ToInt64(atr * pInitialSLATRMultipler);
            vScaleOutTPPips = Convert.ToUInt64(atr * pScaleOutATRMultipler);

            if (vScaleOutTPPips < pMinimumScaleOutTPPips)
                vScaleOutTPPips = pMinimumScaleOutTPPips;

            if (vInitialSLPips < pMinimumSLPips)
                vInitialSLPips = pMinimumSLPips;

            var maxRiskPerPip = maxRiskAmount / vInitialSLPips;
            vVolume = Convert.ToInt64(maxRiskPerPip / Symbol.PipValue);
            vVolume = vVolume - (vVolume % 1000);

            if (vVolume < 1000)
                vVolume = 1000;

            vStartTrailingPips = pStartTrailingATRMultipler * atr;

            vTrailingDistancePips = pTrailingDistanceATRMultipler * atr;

            if (vTrailingDistancePips < pMinimumSLPips)
                vTrailingDistancePips = pMinimumSLPips;

            Chart.DrawStaticText("cMaxRisk", (" Max Risk: $ " + Math.Round(maxRiskAmount, 2)), VerticalAlignment.Top, HorizontalAlignment.Left, "Yellow");
            Chart.DrawStaticText("cATR", ("\n ATR (pips) " + atr), VerticalAlignment.Top, HorizontalAlignment.Left, "Yellow");
            Chart.DrawStaticText("cInitalSL", ("\n\n Initial SL (pips) " + vInitialSLPips), VerticalAlignment.Top, HorizontalAlignment.Left, "Yellow");
            Chart.DrawStaticText("cScaleOutSL", ("\n\n\n Scale Out TP (pips) " + vScaleOutTPPips), VerticalAlignment.Top, HorizontalAlignment.Left, "Yellow");
            Chart.DrawStaticText("cStartTrailingAt", ("\n\n\n\n Start Trailing At (pips) " + vStartTrailingPips), VerticalAlignment.Top, HorizontalAlignment.Left, "Yellow");
            Chart.DrawStaticText("cTrailDistance", ("\n\n\n\n\n Trailing Distance (pips) " + vTrailingDistancePips), VerticalAlignment.Top, HorizontalAlignment.Left, "Yellow");

        }

        #endregion
    }
}
