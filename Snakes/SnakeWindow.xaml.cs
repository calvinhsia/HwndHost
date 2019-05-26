using hWndHost;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
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
using static hWndHost.NativeMethods;

namespace Snakes
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class SnakeWindow : Window
    {
        public SnakeWindow()
        {
            InitializeComponent();
            this.Loaded += SnakeWindow_Loaded;
        }

        private void SnakeWindow_Loaded(object sender, RoutedEventArgs e)
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
<DockPanel Grid.Row=""1"">
    <TextBox 
        Name=""tbxStatus"" 
        HorizontalAlignment=""Left"" 
        Height=""23"" 
        Margin=""10,2,0,0"" 
        IsReadOnly=""True""
        TextWrapping=""Wrap"" 
        VerticalAlignment=""Top"" 
        Width=""220""/>
    <Label Content=""SnakeLength""/>
    <TextBox 
        Text =""{Binding Path=SnakeLength}"" 
        Width = ""25""
        ToolTip=""Length of snake"" />
    <Label Content=""NumSnakes""/>
    <TextBox 
        Text =""{Binding Path=NumSnakes}"" 
        Width = ""25""
        ToolTip=""Length of snake"" />
    <Slider 
        HorizontalAlignment=""Left"" 
        Minimum=""0""
        Maximum=""1000""
        Margin=""12,2,0,0"" 
        Value=""{Binding Path=Delay}""
        VerticalAlignment=""Top"" 
        ToolTip=""Change the delay""
        Width=""100""/>
    <Button 
        Name=""btnStopGo"" 
        Content=""_Go"" 
        HorizontalAlignment=""Left"" 
        Margin=""10,2,0,0"" 
        IsEnabled=""{Binding Path=IsStopped}""
        VerticalAlignment=""Top"" 
        Width=""55""/>

</DockPanel>
</Grid>
";

            var bgd = CreateSolidBrush(new IntPtr(0xffffff));
            var snakePit = new SnakePit(this, bgd);
            var strReader = new System.IO.StringReader(strxaml);
            var xamlreader = XmlReader.Create(strReader);
            var grid = (Grid)(XamlReader.Load(xamlreader));
            grid.DataContext = snakePit;

            grid.Children.Add(snakePit);

            this.Content = grid;
            var btnStopGo = (Button)grid.FindName("btnStopGo");
            btnStopGo.Click += async (og, eg) =>
                {
                    snakePit.IsRunning = !snakePit.IsRunning;
                    if (snakePit.IsRunning)
                    {
                        await snakePit.DoSnake();
                    }
                    else
                    {
                    }
                };
        }
    }
    public enum SnakeBehavior
    {
        Random,
        Momentum,
    }
    public class SnakePit : MyHwndHost, INotifyPropertyChanged
    {
        readonly Random rand = new Random(1);
        Cell[,] cells;
        int numrows;
        int numcols;
        readonly int cellWidth = 15;
        readonly int cellHeight = 15;
        Snake[] snakes;
        readonly SnakeWindow snakePitWindow;
        Size sizePit; // pixels
        Rect rectPit;
        readonly IntPtr bgdColor;
        readonly IntPtr colorSnake = CreateSolidBrush(new IntPtr(0xff));
        //        DispatcherTimer timer = new DispatcherTimer();

        bool _IsRunning;
        public bool IsRunning { get { return _IsRunning; } set { if (_IsRunning != value) { _IsRunning = value; OnMyPropertyChanged(); } } }
        public bool IsStopped => !IsRunning;
        public int SnakeLength { get; set; } = 10;
        public int Delay { get; set; } = 200; //msecs

        public int NumSnakes { get; set; } = 1;

        public SnakeBehavior Behavior { get; set; } = SnakeBehavior.Random;

        Task taskAnimate;

        public event PropertyChangedEventHandler PropertyChanged;
        void OnMyPropertyChanged([CallerMemberName] string propName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public SnakePit(SnakeWindow snakeWindow, IntPtr bgd) : base(bgd)
        {
            this.snakePitWindow = snakeWindow;
            this.bgdColor = bgd;
            //timer.Interval = TimeSpan.FromMilliseconds(SnakeSpeed);
            //timer.IsEnabled = true;
            //timer.Tick += (o, e) =>
            //{
            //    for (int i = 0; i < snakes.Length; i++)
            //    {
            //        if (!snakes[i].Move())
            //        {
            //            var t=DoSnake();
            //            break;
            //        }
            //    }
            //};
        }
        void DrawCell(Cell cell, IntPtr color)
        {
            var rect = new Rect(cell.x * cellWidth, cell.y * cellHeight, cellWidth, cellHeight);
            DrawRect(rect, color);
        }
        void DrawRect(Rect rect, IntPtr color)
        {
            var hDC = GetDC(_hwnd);
            if (color != bgdColor)
            {
                SelectObject(hDC, color);
                Ellipse(hDC,
                    (int)(rect.X),
                    (int)(rect.Y),
                    (int)(rect.X + rect.Width),
                    (int)(rect.Y + rect.Height));
            }
            else
            {
                var wr = rect.ToWinRect();
                FillRect(hDC, ref wr, bgdColor);
            }
            ReleaseDC(_hwnd, hDC);
        }
        public override void OnReady(IntPtr hwnd)
        {
            DoSnake().GetAwaiter();
        }
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            DoSnake().GetAwaiter();
        }
        public async Task DoSnake()
        {
            if (taskAnimate?.IsCompleted == false)
            {
                IsRunning = false;
                await taskAnimate;
            }
            sizePit = new Size(ActualWidth * xScale, ActualHeight * yScale);
            rectPit = new Rect(0, 0, (int)sizePit.Width, (int)sizePit.Height);
            DrawRect(rectPit, bgdColor);
            numrows = (int)(ActualHeight * yScale / cellHeight);
            numcols = (int)(ActualWidth * xScale / cellWidth);
            cells = new Cell[numrows, numcols];
            snakes = new Snake[NumSnakes];
            //WinPoint oldpt=new WinPoint();
            //MoveToEx(hDC, 0, 0, ref oldpt);
            //LineTo(hDC, 0, numrows - 1);
            //DrawCell(0, 0, IntPtr.Zero);
            //DrawCell(0, numrows - 1, IntPtr.Zero);
            //DrawCell(numcols - 1, 0, IntPtr.Zero);
            //DrawCell(numcols - 1, numrows - 1, IntPtr.Zero);
            for (int i = 0; i < NumSnakes; i++)
            {
                snakes[i] = new Snake(this, i);
            }
            taskAnimate = Task.Run(async () =>
             {
                 while (IsRunning)
                 {
                     for (int i = 0; i < snakes.Length; i++)
                     {
                         if (!snakes[i].Move())
                         {
                             IsRunning = false;
                             break;
                         }
                     }
                     if (Delay > 0)
                     {
                         await Task.Delay(TimeSpan.FromMilliseconds(Delay));
                     }
                 }
             });
            //var hDC = GetDC(_hwnd);
            //FillRect(hDC, ref rectPit, bgdColor);

            //var x = 0;
            //var y = 0;
            //WinRect oldRect = new WinRect();
        }

        public class Cell
        {
            public int snakeNum;
            public int x;
            public int y;
            public Cell(int snakeNum, int x, int y)
            {
                this.snakeNum = snakeNum;
                this.x = x;
                this.y = y;
            }
            public override string ToString()
            {
                return $"{ x},{y}";
            }
        }

        public class Snake
        {
            readonly List<Cell> snakeBody = new List<Cell>();
            readonly SnakePit snakePit;
            readonly int snakeNum;
            readonly int[] randDirs = new[] { 0, 1, 2, 3 };
            public Snake(SnakePit snakePit, int snakeNum)
            {
                this.snakePit = snakePit;
                this.snakeNum = snakeNum;
                for (int i = 0; i < snakePit.SnakeLength; i++)
                {
                    if (!Move(fEraseTail: false))
                    {

                    }
                    //var cell = new Cell(snakeNum, x, y + i);
                    //snakePit.cells[y + i, x] = cell;
                    //snakeBody.Add(cell);
                    //snakePit.DrawCell(cell, snakePit.colorSnake);
                }
            }
            void Shuffledirs()
            {
                for (int i = 0; i < randDirs.Length; i++)
                {
                    var r = snakePit.rand.Next(4);
                    var tmp = randDirs[i];
                    randDirs[i] = randDirs[r];
                    randDirs[r] = tmp;
                }
            }
            internal bool Move(bool fEraseTail = true)
            {// we just need to erase the tail and draw the head
                var didMove = false;
                int xStart;
                int yStart;
                if (snakeBody.Count == 0)
                {
                    while (true)
                    {
                        xStart = snakePit.rand.Next(snakePit.numcols);
                        yStart = snakePit.rand.Next(snakePit.numrows);
                        if (snakePit.cells[yStart, xStart] == null)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    xStart = snakeBody[snakeBody.Count - 1].x;
                    yStart = snakeBody[snakeBody.Count - 1].y;
                }
                if (fEraseTail)
                {
                    var tail = snakeBody[0];
                    snakePit.DrawCell(tail, snakePit.bgdColor);
                    snakeBody.RemoveAt(0);
                    snakePit.cells[tail.y, tail.x] = null;
                }
                Shuffledirs();
                var gotGoodDir = false;
                int x = 0, y = 0;
                for (int i = 0; i < randDirs.Length; i++)
                {
                    x = xStart;
                    y = yStart;
                    switch (randDirs[i])
                    {
                        case 0:
                            x += 1;
                            break;
                        case 1:
                            x -= 1;
                            break;
                        case 2:
                            y += 1;
                            break;
                        case 3:
                            y -= 1;
                            break;
                    }
                    if (x >= 0 && x < snakePit.numcols)
                    {
                        if (y >= 0 && y < snakePit.numrows)
                        {
                            if (snakePit.cells[y, x] == null)
                            {
                                gotGoodDir = true;
                                break;
                            }
                        }
                    }
                }
                if (gotGoodDir)
                {
                    var newHead = new Cell(this.snakeNum, x, y);
                    snakeBody.Add(newHead);
                    snakePit.DrawCell(newHead, snakePit.colorSnake);
                    snakePit.cells[newHead.y, newHead.x] = newHead;
                    didMove = true;
                }
                else
                {
                    "".ToString();
                }
                return didMove;
            }
        }
    }
}
