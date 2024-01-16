using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmoothMouse
{
    // Exponential moving average filter
    // Where the amount of smoothing (alpha) is adjustable online
    public class EmaFilter
    {
        private double _lastValue;

        public EmaFilter()
        {
            _lastValue = double.NaN;
        }

        public double Filter(double value, double alpha)
        {
            if (double.IsNaN(_lastValue))
            {
                _lastValue = value;
            }
            else
            {
                _lastValue = alpha * value + (1 - alpha) * _lastValue;
            }

            return _lastValue;
        }
    }

}
