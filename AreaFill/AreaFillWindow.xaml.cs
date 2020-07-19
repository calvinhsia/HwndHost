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
            <CheckBox Content=""DepthFirst"" 
                IsChecked= ""{Binding Path=DepthFirst}"" />
            <CheckBox Content=""FillViaCPP"" 
                IsChecked= ""{Binding Path=FillViaCPP}"" />
            <Button Name=""btnErase"" Content=""_Erase""/>
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
                btnErase.Click += (o, ee) => { areaFillArea.DoErase(); };
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
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }
        private AreaFillWindow _areaFillWindow;
        private IntPtr _bgdOcean;
        CancellationTokenSource cts;
        TaskCompletionSource<int> tcs;
        bool _IsRunning = false;
        private IntPtr _hdc;
        private bool _ResetRequired;
        Stack<Point> _stack;
        Queue<Point> _queue;
        public Point? _ptOld { get; private set; }
        public bool _fPenDown { get; private set; }

        int _oColor = 0xffffff;
        private IntPtr _pen;

        private Point _ptCurrent;
        byte[,] _cells;
        private int nPenWidth = 1;
        private NativeMethods.WinPoint _prevPoint;
        NativeMethods.WinRect wRect;
        private int MK_LBUTTON = 1;
        private int MK_RBUTTON = 2;

        public bool DepthFirst { get; set; }
        public bool FillViaCPP { get; set; } = true;

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
        private bool _penModeDrag = true;

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

        private Point _ptStartFill;

        unsafe void DoFillViaCPP()
        {
            var guidComClass = new Guid("BB4B9EE1-81DE-400B-A58A-687ED53A02E6");
            var hr = CoCreateFromFile("CppLib.dll", guidComClass, typeof(IAreaFill).GUID, out var pObject);
            var iara = (IAreaFill)Marshal.GetTypedObjectForIUnknown(pObject, typeof(IAreaFill));
            _cells[0, 1] = 2;
            _cells[0, 30] = 5;
            fixed (byte* arr = _cells)
            {
                iara.DoAreaFill(_hwnd, new Point(nTotCols, nTotRows), _ptStartFill, DepthFirst, arr);

            }
            FreeLibrary(_hModule);

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
                        if (cts != null)
                        {
                            cts.Cancel();
                            tcs.Task.Wait();
                        }
                        if (_ResetRequired)
                        {
                            InitWorld();
                        }
                        cts = new CancellationTokenSource();
                        tcs = new TaskCompletionSource<int>();
                        ThreadPool.QueueUserWorkItem((o) =>
                        {
                            try

                            {
                                _IsRunning = true;
                                _stack = new Stack<Point>();
                                _queue = new Queue<Point>();
                                if (FillViaCPP)
                                {
                                    DoFillViaCPP();
                                }
                                else
                                {
                                    if (DepthFirst)
                                    {
                                        _stack.Push(_ptStartFill);
                                        while (_stack.Count > 0 && !cts.IsCancellationRequested)
                                        {
                                            var pt = _stack.Pop();
                                            DoAreaFill(pt);
                                        }
                                    }
                                    else
                                    {
                                        _queue.Enqueue(_ptStartFill);
                                        while (_queue.Count > 0 && !cts.IsCancellationRequested)
                                        {
                                            var pt = _queue.Dequeue();
                                            DoAreaFill(pt);
                                        }
                                    }
                                }
                            }
                            catch (Exception)
                            {
                            }
                            tcs.SetResult(0);
                            _IsRunning = false;
                            RaisePropChanged();
                        }
                        );
                    }
                    else
                    {// we're stopping
                        cts.Cancel();
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
        }

        public override void OnReady(IntPtr hwnd)
        {
            DoErase();
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 100; j++)
                {
                    DrawACell(new Point(i, j));
                }
            }
            DoErase();
            //            IsRunning = true;
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
        void DrawLine(Point pt1, Point pt2)
        {

        }

        bool DrawACell(Point pt)
        {
            bool didDraw = false;
            if (pt.X >= 0 && pt.X < _cells.GetLength(0) && pt.Y >= 0 && pt.Y < _cells.GetLength(1))
            {
                if (_cells[pt.X, pt.Y] == 0)
                {
                    _oColor = (_oColor + 140) & 0xffffff;
                    //                    _pen = NativeMethods.CreatePen(nPenStyle: 0, nWidth: nPenWidth, nColor: (IntPtr)_oColor);
                    var br = NativeMethods.CreateSolidBrush((IntPtr)_oColor);
                    wRect.Left = pt.X;
                    wRect.Right = pt.X + _CellWidth;
                    wRect.Top = pt.Y;
                    wRect.Bottom = pt.Y + _CellHeight;

                    NativeMethods.FillRect(_hdc, ref wRect, br);
                    NativeMethods.DeleteObject(br);

                    //m_oColor = SD.Color.FromArgb((int)(((((uint)m_oColor.ToArgb() & 0xffffff) + 140) & 0xffffff) | 0xff000000));
                    //m_brushFill = new SolidBrush(m_oColor);
                    //var g = Graphics.FromHwnd(_hwnd);
                    //m_oGraphics.FillRectangle(br,
                    //    m_Offset.Width + ptcell.X * m_cellSize.Width,
                    //    m_Offset.Height + ptcell.Y * m_cellSize.Height,
                    //    m_cellSize.Width,
                    //    m_cellSize.Height);

                    //NativeMethods.SelectObject(hdc, _pen);
                    //if (_ptOld.HasValue)
                    //{
                    //    NativeMethods.MoveToEx(hdc, _ptOld.Value.X, _ptOld.Value.Y, ref _prevPoint);
                    //}
                    //NativeMethods.LineTo(hdc, pt.X, pt.Y);
                    //NativeMethods.DeleteObject(_pen);
                    //_ptOld = pt;
                    _cells[pt.X, pt.Y] = 1;
                    didDraw = true;
                }
            }
            return didDraw;
        }
        void DrawLineOfCells(Point p1, Point p2)
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
                DrawACell(new Point(cx, cy));
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
        internal void MyOnOnMouseUp(Point ptCurrent, IntPtr wParam, int msg)
        {
            if (IsValidPoint(ptCurrent))
            {
                if (msg == (int)NativeMethods.WM_.WM_RBUTTONUP)
                {
                    _ptStartFill = ptCurrent;
                    IsRunning = true;
                }
                else
                {
                    _ptCurrent = ptCurrent;
                    _ptOld = _ptCurrent;
                    DrawACell(_ptCurrent);
                }
            }
        }

        private void DoAreaFill(Point ptCurrent)
        {
            if (IsValidPoint(ptCurrent))
            {
                if (_cells[ptCurrent.X, ptCurrent.Y] == 0)
                {
                    DrawACell(ptCurrent);
                    if (this.DepthFirst)
                    {
                        _stack.Push(new Point(ptCurrent.X - 1, ptCurrent.Y));
                        _stack.Push(new Point(ptCurrent.X + 1, ptCurrent.Y));
                        _stack.Push(new Point(ptCurrent.X, ptCurrent.Y + 1));
                        _stack.Push(new Point(ptCurrent.X, ptCurrent.Y - 1));
                    }
                    else
                    {
                        _queue.Enqueue(new Point(ptCurrent.X - 1, ptCurrent.Y));
                        _queue.Enqueue(new Point(ptCurrent.X + 1, ptCurrent.Y));
                        _queue.Enqueue(new Point(ptCurrent.X, ptCurrent.Y + 1));
                        _queue.Enqueue(new Point(ptCurrent.X, ptCurrent.Y - 1));
                    }
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
    }

    [ComVisible(true)]
    [Guid("B351FB5A-AB97-4F37-8B72-D8AE7E0ADCA0")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]

    unsafe public interface IAreaFill
    {
        void DoAreaFill(IntPtr hWnd, Point ArraySize, Point StartPoint, bool DepthFirst, byte* array);
    }

}
