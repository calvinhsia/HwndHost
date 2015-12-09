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

namespace Lissajous
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class LissajousWindow : Window
    {
        public TextBox _tbxStatus { get; set; }
        public LissajousWindow()
        {
            InitializeComponent();
            this.Title = "Lissajous";
            WindowState = System.Windows.WindowState.Maximized;
            this.Loaded += (ol, el) =>
            {
                this.Top = 0;
                this.Left = 0;
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
            <Label Content=""AmpX""/>
            <l:MyTextBox 
                Text =""{Binding Path=AmpX}"" 
                ToolTip="""" />
            <Label Content=""AmpY""/>
            <l:MyTextBox 
                Text =""{Binding Path=AmpY}"" 
                ToolTip="""" />
            <Label Content=""FreqX""/>
            <l:MyTextBox 
                Text =""{Binding Path=FreqX}"" 
                ToolTip="""" />
            <Label Content=""FreqY""/>
            <l:MyTextBox 
                Text =""{Binding Path=FreqY}"" 
                ToolTip="""" />
            <Label Content=""Delta""/>
            <l:MyTextBox 
                Text =""{Binding Path=Delta}"" 
                ToolTip="""" />
                

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
    <Button 
        Name=""btnGraph"" 
        Content=""_Graph"" 
        HorizontalAlignment=""Left""
        Margin=""10,2,0,0"" 
        VerticalAlignment=""Top"" 
        ToolTip=""Write to Excel.Hit these keys 'Alt N, N, Enter'
Alt activates Menu
N(Insert) (N Line chart) Enter (choose default 2D line chart)""
        Width=""55""/>
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
                    var bgd = NativeMethods.CreateSolidBrush(
                        new IntPtr(0xffffff));
                    var lissaCtrl = new LissajousControl(this, bgd);

                    var strReader = new System.IO.StringReader(strxaml);
                    var xamlreader = XmlReader.Create(strReader);
                    var grid = (Grid)(XamlReader.Load(xamlreader));
                    grid.DataContext = lissaCtrl;

                    _tbxStatus = (TextBox)grid.FindName("tbxStatus");
                    var btnQuit = (Button)grid.FindName("btnQuit");
                    btnQuit.Click += (ob, eb) =>
                    {
                        App.Current.Shutdown();
                    };
                    _tbxStatus = (TextBox)grid.FindName("tbxStatus");
                    var userCtrl = (UserControl)grid.FindName("MyUserControl");
                    userCtrl.Content = lissaCtrl;
                    this.Content = grid;
                    this.SizeChanged += (os, es) =>
                    {
                        lissaCtrl.OnSizeChanged();
                    };
                    btnQuit.ToolTip = @"";

                }
                catch (Exception ex)
                {
                    this.Content = ex.ToString();
                }
            };
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
    public class LissajousControl : MyHwndHost, INotifyPropertyChanged
    {
        public IntPtr _hdc;
        LissajousWindow _wndParent;
        public Double AmpX { get; set; }
        public Double AmpY { get; set; }
        public Double FreqX { get; set; }
        public Double FreqY { get; set; }
        public Double Delta { get; set; }
        public LissajousControl(LissajousWindow wndParent, IntPtr hbgd) :
            base(hbgd)
        {
            AmpX = 500;
            AmpY = 500;
            Delta = 0;
            FreqX = 1;
            FreqY = 1;
            _wndParent = wndParent;
            _wndParent.Closing += (o, e) =>
            {
                _StopRequested = true;
            };
        }
        public override void OnReady(IntPtr hwnd)
        {
            IsRunning = true;
        }

        bool _StopRequested = false;
        bool _IsRunning = false;
        private Point _boundSize;
        NativeMethods.WinPoint _prevPos = new NativeMethods.WinPoint(0, 0);

        int _ptLen = 1000;
        Point[] _pts;
        int _ptIndex = 0;
        public bool IsRunning
        {
            get { return _IsRunning; }
            set
            {
                if (_IsRunning != value)
                {
                    if (value) // if we're starting
                    {
                        if (_StopRequested)
                        {
                            while (_StopRequested)
                            {
                                Thread.Sleep(100);
                                //System.Windows.Application.Current.Dispatcher.Invoke(
                                //    DispatcherPriority.Background,
                                //    new System.Threading.ThreadStart(() => System.Threading.Thread.Sleep(1)));
                            }
                        }
                        _StopRequested = false;
                        ThreadPool.QueueUserWorkItem((o) =>
                        {
                            _IsRunning = true;
                            SetStatus("Starting");
                            _hdc = NativeMethods.GetDC(_hwnd);
                            if (_boundSize.X == 0)
                            {
                                _boundSize = new Point(
                                    (this.ActualWidth * xScale),
                                    (this.ActualHeight * yScale)
                                );
                            }

                            int nColor = 0;
                            var hForegrd = NativeMethods.CreatePen(nPenStyle: 0, nWidth: 1, nColor: new IntPtr(nColor += 10));
                            _pts = new Point[_ptLen];
                            var time = 0;
                            while (!_StopRequested)
                            {
                                DrawPoint(_ptIndex, _hbrBackground);

                                _pts[_ptIndex] = GetNextPoint(time);
                                DrawPoint(_ptIndex, hForegrd);
                                _ptIndex = GetNextIndex(_ptIndex);
                                time++;
                                if ((time % 10000)==0)
                                {
                                    FreqY += .2;
                                }
                            }
                            SetStatus("stopping");
                            _StopRequested = false; // indicate no pending stop
                            NativeMethods.ReleaseDC(_hwnd, _hdc);
                            _IsRunning = false;
                            var rect = new NativeMethods.WinRect();
                            NativeMethods.GetClientRect(_hwnd, ref rect);
                            NativeMethods.ValidateRect(_hwnd, ref rect);
                        }
                        );
                    }
                    else
                    {// we're stopping
                        _StopRequested = true;
                        //                        Thread.Sleep(100);
                        //while (_IsRunning)
                        //{
                        //    System.Windows.Application.Current.Dispatcher.Invoke(
                        //        DispatcherPriority.Background,
                        //        new System.Threading.ThreadStart(() => System.Threading.Thread.Sleep(1)));
                        //}
                    }
                    _IsRunning = value;
                    RaisePropChanged();
                }
            }
        }

        private Point GetNextPoint(int t)
        {
            var point = new Point();
            point.X = 400 + AmpX * Math.Sin(FreqX * t);
            point.Y = 400 + AmpY * Math.Sin(FreqY * t + Delta);
            return point;
        }

        void DrawPoint(int ptindx, IntPtr hclr)
        {
            Point point = _pts[ptindx];
            var rect = new NativeMethods.WinRect(
                (int)point.X,
                (int)point.Y,
                (int)(point.X + 3),
                (int)(point.Y + 3)
                );
            NativeMethods.FillRect(_hdc, ref rect, hclr);
            //if (point.X != 0 && point.Y != 0)
            //{
            //    NativeMethods.SelectObject(_hdc, hclr);
            //    NativeMethods.MoveToEx(_hdc, (int)point.X, (int)point.Y, ref _prevPos);
            //    var nxt = GetNextIndex(_ptIndex);
            //    var nextPoint = _pts[nxt];
            //    NativeMethods.LineTo(_hdc, (int)nextPoint.X, (int)nextPoint.Y);
            //}
        }
        private int GetNextIndex(int _ptIndex)
        {
            var next = _ptIndex + 1;
            if (next == _ptLen)
            {
                next = 0;
            }
            return next;
        }
        void SetStatus(string msg)
        {
            _wndParent._tbxStatus.Dispatcher.Invoke(
                () =>
                {
                    if (!_StopRequested)
                    {
                        _wndParent._tbxStatus.Text = msg;
                    }
                }
            );
        }
        internal void OnSizeChanged()
        {
            throw new NotImplementedException();
        }
        void RaisePropChanged([CallerMemberName] string propName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
