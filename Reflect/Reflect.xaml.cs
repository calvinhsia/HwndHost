using hWndHost;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
            //            this.WindowState = WindowState.Maximized;
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
            <Label Content=""_Delay"" Target=""txtDelay""/>
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
        const double epsilon = .0001;
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
        internal static BounceFrame _instance;
        List<CMirror> _lstMirrors = new List<CMirror>();
        NativeMethods.WinPoint _ptPrev = new NativeMethods.WinPoint();
        public BounceFrame(
            IntPtr hbrBackground
            ) : base(hbrBackground)
        {
            _instance = this;
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
                    if (mirror.MirrorType == CMirror.MirrorTypes.MirrorTypeLine)
                    {
                        CLine line = mirror._line;
                        NativeMethods.MoveToEx(hdc, (int)(xScale * line.pt0.X), (int)(yScale * line.pt0.Y), ref _ptPrev);
                        NativeMethods.LineTo(hdc, (int)(xScale * line.pt1.X), (int)(yScale * line.pt1.Y));
                    }
                    else
                    {
                        var ellipse = mirror._ellipse;
                        nativeMethods.Arc(hdc,
                            (int)(xScale * ellipse.ptTopLeft.X), (int)(yScale * ellipse.ptTopLeft.Y),
                            (int)(xScale * ellipse.ptBotRight.X), (int)(yScale * ellipse.ptBotRight.Y),
                            (int)(xScale * ellipse.ptStartArc.X), (int)(yScale * ellipse.ptStartArc.Y),
                            (int)(xScale * ellipse.ptEndArc.X), (int)(yScale * ellipse.ptEndArc.Y));
                        //nativeMethods.Arc(hdc,
                        //    (int)(xScale * ellipse.ptTopLeft.X), (int)(yScale * ellipse.ptTopLeft.Y),
                        //    (int)(xScale * ellipse.ptBotRight.X), (int)(yScale * ellipse.ptBotRight.Y),
                        //    (int)(xScale * (ellipse.ptTopLeft.X + ellipse.Width)), (int)(yScale * (ellipse.ptTopLeft.Y + ellipse.Height / 2)),
                        //    (int)(xScale * (ellipse.ptTopLeft.X)), (int)(yScale * (ellipse.ptTopLeft.Y + ellipse.Height / 2)));
                    }
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

                CMirror mirrorClosest = null;
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
                            if (dist > epsilon && dist < minDist)
                            {
                                minDist = dist;
                                mirrorClosest = mirror;
                                ptIntersect = ptIntersectTest.Value;
                            }
                        }
                    }
                }
                if (mirrorClosest == null)
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
                }
                // now draw incident line from orig pt to intersection
                NativeMethods.SelectObject(hDC, _clrFillReflection);
                if (nBounces == 1)
                {
                    NativeMethods.MoveToEx(hDC, (int)(xScale * _ptLight.X), (int)(yScale * _ptLight.Y), ref _ptPrev);
                }
                NativeMethods.LineTo(hDC, (int)(xScale * ptIntersect.Value.X), (int)(yScale * ptIntersect.Value.Y));
                // now reflect vector
                _vecLight = mirrorClosest.Reflect(_ptLight, _vecLight, ptIntersect.Value);
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

        void DrawLine(CLine line)
        {
            var hDC = NativeMethods.GetDC(_hwnd);
            NativeMethods.WinPoint ptPrev = new NativeMethods.WinPoint();
            NativeMethods.MoveToEx(hDC, (int)(xScale * line.pt0.X), (int)(yScale * line.pt0.Y), ref ptPrev);
            var old = NativeMethods.SelectObject(hDC, _clrMirror);
            NativeMethods.LineTo(hDC, (int)(xScale * line.pt1.X), (int)(yScale * line.pt1.Y));
            // restore
            NativeMethods.SelectObject(hDC, old);
            NativeMethods.MoveToEx(hDC, ptPrev.x, ptPrev.y, ref ptPrev);
            NativeMethods.ReleaseDC(_hwnd, hDC);
        }

        void AddMirror(CMirror mirror)
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
            var mrg = 8;
            var ptTopLeft = new Point(mrg, mrg);
            var ptTopRight = new Point(newSize.Width - 1 - mrg, mrg);
            var ptBotLeft = new Point(mrg, newSize.Height - 1 - mrg);
            var ptBotRight = new Point(newSize.Width - 1 - mrg, newSize.Height - 1 - mrg);
            _ptOldMouseDown = null;
            _fPenDown = false;
            _fPenModeDrag = false;
            _ptLight = new Point(140, 140);
            _vecLight = new Vector(10, 10);
            _nOutofBounds = 0;
            _colorReflection = 0;
            if (!fKeepUserMirrors)
            {
                lock (this._lstMirrors)
                {
                    this._lstMirrors.Clear();
                    this._lstMirrors.Add(new CMirror(new CLine(ptTopLeft, ptTopRight)));
                    this._lstMirrors.Add(new CMirror(new CLine(ptTopRight, ptBotRight)));
                    this._lstMirrors.Add(new CMirror(new CLine(ptBotRight, ptBotLeft)));
                    this._lstMirrors.Add(new CMirror(new CLine(ptTopLeft, ptBotLeft)));

                    var distBetweenEllipses = 80;
                    var ellipseTopLeft = new Point(mrg + 10, mrg + 20);
                    var ellipseBotRight = new Point(newSize.Width - mrg * 2, newSize.Height - distBetweenEllipses);
                    var ellipse = new CEllipse(
                        ellipseTopLeft,
                        ellipseBotRight,
                        new Point(0, 0),
                        new Point(0, 0)
                    );
                    _ptLight = new Point(ellipse.Center.X - ellipse.f, ellipse.Center.Y);
                    //var ellipse = new CEllipse(
                    //    ellipseTopLeft,
                    //    ellipseBotRight,
                    //    new Point(20 + 800 - 20, 20 + 400 / 2 - 20),
                    //    new Point(20, 20 + 400 / 2)
                    //);
                    this._lstMirrors.Add(new CMirror(ellipse));
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
                            AddMirror(new CMirror(line));
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
                    AddMirror(new CMirror(line));
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
                        lock (_lstMirrors)
                        {
                            // keep 4 walls
                            if (_lstMirrors.Count > 4)
                            {
                                var lastMirror = _lstMirrors.Last();
                                _lstMirrors.RemoveAt(_lstMirrors.Count - 1);
                                Clear(fKeepUserMirrors: true);
                                _ptOldMouseDown = lastMirror._line.pt1;
                                ShowMouseStatus();
                            }
                        }
                    }
                    break;
            }
        }

        public class CMirror
        {
            public enum MirrorTypes
            {
                MirrorTypeLine,
                MirrorTypeEllipse
            }
            public CLine _line;
            public CEllipse _ellipse;
            public MirrorTypes MirrorType { get { return _line == null ? MirrorTypes.MirrorTypeEllipse : MirrorTypes.MirrorTypeLine; } }
            public CMirror(CLine line)
            {
                this._line = line;
            }
            public CMirror(CEllipse ellipse)
            {
                this._ellipse = ellipse;
            }

            internal Point? IntersectingPoint(Point ptLight, Vector vecLight)
            {
                if (this.MirrorType == MirrorTypes.MirrorTypeLine)
                {
                    return this._line.IntersectingPoint(ptLight, vecLight);
                }
                return this._ellipse.IntersectingPoint(ptLight, vecLight);
            }

            internal Vector Reflect(Point ptLight, Vector vecLight, Point ptIntersect)
            {
                if (this.MirrorType == MirrorTypes.MirrorTypeLine)
                {
                    return this._line.Reflect(ptLight, vecLight, ptIntersect);
                }
                return this._ellipse.Reflect(ptLight, vecLight, ptIntersect);
            }
            public override string ToString()
            {
                if (this.MirrorType == MirrorTypes.MirrorTypeLine)
                {
                    return $"{_line}";
                }
                return $"{_ellipse}";
            }
        }

        /// <summary>
        /// Defined by a bounding rectangle with 2 points: topleft and bottom right
        /// The arc ends where it intersects the radial from the center of the bounding rectangle to the (nXEndArc, nYEndArc) point
        /// </summary>
        public class CEllipse
        {
            public Point ptTopLeft { get; private set; }
            public Point ptBotRight { get; private set; }
            public Point ptStartArc { get; private set; }
            public Point ptEndArc { get; private set; }
            public double Width { get { return ptBotRight.X - ptTopLeft.X; } }
            public double Height { get { return ptBotRight.Y - ptTopLeft.Y; } }
            public double a { get { return Width / 2; } }
            public double b { get { return Height / 2; } }

            public Point Center { get { return new Point(ptTopLeft.X + Width / 2, ptTopLeft.Y + Height / 2); } }
            public double d1 { get { return Center.X; } }
            public double d2 { get { return Center.Y; } }
            /// <summary>
            /// focus: distance from center to focus. other is negative of this
            /// </summary>
            public double f { get { return Math.Sqrt(Math.Abs(a * a - b * b)); } }
            public CEllipse(Point ptTopLeft, Point ptBotRight, Point ptStartArc, Point ptEndArc)
            {
                this.ptTopLeft = ptTopLeft;
                this.ptBotRight = ptBotRight;
                this.ptStartArc = ptStartArc;
                this.ptEndArc = ptEndArc;
            }
            // https://social.msdn.microsoft.com/Forums/windowsapps/en-US/b599db66-a987-4dba-b5b9-7babc9badc9c/finding-the-intersection-points-of-a-line-and-an-ellipse?forum=wpdevelop

            internal Point? IntersectingPoint(Point ptLight, Vector vecLight)
            {
                Point? ptIntersectResult = null;
                Point? ptIntersect0 = null;
                Point? ptIntersect1 = null;
                var lnIncident = new CLine(ptLight, new Point(ptLight.X + vecLight.X, ptLight.Y + vecLight.Y));
                double A = 0, B = 0, C = 0, m = 0, c = 0;
                if (vecLight.Y != 0) // nonVertical
                {
                    m = lnIncident.slope;
                    c = lnIncident.YIntercept;
                    A = b * b + a * a * m * m;
                    B = 2 * a * a * m * (c - d2) - 2 * b * b * d1;
                    C = b * b * d1 * d1 + a * a * ((c - d2) * (c - d2) - b * b);
                }
                else
                {
                    A = a * a;
                    B = -2 * d2 * a * a;
                    C = -a * a * b * b + b * b * (lnIncident.pt0.X - Center.X) * (lnIncident.pt0.X - Center.X);
                }
                // quadratic formula (-b +- sqrt(b*b-4ac)/2a
                var disc = B * B - 4 * A * C;
                if (disc > 0) // else no intersection
                {
                    var sqt = Math.Sqrt(disc);
                    var x = (-B + sqt) / (2 * A);
                    // we have >0 intersections.
                    if (vecLight.Y != 0) // nonVertical
                    {
                        var y = m * x + c;
                        ptIntersect0 = new Point(x, y);
                        x = (-B - sqt) / (2 * A);
                        y = m * x + c;
                        ptIntersect1 = new Point(x, y);
                    }
                    else
                    {
                        ptIntersect0 = new Point(lnIncident.pt0.X, x);
                        x = (-B - sqt) / (2 * A);
                        ptIntersect1 = new Point(lnIncident.pt0.X, x);
                    }
                    // one of the 2 intersections where the light came froM?
                    if (ptIntersect0.Value.DistanceFromPoint(ptLight) < epsilon)
                    {
                        ptIntersectResult = ptIntersect1;
                    }
                    if (ptIntersect1.Value.DistanceFromPoint(ptLight) < epsilon)
                    {
                        ptIntersectResult = ptIntersect0;
                    }
                    if (!ptIntersectResult.HasValue)
                    {
                        // now determine which point is in the right direction 
                        //(could be both if point started outside ellipse)
                        var ss = Math.Sign(vecLight.X);
                        int s2 = 0;
                        if (ptIntersect0.HasValue)
                        {
                            s2 = Math.Sign(ptIntersect0.Value.X - ptLight.X);
                            if (ss * s2 != 1) // not in our direction?
                            {
                                ptIntersect0 = null;
                            }
                        }
                        if (ptIntersect1.HasValue)
                        {
                            s2 = Math.Sign(ptIntersect1.Value.X - ptLight.X);
                            if (ss * s2 != 1) // not in our direction?
                            {
                                ptIntersect1 = null;
                            }
                        }
                        if (ptIntersect0.HasValue)
                        {
                            if (ptIntersect1.HasValue)
                            {// both: choose closest
                                var dist0 = ptIntersect0.Value.DistanceFromPoint(ptLight);
                                var dist1 = ptIntersect1.Value.DistanceFromPoint(ptLight);
                                if (dist0 < dist1)
                                {
                                    ptIntersectResult = ptIntersect0;
                                }
                                else
                                {
                                    ptIntersectResult = ptIntersect1;
                                }
                            }
                            else
                            {
                                ptIntersectResult = ptIntersect0;
                            }
                        }
                        else
                        {
                            ptIntersectResult = ptIntersect1;
                        }
                    }
                }
                return ptIntersectResult;
            }

            internal Vector Reflect(Point ptLight, Vector vecLight, Point ptIntersect)
            {
                // calculate the slope of the tangent line at that point by differentiation
                var m =- b * b * (ptIntersect.X-Center.X) / (a * a * (ptIntersect.Y - Center.Y));
                //                var m = -b * b * ptIntersect.X / (a * a * ptIntersect.Y);
                // create a vector with the desired slope
                var vec = new Vector()
                {
                    X = SpeedMult,
                    Y = SpeedMult * m
                };
                // create a tangent line
                var lnTangent = new CLine(ptIntersect, new Point(ptIntersect.X + vec.X, ptIntersect.Y + vec.Y));
            //    BounceFrame._instance.DrawLine(lnTangent);
                // now reflect the light off that tangent line
                vecLight = lnTangent.Reflect(ptLight, vecLight, ptIntersect);

                return vecLight;
            }
            public override string ToString()
            {
                return $"EL{ptTopLeft}--{ptBotRight}";
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
            // divide by zero yields double.NaN
            public double slope => deltaY / deltaX;
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

            public double DistanceToPoint(Point pt)
            {
                double dist = Math.Abs((pt1.Y - pt0.Y) * pt.X - (pt1.X - pt0.X) * pt.Y + pt1.X * pt0.Y - pt1.Y * pt0.X) /
                    Math.Sqrt((pt1.Y - pt0.Y).squared() + (pt1.X - pt0.X).squared());
                return dist;
            }
            public double YIntercept
            {
                get
                {
                    var yint1 = pt0.Y - this.slope * pt0.X;
                    return yint1;
                }
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
                    if (distPt0 + distPt1 - thislinelen < epsilon)
                    {
                        if (vecLight.X == 0) // vert
                        {
                            var ss = Math.Sign(vecLight.Y);
                            var s2 = Math.Sign(ptIntersectTest.Value.Y - ptLight.Y);
                            if (ss * s2 == 1) // in our direction?
                            {
                                ptIntersect = ptIntersectTest.Value;
                            }
                        }
                        else  // horiz
                        {
                            var ss = Math.Sign(vecLight.X);
                            var s2 = Math.Sign(ptIntersectTest.Value.X - ptLight.X);
                            if (ss * s2 == 1) // in our direction?
                            {
                                ptIntersect = ptIntersectTest.Value;
                            }
                        }
                    }
                }
                return ptIntersect;
            }

            internal Vector Reflect(Point ptLight, Vector vecLight, Point ptIntersect)
            {
                if (this.deltaX == 0) // vertical line
                {
                    vecLight.X = -vecLight.X;
                }
                else if (this.deltaY == 0) // horiz line
                {
                    vecLight.Y = -vecLight.Y;
                }
                else
                {
                    //// create incident line endpoint to intersection with correct seg length
                    var lnIncident = new CLine(ptLight, new Point(ptLight.X + vecLight.X, ptLight.Y + vecLight.Y));
                    //var lnIncident = new CLine(_ptLight, ptTarget.Value);
                    //var angBetween = lnIncidentTest.angleBetween(this);
                    //var angClosest = Math.Atan(this.slope) / _piOver180;
                    //var angIncident = Math.Atan(lnIncidentTest.slope) / _piOver180;
                    //var angReflect = 2 * angClosest - angIncident;
                    var newSlope = Math.Tan(2 * Math.Atan(this.slope) - Math.Atan(lnIncident.slope));
                    // now we have the slope of the desired reflection line: 
                    // now we need to determine the reflection direction (x & y) along the slope
                    // The incident line came from one side (half plane) of the mirror. We need to leave on the same side.
                    // to do so, we assume we're going in a particular direction
                    // then we create a test point using the new slope
                    // we see which half plane the test point is in relation to the mirror.
                    // and which half plane the light source is. If they're different, we reverse the vector

                    // first set the new vector to the new slope in a guessed direction. 
                    vecLight.X = SpeedMult;
                    vecLight.Y = SpeedMult * newSlope;
                    // create a test point along the line of reflection
                    var ptTest = new Point(ptIntersect.X + vecLight.X, ptIntersect.Y + vecLight.Y);
                    var halfplaneLight = this.LeftHalf(ptLight);
                    var halfplaneTestPoint = this.LeftHalf(ptTest);
                    if (halfplaneLight ^ halfplaneTestPoint) // xor
                    {
                        vecLight.X = -vecLight.X;
                        vecLight.Y = -vecLight.Y;
                    }
                }
                return vecLight;
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

    public partial class nativeMethods
    {
        [DllImport("gdi32")]
        public static extern int Arc(
          IntPtr hdc,
          int nLeftRect,
          int nTopRect,
          int nRightRect,
          int nBottomRect,
          int nXStartArc,
          int nYStartArc,
          int nXEndArc,
          int nYEndArc
        );
    }
}