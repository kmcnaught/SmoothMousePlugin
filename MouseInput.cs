using System;
using System.Reactive;
using System.Windows;
using System.ComponentModel;
using System.Threading;
using JuliusSweetland.OptiKey.Contracts;
using System.Runtime.InteropServices;

namespace SmoothMouse
{
    public class MouseInput : IPointService, IDisposable
    {
        #region Fields
        private event EventHandler<Timestamped<Point>> pointEvent;

        private BackgroundWorker pollWorker;

        private EmaFilter _xFilter = new EmaFilter();
        private EmaFilter _yFilter = new EmaFilter();
        private double _alpha;
        private Point _lastPosition;

        #endregion

        #region Ctor

        public MouseInput()
        {
            _alpha = 0.5; // Initial alpha value

            pollWorker = new BackgroundWorker();
            pollWorker.DoWork += pollMouse;
            pollWorker.WorkerSupportsCancellation = true;
        }

        public void Dispose()
        {
            pollWorker.CancelAsync();
            pollWorker.Dispose();
        }

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        #endregion

        #region Properties

        public bool KalmanFilterSupported { get; private set; }

        #endregion

        #region Events

        public event EventHandler<Exception> Error;

        public event EventHandler<Timestamped<Point>> Point
        {
            add
            {
                if (pointEvent == null)
                {
                    // Start polling the mouse
                    pollWorker.RunWorkerAsync();
                }

                pointEvent += value;
            }
            remove
            {
                pointEvent -= value;

                if (pointEvent == null)
                {
                    pollWorker.CancelAsync();
                }
            }
        }

        #endregion

        #region Private methods
        
        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi)]
        private static extern void GetSystemTimePreciseAsFileTime(out long filetime);

        public static DateTime HighResolutionUtcNow
        {
            get
            {
                try
                {
                    long filetime;
                    GetSystemTimePreciseAsFileTime(out filetime);
                    return DateTime.FromFileTimeUtc(filetime);
                }
                catch (EntryPointNotFoundException)
                {
                    // GetSystemTimePreciseAsFileTime is available from Windows 8+
                    // Fall back to lower resolution alternative
                    return DateTime.UtcNow;
                }
            }
        }

        private void pollMouse(object sender, DoWorkEventArgs e)
        {
            while (!pollWorker.CancellationPending)
            {
                lock (this)
                {
                    // Get latest mouse position
                    var timeStamp = HighResolutionUtcNow.ToUniversalTime();

                    // Gets the absolute mouse position, relative to screen
                    POINT cursorPos;
                    GetCursorPos(out cursorPos);

                    double distance = Math.Sqrt(Math.Pow(cursorPos.X - _lastPosition.X, 2) + Math.Pow(cursorPos.Y - _lastPosition.Y, 2));

                    // Update alpha based on distance
                    _alpha = 0.5;
                    //_alpha = distance > 10 ? 0.5 : 0.1; // Adjust these thresholds and values as needed

                    double x = _xFilter.Filter(cursorPos.X, _alpha);
                    double y = _yFilter.Filter(cursorPos.Y, _alpha);

                    _lastPosition = new Point((int)x, (int)y);

                    pointEvent(this, new Timestamped<Point>(
                        new Point((int)x, (int)y),
                        timeStamp));

                    // Sleep thread to avoid hot loop
                    int delay = 30; // ms
                    Thread.Sleep(delay);
                }
            }
        }
        #endregion

        #region Publish Error

        private void PublishError(object sender, Exception ex)
        {
            if (Error != null)
            {
                Error(sender, ex);
            }
        }

        #endregion
    }
}
