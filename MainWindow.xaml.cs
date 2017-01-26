using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;

namespace LagrangeInterpol
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public SeriesCollection GlobalCollection { get; set; }

        private double _to;             //These values control Y-axis zoom
        private double _from;

        private double _x;              //These values control func values
        private double _y;

        private double _x0;
        private double _y0;

        private double _accuracy;

        private double _xEnd;

        private double _yDiffCoeff;
        private double _yCoeff;
        private double _xCoeff;
        private double _lnCoeff;

        private readonly ChartValues<ObservablePoint> _interpolFuncValues;

        public ChartValues<ObservablePoint> PointValues;

        private Func<double, double> _lagrFunc;

        public ObservableCollection<Point> Points;

        public MainWindow()
        {
            InitializeComponent();

            var rand = new Random();
            DataContext = this;
            chart.ChartLegend = null;

            YDiffCoeff = 1.0;
            YCoeff = -1;
            XCoeff = 0.1;
            LnCoeff = 1.0;

            XEnd = 5.0;
            Accuracy = 1.0;

            dataGrid.ItemsSource = Points;

            PointValues = new ChartValues<ObservablePoint>();
            new LineSeries
            {
                Values = PointValues,
                LineSmoothness = 0,
                Fill = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                Stroke = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                PointForeround = new SolidColorBrush(Color.FromRgb((byte)rand.Next(0, 255), (byte)rand.Next(0, 255), (byte)rand.Next(0, 255)))
            };
            _interpolFuncValues = new ChartValues<ObservablePoint>();
            var interpolFuncSeries = new LineSeries
            {
                Values = _interpolFuncValues,
                LineSmoothness = 0,
                Fill = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                PointGeometrySize = 0
            };

            GlobalCollection = new SeriesCollection
            {
                interpolFuncSeries
                //pointSeries
            };
            //Setting up chart viewing region
            From = 0;
            To = 20;
        }

        private static IEnumerable<ObservablePoint> PointToObsPoint(IReadOnlyCollection<Point> points)
        {
            var newPoints = new ObservablePoint[points.Count];
            var i = 0;
            foreach (var point in points)
                newPoints[i++] = new ObservablePoint(point.X, point.Y);
            return newPoints;
        }

        private void setGraphsButton_Click(object sender, RoutedEventArgs e)
        {
            var points = new List<ObservablePoint>(PointValues.ToArray());

            Func<double, double, double> func = (x, y) =>
              {
                  var ret = _yCoeff * y / _yDiffCoeff + x * x / _yDiffCoeff * _xCoeff + (_lnCoeff == 0.0 ? 0.0 : _lnCoeff * Math.Log(x) / _yDiffCoeff);
                  return ret;
              };
            //var temp = KoshiSolver.RungeKutta(_x0, _y0, _xEnd, _accuracy, func);
            //var temp = KoshiSolver.Euler(_x0, _y0, _xEnd, _accuracy, func);
            //var temp = KoshiSolver.Milne(_x0, _y0, _xEnd, _accuracy, func);
            var temp = KoshiSolver.ImprovedEuler(_x0, _y0, _xEnd, _accuracy, func);
            //var temp = KoshiSolver.Predictor_and_corrector(_x0, _y0, _xEnd, _accuracy, func);
            //var temp = KoshiSolver.Adams(_x0, _y0, _xEnd, _accuracy, func);
            var newPoints = new List<ObservablePoint>(PointToObsPoint(temp));
            Points = new ObservableCollection<Point>(temp);
            newPoints.Sort(new PointXSorter());
            dataGrid.ItemsSource = null;
            dataGrid.ItemsSource = Points;
            //If the points we got from the user did not change from the previous ones, then we don't need to redraw the graph
            if (CompareLists(newPoints, points))
                return;
            PointValues.Clear();
            _interpolFuncValues.Clear();
            if (newPoints.Count == 0)
            {
                _lagrFunc = null;
                return;
            }

            PointValues.AddRange(newPoints);
            _lagrFunc = InterpolationService.MnkApprox(newPoints);  //This will give us the function that was created by InterpolationService
            _interpolFuncValues.AddRange(                                          //This will give us the values with the given step, interval 
                PointPlotter.PlotPoints(PointValues[0].X, PointValues[PointValues.Count - 1].X, 0.01, // and function and set it for graphing automatically
                    _lagrFunc));

            //If we received no points for the user, then just set the drawing interval of the base function to some random values
            double maxFunc = double.MinValue, minFunc = double.MaxValue;
            foreach (var point in _interpolFuncValues)
            {
                if (point.Y > maxFunc)
                    maxFunc = point.Y;
                if (point.Y < minFunc)
                    minFunc = point.Y;
            }
            //Setting viewing intervals
            From = Math.Round(minFunc * 0.75, 2);
            To = Math.Round(maxFunc * 1.15, 2);
        }

        private void chart_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                var point = chart.ConvertToChartValues(e.GetPosition(chart));

                X = point.X;
            }
            catch
            {
            }
        }

        private static bool CompareLists(IReadOnlyList<ObservablePoint> listA, IReadOnlyList<ObservablePoint> listB)
        {
            if (listA.Count != listB.Count)
                return false;
            for (var i = 0; i < listA.Count; ++i)
            {
                if (!(listA[i].X == listB[i].X && listA[i].Y == listB[i].Y))
                    return false;
            }
            return true;
        }

        public double X
        {
            get { return _x; }
            set
            {
                _x = value;
                if (_lagrFunc == null)
                {
                    Y = double.NaN;
                    return;
                }
                Y = _lagrFunc(value);
                OnPropertyChanged("X");
            }
        }
        public double Y
        {
            get { return _y; }
            set
            {
                _y = value;
                OnPropertyChanged("Y");
            }
        }

        public double Accuracy
        {
            get { return _accuracy; }
            set
            {
                _accuracy = value;
                OnPropertyChanged("Accuracy");
            }
        }

        public double XEnd
        {
            get { return _xEnd; }
            set
            {
                _xEnd = value;
                OnPropertyChanged("XEnd");
            }
        }

        public double YDiffCoeff
        {
            get { return _yDiffCoeff; }
            set
            {
                _yDiffCoeff = value;
                OnPropertyChanged("YDiffCoeff");
            }
        }
        public double YCoeff
        {
            get { return _yCoeff; }
            set
            {
                _yCoeff = value;
                OnPropertyChanged("YCoeff");
            }
        }
        public double XCoeff
        {
            get { return _xCoeff; }
            set
            {
                _xCoeff = value;
                OnPropertyChanged("XCoeff");
            }
        }
        public double LnCoeff
        {
            get { return _lnCoeff; }
            set
            {
                _lnCoeff = value;
                OnPropertyChanged("LnCoeff");
            }
        }

        public double X0
        {
            get { return _x0; }
            set
            {
                _x0 = value;
                OnPropertyChanged("X0");
            }
        }
        public double Y0
        {
            get { return _y0; }
            set
            {
                _y0 = value;
                OnPropertyChanged("Y0");
            }
        }

        public double From
        {
            get { return _from; }
            set
            {
                _from = value;
                OnPropertyChanged("From");
            }
        }
        public double To
        {
            get { return _to; }
            set
            {
                _to = value;
                OnPropertyChanged("To");
            }
        }

        private void XBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            textBox?.Clear();
        }

        private void XBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var textBox = sender as TextBox;
                var be = textBox?.GetBindingExpression(TextBox.TextProperty);
                be?.UpdateSource();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }

    public class PointXSorter : IComparer<ObservablePoint>
    {
        public int Compare(ObservablePoint x, ObservablePoint y)
        {
            if (x.X < y.X)
                return -1;
            return x.X == y.X ? 0 : 1;
        }
    }

    public class NumberValidation : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            double temp;
            if (double.TryParse((string)value, out temp))
            {
                return new ValidationResult(true, null);
            }
            return new ValidationResult(false, "Not a double value");
        }
    }

    public class IntegerValidation : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            int temp;
            if (int.TryParse((string)value, out temp) && temp >= 0)
            {
                return new ValidationResult(true, null);
            }
            return new ValidationResult(false, "Not a double value");
        }
    }

    public class DoubleToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((double)value).ToString(CultureInfo.InvariantCulture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double temp;
            if (double.TryParse((string)value, out temp))
            {
                return temp;
            }
            throw new ApplicationException("Double value expected");
        }
    }

    public class PointToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;
            var point = (Point)value;
            return point.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new ApplicationException("Double value expected");
        }
    }

    public class CoeffToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && (double)value != 1.0)
                return ((double)value).ToString();
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double temp;
            if ((string)value == string.Empty)
                return 1.0;
            if (double.TryParse((string)value, out temp))
            {
                return temp;
            }
            throw new ApplicationException("Double value expected");
        }
    }
}
