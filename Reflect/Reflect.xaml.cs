using hWndHost;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;

namespace Reflect
{
    public partial class ReflectWindow : Window
    {
        CancellationTokenSource _cts = new CancellationTokenSource();
        TextBox _tbxStatus;

        public ReflectWindow()
        {
            InitializeComponent();
            this.Top = 0;
            this.Left = 0;
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
                // Make a namespace referring to our namespace and assembly
                // using the prefix "l:"
                //xmlns:l=""clr-namespace:Fish;assembly=Fish"""
                var nameSpace = this.GetType().Namespace;
                var asm = System.IO.Path.GetFileNameWithoutExtension(
                    Assembly.GetExecutingAssembly().Location);

                var xmlns = string.Format(
  @"xmlns:l=""clr-namespace:{0};assembly={1}""", nameSpace, asm);
                //there are a lot of quotes (and braces) in XAML
                //and the C# string requires quotes to be doubled
                var strxaml =
  @"<Grid
xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
" + xmlns + // add our xaml namespace
  @" Margin=""5,5,5,5"">
<Grid.RowDefinitions>
    <RowDefinition/>
    <RowDefinition Height=""25"" />
</Grid.RowDefinitions>
<DockPanel Grid.Row=""0"">
    <Grid>
        <Grid.ColumnDefinitions>
        <ColumnDefinition Width = ""70""/>
        <ColumnDefinition/>
        </Grid.ColumnDefinitions>
        <StackPanel Name=""inputPanel"" 
            Orientation=""Vertical"" 
            >
            <CheckBox Content=""_Running"" 
                IsChecked= ""{Binding Path=IsRunning}"" />
            <Button 
                Name=""btnClear"" 
                Content=""_Clear"" 
                Margin=""10,2,0,0"" 
                />

        </StackPanel>
        <UserControl Name=""MyUserControl"" Grid.Column=""1""></UserControl>
    </Grid>
</DockPanel>
<DockPanel Grid.Row=""1"">
    <TextBox 
        Name=""tbxStatus"" 
        HorizontalAlignment=""Left"" 
        Height=""23"" 
        Margin=""10,2,0,0"" 
        IsReadOnly=""True""
        TextWrapping=""Wrap"" 
        VerticalAlignment=""Top"" 
        Width=""420""/>
    <Slider 
        HorizontalAlignment=""Left"" 
        Minimum=""0""
        Maximum=""1000""
        Margin=""12,2,0,0"" 
        Value=""{Binding Path=_nDelay}""
        VerticalAlignment=""Top"" 
        ToolTip=""Change the delay (msecs)""
        Width=""100""/>
    <Button 
        Name=""btnQuit"" 
        Content=""_Quit"" 
        HorizontalAlignment=""Left"" 
        Margin=""10,2,0,0"" 
        VerticalAlignment=""Top"" 
        Width=""55""/>
</DockPanel>
</Grid>
";
                var bounceFrame = new BounceFrame(
                    _cts,
                    NativeMethods.CreateSolidBrush(new IntPtr(0xfff0f0)));

                var strReader = new System.IO.StringReader(strxaml);
                var xamlreader = XmlReader.Create(strReader);
                var grid = (Grid)(XamlReader.Load(xamlreader));
                grid.DataContext = bounceFrame;

                this.Content = grid;
                _tbxStatus = (TextBox)grid.FindName("tbxStatus");
                var btnQuit = (Button)grid.FindName("btnQuit");
                btnQuit.Click += (ob, eb) =>
                {
                    App.Current.Shutdown();
                };
                var btnClear = (Button)grid.FindName("btnClear");
                btnClear.Click += (ob, eb) =>
                {
                    Size size = new Size(bounceFrame.ActualWidth, bounceFrame.ActualHeight);
                    bounceFrame.btnClear(size);
                };
                var userCtrl = (UserControl)grid.FindName("MyUserControl");
                userCtrl.Content = bounceFrame;
                //// forward mouse events to the hWndHost control
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
        IntPtr _clrLines = NativeMethods.CreatePen(nPenStyle: 0, nWidth: 0, nColor: (IntPtr)0x00);
        IntPtr _clrFill = NativeMethods.CreatePen(nPenStyle: 0, nWidth: 0, nColor: (IntPtr)0xff);
        CancellationTokenSource _cts;
        int _maxBounces = 5;
        const int SpeedMult = 10;
        Point _ptLight;
        Vector _vecLight;
        //double _distLineThresh = 1;
        const double _piOver180 = Math.PI / 180;
        Task _tskDrawing;
        public int _nDelay { get; set; }

        bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                _isRunning = value;
                if (value)
                {
                    StartDrawingTask();
                }
                else
                {
                    EnsureCancelled();
                }
            }
        }

        List<CLine> _lstMirrors = new List<CLine>();
        NativeMethods.WinPoint ptPrev = new NativeMethods.WinPoint();
        public BounceFrame(
            CancellationTokenSource cts,
            IntPtr hbrBackground
            ) : base(hbrBackground)
        {
            _nDelay = 10;
            this._cts = cts;
        }
        void DrawMirrors()
        {
            var hdc = NativeMethods.GetDC(_hwnd);
            NativeMethods.SelectObject(hdc, _clrLines);
            foreach (var line in _lstMirrors)
            {
                NativeMethods.MoveToEx(hdc, (int)(xScale * line.pt0.X), (int)(yScale * line.pt0.Y), ref ptPrev);
                NativeMethods.LineTo(hdc, (int)(xScale * line.pt1.X), (int)(yScale * line.pt1.Y));
            }
            NativeMethods.ReleaseDC(_hwnd, hdc);
        }
        void DoReflecting()
        {
            var hdc = NativeMethods.GetDC(_hwnd);
            int nBounces = 0;
            while (!_cts.IsCancellationRequested
                //&& nBounces++ < _maxBounces
                )
            {
                if (_nDelay > 0)
                {
                    Thread.Sleep(_nDelay);
                }
                lock (_lstMirrors)
                {
                    // for each line determine the intersection of our light vector incident line, which is just a segment
                    // if it's behind, ignore it
                    var lnIncident = new CLine(_ptLight, new Point(_ptLight.X + _vecLight.X, _ptLight.Y + _vecLight.Y));

                    CLine lnMirror = null;
                    double minDist = double.MaxValue;
                    Point? ptTarget = null;
                    foreach (var line in _lstMirrors)
                    {
                        var ptIntersect = lnIncident.IntersectingPoint(line);
                        if (ptIntersect.HasValue)
                        {
                            // the incident line intersects the mirror. Our mirrors have non-infinite width
                            // let's see if the intersection point is within the mirror's edges
                            if (line.pt0.DistanceFromPoint(ptIntersect.Value) + ptIntersect.Value.DistanceFromPoint(line.pt1) - line.LineLength < .001)
                            //if (line.pt0.X <= ptIntersect.Value.X && ptIntersect.Value.X <= line.pt1.X &&
                            //    line.pt0.Y <= ptIntersect.Value.Y && ptIntersect.Value.Y <= line.pt1.Y
                            //    ||
                            //    line.pt0.X >= ptIntersect.Value.X && ptIntersect.Value.X >= line.pt1.X &&
                            //    line.pt0.Y >= ptIntersect.Value.Y && ptIntersect.Value.Y >= line.pt1.Y
                            //    )
                            {
                                var ss = Math.Sign(_vecLight.X);
                                var s2 = Math.Sign(ptIntersect.Value.X - _ptLight.X);
                                if (ss * s2 == 1) // in our direction?
                                {
                                    var dist = _ptLight.DistanceFromPoint(ptIntersect.Value);

                                    if (dist > .001 && dist < minDist)
                                    {
                                        minDist = dist;
                                        lnMirror = line;
                                        ptTarget = ptIntersect.Value;
                                    }
                                }
                            }
                        }
                        else
                        {
                            //                            Debug.Assert(false, "parallel");
                        }
                    }
                    //                    Debug.Assert(closestLine != null, "no closest line");
                    if (lnMirror != null)
                    {
                        // now draw incident line from orig pt to intersection
                        NativeMethods.SelectObject(hdc, _clrFill);
                        NativeMethods.MoveToEx(hdc, (int)(xScale * _ptLight.X), (int)(yScale * _ptLight.Y), ref ptPrev);
                        NativeMethods.LineTo(hdc, (int)(xScale * ptTarget.Value.X), (int)(yScale * ptTarget.Value.Y));
                        // now reflect vector
                        if (lnMirror.deltaX == 0) // vertical line
                        {
                            _vecLight.X = -_vecLight.X;
                        }
                        else if (lnMirror.deltaY == 0) // horiz line
                        {
                            _vecLight.Y = -_vecLight.Y;
                        }
                        else
                        {
                            //// create incident line endpoint to intersection with correct seg length
                            //var lnIncident = new CLine(_ptLight, ptTarget.Value);
                            //var angBetween = lnIncidentTest.angleBetween(lnMirror);
                            //var angClosest = Math.Atan(lnMirror.slope) / _piOver180;
                            //var angIncident = Math.Atan(lnIncidentTest.slope) / _piOver180;
                            //var angReflect = 2 * angClosest - angIncident;
                            var newSlope = Math.Tan(2 * Math.Atan(lnMirror.slope) - Math.Atan(lnIncident.slope));
                            // now we have the slope of the desired reflection line: 
                            // now we need to determine the reflection direction (x & y) along the slope
                            // The incident line came from one side (half plane) of the mirror. We need to leave on the same side.
                            // to do so, we assume we're going in a particular direction
                            // then we create a test point using the new slope
                            // we see which half plane the test point is in relation to the mirror.
                            // and which half plane the light source is. If they're different, we reverse the vector

                            // first set the new vector to the new slope in a guessed direction. 
                            _vecLight.X = SpeedMult;
                            _vecLight.Y = SpeedMult * newSlope;
                            // create a test point along the line of reflection
                            var ptTest = new Point(ptTarget.Value.X + _vecLight.X, ptTarget.Value.Y + _vecLight.Y);
                            var halfplaneLight = lnMirror.LeftHalf(_ptLight);
                            var halfplaneTestPoint = lnMirror.LeftHalf(ptTest);
                            if (halfplaneLight ^ halfplaneTestPoint) // xor
                            {
                                _vecLight.X = -SpeedMult;
                                _vecLight.Y = -_vecLight.Y;
                            }
                        }
                        // now set new pt 
                        _ptLight = ptTarget.Value;
                    }
                }
            }
            NativeMethods.ReleaseDC(_hwnd, hdc);
        }

        void EnsureCancelled()
        {
            while (_tskDrawing != null && !(_tskDrawing.IsCanceled || _tskDrawing.IsCompleted))
            {
                _cts.Cancel();
                Thread.Sleep(10);
            }
        }
        public override void OnReady(IntPtr hwnd)
        {
            EnsureCancelled();
        }

        private void StartDrawingTask()
        {
            _cts = new CancellationTokenSource();
            _tskDrawing = Task.Run(() =>
            {
                DoReflecting();
            });
        }

        void AddLine(CLine line)
        {
            lock (_lstMirrors)
            {
                _lstMirrors.Add(line);
            }
        }
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            btnClear(sizeInfo.NewSize);
        }

        public void btnClear(Size newSize)
        {
            this.EraseRect();
            var mrg = 4;
            var ptTopLeft = new Point(mrg, mrg);
            var ptTopRight = new Point(newSize.Width - 1 - mrg, mrg);
            var ptBotLeft = new Point(mrg, newSize.Height - 1 - mrg);
            var ptBotRight = new Point(newSize.Width - 1 - mrg, newSize.Height - 1 - mrg);
            _ptOldMouseDown = null;
            _fPenDown = false;
            _fPenModeDrag = false;
            _ptLight = new Point(mrg * 2, mrg * 2);
            _vecLight = new Vector(10, 1);
            lock (this._lstMirrors)
            {
                this._lstMirrors.Clear();
                this._lstMirrors.Add(new CLine(ptTopLeft, ptTopRight));
                this._lstMirrors.Add(new CLine(ptTopRight, ptBotRight));

                //this._lstMirrors.Add(new CLine(ptBotLeft, ptBotRight));
                this._lstMirrors.Add(new CLine(ptBotRight, ptBotLeft));

                this._lstMirrors.Add(new CLine(ptTopLeft, ptBotLeft));

                // diagonal:
                //this._lstMirrors.Add(new CLine(ptBotLeft, ptTopRight));
                //this._lstMirrors.Add(new CLine(new Point(newSize.Width - 10, 0), new Point(newSize.Width - 9, 400)));
                //var l1 = new CLine(new Point(0, 0), new Point(newSize.Height - 1, newSize.Width - 1));
                //this._lstMirrors.Add(l1);
            }
            DrawMirrors();
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
                    DrawMirrors();
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
                        DrawMirrors();
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
                    DrawMirrors();
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
                DrawMirrors();
            }
        }

        public class CLine
        {
            public Point pt0 { get; private set; }
            public Point pt1 { get; private set; }
            Lazy<double> lazyLineSegLength;
            public double LineLength => lazyLineSegLength.Value;

            public CLine(Point p0, Point p1)
            {
                this.pt0 = p0;
                this.pt1 = p1;
                lazyLineSegLength = new Lazy<double>(() =>
                    Math.Sqrt((pt1.Y - pt0.Y).squared() + (pt1.X - pt0.X).squared()),
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
                var x1 = this.pt0.X;
                var y1 = this.pt0.Y;
                var x2 = this.pt1.X;
                var y2 = this.pt1.Y;
                var x3 = otherLine.pt0.X;
                var y3 = otherLine.pt0.Y;
                var x4 = otherLine.pt1.X;
                var y4 = otherLine.pt1.Y;
                var denom = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);
                if (denom != 0)
                {
                    var x = ((x1 * y2 - y1 * x2) * (x3 - x4) - (x1 - x2) * (x3 * y4 - y3 * x4)) / denom;
                    var y = ((x1 * y2 - y1 * x2) * (y3 - y4) - (y1 - y2) * (x3 * y4 - y3 * x4)) / denom;
                    result = new Point(x, y);
                }
                return result;
            }

            public double DistanceToPoint(Point pt)
            {
                double dist = Math.Abs((pt1.Y - pt0.Y) * pt.X - (pt1.X - pt0.X) * pt.Y + pt1.X * pt0.Y - pt1.Y * pt0.X) /
                    Math.Sqrt((pt1.Y - pt0.Y).squared() + (pt1.X - pt0.X).squared());
                return dist;
            }
            public double YIntercept()
            {
                var yint1 = pt0.Y - this.slope * pt0.X;
                return yint1;
            }

            /// <summary>
            /// given a point ptTest, see if it's on the "left" or Not of the plane
            /// </summary>
            /// <param name="ptTest"></param>
            /// <returns></returns>
            public bool LeftHalf(Point c)
            {
                var a = pt0;
                var b = pt1;
                var res = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
                return res > 0;
            }

            public override string ToString()
            {
                return $"({pt0.X:n1},{pt0.Y:n1}),({pt1.X:n1},{pt1.Y:n1})";
            }
            internal double CalculateDistanceFromPoint(Point ptLight)
            {
                double numerator = Math.Abs((pt1.Y - pt0.Y) * ptLight.X - (pt1.X - pt0.X) * ptLight.Y + pt1.X * pt0.Y - pt1.Y * pt0.X);
                return numerator / lazyLineSegLength.Value;
            }

        }
    }
    static class ExtensionMethods
    {
        public static double squared(this double d1)
        {
            return d1 * d1;
        }
        public static double DistanceFromPoint(this Point pt, Point otherPoint)
        {
            var xd = pt.X - otherPoint.X;
            var yd = pt.Y - otherPoint.Y;
            return Math.Sqrt(xd * xd + yd * yd);
        }
        public static Point Add(this Point pt, Point other)
        {
            Point res = new Point(pt.X + other.X, pt.Y + other.Y);
            return res;
        }

    }
}