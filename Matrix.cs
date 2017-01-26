using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LagrangeInterpol
{
    public class Matrix : ICloneable
    {
        public int Rows { get; }

        public int Cols { get; private set; }

        private double _det = -1;

        public static long CurrIter;
        public double Determinant => CalcDet();

        private readonly List<List<double>> _list = new List<List<double>>();

        public Matrix(int m, int n)
        {
            if (m <= 0 || n <= 0)
                throw new IndexOutOfRangeException("Size cannot be smaller than 1");
            Rows = m;
            Cols = n;
            _list.Capacity = m;
            for (var i = 0; i < m; ++i)
            {
                _list.Add(new List<double>(n));
            }
        }

        public void AddCol(double[] arr)
        {
            if (Cols != arr.Length)
                return;
            for (var i = 0; i < Cols; ++i)
                _list[i].Add(arr[i]);
            ++Cols;
        }

        /// <summary>
        /// Sets the given row of the matrix to the given array
        /// </summary>
        /// <param name="i">Row to set</param>
        /// <param name="arr">Numbers</param>
        public void SetRow(int i, double[] arr)
        {
            if (i < 0 || i >= Rows)
                return;
            if (arr.Length != Cols)
                return;
            _list[i].AddRange(arr);
        }

        public void SetRandom()
        {
            var rand = new Random();
            foreach (var arr in _list)
                for (var i = 0; i < Cols; ++i)
                    arr.Add(rand.NextDouble() * 200.0 - 100.0);
        }

        public double Get(int i, int j)
        {
            return _list[i][j];
        }

        public void Set(int i, int j, double value)
        {
            _list[i][j] = value;
        }

        public Matrix GetSubmatrix(int rowStart, int colStart, int rowEnd, int colEnd)
        {
            if (rowStart < 0 || colStart < 0 || rowEnd >= Cols || colEnd >= Cols)
                return null;
            var m = rowEnd - rowStart + 1;
            var n = colEnd - colStart + 1;
            var retMatrix = new Matrix(m, n);
            for (var i = 0; i < m; ++i)
            {
                var arr = _list[rowStart + i].GetRange(colStart, n).ToArray<double>();
                retMatrix.SetRow(i, arr);
            }
            return retMatrix;
        }

        public Matrix GetMinor(int m, int n)
        {
            var retMatrix = new Matrix(Rows - 1, Cols - 1);
            if (m < 0 || n < 0 || m >= Cols || n >= Cols)
                return null;
            for (var i = 0; i < Rows; ++i)
            {
                if (i == m)
                    continue;
                var list = new List<double>(Cols - 1);
                list.AddRange(_list[i].GetRange(0, n));
                list.AddRange(_list[i].GetRange(n + 1, Cols - n - 1));
                if (i > m)
                    retMatrix.SetRow(i - 1, list.ToArray<double>());
                else
                    retMatrix.SetRow(i, list.ToArray<double>());
            }
            return retMatrix;
        }

        private double CalcDet()
        {
            if (_det == -1)
            {
                var n = Rows;
                ++CurrIter;
                if (n == 2)
                    return Get(0, 0) * Get(1, 1) - Get(1, 0) * Get(0, 1);
                double tempDet = 0;
                for (var i = 0; i < n; ++i)
                {
                    if ((i + 1) % 2 != 0)
                        tempDet -= Get(0, i) * GetMinor(0, i).CalcDet();
                    else
                        tempDet += Get(0, i) * GetMinor(0, i).CalcDet();
                }
                return tempDet;
            }
            return _det;
        }

        public void SwapRows(int i1, int i2)
        {
            var temp = _list[i1].ToArray<double>();
            _list[i1].Clear();
            _list[i1].AddRange(_list[i2]);
            _list[i2].Clear();
            _list[i2].AddRange(temp);
        }

        public object Clone()
        {
            var retMatrix = new Matrix(Rows, Cols);
            for (var i = 0; i < Rows; ++i)
                retMatrix.SetRow(i, _list[i].ToArray<double>());
            return retMatrix;
        }

        public double CalculateGaussDeterminant()//use only if matrix is triangular
        {
            double tempDet = 1;
           
            for (var i = 0; i < Rows; ++i)
            {
                tempDet *= Get(i, i);
            }
            if (double.IsNaN(tempDet))
                tempDet = 0;
            _det = tempDet;
            return tempDet;
        }

        public override string ToString()
        {
            var arr = new object[Cols + 1];
            arr[0] = 0;
            var strbuild = new StringBuilder();
            var tempstr = new StringBuilder();
            tempstr.Append("{0,2}");
            for (var i = 1; i <= Cols; ++i)
            {
                tempstr.Append("{" + i + ",8}");
                arr[i] = i;
            }
            strbuild.AppendFormat(tempstr.ToString(), arr);
            strbuild.AppendLine();
            for (var i = 1; i <= Rows; ++i)
            {
                arr[0] = i;
                tempstr.Clear();
                tempstr.Append("{0,2}");
                for (var j = 1; j <= Cols; ++j)
                {
                    if (j > Rows)
                    {
                        tempstr.Append(" | ");
                    }
                    tempstr.Append("{" + j + ",8:F2}");
                    arr[j] = _list[i - 1][j - 1];
                }
                strbuild.AppendFormat(tempstr.ToString(), arr);
                strbuild.AppendLine();
            }
            return strbuild.ToString();
        }
    }
}
