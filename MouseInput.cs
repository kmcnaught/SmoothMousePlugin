using System;
using System.Reactive;
using System.Windows;
using System.ComponentModel;
using System.Threading;
using JuliusSweetland.OptiKey.Contracts;
using JuliusSweetland.OptiKey.Static;
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
        private double _alpha = 0.1;

        #endregion

        #region Ctor

        public MouseInput()
        {
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

        private void pollMouse(object sender, DoWorkEventArgs e)
        {
            while (!pollWorker.CancellationPending)
            {
                lock (this)
                {
                    // Get latest mouse position
                    var timeStamp = Time.HighResolutionUtcNow.ToUniversalTime();

                    // Gets the absolute mouse position, relative to screen
                    POINT cursorPos;
                    GetCursorPos(out cursorPos);

                    // Apply smoothing
                    _alpha = 0.1;
                    double x = _xFilter.Filter(cursorPos.X, _alpha);
                    double y = _yFilter.Filter(cursorPos.Y, _alpha);

                    // Emit a point event
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
