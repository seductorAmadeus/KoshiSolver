using System;
using System.Collections.Generic;
using System.Linq;
using LiveCharts.Defaults;

namespace LagrangeInterpol
{
    internal static class InterpolationService
    {
        public static Func<double, double> LagrangeInterpolation(ICollection<ObservablePoint> pointCollection)
        {
            var funcs = new Func<double, int, double>[pointCollection.Count];
            for (var i = 0; i < pointCollection.Count; ++i)
            {
                funcs[i] = (x, index) =>
                {
                    double k = 1;
                    for (var j = 0; j < pointCollection.Count; ++j)
                    {
                        if (j == index)
                            continue;
                        if (k == 0)
                            break;
                        k *= (x - pointCollection.ElementAt(j).X) / (pointCollection.ElementAt(index).X - pointCollection.ElementAt(j).X);
                    }
                    return pointCollection.ElementAt(index).Y * k;
                };
            }
            Func<double, double> mainFunc = x =>
            {
                double sum = 0;
                for (var i = 0; i < pointCollection.Count; ++i)
                    sum += funcs[i](x, i);
                return sum;
            };
            return mainFunc;
        }

        public static Func<double, double> MnkApprox(ICollection<ObservablePoint> pointCollection)
        {
            var n = pointCollection.Count;
            const int m = 3;
            var b = new Matrix(m + 1, m + 1);
            b.SetRandom();
            var right = new double[m + 1];
            for (var k = 0; k < m + 1; k++)
            {
                double sum = 0;

                for (var l = 0; l < m + 1; l++)
                {
                    var pow = k + l;
                    sum = 0;

                    for (var i = 0; i < n; i++)
                    {
                        sum += Math.Pow(pointCollection.ElementAt(i).X, pow);
                    }

                    b.Set(k, l, sum);
                }

                sum = 0;

                for (var i = 0; i < n; i++)
                {
                    var t = Math.Pow(pointCollection.ElementAt(i).X, k);
                    t *= pointCollection.ElementAt(i).Y;
                    sum += t;
                }

                right[k] = sum;
            }
            var calc = new Calc(b, right);
            var solution = calc.GetSolution();
            Func<double, double> mainFunc = x =>
            {
                return solution.Select((t, i) => Math.Pow(x, i) * t).Sum();
            };
            return mainFunc;
        }

        public static Func<double, double> NewtonInterpolation(ICollection<ObservablePoint> pointCollection, double convergeAccuracy)
        {
            Func<double, double> mainFunc = null;
            var list = new List<double[]> {new double[pointCollection.Count], new double[pointCollection.Count]};
            var i = 0;
            foreach (var point in pointCollection)
            {
                list[0][i] = point.X;
                list[1][i++] = point.Y;
            }

            var converged = false;
            i = 0;
            do
            {
                var m = pointCollection.Count - ++i;
                list.Add(new double[m]);
                converged = true;
                for (var j = 0; j < m; ++j)
                {
                    list[i + 1][j] = list[i][j + 1] - list[i][j];
                }
                var convergeCount = 0;
                for (var j = 0; j < m - 1; ++j)
                {
                    if (list[i + 1].Length == 1)
                        break;
                    if (!(Math.Abs(list[i + 1][j] - list[i + 1][j + 1]) > convergeAccuracy))
                    {
                        ++convergeCount;
                    }
                }
                if (convergeCount < list[i + 1].Length / 2)
                    converged = false;
            } while (!converged);

            mainFunc = x =>
            {
                var res = list[1][0];
                i = list.Count - 2;
                var h = list[0][1] - list[0][0];
                var q = (x - list[0][0]) / h;
                for (var j = 0; j < i; ++j)
                {
                    var k = list[j + 2][0];
                    k *= q;
                    for (var p = 0; p < j; ++p)
                    {
                        k *= (q - p - 1);
                    }
                    k /= Factorial(j + 1);
                    res += k;
                }
                return res;
            };
            return mainFunc;
        }

        private static int Factorial(int factNo)
        {
            var temno = 1;

            for (var i = 1; i <= factNo; i++)
            {
                temno = temno * i;
            }

            return temno;
        }
    }
}
