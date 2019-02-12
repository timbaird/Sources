using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class BOTS_SELL_PIPS : Robot
    {
        #region parameters
        [Parameter("% Acct Risk per Trade", DefaultValue = 2)]
        public double pPercAcctRisk { get; set; }

        [Parameter("Initial Stop Loss Pips", DefaultValue = 10)]
        public double pInitialSLPips { get; set; }

        [Parameter("Scale Out Take Profit Pips", DefaultValue = 10)]
        public double pScaleOutPips { get; set; }
        
        [Parameter("Use Trailing Stops", DefaultValue = true)]
        public bool pUseTrailingStop { get; set; }
        
        [Parameter("Trailing Stop Pips", DefaultValue = 20)]
        public double pTrailingPips { get; set; }
        
        [Parameter("Use Exit Indicator", DefaultValue = true)]
        public bool pUseExitIndicator { get; set; }
        
        #endregion

        #region private fields
        // EDIT FOR OTHER DIRECTION    Buy or Sell
        private TradeType vDirection = TradeType.Sell;

        private string vLabel;
        private long vVolume;
        private Position vPos;
        private bool vAlreadyScaledOut = false;
        private bool vClosedByExitIndicator = false;
        private double vDivisor = 10000;
        private double vScaleOutTPPrice;
        private double vStartTrailingPrice;

        #endregion

        #region indicators

        // exit indicator
        private HeikenAshiDirectionExitIndicator i_ha;

        #endregion

        #region cTrader events

        protected override void OnStart()
        {

            if (vDirection == TradeType.Buy)
            {
                vLabel = "BOTS_BUY_PIPS_" + Symbol.Code;
            }
            else
            {
                vLabel = "BOTS_SELL_PIPS_" + Symbol.Code;
            }

            if (Symbol.Code.Contains("JPY"))
                vDivisor = 100;

            // Instantiate Indicators
            if(pUseExitIndicator)
            i_ha = Indicators.GetIndicator<HeikenAshiDirectionExitIndicator>(TimeFrame);

            Positions.Opened += PositionsOnOpened;
            Positions.Closed += PositionsOnClosed;

            CalculateTradeParameters();

            // check if a position is already open
            var posList = Positions.FindAll(vLabel);

            if (posList.Length == 0)
            {

                var vResult = ExecuteMarketOrder(vDirection, Symbol, vVolume, vLabel);
                if (vResult.IsSuccessful)
                {
                    vPos = vResult.Position;
                    vResult = vPos.ModifyStopLossPips(pInitialSLPips);
                    if (vResult.IsSuccessful)
                    {
                        Print(vLabel + " INITIAL POSITION OPENED SUCCESSFULLY");
                    }
                    else
                    {
                        Print(vLabel + " INITIAL POSITION OPENED - PROBLEM MODIFYING STOP LOSS - " + vResult.ToString());
                    }
                }
                else
                {
                    Print(vLabel + " FAILED TO OPEN INITIAL POSITION - " + vResult.ToString());
                    Stop();
                }
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
                    var vResult = vPos.ModifyTakeProfitPrice(d);

                    if (vResult.IsSuccessful)
                    {
                        Print(vLabel + " RESTARTED SUCCESSFULLY - EXISTING POSITION IDENTIFIED");
                    }
                    else
                    {
                        Print(vLabel + " PROBLEM RESTARTING POSITION - " + vResult.ToString());
                    }
                }
            }

            if (vDirection == TradeType.Buy)
            {
                vScaleOutTPPrice = vPos.EntryPrice + (pScaleOutPips / vDivisor);
                vStartTrailingPrice = vPos.EntryPrice + (pTrailingPips / vDivisor);
            }
            else
            {
                vScaleOutTPPrice = vPos.EntryPrice - (pScaleOutPips / vDivisor);
                vStartTrailingPrice = vPos.EntryPrice - (pTrailingPips / vDivisor);
            }
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
                        var vResult = vPos.ModifyVolume(vNewVolume);

                        if (vResult.IsSuccessful)
                        {
                            Print(vLabel + " SCALED OUT SUCCESSFULLY AT : " + Symbol.Bid);
                            vAlreadyScaledOut = true;
                            vResult = vPos.ModifyStopLossPips(-1);
                        }
                        else
                        {
                            Print(vLabel + " PROBLEM MODIFYING VOLUME ON SCALE OUT - " + vResult.ToString());
                        }
                    }
                    else
                    {
                        var vResult = vPos.Close();
                        
                        if (vResult.IsSuccessful)
                        {
                            Print(vLabel + " VOLUME OF 1000 - POSITION CLOSED ON SCALE OUT");
                            vAlreadyScaledOut = true;
                        }
                        else
                        {
                            Print(vLabel + " VOLUME OF 1000 - PROBLEM CLOSING POSITION - " + vResult.ToString());
                        }
                    }
                }
            }
            else if (pUseTrailingStop && !vPos.HasTrailingStop)
            {   
                if (vDirection == TradeType.Buy && Symbol.Bid > vStartTrailingPrice ||
                     vDirection == TradeType.Sell && Symbol.Ask < vStartTrailingPrice)
                {
                    var vResult = vPos.ModifyTrailingStop(true);
                        
                    if (vResult.IsSuccessful)
                    {
                        Print(vLabel + " STOP LOSS SUCCESSFULLY CHANGED TO TRAILING STOP : " + Symbol.Bid);
                    }
                    else
                    {
                        Print(vLabel + " PROBLEM SETTING TRAILING STOP ON SCALE OUT - " + vResult.ToString());
                    }
                 }
             }   
        }


        protected override void OnBar()
        {
            // check exit indicator
            if (pUseExitIndicator && vAlreadyScaledOut && (vDirection == TradeType.Buy && i_ha.haDirection.Last(1) == -1 || vDirection == TradeType.Sell && i_ha.haDirection.Last(1) == 1))
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
            if (args.Position.Label == vLabel)
            {
                //Print(vLabel + " POSITION OPENED DELEGATE CALLED");
                vPos = args.Position;
            }
        }

        private void PositionsOnClosed(PositionClosedEventArgs args)
        {
            if (args.Position.Label == vLabel)
            {
                //Print(vLabel + " POSITION CLOSED DELEGATE CALLED");

                if (vClosedByExitIndicator)
                {
                    Print(vLabel + " POSITION CLOSED FROM EXIT INDICATOR");
                }
                else
                {
                    Print(vLabel + " POSITION CLOSED FROM STOP LOSS");
                }
                Stop();
            }
        }

        #endregion

        #region utility methods

        private void CalculateTradeParameters()
        {
            var maxRiskAmount = Account.Balance * (pPercAcctRisk / 100);
            var maxRiskPerPip = maxRiskAmount / pInitialSLPips;
            vVolume = Convert.ToInt64(maxRiskPerPip / Symbol.PipValue);
            vVolume = vVolume - (vVolume % 1000);

            if (vVolume < 1000)
                vVolume = 1000;
                
            HorizontalAlignment vAlign = HorizontalAlignment.Left;

            if (vDirection == TradeType.Sell)
            {
                vAlign = HorizontalAlignment.Right;
                Chart.DrawStaticText("cTitle", "BOTS SELL BOT", VerticalAlignment.Top, vAlign, "Yellow");
            }
            else
            {
                Chart.DrawStaticText("cTitle", "BOTS BUY BOT", VerticalAlignment.Top, vAlign, "Yellow");
            }

            Chart.DrawStaticText("cMaxRisk", ("\nMax Risk: $ " + Math.Round(maxRiskAmount, 2)), VerticalAlignment.Top, vAlign, "Yellow");
            Chart.DrawStaticText("cInitalSL", ("\n\nInitial SL (pips) " + pInitialSLPips), VerticalAlignment.Top, vAlign, "Yellow");
            Chart.DrawStaticText("cScaleOutTP", ("\n\n\nScale Out TP (pips) " + pScaleOutPips), VerticalAlignment.Top, vAlign, "Yellow");
            
            if (pUseTrailingStop)
                Chart.DrawStaticText("cTrailingDistance", ("\n\n\n\nTrailing SL at (pips) " + pTrailingPips), VerticalAlignment.Top, vAlign, "Yellow");
        
            if(pUseExitIndicator)
            {
            
                if (pUseTrailingStop)
                {
                    Chart.DrawStaticText("cExitIndicator", "\n\n\n\n\nUsing Exit Indicator", VerticalAlignment.Top, vAlign, "Yellow");
                }
                else
                {
                    Chart.DrawStaticText("cExitIndicator", "\n\n\n\nUsing Exit Indicator", VerticalAlignment.Top, vAlign, "Yellow");
                }
            }
        }

        #endregion
    }
}
