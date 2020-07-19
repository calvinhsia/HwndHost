using hWndHost;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using SD = System.Drawing;
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
                                _hdc = NativeMethods.GetDC(_hwnd);
                                while (!cts.IsCancellationRequested)
                                {
                                    //                                  DoGenerations();
                                }
                                NativeMethods.ReleaseDC(_hwnd, _hdc);
                                var rect = new NativeMethods.WinRect();
                                NativeMethods.GetClientRect(_hwnd, ref rect);
                                NativeMethods.ValidateRect(_hwnd, ref rect);
                            }
                            catch (Exception)
                            {
                            }
                            tcs.SetResult(0);
                            _IsRunning = false;
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

        public SD.Point? _ptOld { get; private set; }
        public bool _fPenDown { get; private set; }

        SD.SolidBrush m_brushFill = new SD.SolidBrush(SD.Color.Blue);
        SD.Color m_oColor = SD.Color.Black;
        private IntPtr _pen;
        private SD.Point _ptCurrent;
        bool[,] _cells;
        private int nPenWidth = 1;
        private NativeMethods.WinPoint _prevPoint;

        public AreaFillArea(AreaFillWindow areaFillWindow, IntPtr bgdOcean) : base(bgdOcean)
        {
            LstWndProcMsgs.Add((int)NativeMethods.WM_.WM_NCHITTEST);
            LstWndProcMsgs.Add((int)NativeMethods.WM_.WM_MOUSEMOVE);
            LstWndProcMsgs.Add((int)NativeMethods.WM_.WM_LBUTTONDOWN);
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
                        var x = lParam.ToInt32() & 0xffff;
                        var y = (lParam.ToInt32() >> 16) & 0xffff;
                        var pt = new SD.Point(x, y);
                        //var arg = new MouseEventArgs(Mouse.PrimaryDevice, 0);
                        //arg.RoutedEvent = Mouse.MouseMoveEvent;
                        switch ((NativeMethods.WM_)msg)
                        {
                            case NativeMethods.WM_.WM_MOUSEMOVE:
                                this.MyOnMouseMove(pt);
                                break;
                            case NativeMethods.WM_.WM_LBUTTONDOWN:
                                this.MyOnOnMouseDown(pt);
                                break;
                            case NativeMethods.WM_.WM_LBUTTONUP:
                                this.MyOnOnMouseUp(pt);
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
            _cells = new bool[nTotCols, nTotRows];
        }

        public override void OnReady(IntPtr hwnd)
        {
            InitWorld();
            DrawACell(new SD.Point(100, 100));
            //            IsRunning = true;
        }

        internal void OnSizeChanged()
        {
        }
        SD.Point SdPointFromWpfPt(Point pt)
        {
            return new SD.Point((int)(pt.X * xScale), (int)(pt.Y * yScale));
        }
        void DrawLine(SD.Point pt1, SD.Point pt2)
        {

        }
        bool DrawACell(SD.Point pt)
        {
            bool didDraw = false;
            if (pt.X >= 0 && pt.X < _cells.GetLength(0) && pt.Y >= 0 && pt.Y < _cells.GetLength(1))
            {
                if (!_cells[pt.X, pt.Y])
                {
                    var hdc = NativeMethods.GetDC(_hwnd);
                    m_oColor = SD.Color.FromArgb((int)(((((uint)m_oColor.ToArgb() & 0xffffff) + 140) & 0xffffff) | 0xff000000));
                    //                    NativeMethods.SetPixel(hdc, pt.X, pt.Y, (IntPtr)m_oColor.ToArgb());
                    _pen = NativeMethods.CreatePen(nPenStyle: 0, nWidth: nPenWidth, nColor: (IntPtr)m_oColor.ToArgb());
                    var br = NativeMethods.CreateSolidBrush((IntPtr)m_oColor.ToArgb());
                    NativeMethods.SelectObject(hdc, br);
                    if (_ptOld.HasValue)
                    {
                        NativeMethods.MoveToEx(hdc, _ptOld.Value.X, _ptOld.Value.Y, ref _prevPoint);
                    }
                    NativeMethods.LineTo(hdc, pt.X, pt.Y);
                    NativeMethods.DeleteObject(_pen);
                    NativeMethods.ReleaseDC(_hwnd, hdc);
                    didDraw = true;
                }
            }
            return didDraw;
        }
        bool IsValid(SD.Point pt)
        {
            if (pt.X >= 0 && pt.X < _cells.GetLength(0) && pt.Y >= 0 && pt.Y < _cells.GetLength(1))
            {
                return true;
            }
            return false;
        }
        internal void MyOnOnMouseDown(SD.Point ptCurrent)
        {
            if (Mouse.PrimaryDevice.LeftButton == MouseButtonState.Pressed)
            {
                this._penModeDrag = !this._penModeDrag;
            }
            else
            {
                if (IsValid(ptCurrent))
                {
                    if (_penModeDrag)
                    {
                        _ptOld = ptCurrent;
                    }
                    else
                    {
                        if (Mouse.PrimaryDevice.LeftButton == MouseButtonState.Pressed)
                        {
                            _fPenDown = false;
                            _ptOld = null;
                        }
                        else
                        {
                            _fPenDown = true;
                            _ptCurrent = ptCurrent;
                            if (!_ptOld.HasValue)
                            {
                                _ptOld = _ptCurrent;
                            }
                            //                        InvalidateVisual();
                        }
                    }
                }
            }
        }
        internal void MyOnMouseMove(SD.Point ptCurrent)
        {
            if (_penModeDrag)
            {
                //                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    if (_ptOld.HasValue)
                    {
                        //                        var ptCurrent = SdPointFromWpfPt(e.GetPosition(this));
                        if (IsValid(ptCurrent))
                        {
                            _ptCurrent = ptCurrent;
                            DrawACell(_ptCurrent);
                            _cells[_ptCurrent.X, _ptCurrent.Y] = true;
                            _ptOld = _ptCurrent;
                        }
                    }
                }
            }
            else
            {

            }
        }
        internal void MyOnOnMouseUp(SD.Point ptCurrent)
        {
            //if (_fPenDown)
            {
                if (IsValid(ptCurrent))
                {
                    _ptCurrent = ptCurrent;
                    _ptOld = _ptCurrent;
                    _fPenDown = false;
                    _cells[_ptCurrent.X, _ptCurrent.Y] = true;
                    DrawACell(_ptCurrent);
                    //                InvalidateVisual();
                }
            }
        }
    }
}
