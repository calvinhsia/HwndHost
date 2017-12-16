using hWndHost;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
        static TextBox _tbxStatus;

        public ReflectWindow()
        {
            InitializeComponent();
            this.Top = 0;
            this.Left = 0;
            this.Title = "Reflect";
            this.WindowState = WindowState.Maximized;
            this.Loaded += Reflect_Loaded;
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
            <CheckBox Content=""ChangeColor"" 
                IsChecked= ""{Binding Path=ChangeColor}"" />
            <Button 
                Name=""btnClear"" 
                Content=""_Clear"" 
                ToolTip=""Clear all, but keep the user drawn mirrors""
                Margin=""10,2,0,0"" 
                />
            <Button 
                Name=""btnClearMirrors"" 
                Content=""Clear_Mirrors"" 
                ToolTip=""Clear the user drawn mirrors (Ctrl-Z will undo one at a time)""
                Margin=""10,2,0,0"" 
                />
            <Label Content=""Delay""/>
            <StackPanel Orientation=""Horizontal"">
                <l:MyTextBox 
                    Text =""{Binding Path=nDelay}"" 
                    Name=""txtDelay""
                    Width=""50""
                    ToolTip=""Delay between line drawing in msecs. Changes by 10%"" />
                <StackPanel Orientation=""Vertical"">
                    <Button
                        Name=""DelayUp""
                        Content=""á""
                        FontFamily=""WingDings""
                        FontSize=""8""
                        Width=""16""
                        Height=""10""
                        />
                    <Button
                        Name=""DelayDn""
                        Content=""â""
                        FontFamily=""WingDings""
                        FontSize=""8""
                        Width=""16""
                        Height=""10""
                        />
                </StackPanel>
            </StackPanel>
                <Label Content=""PenWidth""/>
                <l:MyTextBox 
                    Text =""{Binding Path=nPenWidth}"" 
                    Width=""50""
                    ToolTip=""Width of reflection pen"" />

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
        FontFamily=""Courier New""
        FontSize = ""10""
        VerticalAlignment=""Top"" 
        Width=""820""/>
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

                var strReader = new System.IO.StringReader(strxaml);
                var xamlreader = XmlReader.Create(strReader);
                var grid = (Grid)(XamlReader.Load(xamlreader));
                this.Content = grid;
                _tbxStatus = (TextBox)grid.FindName("tbxStatus");
                var btnQuit = (Button)grid.FindName("btnQuit");
                btnQuit.Click += (ob, eb) =>
                {
                    App.Current.Shutdown();
                };
                var bounceFrame = new BounceFrame(
                    NativeMethods.CreateSolidBrush(new IntPtr(0xfff0f0)));
                grid.DataContext = bounceFrame;
                var btnClear = (Button)grid.FindName("btnClear");
                btnClear.Click += (ob, eb) =>
                {
                    bounceFrame.Clear(fKeepUserMirrors: true);
                };
                var btnClearMirrors = (Button)grid.FindName("btnClearMirrors");
                btnClearMirrors.Click += (ob, eb) =>
                {
                    bounceFrame.Clear(fKeepUserMirrors: false);
                };
                var btnDelayUp = (Button)grid.FindName("DelayUp");
                Func<int> CalcDelayInc = () =>
                  {
                      var inc = 1;
                      if (bounceFrame.nDelay > 0)
                      {
                          //inc = (int)Math.Pow(10, (int)Math.Log10(bounceFrame.nDelay) - 1); // logarithmic
                          inc = (int)(.5 * bounceFrame.nDelay); // 50%
                          if (inc == 0)
                          {
                              inc = 1;
                          }
                      }
                      return inc;
                  };
                btnDelayUp.Click += (ob, eb) =>
                 {
                     bounceFrame.nDelay += CalcDelayInc();
                 };
                var btnDelayDn = (Button)grid.FindName("DelayDn");
                btnDelayDn.Click += (ob, eb) =>
                {
                    bounceFrame.nDelay -= CalcDelayInc();
                    if (bounceFrame.nDelay < 0)
                    {
                        bounceFrame.nDelay = 0;
                    }
                };
                var userCtrl = (UserControl)grid.FindName("MyUserControl");
                userCtrl.Content = bounceFrame;
                this.Closing += (oc, ec) =>
                {
                    bounceFrame.IsRunning = false;
                };
                //// forward mouse events to the hWndHost control
                this.MouseDown += (om, em) => bounceFrame.DoMouseDown(om, em);
                this.MouseMove += (om, em) => bounceFrame.DoMouseMove(om, em);
                this.MouseUp += (om, em) => bounceFrame.DoMouseUp(om, em);
                this.KeyDown += (ok, ek) => bounceFrame.DoKeyDown(ok, ek);
            }
            catch (Exception ex)
            {
                this.Content = ex.ToString();
            }
        }
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
        }
        public static void AddStatusMessage(string msg, params object[] parms)
        {
            var txt = string.Format(msg, parms);
            _tbxStatus.Dispatcher.BeginInvoke(new Action(() =>
            {
                _tbxStatus.Text = txt;
            }));
        }
    }

    public class BounceFrame : MyHwndHost, INotifyPropertyChanged
    {
        IntPtr _clrMirror = NativeMethods.CreatePen(nPenStyle: 0, nWidth: 2, nColor: (IntPtr)0x00);
        IntPtr _clrFillReflection = NativeMethods.CreatePen(nPenStyle: 0, nWidth: 0, nColor: (IntPtr)0xff);
        int _colorReflection;
        Random _rand = new Random(1);

        CancellationTokenSource _cts;
        const int SpeedMult = 1000;
        Point _ptLight;
        Vector _vecLight;
        //double _distLineThresh = 1;
        const double _piOver180 = Math.PI / 180;
        Task _tskDrawing;
        int _nDelay = 0;
        int _nOutofBounds = 0;
        public int nDelay { get { return _nDelay; } set { _nDelay = value; RaisePropChanged(); } }

        int _nPenWidth = 1;
        public int nPenWidth { get { return _nPenWidth; } set { _nPenWidth = value; RaisePropChanged(); } }
        public bool ChangeColor { get; set; } = true;

        bool _isRunning;
        List<CLine> _lstMirrors = new List<CLine>();
        NativeMethods.WinPoint _ptPrev = new NativeMethods.WinPoint();
        public BounceFrame(
            IntPtr hbrBackground
            ) : base(hbrBackground)
        {
            this._cts = new CancellationTokenSource();
            Clear(fKeepUserMirrors: false);
        }
        void DrawMirrors()
        {
            var hdc = NativeMethods.GetDC(_hwnd);
            NativeMethods.SelectObject(hdc, _clrMirror);
            lock (_lstMirrors)
            {
                foreach (var mirror in _lstMirrors)
                {
                    NativeMethods.MoveToEx(hdc, (int)(xScale * mirror.pt0.X), (int)(yScale * mirror.pt0.Y), ref _ptPrev);
                    NativeMethods.LineTo(hdc, (int)(xScale * mirror.pt1.X), (int)(yScale * mirror.pt1.Y));
                }
                NativeMethods.ReleaseDC(_hwnd, hdc);
            }
        }
        void ChooseRandomStartingRay()
        {
            _ptLight.X = _rand.Next((int)(this.ActualWidth));
            _ptLight.Y = _rand.Next((int)(this.ActualHeight));
            _vecLight.X = 1;
            _vecLight.Y = _rand.NextDouble();
        }
        void DoReflecting()
        {
            var hDC = NativeMethods.GetDC(_hwnd);
            int nBounces = 0;
            int nLastBounceWhenStagnant = 0;
            _nOutofBounds = 0;
            var sw = new Stopwatch();
            sw.Start();
            while (!_cts.IsCancellationRequested
                //&& nBounces++ < _maxBounces
                )
            {
                if (nBounces++ % 1000 == 0)
                {
                    int bouncesPerSecond = 0;
                    if (sw.ElapsedMilliseconds > 0)
                    {
                        bouncesPerSecond = (int)(1000 * nBounces / sw.ElapsedMilliseconds);
                    }
                    ReflectWindow.AddStatusMessage(
                        $"# Lines= {_lstMirrors.Count} bounces = {nBounces:n0}" +
                        $" ({_ptLight.X,8:n1},{_ptLight.Y,8:n1}) ({_vecLight.X,8:n4},{_vecLight.Y,8:n4})" +
                        $" OOB={_nOutofBounds}" +
                        $" B/S={bouncesPerSecond}");
                }
                if (nDelay > 0)
                {
                    Thread.Sleep(nDelay);
                }
                // for each line determine the intersection of our light vector incident line, which is just a segment
                // if it's behind, ignore it
                var lnIncident = new CLine(_ptLight, new Point(_ptLight.X + _vecLight.X, _ptLight.Y + _vecLight.Y));

                CLine lnMirror = null;
                double minDist = double.MaxValue;
                Point? ptIntersect = null; // the point of intersection of the light vector and the closest mirror
                lock (_lstMirrors)
                {
                    foreach (var mirror in _lstMirrors)
                    {
                        var ptIntersectTest = mirror.IntersectingPoint(_ptLight, _vecLight);
                        if (ptIntersectTest.HasValue)
                        {
                            var dist = _ptLight.DistanceFromPoint(ptIntersectTest.Value);
                            if (dist > .001 && dist < minDist)
                            {
                                minDist = dist;
                                lnMirror = mirror;
                                ptIntersect = ptIntersectTest.Value;
                            }
                        }
                    }
                }
                //                                        Debug.Assert(lnMirror != null, "no closest mirror");
                if (lnMirror == null)
                {
                    //ReflectWindow.AddStatusMessage($"No closest mirror pt = {_ptLight} vec = {_vecLight}");
                    if (nLastBounceWhenStagnant == nBounces - 1)
                    {// both the last bounce and this bounce were stagnant
                        nLastBounceWhenStagnant = nBounces;
                        _nOutofBounds++;
                        ChooseRandomStartingRay();
                    }
                    else
                    {
                        _vecLight.X = -_vecLight.X;
                        nLastBounceWhenStagnant = nBounces;
                    }
                    continue;
                    //_cts.Cancel();
                    //IsRunning = false;
                    //break;
                }
                // now draw incident line from orig pt to intersection
                NativeMethods.SelectObject(hDC, _clrFillReflection);
                if (nBounces == 1)
                {
                    NativeMethods.MoveToEx(hDC, (int)(xScale * _ptLight.X), (int)(yScale * _ptLight.Y), ref _ptPrev);
                }
                NativeMethods.LineTo(hDC, (int)(xScale * ptIntersect.Value.X), (int)(yScale * ptIntersect.Value.Y));
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
                    var ptTest = new Point(ptIntersect.Value.X + _vecLight.X, ptIntersect.Value.Y + _vecLight.Y);
                    var halfplaneLight = lnMirror.LeftHalf(_ptLight);
                    var halfplaneTestPoint = lnMirror.LeftHalf(ptTest);
                    if (halfplaneLight ^ halfplaneTestPoint) // xor
                    {
                        _vecLight.X = -_vecLight.X;
                        _vecLight.Y = -_vecLight.Y;
                    }
                }
                // now set new pt 
                _ptLight = ptIntersect.Value;
                SetColor((_colorReflection + 1) & 0xffffff);
            }
            NativeMethods.ReleaseDC(_hwnd, hDC);
        }

        void SetColor(int color)
        {
            if (ChangeColor)
            {
                _colorReflection = color;
                NativeMethods.DeleteObject(_clrFillReflection);
                _clrFillReflection = NativeMethods.CreatePen(nPenStyle: 0, nWidth: _nPenWidth, nColor: (IntPtr)_colorReflection);
            }
            else
            {
                if (_colorReflection != 0xff) // red
                {
                    _colorReflection = 0xff;
                    NativeMethods.DeleteObject(_clrFillReflection);
                    _clrFillReflection = NativeMethods.CreatePen(nPenStyle: 0, nWidth: _nPenWidth, nColor: (IntPtr)_colorReflection);
                }
            }
        }

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
                RaisePropChanged();
            }
        }

        void EnsureCancelled()
        {
            while (_tskDrawing != null && !(_tskDrawing.IsCanceled || _tskDrawing.IsCompleted))
            {
                _cts.Cancel();
                //App.Current.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => { })).Wait(); ;
                Thread.Sleep(10);
            }
        }
        public override void OnReady(IntPtr hwnd)
        {
            IsRunning = false;
            Clear(fKeepUserMirrors: true);
        }

        private void StartDrawingTask()
        {
            // do drawing in a background thread
            _cts = new CancellationTokenSource();
            _tskDrawing = Task.Run(() =>
            {
                DoReflecting();
            });
        }

        void AddLine(CLine mirror)
        {
            lock (_lstMirrors)
            {
                _lstMirrors.Add(mirror);
            }
        }
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            //            btnClear(sizeInfo.NewSize);
            Clear(fKeepUserMirrors: false);
        }

        public void Clear(bool fKeepUserMirrors)
        {
            var newSize = new Size(this.ActualWidth, this.ActualHeight);

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
            _vecLight = new Vector(10, 10);
            _nOutofBounds = 0;
            if (!fKeepUserMirrors)
            {
                lock (this._lstMirrors)
                {
                    this._lstMirrors.Clear();
                    this._lstMirrors.Add(new CLine(ptTopLeft, ptTopRight));
                    this._lstMirrors.Add(new CLine(ptTopRight, ptBotRight));
                    this._lstMirrors.Add(new CLine(ptBotRight, ptBotLeft));
                    this._lstMirrors.Add(new CLine(ptTopLeft, ptBotLeft));
                }
            }
            DrawMirrors();
        }

        Point? _ptOldMouseDown;
        Point _ptCurrentMouseDown;
        bool _fPenModeDrag = false;
        bool _fPenDown = false;

        public event PropertyChangedEventHandler PropertyChanged;
        void RaisePropChanged([CallerMemberName] string propName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }
        //const int WM_MOUSEMOVE = 0x200;
        //const int WM_LBUTTONDOWN = 0x201;
        //const int WM_LBUTTONUP = 0x202;
        //const int WM_RBUTTONDOWN = 0x204;
        //const int WM_RBUTTONUP = 0x205;

        //protected override IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        //{
        //    switch (msg)
        //    {
        //        case WM_MOUSEMOVE:
        //            var x = 2;
        //            break;
        //        case WM_LBUTTONDOWN:
        //            var x2 = 2;
        //            break;
        //        case WM_LBUTTONUP:
        //            var x3 = 2;
        //            break;
        //    }
        //    return base.WndProc(hwnd, msg, wParam, lParam, ref handled);
        //}

        void ShowMouseStatus()
        {
            ReflectWindow.AddStatusMessage($"PenDown={_fPenDown} Drag={_fPenModeDrag} OldPt={_ptOldMouseDown}  CurPt={_ptCurrentMouseDown}");
        }
        internal void DoMouseDown(object om, MouseButtonEventArgs em)
        {
            if (em.RightButton == MouseButtonState.Pressed)
            {
                _fPenModeDrag = !_fPenModeDrag;
                _ptOldMouseDown = em.GetPosition(this);
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
                    //                    DrawMirrors();
                }
            }
            ShowMouseStatus();
        }

        internal void DoMouseMove(object om, MouseEventArgs em)
        {
            if (_fPenModeDrag)
            {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    if (_ptOldMouseDown.HasValue)
                    {
                        _ptCurrentMouseDown = em.GetPosition(this);
                        if (_ptCurrentMouseDown != _ptOldMouseDown)
                        {
                            var line = new CLine(_ptOldMouseDown.Value, _ptCurrentMouseDown);
                            AddLine(line);
                            _ptOldMouseDown = _ptCurrentMouseDown;
                            DrawMirrors();
                        }
                    }
                }
                else
                {
                    _ptOldMouseDown = null;
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
                    //                    DrawMirrors();
                }
            }
            ShowMouseStatus();
        }

        internal void DoMouseUp(object om, MouseButtonEventArgs em)
        {
            if (_fPenDown)
            {
                _ptCurrentMouseDown = em.GetPosition(this);
                if (_ptOldMouseDown.HasValue && _ptCurrentMouseDown != _ptOldMouseDown)
                {
                    var line = new CLine(_ptOldMouseDown.Value, _ptCurrentMouseDown);
                    AddLine(line);
                    _ptOldMouseDown = _ptCurrentMouseDown;
                    _fPenDown = false;
                    DrawMirrors();
                }
            }
            ShowMouseStatus();
        }

        internal void DoKeyDown(object ok, KeyEventArgs ek)
        {
            switch (ek.Key)
            {
                case Key.Z:
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                    {
                        // keep 4 walls
                        lock (_lstMirrors)
                        {
                            if (_lstMirrors.Count > 4)
                            {
                                var lastMirror = _lstMirrors.Last();
                                _lstMirrors.RemoveAt(_lstMirrors.Count - 1);
                                Clear(fKeepUserMirrors: true);
                                _ptOldMouseDown = lastMirror.pt1;
                                ShowMouseStatus();
                            }
                        }
                    }
                    break;
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
                var denom = (this.pt0.X - this.pt1.X) * (otherLine.pt0.Y - otherLine.pt1.Y) - (this.pt0.Y - this.pt1.Y) * (otherLine.pt0.X - otherLine.pt1.X);

                if (denom != 0)
                {
                    var x = ((this.pt0.X * this.pt1.Y - this.pt0.Y * this.pt1.X) * (otherLine.pt0.X - otherLine.pt1.X) - (this.pt0.X - this.pt1.X) * (otherLine.pt0.X * otherLine.pt1.Y - otherLine.pt0.Y * otherLine.pt1.X)) / denom;
                    var y = ((this.pt0.X * this.pt1.Y - this.pt0.Y * this.pt1.X) * (otherLine.pt0.Y - otherLine.pt1.Y) - (this.pt0.Y - this.pt1.Y) * (otherLine.pt0.X * otherLine.pt1.Y - otherLine.pt0.Y * otherLine.pt1.X)) / denom;
                    result = new Point(x, y);
                }
                return result;
            }
            /// <summary>
            /// Given a point and a vector direction, determine the point of intersection if any
            /// </summary>
            /// <returns></returns>
            internal Point? IntersectingPoint(Point ptLight, Vector vecLight)
            {
                Point? ptIntersect = null;
                var lnIncident = new CLine(ptLight, new Point(ptLight.X + vecLight.X, ptLight.Y + vecLight.Y));
                Point? ptIntersectTest = this.IntersectingPoint(lnIncident);
                // the incident line intersects the mirror. Our mirrors have finite width
                // let's see if the intersection point is within the mirror's edges
                if (ptIntersectTest.HasValue)
                {
                    var distPt0 = this.pt0.DistanceFromPoint(ptIntersectTest.Value);
                    var distPt1 = ptIntersectTest.Value.DistanceFromPoint(this.pt1);
                    var thislinelen = this.LineLength;
                    if (distPt0 + distPt1 - thislinelen < .00001)
                    {
                        var ss = Math.Sign(vecLight.X);
                        var s2 = Math.Sign(ptIntersectTest.Value.X - ptLight.X);
                        if (ss * s2 == 1) // in our direction?
                        {
                            ptIntersect = ptIntersectTest.Value;
                        }
                    }
                }
                return ptIntersect;
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
        }
    }
    // a textbox that selects all when focused:
    public class MyTextBox : TextBox
    {
        public MyTextBox()
        {
            this.GotFocus += (o, e) =>
            {
                this.SelectAll();
            };
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
            return Math.Sqrt(xd.squared() + yd.squared());
        }
        public static Point Add(this Point pt, Point other)
        {
            Point res = new Point(pt.X + other.X, pt.Y + other.Y);
            return res;
        }

    }
}