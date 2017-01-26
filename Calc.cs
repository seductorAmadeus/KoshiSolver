using System;

namespace LagrangeInterpol
{
    class Calc
    {
        private readonly Matrix _matrix;
        private double[] _right;
        private double _det;
        private readonly Matrix _completeMatrix;
        private double[] _solution;
        private double[] _error;

        public double Determinant => CalcDet();

        public Calc(Matrix matrix, double[] right)
        {
            if (matrix.Rows != right.Length)
                throw new IndexOutOfRangeException();
            _matrix = matrix;
            _right = right;
            _completeMatrix = (Matrix)matrix.Clone();
            _completeMatrix.AddCol(right);
            _det = -1;
        }

        private long Factorial(long i)
        {
            if (i <= 1)
                return 1;
            return i * Factorial(i - 1);
        }

        private double CalcDet()
        {
            if (_det == -1)
            {
                if (_matrix.Cols > 10)
                {
                    var tempcalc = new Calc((Matrix)_matrix.Clone(), _right);
                    tempcalc.GetSolution(); //приведение к треугольному виду
                    _det = tempcalc._matrix.CalculateGaussDeterminant();
                    return _det;
                }
                _det = _matrix.Determinant;
                return _det;
            }
            return _det;
        }

        public double[] GetSolution()
        {
            if (_solution != null) return _solution;
            //if (calcDet() == 0)
            //   return null;
            //Прямой ход:
            for (var i = 0; i < _completeMatrix.Cols - 2; ++i)// -2?
            {
                var maxIndex = i;
                for (var j = i; j < _completeMatrix.Rows; ++j)
                {
                    if (Math.Abs(_completeMatrix.Get(maxIndex, i)) < Math.Abs(_completeMatrix.Get(j, i)))
                        maxIndex = j;
                }
                if (maxIndex != i)
                {
                    _completeMatrix.SwapRows(i, maxIndex);
                }
                for (var j = i + 1; j < _completeMatrix.Rows; ++j)
                {
                    var k = _completeMatrix.Get(j, i) / _completeMatrix.Get(i, i);
                    for (var p = i; p < _completeMatrix.Cols; ++p)
                    {
                        _completeMatrix.Set(j, p, _completeMatrix.Get(j, p) - k * _completeMatrix.Get(i, p));
                    }
                }
            }
            //
            //we can find our determinant by now

            _det = GetCombinedMatrix().CalculateGaussDeterminant();
            if (_det == 0)
                return null;
            //Обратный ход:
            _solution = new double[_completeMatrix.Rows];
            _solution[_completeMatrix.Rows - 1] = _completeMatrix.Get(_completeMatrix.Rows - 1, _completeMatrix.Cols - 1) / _completeMatrix.Get(_completeMatrix.Rows - 1, _completeMatrix.Cols - 2);
            for (var i = _completeMatrix.Rows - 2; i >= 0; --i)
            {
                double sum = 0;
                for (var j = i + 1; j < _completeMatrix.Cols - 1; ++j)
                {
                    sum += _completeMatrix.Get(i, j) * _solution[j];
                }
                _solution[i] = (_completeMatrix.Get(i, _completeMatrix.Cols - 1) - sum) / _completeMatrix.Get(i, i);
            }
            return _solution;
        }

        public double[] GetError()
        {
            if (_error != null) return _error;
            if (_solution == null)
            {
                GetSolution();
            }
            _error = new double[_matrix.Rows];
            for (var i = 0; i < _matrix.Rows; ++i)
            {
                double sum = 0;
                for (var j = 0; j < _matrix.Cols; ++j)
                {
                    sum += _matrix.Get(i, j) * _solution[j];
                }
                _error[i] = _right[i] - sum;
            }
            return _error;
        }

        public Matrix GetSystemMatrix()
        {
            return _completeMatrix.GetSubmatrix(0, 0, _completeMatrix.Rows - 1, _completeMatrix.Cols - 2);
        }

        public Matrix GetCombinedMatrix()
        {
            return (Matrix)_completeMatrix.Clone();
        }
    }
}
