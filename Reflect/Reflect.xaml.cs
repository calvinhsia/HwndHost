using hWndHost;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Reflect
{
    public partial class ReflectWindow : Window
    {
        CancellationTokenSource _cts = new CancellationTokenSource();
        public ReflectWindow()
        {
            InitializeComponent();
            this.Title = "Reflect";
            this.Loaded += Reflect_Loaded;
            this.Closing += (o, e) =>
            {
                _cts.Cancel();
            };
        }

        private void Reflect_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var dp = new DockPanel();
                this.Content = dp;
                var border = new Border();
                dp.Children.Add(border);
                var spControls = new StackPanel()
                {
                    Orientation = Orientation.Horizontal
                };
                var btnQuit = new Button()
                {
                    Content = "Quit"
                };
                btnQuit.Click += (ob, eb) =>
                {
                    this.Close();
                };
                spControls.Children.Add(btnQuit);
                dp.Children.Add(spControls);
                var bounceFrame = new BounceFrame(
                    _cts,
                    NativeMethods.CreateSolidBrush(new IntPtr(0xfff0f0)));
                dp.Children.Add(bounceFrame);
                // forward mouse events to the hWndHost control
                this.MouseDown += (om, em) => bounceFrame.DoMouseDown(om, em);
                this.MouseMove += (om, em) => bounceFrame.DoMouseMove(om, em);
                this.MouseUp += (om, em) => bounceFrame.DoMouseUp(om, em);
            }
            catch (Exception ex)
            {
                this.Content = ex.ToString();
            }
        }
    }
    public class BounceFrame : MyHwndHost
    {
        IntPtr _clrLines = NativeMethods.CreatePen(nPenStyle: 0, nWidth: 0, nColor: (IntPtr)0xff00);
        IntPtr _clrFill = NativeMethods.CreatePen(nPenStyle: 0, nWidth: 0, nColor: (IntPtr)0xff);
        CancellationTokenSource _cts;

        Point _ptLight;
        double _angleLight;
        double _lightSpeed = 1;
        double _distLineThresh = 1;
        const double _piOver180 = Math.PI / 180;

        List<CLine> _lstLines = new List<CLine>();
        NativeMethods.WinPoint ptPrev = new NativeMethods.WinPoint();
        bool _fReDraw = false;
        public BounceFrame(
            CancellationTokenSource cts,
            IntPtr hbrBackground
            ) : base(hbrBackground)
        {
            this._cts = cts;
        }
        void DoDrawing()
        {
            var hdc = NativeMethods.GetDC(_hwnd);
            NativeMethods.SelectObject(hdc, _clrLines);
            while (!_cts.IsCancellationRequested)
            {
                Thread.Sleep(10);
                lock (_lstLines)
                {
                    if (_fReDraw)
                    {
                        foreach (var line in _lstLines)
                        {
                            NativeMethods.MoveToEx(hdc, (int)(xScale * line.pt0.X), (int)(yScale * line.pt0.Y), ref ptPrev);
                            NativeMethods.LineTo(hdc, (int)(xScale * line.pt1.X), (int)(yScale * line.pt1.Y));
                        }
                        _fReDraw = false;
                    }
                    // move the point, find the nearest line, see if it's within a Delta
                    var ptOrigLight = _ptLight;
                    var delX = Math.Cos(_lightSpeed * _angleLight * _piOver180);
                    var delY = Math.Sin(_lightSpeed * _angleLight * _piOver180);
                    _ptLight.X += delX;
                    _ptLight.Y += delY;

                    // calculate the closest line within a threshold
                    // make sure that the next move won't get closer
                    CLine closestLine = null;
                    double minDist = double.MaxValue;
                    foreach (var line in _lstLines)
                    {
                        var dist = line.CalculateDistanceFromPoint(_ptLight);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            closestLine = line;
                        }
                    }
                    if (minDist > _distLineThresh) // too far away
                    {
                        closestLine = null;
                    }
                    else
                    {
                        // we got the closest line. Let's see if the next move won't be closer
                        var nextDist = closestLine.CalculateDistanceFromPoint(new Point(_ptLight.X + delX, _ptLight.Y + delY));
                        if (nextDist < minDist)
                        {// next move will be closer, so we won't reflect til next move
                            closestLine = null;
                        }
                    }

                    if (closestLine != null)
                    {
                        var curline = new CLine(ptOrigLight, _ptLight);
                        if (closestLine.deltaX == 0) // vertical line
                        {
                            delX = -delX;
                            var newangle =Math.Tan( delY / delX)/_piOver180;

                        }
                        else if (closestLine.deltaY == 0) // horiz line
                        {
                            delY = -delY;
                            var newangle = Math.Tan(delY / delX) / _piOver180;

                        }
                        else
                        {
                            var angBetween = closestLine.angleBetween(curline);
                            var newSlope = Math.Tan(Math.Atan(closestLine.slope) + angBetween * _piOver180) / _piOver180;

                        }
                    }
                    else
                    {
                        NativeMethods.SetPixel(hdc, (int)(xScale * _ptLight.X), (int)(yScale * _ptLight.Y), _clrFill);
                    }
                }

                //for (int i = 0; i < 1000; i++)
                //{
                //    for (int j = 0; j < 1000; j++)
                //    {
                //        //var curpixel=NativeMethods.GetPixel(hdc, i, j);
                //        //if (curpixel != _colorDraw)
                //        {
                //            NativeMethods.SetPixel(hdc, i, j, _colorDraw);
                //        }
                //    }
                //}
            }
            NativeMethods.ReleaseDC(_hwnd, hdc);

        }
        public override void OnReady(IntPtr hwnd)
        {
            var tsk = Task.Run(() =>
            {
                DoDrawing();
            });
        }

        void AddLine(CLine line)
        {
            lock (_lstLines)
            {
                _lstLines.Add(line);
            }
        }
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            var mrg = 4;
            var ptTopLeft = new Point(mrg, mrg);
            var ptTopRight = new Point(sizeInfo.NewSize.Width - 1 - mrg, mrg);
            var ptBotLeft = new Point(mrg, sizeInfo.NewSize.Height - 1 - mrg);
            var ptBotRight = new Point(sizeInfo.NewSize.Width - 1 - mrg, sizeInfo.NewSize.Height - 1 - mrg);
            _ptOldMouseDown = null;
            _fPenDown = false;
            _fPenModeDrag = false;
            _ptLight = new Point(mrg * 2, mrg * 2);
            _angleLight = 4;
            lock (this._lstLines)
            {
                this._lstLines.Clear();
                this._lstLines.Add(new CLine(ptTopLeft, ptTopRight));
                this._lstLines.Add(new CLine(ptTopRight, ptBotRight));
                this._lstLines.Add(new CLine(ptBotRight, ptBotLeft));
                this._lstLines.Add(new CLine(ptTopLeft, ptBotLeft));
               //  this._lstLines.Add(new CLine(ptBotLeft, ptTopRight));
                var l1 = new CLine(new Point(0, 0), new Point(sizeInfo.NewSize.Height - 1, sizeInfo.NewSize.Width - 1));
                this._lstLines.Add(l1);
            }
            _fReDraw = true;
        }

        Point? _ptOldMouseDown;
        Point _ptCurrentMouseDown;
        bool _fPenModeDrag = false;
        bool _fPenDown = false;
        internal void DoMouseDown(object om, MouseButtonEventArgs em)
        {
            if (em.RightButton == MouseButtonState.Pressed)
            {
                _fPenModeDrag = !_fPenModeDrag;
            }
            else
            {
                if (_fPenModeDrag)
                {
                    _ptOldMouseDown = em.GetPosition(this);
                }
                else
                {
                    _fPenDown = true;
                    _ptCurrentMouseDown = em.GetPosition(this);
                    if (!_ptOldMouseDown.HasValue)
                    {
                        _ptOldMouseDown = _ptCurrentMouseDown;
                    }
                    _fReDraw = true;
                }
            }
        }

        internal void DoMouseMove(object om, MouseEventArgs em)
        {
            if (_fPenModeDrag)
            {
                if (em.LeftButton == MouseButtonState.Pressed)
                {
                    if (_ptOldMouseDown.HasValue)
                    {
                        _ptCurrentMouseDown = em.GetPosition(this);
                        var line = new CLine(_ptOldMouseDown.Value, _ptCurrentMouseDown);
                        AddLine(line);
                        _ptOldMouseDown = _ptCurrentMouseDown;
                        _fReDraw = true;
                    }
                }
            }
            else
            {
                if (_fPenDown)
                {
                    if (_ptOldMouseDown.HasValue)
                    {
                        _ptCurrentMouseDown = em.GetPosition(this);
                    }
                    _fReDraw = true;
                }
            }
        }

        internal void DoMouseUp(object om, MouseButtonEventArgs em)
        {
            if (_fPenDown)
            {
                _ptCurrentMouseDown = em.GetPosition(this);
                var line = new CLine(_ptOldMouseDown.Value, _ptCurrentMouseDown);
                AddLine(line);
                _ptOldMouseDown = _ptCurrentMouseDown;
                _fPenDown = false;
                _fReDraw = true;
            }
        }

        public class CLine
        {
            public Point pt0 { get; private set; }
            public Point pt1 { get; private set; }
            Lazy<double> lazyLineSegLength;
            public CLine(Point p0, Point p1)
            {
                this.pt0 = p0;
                this.pt1 = p1;
                lazyLineSegLength = new Lazy<double>(() =>
                    Math.Sqrt(square(pt1.Y - pt0.Y) + square(pt1.X - pt0.X)),
                   isThreadSafe: false // only accessed from one thread
                );
            }
            public double slope
            {
                get
                {
                    // divide by zero yields double.NaN
                    return deltaY / deltaX;
                }
            }
            public double deltaX => pt1.X - pt0.X;
            public double deltaY => pt1.Y - pt0.Y;

            public double angleBetween(CLine otherLine)
            {
                double tantheta = 0;
                if (pt1.X == pt0.X) // vertical line
                {
                    tantheta = Math.Abs(1 / otherLine.slope);
                }
                else
                {
                    var m1 = this.slope;
                    var m2 = otherLine.slope;
                    if (otherLine.pt0.X == otherLine.pt1.X)
                    {
                        throw new InvalidOperationException("Both lines vertical?");
                    }
                    tantheta = Math.Abs((m2 - m1) / (1 + m1 * m2));
                }
                return Math.Atan(tantheta) / _piOver180;
            }

            public Point? IntersectingPoint(CLine otherLine)
            {
                Point? result = null;

                return result;
            }
            public override string ToString()
            {
                return $"({pt0}),({pt1})";
            }
            double square(double x)
            {
                return x * x;
            }
            internal double CalculateDistanceFromPoint(Point ptLight)
            {
                double numerator = Math.Abs((pt1.Y - pt0.Y) * ptLight.X - (pt1.X - pt0.X) * ptLight.Y + pt1.X * pt0.Y - pt1.Y * pt0.X);
                return numerator / lazyLineSegLength.Value;
            }

        }
    }
}