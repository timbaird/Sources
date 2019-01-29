using System;
using cAlgo.API;
using cAlgo.API.Indicators;

namespace cAlgo.Indicators
{
    [Indicator(IsOverlay = false, AccessRights = AccessRights.None)]
    public class HeikenAshiDirectionExitIndicator : Indicator
    {
        private IndicatorDataSeries _haOpen;
        private IndicatorDataSeries _haClose;

        [Parameter("Exit Timeframe", DefaultValue = "Minute15")]
        public TimeFrame pExitTimeFrame { get; set; }

        [Output("HA Direction", LineColor = "Yellow")]
        public IndicatorDataSeries haDirection { get; set; }

        protected override void Initialize()
        {
            _haOpen = CreateDataSeries();
            _haClose = CreateDataSeries();
        }

        public override void Calculate(int index)
        {
            var series = MarketData.GetSeries(pExitTimeFrame);

            var open = series.Open[index];
            var high = series.High[index];
            var low = series.Low[index];
            var close = series.Close[index];

            var haClose = (open + high + low + close) / 4;
            double haOpen;
            if (index > 0)
                haOpen = (_haOpen[index - 1] + _haClose[index - 1]) / 2;
            else
                haOpen = (open + close) / 2;

            var haHigh = Math.Max(Math.Max(high, haOpen), haClose);
            var haLow = Math.Min(Math.Min(low, haOpen), haClose);

            _haOpen[index] = haOpen;
            _haClose[index] = haClose;

            haDirection[index] = haOpen > haClose ? -1 : 1;
        }
    }
}
