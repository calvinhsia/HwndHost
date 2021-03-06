﻿using hWndHost;
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
            this.Height = 450;
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
                <Label Content=""NumLasers""/>
                <l:MyTextBox 
                    Text =""{Binding Path=nLasers}"" 
                    Width=""50""
                    ToolTip=""Number of laser lights"" />
                <Label Content=""InitPtX""/>
                <l:MyTextBox 
                    Text =""{Binding Path=InitPtX}"" 
                    Width=""50""
                    ToolTip=""Initial Laser x coord"" />
                <Label Content=""InitPtY""/>
                <l:MyTextBox 
                    Text =""{Binding Path=InitPtY}"" 
                    Width=""50""
                    ToolTip=""Initial Laser Y coord"" />
                <Label Content=""InitDx""/>
                <l:MyTextBox 
                    Text =""{Binding Path=InitDx}"" 
                    Width=""50""
                    ToolTip=""Initial Laser x direction"" />
                <Label Content=""InitDy""/>
                <l:MyTextBox 
                    Text =""{Binding Path=InitDy}"" 
                    Width=""50""
                    ToolTip=""Initial Laser y direction"" />
                <Label Content=""ElDist""/>
                <l:MyTextBox 
                    Text =""{Binding Path=distBetweenEllipses}"" 
                    Width=""50""
                    ToolTip=""Distance between 2 ellipse halves"" />
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
                btnDelayUp.Click += (ob, eb) =>
                {
                    if (bounceFrame.nDelay == 0)
                    {
                        bounceFrame.nDelay = 1;
                    }
                    else
                    {
                        bounceFrame.nDelay *= 8;
                    }
                };
                var btnDelayDn = (Button)grid.FindName("DelayDn");
                btnDelayDn.Click += (ob, eb) =>
                {
                    bounceFrame.nDelay /= 8;
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
        public const int SpeedMult = 1000;
        public const double epsilon = .00001;
        public const double piOver180 = Math.PI / 180;
        public class Laser
        {
            public Point _ptLight;
            public Vector _vecLight;
            public override string ToString()
            {
                return $"{_ptLight} {_vecLight}";
            }
        }
        public List<Laser> _lstLasers;
        //double _distLineThresh = 1;
        Task _tskDrawing;
        int _nDelay = 0;
        int _nOutofBounds = 0;
        public int nDelay { get { return _nDelay; } set { _nDelay = value; RaisePropChanged(); } }
        public int nLasers { get; set; } = 10;

        int _nPenWidth = 1;
        public int nPenWidth { get { return _nPenWidth; } set { _nPenWidth = value; RaisePropChanged(); } }
        Point InitPt = new Point(140, 140);
        Vector InitVec = new Vector(10, 10);
        public double InitPtX { get { return InitPt.X; } set { InitPt.X = value; RaisePropChanged(); } }
        public double InitPtY { get { return InitPt.Y; } set { InitPt.Y = value; RaisePropChanged(); } }
        public double InitDx { get { return InitVec.X; } set { InitVec.X = value; RaisePropChanged(); } }
        public double InitDy { get { return InitVec.Y; } set { InitVec.Y = value; RaisePropChanged(); } }
        public bool ChangeColor { get; set; } = true;
        public bool AddEllipse { get; set; } = true;

        public bool AddMushrooms { get; set; } = true;

        public double distBetweenEllipses { get; set; } = 85;
        public bool ShowEllipsePts { get; set; }

        bool _isRunning;
        internal static BounceFrame _instance;
        List<IMirror> _lstMirrors = new List<IMirror>();
        public NativeMethods.WinPoint _ptPrev = new NativeMethods.WinPoint();
        public BounceFrame(
            IntPtr hbrBackground
            ) : base(hbrBackground)
        {
            _instance = this;
            this._cts = new CancellationTokenSource();
            Clear(fKeepUserMirrors: false);
        }
        public void Clear(bool fKeepUserMirrors, Size? newSize = null)
        {
            IsRunning = false;
            if (!newSize.HasValue)
            {
                newSize = new Size(this.ActualWidth, this.ActualHeight);
            }

            this.EraseRect();
            var mrg = 18;
            var ptTopLeft = new Point(mrg, mrg);
            var ptTopRight = new Point(newSize.Value.Width - 1 - mrg, mrg);
            var ptBotLeft = new Point(mrg, newSize.Value.Height - 1 - mrg);
            var ptBotRight = new Point(newSize.Value.Width - 1 - mrg, newSize.Value.Height - 1 - mrg);
            _nOutofBounds = 0;
            _colorReflection = 0;
            _fPenDown = false;
            _fPenModeDrag = false;
            _ptOldMouseDown = null;

            if (!fKeepUserMirrors)
            {
                lock (this._lstMirrors)
                {
                    this._lstMirrors.Clear();
                    this._lstMirrors.Add(new CLine(ptTopLeft, ptTopRight));
                    this._lstMirrors.Add(new CLine(ptTopRight, ptBotRight));
                    this._lstMirrors.Add(new CLine(ptBotRight, ptBotLeft));
                    this._lstMirrors.Add(new CLine(ptTopLeft, ptBotLeft));
                    if (AddEllipse || AddMushrooms)
                    {
                        var ellipseTopLeft = new Point(mrg + 10, mrg + 20);
                        var ellipseBotRight = new Point(newSize.Value.Width - mrg * 2,
                            newSize.Value.Height - distBetweenEllipses * 2);
                        var ellipse = new CEllipse(
                            ellipseTopLeft,
                            ellipseBotRight,
                            new Point(0, 0),
                            new Point(0, 0)
                        );

                        //InitPt.X = ellipse.Center.X;
                        //InitPt.Y = ellipse.Center.Y - (ellipse.Height / 2 - 10);

                        this._lstMirrors.Add(ellipse);
                        if (AddMushrooms)
                        {
                            ellipse.ptStartArc = new Point(ellipse.Center.X + ellipse.Width / 2,
                                ellipse.Center.Y);
                            ellipse.ptEndArc = new Point(ellipse.Center.X - ellipse.Width / 2,
                                ellipse.Center.Y);
                            //                        ReflectWindow.AddStatusMessage($"{ellipse}, Center= {ellipse.Center} F1={ellipse.Focus1} F2={ellipse.Focus2}");
                            ellipseTopLeft.Y += distBetweenEllipses;
                            ellipseBotRight.Y += distBetweenEllipses;
                            var el2 = new CEllipse(
                                ellipseTopLeft,
                                ellipseBotRight,
                                new Point(0, 0),
                                new Point(0, 0)
                            );
                            el2.ptStartArc = new Point(el2.Center.X - el2.Width / 2,
                                el2.Center.Y);
                            el2.ptEndArc = new Point(el2.Center.X + el2.Width / 2,
                                el2.Center.Y);

                            this._lstMirrors.Add(el2);
                            // now add the line segments
                            var dGapHeight = 20;
                            var dGapWidth = 20;
                            var elInnerWidth = 43;
                            var segLenVert = (distBetweenEllipses - dGapHeight) / 2;
                            // upper ellipse vert down
                            var pt0 = new Point(ellipse.Center.X - ellipse.Width / 2,
                                    ellipse.Center.Y);
                            var pt1 = new Point(pt0.X,
                                    ellipse.Center.Y + segLenVert);
                            this._lstMirrors.Add(new CLine(pt0, pt1));
                            // horiz across
                            pt0 = pt1;
                            pt1.X += dGapWidth;
                            pt1.Y = pt0.Y;
                            this._lstMirrors.Add(new CLine(pt0, pt1));
                            // upper ellipse vert up
                            pt0 = pt1;
                            pt1 = pt0;
                            pt1.Y = pt0.Y - segLenVert;
                            this._lstMirrors.Add(new CLine(pt0, pt1));
                            // left inner ellipse
                            pt0 = pt1;
                            var elLeftInner = new CEllipse(
                                new Point(pt0.X - elInnerWidth / 2, pt0.Y),
                                new Point(pt0.X + elInnerWidth / 2, pt0.Y + distBetweenEllipses),
                                new Point(pt0.X, pt0.Y + distBetweenEllipses),
                                new Point(pt0.X, pt0.Y)
                                );
                            this._lstMirrors.Add(elLeftInner);
                            // lower vert up
                            pt0.Y += distBetweenEllipses;
                            pt1 = pt0;
                            pt1.Y -= segLenVert;
                            this._lstMirrors.Add(new CLine(pt0, pt1));
                            // lower horiz left
                            pt0 = pt1;
                            pt1.X -= dGapWidth;
                            pt1.Y = pt0.Y;
                            this._lstMirrors.Add(new CLine(pt0, pt1));
                            // lower left vert
                            pt0 = pt1;
                            pt1.Y += segLenVert;
                            this._lstMirrors.Add(new CLine(pt0, pt1));
                            // right side: vert down
                            pt0.X = ellipse.Center.X + ellipse.Width / 2;
                            pt0.Y = ellipse.Center.Y;
                            pt1 = pt0;
                            pt1.Y += segLenVert;
                            this._lstMirrors.Add(new CLine(pt0, pt1));
                            // horiz to left
                            pt0 = pt1;
                            pt1.X -= dGapWidth;
                            this._lstMirrors.Add(new CLine(pt0, pt1));
                            // vert up
                            pt0 = pt1;
                            pt1.Y -= segLenVert;
                            this._lstMirrors.Add(new CLine(pt0, pt1));
                            // right inner ellipse
                            pt0 = pt1;
                            var elRightInner = new CEllipse(
                                new Point(pt0.X - elInnerWidth / 2, pt0.Y),
                                new Point(pt0.X + elInnerWidth / 2, pt0.Y + distBetweenEllipses),
                                new Point(pt0.X, pt0.Y),
                                new Point(pt0.X, pt0.Y + distBetweenEllipses)
                                );
                            this._lstMirrors.Add(elRightInner);
                            // right side left: vert up
                            pt0.Y += distBetweenEllipses;
                            pt1 = pt0;
                            pt1.Y -= segLenVert;
                            this._lstMirrors.Add(new CLine(pt0, pt1));
                            // right side horiz
                            pt0 = pt1;
                            pt1 = pt0;
                            pt1.X += dGapWidth;
                            this._lstMirrors.Add(new CLine(pt0, pt1));
                            // right side right lower vert
                            pt0 = pt1;
                            pt1 = pt0;
                            pt1.Y += segLenVert;
                            this._lstMirrors.Add(new CLine(pt0, pt1));
                            // now join the 2 halves with straight lines
                            //var lin = new CLine(new Point(ellipse.Center.X - ellipse.Width / 2, ellipse.Center.Y),
                            //    new Point(el2.Center.X - el2.Width / 2, el2.Center.Y)
                            //    );
                            //this._lstMirrors.Add(lin);
                            //var lin2 = new CLine(new Point(ellipse.Center.X + ellipse.Width / 2, ellipse.Center.Y),
                            //    new Point(el2.Center.X + el2.Width / 2, el2.Center.Y)
                            //    );
                            //this._lstMirrors.Add(lin2);
                        }
                    }
                }
            }
            DrawMirrors();
        }

        public void DrawMirrors()
        {
            var hdc = NativeMethods.GetDC(_hwnd);
            NativeMethods.SelectObject(hdc, _clrMirror);
            lock (_lstMirrors)
            {
                foreach (var mirror in _lstMirrors)
                {
                    mirror.Draw(hdc);
                }
            }
            NativeMethods.ReleaseDC(_hwnd, hdc);
        }
        void ChooseRandomStartingRay()
        {
            _lstLasers[0]._ptLight.X = 2;
            for (int i = 0; i < _lstLasers.Count; i++)
            {
                _lstLasers[i]._ptLight.X = _rand.Next((int)(this.ActualWidth));
                _lstLasers[i]._ptLight.Y = _rand.Next((int)(this.ActualHeight));
                _lstLasers[i]._vecLight.X = 1;
                _lstLasers[i]._vecLight.Y = _rand.NextDouble();
            }
        }
        void DoReflecting()
        {
            var hDC = NativeMethods.GetDC(_hwnd);
            int nBounces = 0;
            int nLastBounceWhenStagnant = 0;
            _nOutofBounds = 0;
            _lstLasers = new List<Laser>();
            for (int i = 0; i < nLasers; i++)
            {
                var vec = new Vector(InitVec.X, InitVec.Y);
                if (nLasers > 1)
                {
                    var deltAngle = 2 * Math.PI / nLasers;
                    var angle = deltAngle * i;
                    vec.X = SpeedMult * Math.Cos(angle);
                    vec.Y = SpeedMult * Math.Sin(angle);
                }
                _lstLasers.Add(new Laser()
                {
                    _ptLight = new Point(InitPt.X, InitPt.Y),
                    _vecLight = vec
                });
            }
            var sw = new Stopwatch();
            sw.Start();
            while (!_cts.IsCancellationRequested
                //&& nBounces++ < _maxBounces
                )
            {
                foreach (var laser in _lstLasers)
                {
                    if (_cts.IsCancellationRequested)
                    {
                        break;
                    }
                    var ptLight = laser._ptLight;
                    var vecLight = laser._vecLight;
                    // for each line determine the intersection of our light vector incident line, which is just a segment
                    // if it's behind, ignore it
                    var lnIncident = new CLine(ptLight, new Point(ptLight.X + vecLight.X, ptLight.Y + vecLight.Y));

                    IMirror mirrorClosest = null;
                    double minDist = double.MaxValue;
                    Point? ptIntersect = null; // the point of intersection of the light vector and the closest mirror
                    lock (_lstMirrors)
                    {
                        foreach (var mirror in _lstMirrors)
                        {
                            var ptIntersectTest = mirror.IntersectingPoint(ptLight, vecLight);
                            if (ptIntersectTest.HasValue)
                            {
                                var dist = ptLight.DistanceFromPoint(ptIntersectTest.Value);
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
                            vecLight.X = -vecLight.X;
                            nLastBounceWhenStagnant = nBounces;
                        }
                        continue;
                    }
                    // now draw incident line from orig pt to intersection
                    NativeMethods.SelectObject(hDC, _clrFillReflection);
                    NativeMethods.MoveToEx(hDC, (int)(xScale * ptLight.X), (int)(yScale * ptLight.Y), ref _ptPrev);
                    //                this.EraseRect();this.DrawMirrors();
                    //                BounceFrame._instance.DrawLine(lnIncident);
                    if (AddEllipse && _lstMirrors.Count > 4)
                    {
                        if (mirrorClosest as CEllipse == null)
                        {
                            this.ToString();
                        }
                        var ellipse = _lstMirrors[4] as CEllipse;
                        if (!ellipse.IsPointInside(ptIntersect.Value))
                        {
                            "point not in ellipse".ToString();
                            //this.EraseRect();
                            //this.DrawMirrors();
                            //BounceFrame._instance.DrawLine(lnIncident);
                            //var pp = ellipse.IntersectingPoint(_ptLight, _vecLight);
                            //if (pp.HasValue)
                            //{
                            //    var d = _ptLight.DistanceFromPoint(pp.Value);
                            //}
                            //var dline = _ptLight.DistanceFromPoint(ptIntersect.Value);
                        }
                    }
                    if (mirrorClosest as CLine != null)
                    {
                        "expected no line".ToString();
                    }
                    NativeMethods.LineTo(hDC, (int)(xScale * ptIntersect.Value.X), (int)(yScale * ptIntersect.Value.Y));

                    // now reflect vector
                    var newvecLight = mirrorClosest.Reflect(ptLight, vecLight, ptIntersect.Value);
                    if (newvecLight.Y >= 82445778296.655792 - 10)
                    {
                        "wowww".ToString();
                    }
                    vecLight = newvecLight;
                    // now set new pt 
                    ptLight = ptIntersect.Value;
                    laser._ptLight = ptLight;
                    laser._vecLight = vecLight;
                    SetColor((_colorReflection + 1) & 0xffffff);
                    if (nBounces % 1000 == 0 || nDelay > 10)
                    {
                        int bouncesPerSecond = 0;
                        if (sw.ElapsedMilliseconds > 0)
                        {
                            bouncesPerSecond = (int)(1000 * nBounces / sw.ElapsedMilliseconds);
                        }
                        ReflectWindow.AddStatusMessage(
                            $"# Mirrors= {_lstMirrors.Count} bounces = {nBounces:n0}" +
                            //                        $" ({_ptLight.X,8:n1},{_ptLight.Y,8:n1}) ({_vecLight.X,8:n4},{_vecLight.Y,8:n4})" +
                            $" OOB={_nOutofBounds}" +
                            $" B/S={bouncesPerSecond}");
                    }
                    if (nDelay > 0)
                    {
                        Thread.Sleep(nDelay);
                    }
                    nBounces++;
                }
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
                //if (value)
                //{ // if triggered by hotkey make sure textboxes are updated
                //    var bindings = BindingOperations.GetSourceUpdatingBindings(this);
                //    foreach (var b in bindings)
                //    {
                //        b.UpdateSource();
                //    }
                //}
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

        internal void DrawLine(CLine line)
        {
            var hPen = NativeMethods.CreatePen(0, 10, (IntPtr)0xff);
            var hDC = NativeMethods.GetDC(_hwnd);
            NativeMethods.WinPoint ptPrev = new NativeMethods.WinPoint();
            NativeMethods.MoveToEx(hDC, (int)(xScale * line.pt0.X), (int)(yScale * line.pt0.Y), ref ptPrev);
            var old = NativeMethods.SelectObject(hDC, hPen);
            var res = NativeMethods.LineTo(hDC, (int)(xScale * line.pt1.X), (int)(yScale * line.pt1.Y));
            // restore
            NativeMethods.SelectObject(hDC, old);
            NativeMethods.MoveToEx(hDC, ptPrev.x, ptPrev.y, ref ptPrev);
            NativeMethods.ReleaseDC(_hwnd, hDC);
            NativeMethods.DeleteObject(hPen);
        }
        internal void DrawPoint(Point pt)
        {
            var hDC = NativeMethods.GetDC(_hwnd);
            NativeMethods.WinPoint ptPrev = new NativeMethods.WinPoint();
            NativeMethods.MoveToEx(hDC, (int)(xScale * pt.X), (int)(yScale * pt.Y), ref ptPrev);
            var old = NativeMethods.SelectObject(hDC, _clrMirror);
            int s = 2;
            NativeMethods.Ellipse(hDC, (int)(xScale * (pt.X - s)), (int)(yScale * (pt.Y - s)), (int)(xScale * (pt.X + s)), (int)(yScale * (pt.Y + s)));
            // restore
            NativeMethods.SelectObject(hDC, old);
            NativeMethods.MoveToEx(hDC, ptPrev.x, ptPrev.y, ref ptPrev);
            NativeMethods.ReleaseDC(_hwnd, hDC);
        }
        void AddMirror(IMirror mirror)
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
                var pos = em.GetPosition(this);
                var x = new Rect(new Size(this.ActualWidth, this.ActualHeight));
                if (x.Contains(pos))
                {
                    ShowContextMenu(pos);
                }
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

        private void ShowContextMenu(Point pos)
        {
            var cm = new ContextMenu();
            cm.AddMnuItem(
                $"_Set Initial Laser Light to {pos}",
                "The light has an initial position and dirction. This sets initial position to right click",
                (o, e) =>
                {
                    InitPtX = pos.X;
                    InitPtY = pos.Y;
                }
                );
            cm.AddMnuItem(
                "Toggle Drag mode",
                "You can draw mirrors with the mouse. ",
                (o, e) =>
                {
                    _fPenModeDrag = !_fPenModeDrag;
                    _ptOldMouseDown = pos;
                    ShowMouseStatus();
                }
                );
            var mItemAddEllipse = cm.AddMnuItem(
                "Add Ellipse",
                "add an ellipse",
                (o, e) =>
                {
                    AddEllipse = !AddEllipse;
                    Clear(fKeepUserMirrors: false);
                }
                );
            mItemAddEllipse.IsCheckable = true;
            mItemAddEllipse.IsChecked = AddEllipse;
            var mItemAddMushrooms = cm.AddMnuItem(
                "Add Mushrooms",
                "add mushrooms",
                (o, e) =>
                {
                    AddMushrooms = !AddMushrooms;
                    Clear(fKeepUserMirrors: false);
                }
                );
            mItemAddMushrooms.IsCheckable = true;
            mItemAddMushrooms.IsChecked = AddMushrooms;

            var mitemColors = cm.AddMnuItem(
                "Change Colors",
                "The laser color changes automatically",
                (o, e) =>
                {
                    ChangeColor = !ChangeColor;
                }
                );
            mitemColors.IsCheckable = true;
            mitemColors.IsChecked = ChangeColor;
            var mitemShowEllipsePts = cm.AddMnuItem(
                "ShowEllipsePts",
                "Show Center and 2 foci of ellipse",
                (o, e) =>
                {
                    ShowEllipsePts = !ShowEllipsePts;
                    Clear(fKeepUserMirrors: true);
                }
                );
            mitemShowEllipsePts.IsCheckable = true;
            mitemShowEllipsePts.IsCheckable = ShowEllipsePts;

            cm.AddMnuItem(
                "SetSize",
                "Set size to 1000 X 800",
                (o, e) =>
                {
                    Clear(fKeepUserMirrors: false, newSize: new Size(1000, 800));
                }
            );

            cm.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
            cm.IsOpen = true;
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
                            AddMirror(line);
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
                    AddMirror(line);
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
                    if (!IsRunning && // avoid deadlock
                        Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                    {
                        lock (_lstMirrors)
                        {
                            // keep 4 walls
                            if (_lstMirrors.Count > 4)
                            {
                                var lastMirror = _lstMirrors.Last();
                                if (lastMirror as CLine != null)
                                    _lstMirrors.RemoveAt(_lstMirrors.Count - 1);
                                Clear(fKeepUserMirrors: true);
                                _ptOldMouseDown = (lastMirror as CLine).pt1;
                                ShowMouseStatus();
                            }
                        }
                    }
                    break;
            }
        }

    }

    /// <summary>
    /// A light beam represented by a Point and a direction can intersect a mirror 
    /// A beam can also be reflected 
    /// </summary>
    interface IMirror
    {
        Point? IntersectingPoint(Point ptLight, Vector vecLight);
        Vector Reflect(Point ptLight, Vector vecLight, Point ptIntersect);
        void Draw(IntPtr hDC);
    }
    /// <summary>
    /// Defined by a bounding rectangle with 2 points: topleft and bottom right
    /// The arc ends where it intersects the radial from the center of the bounding rectangle to the (nXEndArc, nYEndArc) point
    /// </summary>
    public class CEllipse : IMirror
    {
        public Point ptTopLeft { get; set; }
        public Point ptBotRight { get; set; }
        public Point ptStartArc { get; set; }
        public Point ptEndArc { get; set; }
        public double Width { get { return ptBotRight.X - ptTopLeft.X; } }
        public double Height { get { return ptBotRight.Y - ptTopLeft.Y; } }
        public double a { get { return Width / 2; } }
        public double b { get { return Height / 2; } }

        public Point Center { get { return new Point(ptTopLeft.X + Width / 2, ptTopLeft.Y + Height / 2); } }
        public double d2 { get { return Center.Y; } }
        /// <summary>
        /// focus: distance from center to focus. other is negative of this
        /// </summary>
        public double f { get { return Math.Sqrt(Math.Abs(a * a - b * b)); } }
        private Point? _ptonArc;
        public Point Focus1
        {
            get
            {
                if (a > b)
                {
                    return new Point(Center.X - f, Center.Y);
                }
                return new Point(Center.X, Center.Y - f);
            }
        }
        public Point Focus2
        {
            get
            {
                if (a > b)
                {
                    return new Point(Center.X + f, Center.Y);
                }
                return new Point(Center.X, Center.Y + f);
            }
        }
        public CEllipse(Point ptTopLeft, Point ptBotRight, Point ptStartArc, Point ptEndArc)
        {
            this.ptTopLeft = ptTopLeft;
            this.ptBotRight = ptBotRight;
            this.ptStartArc = ptStartArc;
            this.ptEndArc = ptEndArc;
        }

        bool IsCompleteEllipse { get { return ptStartArc == ptEndArc; } }
        // https://social.msdn.microsoft.com/Forums/windowsapps/en-US/b599db66-a987-4dba-b5b9-7babc9badc9c/finding-the-intersection-points-of-a-line-and-an-ellipse?forum=wpdevelop
        public bool IsPointInside(Point pt)
        {
            var IsInside = false;
            var val = (pt.X - Center.X).squared() / (a * a) + (pt.Y - Center.Y).squared() / (b * b);
            if (val <= 1 + 100 * BounceFrame.epsilon)
            {
                IsInside = true;
            }
            return IsInside;
        }
        public Point? IntersectingPoint(Point ptLight, Vector vecLight)
        {
            //BounceFrame._instance.DrawPoint(ptLight);
            Point? ptIntersectResult = null;
            Point? ptIntersect0 = null;
            Point? ptIntersect1 = null;
            var lnIncident = new CLine(ptLight, new Point(ptLight.X + vecLight.X, ptLight.Y + vecLight.Y));
            //BounceFrame._instance.DrawLine(lnIncident);
            double A = 0, B = 0, C = 0, m = 0, c = 0;
            var Isvertical = Math.Abs(vecLight.X) < 10 * BounceFrame.epsilon;
            if (!Isvertical)
            {
                m = lnIncident.slope;
                c = lnIncident.YIntercept;
                A = b * b + a * a * m * m;
                B = 2 * a * a * m * (c - Center.Y) - 2 * b * b * Center.X;
                C = b * b * Center.X * Center.X + a * a * ((c - Center.Y) * (c - Center.Y) - b * b);
            }
            else
            {
                A = a * a;
                B = -2 * Center.Y * a * a;
                C = -a * a * b * b + b * b * (lnIncident.pt0.X - Center.X) * (lnIncident.pt0.X - Center.X);
            }
            // quadratic formula (-b +- sqrt(b*b-4ac)/2a
            var disc = B * B - 4 * A * C;
            if (disc > BounceFrame.epsilon) // else no intersection (==0 means both points are the same)
            {
                var sqt = Math.Sqrt(disc);
                var x = (-B + sqt) / (2 * A);
                // we have >0 intersections.
                if (!Isvertical)
                {
                    var y = m * x + c;
                    ptIntersect0 = new Point(x, y);
                    x = (-B - sqt) / (2 * A);
                    y = m * x + c;
                    ptIntersect1 = new Point(x, y);
                }
                else
                {
                    //ptIntersect0 = new Point(lnIncident.pt0.X, x);
                    //x = (-B - sqt) / (2 * A);
                    //ptIntersect1 = new Point(lnIncident.pt0.X, x);
                    var y = (b / a) * Math.Sqrt(a.squared() - (ptLight.X - Center.X).squared()) + Center.Y;
                    var y2 = -(b / a) * Math.Sqrt(a.squared() - (ptLight.X - Center.X).squared()) + Center.Y;
                    ptIntersect0 = new Point(ptLight.X, y);
                    ptIntersect1 = new Point(ptLight.X, y2);
                }
                // we have 2 pts: choose which one
                //BounceFrame._instance.DrawLine(lnIncident);
                //BounceFrame._instance.DrawPoint(ptIntersect0.Value);
                //BounceFrame._instance.DrawPoint(ptIntersect1.Value);
                if (!IsCompleteEllipse)
                {
                    if (!IsPointOnArc(ptIntersect0.Value))
                    {
                        ptIntersect0 = null;
                    }
                    if (!IsPointOnArc(ptIntersect1.Value))
                    {
                        ptIntersect1 = null;
                    }
                }
                ////is one of the 2 intersections where the light came from?
                if (ptIntersect0.HasValue && ptIntersect0.Value.DistanceFromPoint(ptLight) < 1)
                {
                    ptIntersect0 = null;
                }
                if (ptIntersect1.HasValue && ptIntersect1.Value.DistanceFromPoint(ptLight) < 1)
                {
                    ptIntersect1 = null;
                }
                // now determine which point is in the right direction 
                //(could be both if point started outside ellipse)
                if (ptIntersect0.HasValue &&
                    !ptIntersect0.Value.IsVectorInSameDirection(ptLight, vecLight))
                {
                    ptIntersect0 = null;
                }
                if (ptIntersect1.HasValue &&
                    !ptIntersect1.Value.IsVectorInSameDirection(ptLight, vecLight))
                {
                    ptIntersect1 = null;
                }
                if (ptIntersect0.HasValue)
                {
                    if (ptIntersect1.HasValue)// 2 pts still: choose closer
                    {
                        var dist0 = ptLight.DistanceFromPoint(ptIntersect0.Value);
                        var dist1 = ptLight.DistanceFromPoint(ptIntersect1.Value);
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
            if (!ptIntersectResult.HasValue)
            {
                "no intersection on ellipse?".ToString();
            }
            if (ptIntersectResult.HasValue && !this.IsPointInside(ptIntersectResult.Value)) // we said that it intersects at this point, but the point is not in the ellipse. Let's recalc to force it onto ellipse
            {
                BounceFrame._instance.EraseRect();
                BounceFrame._instance.DrawMirrors();
                BounceFrame._instance.DrawPoint(ptIntersectResult.Value);
                "PtIntersect not on ellipse".ToString();
                ptIntersectResult = GetPointOnEllipseClosestToPoint(ptIntersectResult.Value);
            }
            return ptIntersectResult;
        }

        bool IsPointOnArc(Point pt)
        {
            var result = false;
            if (IsCompleteEllipse)
            {
                result = true;
            }
            else
            {
                //BounceFrame._instance.DrawPoint(pt);
                // imagine a straight line between the arc start and arc end pts on the ellipse
                // the intpoint is either in the same half plane or not as the arcpt
                var lnArcStartEnd = new CLine(ptStartArc, ptEndArc);
                var isLeft = lnArcStartEnd.LeftHalf(pt);
                var ptArc = GetPointOnArc();
                var isPtOnArcLeft = lnArcStartEnd.LeftHalf(ptArc);
                if (isLeft == isPtOnArcLeft)
                {
                    result = true;
                }
            }
            return result;
        }
        /// <summary>
        /// To determine if a point is included in the arc of an ellipse
        /// imagine a line from the start and end arc pts. 
        /// Get a point in the half plane that includes the arc
        /// </summary>
        private Point GetPointOnArc()
        {
            if (!_ptonArc.HasValue)
            {
                // default direction of arc is counter clockwise.
                if (ptStartArc.X > ptEndArc.X)
                {
                    _ptonArc = new Point(ptStartArc.X - 1, ptStartArc.Y - 1);
                }
                else
                {
                    if (ptStartArc.X == ptEndArc.X)
                    {
                        if (ptStartArc.Y > ptEndArc.Y)
                        {
                            _ptonArc = new Point(ptStartArc.X + 1, ptStartArc.Y - 1);
                        }
                        else
                        {
                            _ptonArc = new Point(ptStartArc.X - 1, ptStartArc.Y - 1);
                        }
                    }
                    else
                    {
                        _ptonArc = new Point(ptStartArc.X + 1, ptStartArc.Y + 1);
                    }
                }
            }
            return _ptonArc.Value;
        }
        public CLine GetTangentLineAtPoint(Point ptIntersect)
        {
            double m = 0;
            if (Math.Abs(ptIntersect.Y - Center.Y) < BounceFrame.epsilon) // vertical 
            {
                " the tangent to a vertical line is horizontal (slope = 0)".ToString();
            }
            else
            {
                // calculate the slope of the tangent line at that point by differentiation
                m = -b * b * (ptIntersect.X - Center.X) / (a * a * (ptIntersect.Y - Center.Y));
            }
            var lnTangent = new CLine(
                ptIntersect,
                new Point(ptIntersect.X + BounceFrame.SpeedMult,
                        ptIntersect.Y + BounceFrame.SpeedMult * m),
                IsLimitedInLength: false // a tangent has infinite length
                        );
            return lnTangent;
        }

        public Vector Reflect(Point ptLight, Vector vecLight, Point ptIntersect)
        {
            // now reflect the light off that tangent line
            var lnTangent = GetTangentLineAtPoint(ptIntersect);
            //BounceFrame._instance.DrawLine(lnTangent);
            vecLight = lnTangent.Reflect(ptLight, vecLight, ptIntersect);
            return vecLight;
        }

        /// <summary>
        /// due to rounding error, we might get a point very close, but not on ellipse.
        /// </summary>
        /// <param name="ptIntersect"></param>
        /// <returns></returns>
        public Point GetPointOnEllipseClosestToPoint(Point ptIntersect)
        {
            var lnTangent = GetTangentLineAtPoint(ptIntersect);
            //BounceFrame._instance.DrawLine(lnTangent);
            var m = 0.0;
            if (lnTangent.deltaX == 0) // vertical tangent
            {
                "isvert".ToString();
            }
            else
            {
                m = -1 / lnTangent.slope; // perpendicular to tangent
            }

            var vec = new Vector(
                BounceFrame.SpeedMult,
                BounceFrame.SpeedMult * m
            );
            var newpt = lnTangent.IntersectingPoint(ptIntersect, vec);
            //BounceFrame._instance.DrawPoint(newpt.Value);
            if (newpt.HasValue)
            {
                var distBetween = ptIntersect.DistanceFromPoint(newpt.Value);
                ptIntersect = newpt.Value;
                var isptInEllipse = this.IsPointInside(ptIntersect);
            }
            else
            {
                "newpt no value".ToString();
            }
            return ptIntersect;
        }

        public void Draw(IntPtr hDC)
        {
            var xScale = BounceFrame._instance.xScale;
            var yScale = BounceFrame._instance.yScale;

            nativeMethods.Arc(hDC,
                (int)(xScale * this.ptTopLeft.X), (int)(yScale * this.ptTopLeft.Y),
                (int)(xScale * this.ptBotRight.X), (int)(yScale * this.ptBotRight.Y),
                (int)(xScale * this.ptStartArc.X), (int)(yScale * this.ptStartArc.Y),
                (int)(xScale * this.ptEndArc.X), (int)(yScale * this.ptEndArc.Y));
            // half ellipse:
            //nativeMethods.Arc(hDC,
            //    (int)(xScale * this.ptTopLeft.X), (int)(yScale * this.ptTopLeft.Y),
            //    (int)(xScale * this.ptBotRight.X), (int)(yScale * this.ptBotRight.Y),
            //    (int)(xScale * (this.ptTopLeft.X + this.Width)), (int)(yScale * (this.ptTopLeft.Y + this.Height / 2)),
            //    (int)(xScale * (this.ptTopLeft.X)), (int)(yScale * (this.ptTopLeft.Y + this.Height / 2)));
            if (BounceFrame._instance.ShowEllipsePts)
            {
                BounceFrame._instance.DrawPoint(this.Center);
                BounceFrame._instance.DrawPoint(this.Focus1);
                BounceFrame._instance.DrawPoint(this.Focus2);
            }
        }
        public override string ToString()
        {
            return $"EL({ptTopLeft})-({ptBotRight})";
        }
    }


    /// <summary>
    /// A line may be limited in length (a mirror is only so wide)
    /// </summary>
    public class CLine : IMirror
    {
        public Point pt0 { get; private set; }
        public Point pt1 { get; private set; }
        public bool IsLimitedInLength { get; set; } = true;
        Lazy<double> lazyLineSegLength;
        public double LineLength => lazyLineSegLength.Value;

        public CLine(Point p0, Point p1, bool IsLimitedInLength = true)
        {
            this.pt0 = p0;
            this.pt1 = p1;
            lazyLineSegLength = new Lazy<double>(() =>
                Math.Sqrt((pt1.Y - pt0.Y).squared() + (pt1.X - pt0.X).squared()),
               isThreadSafe: false // only accessed from one thread
            );
            this.IsLimitedInLength = IsLimitedInLength;
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
            return Math.Atan(tantheta) / BounceFrame.piOver180;
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
        public Point? IntersectingPoint(Point ptLight, Vector vecLight)
        {
            Point? ptIntersect = null;
            var lnIncident = new CLine(ptLight, new Point(ptLight.X + vecLight.X, ptLight.Y + vecLight.Y));
            //BounceFrame._instance.DrawLine(lnIncident);
            Point? ptIntersectTest = this.IntersectingPoint(lnIncident);
            // the incident line intersects the mirror. Our mirrors have finite width
            // let's see if the intersection point is within the mirror's edges
            if (ptIntersectTest.HasValue)
            {
                if (this.IsLimitedInLength)
                {
                    var distPt0 = this.pt0.DistanceFromPoint(ptIntersectTest.Value);
                    var distPt1 = ptIntersectTest.Value.DistanceFromPoint(this.pt1);
                    var thislinelen = this.LineLength;
                    if (distPt0 + distPt1 - thislinelen < BounceFrame.epsilon)
                    {
                        var Isvertical = Math.Abs(vecLight.X) < BounceFrame.epsilon;
                        if (Isvertical)
                        {
                            var ss = Math.Sign(vecLight.Y);
                            var s2 = Math.Sign(ptIntersectTest.Value.Y - ptLight.Y);
                            if (ss * s2 == 1) // in our direction?
                            {
                                ptIntersect = ptIntersectTest.Value;
                            }
                        }
                        else  // non-vertical
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
                else
                {
                    ptIntersect = ptIntersectTest;
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
            var res = (pt1.X - pt0.X) * (c.Y - pt0.Y) - (pt1.Y - pt0.Y) * (c.X - pt0.X);
            return res > BounceFrame.epsilon;
        }

        public Vector Reflect(Point ptLight, Vector vecLight, Point ptIntersect)
        {
            if (Math.Abs(this.deltaX) < BounceFrame.epsilon) // vertical line
            {
                vecLight.X = -vecLight.X;
            }
            else if (Math.Abs(this.deltaY) < BounceFrame.epsilon) // horiz line
            {
                vecLight.Y = -vecLight.Y;
            }
            else
            {
                //// create incident line endpoint to intersection with correct seg length
                var lnIncident = new CLine(ptLight, new Point(ptLight.X + vecLight.X, ptLight.Y + vecLight.Y));
                //BounceFrame._instance.EraseRect();
                //BounceFrame._instance.DrawMirrors();
                //BounceFrame._instance.DrawLine(lnIncident);
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
                vecLight.X = BounceFrame.SpeedMult;
                vecLight.Y = BounceFrame.SpeedMult * newSlope;
                // create a test point along the line of reflection
                var ptTest = new Point(ptIntersect.X + vecLight.X, ptIntersect.Y + vecLight.Y);
                //if (!ptTest.IsVectorInSameDirection(ptIntersect, vecLight))
                //{
                //    vecLight.X = -vecLight.X;
                //    vecLight.Y = -vecLight.Y;
                //}
                //BounceFrame._instance.DrawLine(this);
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

        public void Draw(IntPtr hDC)
        {
            NativeMethods.MoveToEx(hDC, (int)(BounceFrame._instance.xScale * this.pt0.X), (int)(BounceFrame._instance.yScale * this.pt0.Y), ref BounceFrame._instance._ptPrev);
            NativeMethods.LineTo(hDC, (int)(BounceFrame._instance.xScale * this.pt1.X), (int)(BounceFrame._instance.yScale * this.pt1.Y));
        }
        public override string ToString()
        {
            return $"({pt0.X} , {pt0.Y}),({pt1.X} , {pt1.Y})";
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
        public static MenuItem AddMnuItem(this ContextMenu ctxmenu, string name, string tip, RoutedEventHandler hndlr)
        {
            var mitem = new MenuItem()
            {
                Header = name,
                ToolTip = tip
            };
            ctxmenu.Items.Add(mitem);
            mitem.Click += hndlr;
            return mitem;
        }
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
        /// <summary>
        /// Given a point and a vector direction from that point determining a light ray
        /// return true if the Test Point is on the ray in the current direction
        /// </summary>
        public static bool IsVectorInSameDirection(this Point ptTest, Point ptLight, Vector vecLight)
        {
            var fIsSameDirection = false;
            if (Math.Abs(vecLight.X) < BounceFrame.epsilon) // vertical line
            {
                if (Math.Sign(ptTest.Y - ptLight.Y) == Math.Sign(vecLight.Y))
                {
                    fIsSameDirection = true;
                }
            }
            else if (Math.Abs(vecLight.Y) < BounceFrame.epsilon) // horiz
            {
                if (Math.Sign(ptTest.X - ptLight.X) == Math.Sign(vecLight.X))
                {
                    fIsSameDirection = true;
                }
            }
            else
            { // non-vertical. Construct a normal through the ptLight (the ray can extend beyond the test pt)
                var vecNormal = new Vector(-vecLight.Y, vecLight.X);
                var lnNormal = new CLine(ptLight, new Point(ptLight.X + vecNormal.X, ptLight.Y + vecNormal.Y));
                //BounceFrame._instance.DrawLine(lnNormal);
                var lefthalfTestPt = lnNormal.LeftHalf(ptTest);
                var ptvecTip = ptLight.Add(new Point(vecLight.X, vecLight.Y));
                var lefthalfVectorTip = lnNormal.LeftHalf(ptvecTip);
                if (!(lefthalfTestPt ^ lefthalfVectorTip)) // xor
                {
                    fIsSameDirection = true;
                }
            }
            return fIsSameDirection;
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
        // c:\Program Files (x86)\Windows Kits\8.1\Include\um\wingdi.h
        public enum _ArcDirection
        {
            AD_COUNTERCLOCKWISE = 1,
            AD_CLOCKWISE = 2
        }
        [DllImport("gdi32")]
        public static extern _ArcDirection GetArcDirection(IntPtr hdc);
        [DllImport("gdi32")]
        public static extern int SetArcDirection(IntPtr hdc, _ArcDirection ArcDirection);
    }
}