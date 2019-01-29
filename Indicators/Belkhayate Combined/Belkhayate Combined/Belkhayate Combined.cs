using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;

namespace cAlgo
{
    [Indicator(IsOverlay = false, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class BelkhayateCombined : Indicator
    {


        [Parameter("Timing Trigger Level", DefaultValue = 2, MaxValue = 2, MinValue = 1)]
        public int timingTriggerLevel { get; set; }

        [Parameter("Regression Trigger Level", DefaultValue = 3, MaxValue = 3, MinValue = 1)]
        public int regressionTriggerLevel { get; set; }

        [Parameter("Regression Periods", DefaultValue = 120)]
        public int regressionPeriods { get; set; }

        [Output("Action", LineColor = "Yellow")]
        public IndicatorDataSeries Action { get; set; }

        //[Output("Regression", LineColor = "Blue")]
        //public IndicatorDataSeries ResultRegression { get; set; }

        private double ResultRegression;

        //[Output("Timing", LineColor = "Red")]
        //public IndicatorDataSeries ResultTiming { get; set; }
        
        private double ResultTiming;
        
        private string upArrow = "▲";
        private string downArrow = "▼";
        private const VerticalAlignment vAlign = VerticalAlignment.Top;
        private const HorizontalAlignment hAlign = HorizontalAlignment.Center;
        Color colorDown = Color.Red;
        Color colorUp = Color.Green;

        private double arrowOffset;

        public BelkhayateTimingX bt;
        public BelkhayatePolynomialRegressionX bpr;

        protected override void Initialize()
        {
            // Initialize and create nested indicators
            arrowOffset = Symbol.PipSize * 5;
            bt = Indicators.GetIndicator<BelkhayateTimingX>();
            bpr = Indicators.GetIndicator<BelkhayatePolynomialRegressionX>(3, regressionPeriods, 1.4, 2.4, 3.4);
        }

        public override void Calculate(int index)
        {
            int x = index;
            double y;
            string arrowName;
            double btsell = 0;
            double btbuy = 0;
            int bprLevelHigh = 0;
            int bprLevelLow = 0;
            int n = 0;
            var high = MarketSeries.High[index];
            var low = MarketSeries.Low[index];

            // deal with the unpopulated data as indicator does initialcalculations
            if (!double.IsNaN(bpr.sqh.Last(0)))
            {
                for (n = 0; n < regressionPeriods; n++)
                {
                    // ------------------
                    // deal with polynomial regression
                    // ------------------

                    high = MarketSeries.High.Last(n);
                    low = MarketSeries.Low.Last(n);

                    //if (Symbol.Bid > bpr.sqh3.Last(n))
                    if (high > bpr.sqh3.Last(n))
                    {
                        bprLevelHigh = 3;
                    }
                    //else if (Symbol.Bid > bpr.sqh2.Last(n))
                    else if (high > bpr.sqh2.Last(n))
                    {
                        bprLevelHigh = 2;
                    }
                    //else if (Symbol.Bid > bpr.sqh.Last(n))
                    else if (high > bpr.sqh.Last(n))
                    {
                        bprLevelHigh = 1;
                    }
                    else
                    {
                        bprLevelHigh = 0;
                    }

                    //if (Symbol.Ask < bpr.sql3.Last(n))
                    if (low < bpr.sql3.Last(n))
                    {
                        bprLevelLow = -3;
                    }
                    //else if (Symbol.Ask < bpr.sql2.Last(n))
                    else if (low < bpr.sql2.Last(n))
                    {
                        bprLevelLow = -2;
                    }
                    //else if (Symbol.Ask < bpr.sql.Last(n))
                    else if (low < bpr.sql.Last(n))
                    {
                        bprLevelLow = -1;
                    }
                    else
                    {
                        bprLevelLow = 0;
                    }

                    if (bprLevelHigh > 0 && bprLevelLow == 0)
                    {
                        //ResultRegression[index - n] = bprLevelHigh;
                        ResultRegression = bprLevelHigh;
                    }
                    else if (bprLevelLow < 0 && bprLevelHigh == 0)
                    {
                        //ResultRegression[index - n] = bprLevelLow;
                        ResultRegression = bprLevelLow;
                    }
                    else
                    {
                        //ResultRegression[index - n] = 0;
                        ResultRegression = 0;
                    }

                    // ---------------------
                    // -- deal with belkhayate timing
                    // ---------------------

                    if (!double.IsNaN(bt.High.Last(n)))
                    {

                        if (timingTriggerLevel != 1 && timingTriggerLevel != 2)
                        {
                            throw new Exception("Invalid Trigger Level Value - must be either 1 or 2");
                        }

                        if (bt.High.Last(n) > bt.SellLine2.Last(n))
                        {
                            btsell = 2;
                        }
                        else if (bt.High.Last(n) > bt.SellLine1.Last(n))
                        {
                            btsell = 1;
                        }
                        else
                        {
                            btsell = 0;
                        }

                        if (bt.Low.Last(n) < bt.BuyLine2.Last(n))
                        {
                            btbuy = -2;
                        }
                        else if (bt.Low.Last(n) < bt.BuyLine1.Last(n))
                        {
                            btbuy = -1;
                        }
                        else
                        {
                            btbuy = 0;
                        }

                        if (btsell > 0 && btbuy == 0)
                        {
                            //ResultTiming[index - n] = btsell;
                            ResultTiming = btsell;
                        }
                        else if (btbuy < 0 && btsell == 0)
                        {
                            //ResultTiming[index - n] = btbuy;
                            ResultTiming = btbuy;
                        }
                        else
                        {
                            //ResultTiming[index - n] = 0;
                            ResultTiming = 0;
                        }

                        //--------------------
                        // Draw the arrows as needed
                        //-------------------

/*
                        if (ResultTiming[index - n] >= 1 && 
                            ResultTiming[index - n] >= timingTriggerLevel && 
                            ResultRegression[index - n] >= 1 && 
                            ResultRegression[index - n] >= regressionTriggerLevel)
                            */
                        if (ResultTiming >= 1 && 
                            ResultTiming >= timingTriggerLevel && 
                            ResultRegression >= 1 && 
                            ResultRegression >= regressionTriggerLevel)
                        {
                            arrowName = string.Format("ArrowSell {0}", index - n);
                            y = high + arrowOffset;
                            var arrow = Chart.DrawText(arrowName, downArrow, MarketSeries.OpenTime[index - n], y, colorDown);
                            arrow.VerticalAlignment = VerticalAlignment.Top;
                            arrow.HorizontalAlignment = HorizontalAlignment.Center;
                            Action[index - n] = -1;
                        }
                        
                        /*
                        else if (ResultTiming[index - n] <= -1 && 
                                 ResultTiming[index - n] <= -timingTriggerLevel && 
                                 ResultRegression[index - n] <= -1 && 
                                 ResultRegression[index - n] <= -regressionTriggerLevel)
                         */
                         
                        else if (ResultTiming <= -1 && 
                                 ResultTiming <= -timingTriggerLevel && 
                                 ResultRegression <= -1 && 
                                 ResultRegression <= -regressionTriggerLevel)
                        {
                            arrowName = string.Format("ArrowBuy {0}", index - n);
                            y = low - arrowOffset;
                            var arrow = Chart.DrawText(arrowName, upArrow, MarketSeries.OpenTime[index - n], y, colorUp);
                            arrow.VerticalAlignment = VerticalAlignment.Bottom;
                            arrow.HorizontalAlignment = HorizontalAlignment.Center;
                            Action[index - n] = 1;
                        }
                        else
                        {
                            Action[index - n] = 0;
                        }
                    }
                }

            }
        }
    }
}

