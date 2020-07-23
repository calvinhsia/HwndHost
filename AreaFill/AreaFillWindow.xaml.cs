using hWndHost;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Xml;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.ExceptionServices;
using System.Collections.Concurrent;

namespace AreaFill
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class AreaFillWindow : Window
    {
        private TextBox _tbxStatus;

        public AreaFillWindow()
        {
            //            InitializeComponent();
            WindowState = WindowState.Maximized;
            this.Loaded += AreaFillWindow_Loaded;
        }

        private void AreaFillWindow_Loaded(object sender, RoutedEventArgs e)
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
            <CheckBox Content=""Gravity"" 
                IsChecked= ""{Binding Path=Gravity}"" ToolTip=""Use Gravity or Bezier curves"" />
            <CheckBox Content=""DepthFirst"" 
                IsChecked= ""{Binding Path=DepthFirst}"" />
            <CheckBox Content=""FillViaCPP"" 
                IsChecked= ""{Binding Path=FillViaCPP}"" />
            <Button Name=""btnErase"" Content=""_Erase""/>
            <Label Content=""NumPts""/>
            <l:MyTextBox 
                Text =""{Binding Path=NumPts}"" 
                ToolTip=""NumPts"" />
            <Label Content=""NumSegs""/>
            <l:MyTextBox 
                Text =""{Binding Path=NumSegs}"" 
                ToolTip=""NumSegs"" />
            <Label Content=""CellWidth""/>
            <l:MyTextBox 
                Text =""{Binding Path=CellWidth}"" 
                ToolTip=""Width of a cell"" />
            <Label Content=""CellHeight""/>
            <l:MyTextBox 
                Text =""{Binding Path=CellHeight}"" 
                ToolTip=""Height of a cell"" />

            <Label Content=""Rows""/>
            <TextBlock
                Text =""{Binding Path=nTotRows}"" 
                />
            <Label Content=""Cols""/>
            <TextBlock
                Text =""{Binding Path=nTotCols}"" 
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
        ToolTip=""Change the delay""
        Width=""100""/>

</DockPanel>
</Grid>
";
                var bgdOcean = NativeMethods.CreateSolidBrush(
                    new IntPtr(0xffffff));
                var areaFillArea = new AreaFillArea(this, bgdOcean);

                var strReader = new System.IO.StringReader(strxaml);
                var xamlreader = XmlReader.Create(strReader);
                var grid = (Grid)(XamlReader.Load(xamlreader));
                grid.DataContext = areaFillArea;

                _tbxStatus = (TextBox)grid.FindName("tbxStatus");
                var btnErase = (Button)grid.FindName("btnErase");
                btnErase.Click += (o, ee) => { UpdateBindings(this); areaFillArea.DoErase(); };
                var userCtrl = (UserControl)grid.FindName("MyUserControl");
                userCtrl.Content = areaFillArea;
                this.Content = grid;
                this.SizeChanged += (os, es) =>
                {
                    areaFillArea.OnSizeChanged();
                };
                //this.TouchMove += (om, em) =>
                //{
                //    var pos = em.GetTouchPoint(this);
                //    Debug.WriteLine($"TM {pos}");
                //};
                //this.PreviewMouseMove += (om, em) =>
                //  {
                //      var pos = em.GetPosition(this);
                //      Debug.WriteLine($"pMM {pos}");
                //  };
                //this.MouseMove += (om, em) =>
                //  {
                //      //                      if (!em.Handled)
                //      {
                //          var pos = em.GetPosition(this);
                //          Debug.WriteLine($"MM  {pos}");
                //          areaFillArea.MyOnMouseMove(em);
                //      }
                //  };
                //this.MouseUp += (om, em) =>
                // {
                //     areaFillArea.MyOnOnMouseUp(em);
                // };
                //this.MouseDown += (om, em) =>
                //{
                //    areaFillArea.MyOnOnMouseDown(em);
                //};
            }
            catch (Exception ex)
            {
                this.Content = ex.ToString();
            }
        }
        internal static void UpdateBindings(FrameworkElement element)
        {
            // hotkey doesn't update binding, so we need to update
            //http://social.msdn.microsoft.com/Forums/vstudio/en-US/e75a6449-1f40-463c-a251-12d317350bf2/textbox-focus-databinding-and-hotkeys?forum=wpf
            var bindings = BindingOperations.GetSourceUpdatingBindings(element); // get all bindings
            foreach (var b in bindings)
            {
                b.UpdateSource();
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

    internal class AreaFillArea : MyHwndHost, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        void RaisePropChanged([CallerMemberName] string propName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
        private readonly AreaFillWindow _areaFillWindow;
        private readonly IntPtr _bgdOcean;
        CancellationTokenSource cts;
        List<Task> _lstTasks;
        bool _IsRunning = false;
        private IntPtr _hdc;
        private bool _ResetRequired;
        public Random _rand = new Random(1);

        public Point? _ptOld { get; set; }
        public bool Gravity { get; set; }

        int _oColor = 0xffffff;

        private Point _ptCurrent;
        byte[,] _cells;
        private const int MK_LBUTTON = 1;
        //        private const int MK_RBUTTON = 2;

        public bool DepthFirst { get; set; }
        public bool FillViaCPP { get; set; } = true;

        public int NumPts { get; set; } = 10;
        public int NumSegs { get; set; } = 5;

        int _CellWidth = 1;
        public int CellWidth
        {
            get { return _CellWidth; }
            set { _CellWidth = value; _ResetRequired = true; }
        }

        int _CellHeight = 1;
        public int CellHeight
        {
            get { return _CellHeight; }
            set { _CellHeight = value; _ResetRequired = true; }
        }
        private int _nTotRows;
        private int _nTotCols;

        public int nTotRows
        {
            get { return _nTotRows; }
            set { if (_nTotRows != value) { _nTotRows = value; RaisePropChanged(); } }
        }
        public int nTotCols
        {
            get { return _nTotCols; }
            set { if (_nTotCols != value) { _nTotCols = value; RaisePropChanged(); } }
        }


        unsafe void DoFillViaCPP(CancellationTokenSource cts, Point ptStartFill)
        {
            int cancellationRequested = 0;
            using (var reg = cts.Token.Register(() =>
            {
                cancellationRequested = 1;
            }))
            {
                var guidComClass = new Guid("BB4B9EE1-81DE-400B-A58A-687ED53A02E6");
                var hr = CoCreateFromFile("CppLib.dll", guidComClass, typeof(IAreaFill).GUID, out var pObject);
                var iara = (IAreaFill)Marshal.GetTypedObjectForIUnknown(pObject, typeof(IAreaFill));
                fixed (byte* arr = _cells)
                {
                    AreaFillData areaFillData = new AreaFillData()
                    {
                        hWnd = _hwnd,
                        ArraySize = new Point(nTotCols, nTotRows),
                        StartPoint = ptStartFill,
                        DepthFirst = DepthFirst
                    };
                    iara.DoAreaFill(areaFillData, ref cancellationRequested, arr);
                }
                Marshal.ReleaseComObject(iara);
                GC.Collect();
                Marshal.CleanupUnusedObjectsInCurrentContext();
                FreeLibrary(_hModule);
            }
        }
        public bool IsRunning
        {
            get { return _IsRunning; }
            set
            {
                if (_IsRunning != value)
                {
                    if (value) // if we're starting
                    {
                    }
                    else
                    {// we're stopping
                        cts.Cancel();
                        Task.WaitAll(_lstTasks.ToArray());
                        cts = new CancellationTokenSource();
                    }
                    _IsRunning = value;
                    RaisePropChanged();
                }
            }
        }

        public AreaFillArea(AreaFillWindow areaFillWindow, IntPtr bgdOcean) : base(bgdOcean)
        {
            LstWndProcMsgs.Add((int)NativeMethods.WM_.WM_NCHITTEST);
            LstWndProcMsgs.Add((int)NativeMethods.WM_.WM_MOUSEMOVE);
            LstWndProcMsgs.Add((int)NativeMethods.WM_.WM_LBUTTONDOWN);
            LstWndProcMsgs.Add((int)NativeMethods.WM_.WM_RBUTTONUP);
            this._areaFillWindow = areaFillWindow;
            this._bgdOcean = bgdOcean;
            this.WndProcExt = (int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
            {
                switch ((NativeMethods.WM_)msg)
                {
                    case NativeMethods.WM_.WM_NCHITTEST:
                        handled = true;
                        return new IntPtr(1); // HTCLIENT
                    case NativeMethods.WM_.WM_LBUTTONDOWN:
                    case NativeMethods.WM_.WM_LBUTTONUP:
                    case NativeMethods.WM_.WM_MOUSEMOVE:
                    case NativeMethods.WM_.WM_RBUTTONUP:
                        var x = lParam.ToInt32() & 0xffff;
                        var y = (lParam.ToInt32() >> 16) & 0xffff;
                        var pt = new Point(x, y);
                        //var arg = new MouseEventArgs(Mouse.PrimaryDevice, 0);
                        //arg.RoutedEvent = Mouse.MouseMoveEvent;
                        switch ((NativeMethods.WM_)msg)
                        {
                            case NativeMethods.WM_.WM_MOUSEMOVE:
                                this.MyOnMouseMove(pt, wParam);
                                break;
                            case NativeMethods.WM_.WM_LBUTTONDOWN:
                                this.MyOnOnMouseDown(pt, wParam);
                                break;
                            case NativeMethods.WM_.WM_LBUTTONUP:
                            case NativeMethods.WM_.WM_RBUTTONUP:
                                this.MyOnOnMouseUp(pt, wParam, msg);
                                break;
                        }
                        break;
                }
                return IntPtr.Zero;
            };
        }
        private void InitWorld()
        {
            nTotRows = (int)(this.ActualHeight * yScale / CellHeight);
            nTotCols = (int)(this.ActualWidth * xScale / CellWidth);
            _cells = new byte[nTotCols, nTotRows];
            if (_hdc != IntPtr.Zero)
            {
                NativeMethods.ReleaseDC(_hwnd, _hdc);
            }
            _hdc = NativeMethods.GetDC(_hwnd);
            if (Gravity)
            {
                DrawGravityLines();

            }
            else
            {
                DrawBezierLines();
            }
        }
        /// <summary>
        /// can be called multiple times for multiple start points
        /// </summary>
        Task DoAreaFillAsync(Point ptStartFill)
        {
            if (_ResetRequired)
            {
                InitWorld();
            }
            var tcs = new TaskCompletionSource<int>();
            var tsk = Task.Run(() =>
            {
                try
                {
                    IsRunning = true;
                    if (FillViaCPP)
                    {
                        DoFillViaCPP(cts, ptStartFill);
                    }
                    else
                    {
                        var oColor = _oColor;
                        if (DepthFirst)
                        {
                            var stack = new Stack<Point>();
                            stack.Push(ptStartFill);
                            while (stack.Count > 0 && !cts.IsCancellationRequested)
                            {
                                var ptCurrent = stack.Pop();
                                if (IsValidPoint(ptCurrent))
                                {
                                    if (_cells[ptCurrent.X, ptCurrent.Y] == 0)
                                    {
                                        DrawACell(ptCurrent, ref oColor);
                                        stack.Push(new Point(ptCurrent.X - 1, ptCurrent.Y));
                                        stack.Push(new Point(ptCurrent.X + 1, ptCurrent.Y));
                                        stack.Push(new Point(ptCurrent.X, ptCurrent.Y + 1));
                                        stack.Push(new Point(ptCurrent.X, ptCurrent.Y - 1));
                                    }
                                }
                            }
                        }
                        else
                        {
                            var queue = new Queue<Point>();
                            queue.Enqueue(ptStartFill);
                            while (queue.Count > 0 && !cts.IsCancellationRequested)
                            {
                                var ptCurrent = queue.Dequeue();
                                if (IsValidPoint(ptCurrent))
                                {
                                    if (_cells[ptCurrent.X, ptCurrent.Y] == 0)
                                    {
                                        Thread.Sleep(1);
                                        DrawACell(ptCurrent, ref oColor);
                                        queue.Enqueue(new Point(ptCurrent.X - 1, ptCurrent.Y));
                                        queue.Enqueue(new Point(ptCurrent.X + 1, ptCurrent.Y));
                                        queue.Enqueue(new Point(ptCurrent.X, ptCurrent.Y + 1));
                                        queue.Enqueue(new Point(ptCurrent.X, ptCurrent.Y - 1));
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                }
                tcs.SetResult(0);
            });
            return tsk;
        }

        private void DrawGravityLines() // https://github.com/calvinhsia/Cartoon
        {
            var fForce = _nTotCols / 5;
            var InitSpeed = 10;
            var wallBound = 15;
            var pos0 = new Vector(_rand.NextDouble() * _nTotCols, _rand.NextDouble() * _nTotRows);
            var startPos = pos0;
            var vel = new Vector(_rand.NextDouble() * InitSpeed, _rand.NextDouble() * InitSpeed);
            for (int nSeg = 0; nSeg < NumSegs; nSeg++)
            {
                var pos1 = pos0 + vel;
                DrawLineOfCells(pos0.ToPoint(), pos1.ToPoint());
                //                DrawACell(pos1.ToPoint(), MakeVisible: true);
                var dWest = Math.Max(pos1._X, wallBound);
                var accWest = fForce / dWest / dWest;
                var dEast = Math.Max(_nTotCols - pos1._X, wallBound);
                var accEast = -fForce / dEast / dEast;
                var dNorth = Math.Max(pos1._Y, wallBound);
                var accNorth = fForce / dNorth / dNorth;
                var dSouth = Math.Max(_nTotRows - pos1._Y, wallBound);
                var accSouth = -fForce / dSouth / dSouth;
                var accEastWest = (accEast + accWest) * _rand.NextDouble() * 20;
                var accNothSoutch = (accNorth + accSouth) * _rand.NextDouble() * 20;
                var acc = new Vector(accEastWest, accNothSoutch);
                vel += acc;
                pos0 = pos1;
            }
        }

        private void DrawBezierLines()
        {
            var lstStartPoints = new List<Vector>();
            //            _rand = new Random(1);
            for (int i = 0; i < NumPts; i++)
            {
                lstStartPoints.Add(new Vector(_rand.NextDouble() * _nTotCols, _rand.NextDouble() * _nTotRows));
            }
            //lstStartPoints.Clear();
            //lstStartPoints.Add(new Vector(800, 1100));
            //lstStartPoints.Add(new Vector(1300, 1100));
            //lstStartPoints.Add(new Vector(1300, 1200));
            //lstStartPoints.Add(new Vector(800, 1400));
            //foreach (var pt in lstStartPoints)
            //{
            //    DrawACell(pt.ToPoint(), MakeVisible: true);
            //}
            BezierPath(nSeg: NumSegs, ctrlPoints: lstStartPoints);
            var lstInterpolate = BezierInterpolate(lstStartPoints, 0.5);
            BezierPath(nSeg: NumSegs, ctrlPoints: lstInterpolate);

            //            var lstPts = BezierPath(nSeg: 10, ctrlPoints: lstStartPoints);
            //for (int i = 0; i < 10; i++)
            //{
            //    GenSplines(22);
            //}
        }

        List<Vector> BezierInterpolate(List<Vector> SegPts, double scale)
        {
            var ctrlPoints = new List<Vector>();
            if (SegPts.Count >= 2)
            {
                for (int i = 0; i < SegPts.Count; i++)
                {
                    if (i == 0)
                    {
                        var p1 = SegPts[i];
                        var p2 = SegPts[i + 1];
                        var tangent = (p2 - p1);
                        var q1 = p1 + scale * tangent;
                        ctrlPoints.Add(p1);
                        ctrlPoints.Add(q1);
                        //DrawACell(p1.ToPoint(), MakeVisible: true);
                        //DrawACell(q1.ToPoint(), MakeVisible: true);
                    }
                    else if (i == SegPts.Count - 1)
                    {
                        var p0 = SegPts[i - 1];
                        var p1 = SegPts[i];
                        var tangent = (p1 - p0);
                        var q0 = p1 - scale * tangent;
                        ctrlPoints.Add(q0);
                        ctrlPoints.Add(p1);
                        //DrawACell(q0.ToPoint(), MakeVisible: true);
                        //DrawACell(p1.ToPoint(), MakeVisible: true);
                    }
                    else
                    {
                        var p0 = SegPts[i - 1];
                        var p1 = SegPts[i];
                        var p2 = SegPts[i + 1];
                        var tangent = (p1 - p0).Normalized();
                        var q0 = p1 - scale * tangent * ((p1 - p0).Magnitude());
                        var q1 = p1 + scale * tangent * ((p2 - p1).Magnitude());
                        ctrlPoints.Add(q0);
                        ctrlPoints.Add(p1);
                        ctrlPoints.Add(q1);
                        //DrawACell(q0.ToPoint(), MakeVisible: true);
                        //DrawACell(p1.ToPoint(), MakeVisible: true);
                        //DrawACell(q1.ToPoint(), MakeVisible: true);
                    }
                }
            }
            return ctrlPoints;
        }

        public override void OnReady(IntPtr hwnd)
        {
            DoErase();
            //            IsRunning = true;
        }
        void GenSplines(int nSeg)
        {
            Vector p0 = new Vector(_rand.NextDouble() * _nTotCols, _rand.NextDouble() * _nTotRows);
            Vector p1 = new Vector(_rand.NextDouble() * _nTotCols, _rand.NextDouble() * _nTotRows);
            Vector p2 = new Vector(_rand.NextDouble() * _nTotCols, _rand.NextDouble() * _nTotRows);
            Vector p3 = new Vector(_rand.NextDouble() * _nTotCols, _rand.NextDouble() * _nTotRows);
            //DrawACell(p0.toPoint());
            //DrawACell(p1.toPoint());
            //DrawACell(p2.toPoint());
            //DrawLineOfCells(p0.toPoint(), p1.toPoint());
            //DrawLineOfCells(p1.toPoint(), p2.toPoint());
            //DrawLineOfCells(p2.toPoint(), p3.toPoint());

            var q0 = CalculateBezierPoint(0, p0, p1, p2, p3);
            for (int i = 1; i <= nSeg; i++)
            {
                var t = i / (double)nSeg;
                var q1 = CalculateBezierPoint(t, p0, p1, p2, p3);
                DrawLineOfCells(q0.ToPoint(), q1.ToPoint());
                q0 = q1;
            }
        }
        List<Vector> BezierPath(int nSeg, List<Vector> ctrlPoints)
        {
            List<Vector> drawingPoints = new List<Vector>();
            Vector q0 = null;
            for (int i = 0; i < ctrlPoints.Count() - 3; i += 3)
            {
                Vector p0 = ctrlPoints[i];
                Vector p1 = ctrlPoints[i + 1];
                Vector p2 = ctrlPoints[i + 2];
                Vector p3 = ctrlPoints[i + 3];
                if (i == 0)
                {
                    q0 = CalculateBezierPoint(0, p0, p1, p2, p3);
                    drawingPoints.Add(q0);
                }
                for (int j = 1; j <= nSeg; j++)
                {
                    var t = j / (double)nSeg;
                    var q1 = CalculateBezierPoint(t, p0, p1, p2, p3);
                    drawingPoints.Add(q1);
                    DrawLineOfCells(q0.ToPoint(), q1.ToPoint());
                    q0 = q1;
                }
            }
            return drawingPoints;
        }
        /// <summary>
        /// P = (1 - t)^3 * P0 + 3 (1 - t)^2 * t * P1 + 3(1 - t)*t^2 * P2 + t^3 * P3
        /// </summary>
        Vector CalculateBezierPoint(double t, Vector p0, Vector p1, Vector p2, Vector p3)
        {
            double oneMinusT = 1 - t;
            double tSquared = t * t;
            double oneMinusTSquared = oneMinusT * oneMinusT;
            double oneMinusTCubed = oneMinusTSquared * oneMinusT;
            double tCubed = tSquared * t;
            Vector p = oneMinusTCubed * p0;
            p += 3 * oneMinusTSquared * t * p1;
            p += 3 * oneMinusT * tSquared * p2;
            p += tCubed * p3;
            return p;
        }
        class PointD
        {
            public double _X;
            public double _Y;
        }
        class Vector : PointD
        {
            public Vector(double X, double Y)
            {
                _X = X;
                _Y = Y;
            }
            public static Vector operator *(double f, Vector v) => new Vector(f * v._X, f * v._Y);
            public static Vector operator *(Vector v, double f) => new Vector(f * v._X, f * v._Y);
            public static Vector operator +(Vector v1, Vector v2) => new Vector(v1._X + v2._X, v1._Y + v2._Y);
            public static Vector operator -(Vector v1, Vector v2) => new Vector(v1._X - v2._X, v1._Y - v2._Y);
            public double Magnitude()
            {
                return Math.Sqrt(_X * _X + _Y * _Y);
            }
            public Vector Normalized()
            {
                var mag = Magnitude();
                if (mag != 0)
                {
                    return new Vector(_X / mag, _Y / mag);
                }
                return new Vector(0, 0);
            }
            public Point ToPoint() => new Point((int)_X, (int)_Y);
            public override string ToString()
            {
                return $"{_X:f0},{_Y:f0}";
            }
        }
        public void DoErase()
        {
            IsRunning = false;
            _ptOld = null;
            _oColor = 0xffffff;
            EraseRect();
            InitWorld();
        }

        internal void OnSizeChanged()
        {
        }
        Point SdPointFromWpfPt(System.Windows.Point pt)
        {
            return new Point((int)(pt.X * xScale), (int)(pt.Y * yScale));
        }
        bool DrawACell(Point pt, ref int oColor, bool MakeVisible = false)
        {
            bool didDraw = false;
            if (pt.X >= 0 && pt.X < _cells.GetLength(0) && pt.Y >= 0 && pt.Y < _cells.GetLength(1))
            {
                if (_cells[pt.X, pt.Y] == 0)
                {
                    _cells[pt.X, pt.Y] = 1;
                    oColor = (oColor + 140) & 0xffffff;
                    //                    _pen = NativeMethods.CreatePen(nPenStyle: 0, nWidth: nPenWidth, nColor: (IntPtr)_oColor);
                    //*
                    NativeMethods.SetPixel(_hdc, pt.X, pt.Y, (IntPtr)oColor);
                    if (MakeVisible)
                    {
                        for (int i = 1; i < 10; i++)
                        {
                            for (int j = 1; j < 10; j++)
                            {
                                NativeMethods.SetPixel(_hdc, pt.X + i, pt.Y + j, (IntPtr)oColor);
                            }
                        }
                    }
                    /*/
                    var br = NativeMethods.CreateSolidBrush((IntPtr)_oColor);
                    wRect.Left = pt.X;
                    wRect.Right = pt.X + _CellWidth;
                    wRect.Top = pt.Y;
                    wRect.Bottom = pt.Y + _CellHeight;

                    NativeMethods.FillRect(_hdc, ref wRect, br);
                    NativeMethods.DeleteObject(br);
                     //*/

                    didDraw = true;
                }
            }
            return didDraw;
        }
        void DrawLineOfCells(Point p1, Point p2, bool MakeVisible = false)
        {
            // http://en.wikipedia.org/wiki/Bresenham%27s\_line\_algorithm
            int x0 = p1.X;
            int y0 = p1.Y;
            int x1 = p2.X;
            int y1 = p2.Y;
            int x, cx, deltax, xstep,
                  y, cy, deltay, ystep,
                   error;
            bool st;
            // find largest delta for pixel steps
            st = (Math.Abs(y1 - y0) > Math.Abs(x1 - x0));
            // if deltay > deltax then swap x,y
            if (st)
            {
                x0 ^= y0; y0 ^= x0; x0 ^= y0; // swap(x0, y0);
                x1 ^= y1; y1 ^= x1; x1 ^= y1; // swap(x1, y1);
            }
            deltax = Math.Abs(x1 - x0);
            deltay = Math.Abs(y1 - y0);
            error = (deltax / 2);
            y = y0;
            if (x0 > x1) { xstep = -1; }
            else { xstep = 1; }
            if (y0 > y1) { ystep = -1; }
            else { ystep = 1; }
            for (x = x0; (x != (x1 + xstep)); x += xstep)
            {
                cx = x; cy = y; // copy of x, copy of y
                // if x,y swapped above, swap them back now
                if (st)
                {
                    cx ^= cy; cy ^= cx; cx ^= cy;
                }
                DrawACell(new Point(cx, cy), ref _oColor, MakeVisible);
                //if (drawit(new Point(cx, cy), br))
                //{
                //    br = m_brushGenerated;
                //}
                error -= deltay; // converge toward end of line
                if (error < 0)
                { // not done yet
                    y += ystep;
                    error += deltax;
                }
            }
        }

        bool IsValidPoint(Point pt)
        {
            if (pt.X >= 0 && pt.X < _cells.GetLength(0) * _CellWidth && pt.Y >= 0 && pt.Y < _cells.GetLength(1) * _CellHeight)
            {
                return true;
            }
            return false;
        }
        internal void MyOnOnMouseDown(Point ptCurrent, IntPtr wParam)
        {
            if (IsValidPoint(ptCurrent))
            {
                _ptOld = ptCurrent;
            }
        }
        internal void MyOnMouseMove(Point ptCurrent, IntPtr wParam)
        {
            if (wParam.ToInt32() == MK_LBUTTON)
            {
                if (_ptOld.HasValue)
                {
                    if (IsValidPoint(ptCurrent))
                    {
                        _ptCurrent = ptCurrent;
                        DrawLineOfCells(_ptOld.Value, _ptCurrent);
                        _ptOld = _ptCurrent;
                    }
                }
            }
            else
            {

            }
        }
        internal async void MyOnOnMouseUp(Point ptCurrent, IntPtr wParam, int msg)
        {
            if (IsValidPoint(ptCurrent))
            {
                if (msg == (int)NativeMethods.WM_.WM_RBUTTONUP)
                {
                    IsRunning = true;
                    if (cts == null)
                    {
                        cts = new CancellationTokenSource();
                    }
                    if (_lstTasks == null)
                    {
                        _lstTasks = new List<Task>();
                    }
                    var tsk = DoAreaFillAsync(ptCurrent);
                    _lstTasks.Add(tsk);
                    await Task.WhenAll(_lstTasks.ToArray());
                    IsRunning = false;
                    _lstTasks.Clear();
                }
                else
                {
                    _ptCurrent = ptCurrent;
                    _ptOld = _ptCurrent;
                    DrawACell(_ptCurrent, ref _oColor);
                }
            }
        }
        static class HResult
        {
            public const int S_OK = 0;
            public const int S_FALSE = 1;
            public const int E_FAIL = unchecked((int)0x80004005);
        }
        internal delegate int DllGetClassObject(
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid ClassId,
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            out IntPtr ppvObject);

        delegate int CanUnloadNowRoutine();
        CanUnloadNowRoutine _deldllCanUnloadNow;

        IntPtr _hModule = IntPtr.Zero;
        /// <summary>Creates com object with the given clsid in the specified file</summary>
        /// <param name="fnameComClass">The path of the module</param>
        /// <param name="clsidOfComObj">The CLSID of the com object</param>
        /// <param name="riid">The IID of the interface requested</param>
        /// <param name="pvObject">The interface pointer. Upon failure pvObject is IntPtr.Zero</param>
        /// <returns>An HRESULT</returns>
        [HandleProcessCorruptedStateExceptions]
        internal int CoCreateFromFile(string fnameComClass, Guid clsidOfComObj, Guid riid, out IntPtr pvObject)
        {
            pvObject = IntPtr.Zero;
            int hr = HResult.E_FAIL;
            try
            {
                _hModule = LoadLibrary(fnameComClass);
                if (_hModule != IntPtr.Zero)
                {
                    IntPtr optrDllGetClassObject = GetProcAddress(_hModule, "DllGetClassObject");
                    if (optrDllGetClassObject != IntPtr.Zero)
                    {
                        var delDllGetClassObject = Marshal.GetDelegateForFunctionPointer<DllGetClassObject>(optrDllGetClassObject);
                        var optrDllCanUnloadNow = GetProcAddress(_hModule, "DllCanUnloadNow");
                        _deldllCanUnloadNow = Marshal.GetDelegateForFunctionPointer<CanUnloadNowRoutine>(optrDllCanUnloadNow);

                        IntPtr pClassFactory = IntPtr.Zero;
                        Guid iidIUnknown = new Guid(IUnknownGuid);
                        hr = delDllGetClassObject(clsidOfComObj, iidIUnknown, out pClassFactory);
                        if (hr == HResult.S_OK)
                        {
                            var classFactory = (IClassFactory)Marshal.GetTypedObjectForIUnknown(pClassFactory, typeof(IClassFactory));
                            hr = classFactory.CreateInstance(IntPtr.Zero, ref riid, out pvObject);
                            Marshal.ReleaseComObject(classFactory);
                            Marshal.Release(pClassFactory);
                        }
                    }
                    else
                    {
                        hr = Marshal.GetHRForLastWin32Error();
                        Debug.Assert(false, $"Unable to find DllGetClassObject: {hr}");
                    }
                }
                else
                {
                    hr = Marshal.GetHRForLastWin32Error();
                    Debug.Assert(false, $"Unable to load {fnameComClass}: {hr}");
                }
            }
            catch (Exception ex)
            {
                var x = ex.ToString(); // HandleProcessCorruptedStateExceptions
                throw new InvalidOperationException(x);

            }
            return hr;
        }


        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr LoadLibrary(string dllName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int FreeLibrary(IntPtr handle);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procname);

        const string IUnknownGuid = "00000001-0000-0000-C000-000000000046";
        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid(IUnknownGuid)]
        private interface IClassFactory
        {
            [PreserveSig]
            int CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject);
            int LockServer(int fLock);
        }
    }
    // the Register For COM Interop is disabled for EXEs, so we need to run regasm <exe> /tlb
    /*
        call "$(DevEnvDir)..\..\vc\Auxiliary\build\vcvarsall.bat" x86
        regasm.exe "$(TargetPath)"  /tlb
     */

    [ComVisible(true)]
    [Guid("98D44702-2AB4-47F3-97A7-85EE798EEF90")]
    public struct Point
    {
        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }
        public int X;
        public int Y;
        public override string ToString()
        {
            return $"({X},{Y})";
        }
    }
    [ComVisible(true)]
    [Guid("94D86FC9-57BC-402C-8E77-6F8EFD49B851")]
    public struct AreaFillData
    {
        public Point StartPoint;
        public Point ArraySize;
        public IntPtr hWnd;
        public bool DepthFirst;
    }

    [ComVisible(true)]
    [Guid("B351FB5A-AB97-4F37-8B72-D8AE7E0ADCA0")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    unsafe public interface IAreaFill
    {
        void DoAreaFill(AreaFillData areaFillData, ref int pIsCancellationRequested, byte* array);
    }

}
