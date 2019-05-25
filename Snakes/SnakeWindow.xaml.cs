using hWndHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            var bgd = CreateSolidBrush(
                new IntPtr(0xf00f));
            var snakePit = new SnakePit(this, bgd);
            this.Content = snakePit;

        }
    }
    public class SnakePit : MyHwndHost
    {
        Random rand = new Random(1);
        Cell[,] cells;
        int numrows;
        int numcols;
        int cellWidth = 15;
        int cellHeight = 15;
        int numSnakes = 1;
        Snake[] snakes;
        SnakeWindow snakePitWindow;
        Size sizePit; // pixels
        WinRect rectPit;
        IntPtr bgdColor;
        IntPtr colorSnake = CreateSolidBrush(new IntPtr(0xffff));
        DispatcherTimer timer = new DispatcherTimer();
        public SnakePit(SnakeWindow snakeWindow, IntPtr bgd) : base(bgd)
        {
            this.snakePitWindow = snakeWindow;
            this.bgdColor = bgd;
            timer.Interval = TimeSpan.FromMilliseconds(10);
            timer.IsEnabled = true;
            timer.Tick += (o, e) =>
            {
                for (int i = 0; i < numSnakes; i++)
                {
                    if (!snakes[i].Move())
                    {
                        DoSnake();
                        break;
                    }
                }
                ////                  var hDC = GetDC(_hwnd);
                //if (oldRect.Left > 0)
                //{
                //    FillRect(hDC, ref oldRect, bgdColor);
                //}
                //var rect = new WinRect(x, y, 50 + x, 50 + y);
                //var clr = CreateSolidBrush(new IntPtr(0xffff));
                //SelectObject(hDC, clr);
                //Ellipse(hDC,
                //    x, y, 50 + x, 50 + y);
                //x++;
                //y++;
                //oldRect = rect;
            };
        }
        void DrawCell(int x, int y, IntPtr color)
        {
            var hDC = GetDC(_hwnd);
            //if (oldRect.Left > 0)
            //{
            //    FillRect(hDC, ref oldRect, bgdColor);
            //}
            //var rect = new WinRect(
            //    x * cellWidth,
            //    y * cellHeight,
            //    (x + 1) * cellWidth,
            //    (y + 1) * cellHeight);
            SelectObject(hDC, color);
            Ellipse(hDC,
                x * cellWidth,
                y * cellHeight,
                (x + 1) * cellWidth,
                (y + 1) * cellHeight);
            x++;
            y++;
            ReleaseDC(_hwnd, hDC);
        }
        public override void OnReady(IntPtr hwnd)
        {
            DoSnake();
        }
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            DoSnake();
        }
        private void DoSnake()
        {
            sizePit = new Size(ActualWidth * xScale, ActualHeight * yScale);
            rectPit = new WinRect(0, 0, (int)sizePit.Width, (int)sizePit.Height);
            numrows = (int)(ActualHeight * yScale / cellHeight);
            numcols = (int)(ActualWidth * xScale / cellWidth);
            cells = new Cell[numrows, numcols];
            snakes = new Snake[numSnakes];
            //WinPoint oldpt=new WinPoint();
            //MoveToEx(hDC, 0, 0, ref oldpt);
            //LineTo(hDC, 0, numrows - 1);
            DrawCell(0, 0, IntPtr.Zero);
            DrawCell(0, numrows - 1, IntPtr.Zero);
            DrawCell(numcols - 1, 0, IntPtr.Zero);
            DrawCell(numcols - 1, numrows - 1, IntPtr.Zero);
            for (int i = 0; i < numSnakes; i++)
            {
                snakes[i] = new Snake(this, i);
            }
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
                return $"{x},{y}";
            }
        }

        public class Snake
        {
            const int snakeLength = 10; // # cells
            List<Cell> snakeBody = new List<Cell>();
            SnakePit snakePit;
            int snakeNum;
            int[] randDirs = new[] { 0, 1, 2, 3 };
            public Snake(SnakePit snakePit, int snakeNum)
            {
                this.snakePit = snakePit;
                this.snakeNum = snakeNum;
                var x = snakePit.numcols / 2;
                var y = snakePit.numrows / 2;
                for (int i = 0; i < snakeLength; i++)
                {
                    var cell = new Cell(snakeNum, x, y + i);
                    snakePit.cells[y + i, x] = cell;
                    snakeBody.Add(cell);
                    snakePit.DrawCell(cell.x, cell.y, snakePit.colorSnake);
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
            internal bool Move()
            {// we just need to erase the tail and draw the head
                var didMove = false;
                var oldHead = snakeBody[snakeLength - 1];
                var tail = snakeBody[0];
                snakePit.DrawCell(tail.x, tail.y, snakePit.bgdColor);
                snakeBody.RemoveAt(0);
                snakePit.cells[tail.y, tail.x] = null;
                Shuffledirs();
                var gotGoodDir = false;
                int x = 0, y = 0;
                for (int i = 0; i < randDirs.Length; i++)
                {
                    x = oldHead.x;
                    y = oldHead.y;
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
                    snakePit.DrawCell(newHead.x, newHead.y, snakePit.colorSnake);
                    snakePit.cells[newHead.y, newHead.x] = newHead;
                    didMove = true;
                }
                return didMove;
            }
        }
    }
}
