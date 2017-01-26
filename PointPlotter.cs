using System;
using LiveCharts.Defaults;

namespace LagrangeInterpol
{
    public static class PointPlotter
    {
        public static ObservablePoint[] PlotPoints(double beg, double end, double step, Func<double, double> function)
        {
            var doInc = (end - beg) % step != 0;
            var partitionNum = (int)Math.Truncate((end - beg) / step);
            var arr = new ObservablePoint[partitionNum + (doInc ? 2 : 1)];
            for (var i = 0; i <= partitionNum; ++i)
            {
                arr[i] = new ObservablePoint(beg + step * i, function(beg + step * i));
            }
            if (doInc)
                arr[arr.Length - 1] = new ObservablePoint(end, function(end));
            return arr;
        }
    }
}
