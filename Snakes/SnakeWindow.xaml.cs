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
        private SnakeWindow snakePitWindow;
        Size sizePit;
        WinRect rectPit;
        IntPtr bgdColor;
        DispatcherTimer timer = new DispatcherTimer();
        public SnakePit(SnakeWindow snakeWindow, IntPtr bgd) : base(bgd)
        {
            this.snakePitWindow = snakeWindow;
            this.bgdColor = bgd;
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
            sizePit = new Size(ActualWidth, ActualHeight);
            rectPit = new WinRect(0, 0, (int)sizePit.Width, (int)sizePit.Height);
            var hDC = GetDC(_hwnd);
            FillRect(hDC, ref rectPit, bgdColor);

            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.IsEnabled = true;
            var x = 0;
            var y = 0;
            WinRect oldRect = new WinRect();
            timer.Tick += (o, e) =>
              {
//                  var hDC = GetDC(_hwnd);
                  if (oldRect.Left > 0)
                  {
                      FillRect(hDC, ref oldRect, bgdColor);
                  }
                  var rect = new WinRect(x, y, 50 + x, 50 + y);
                  var clr = CreateSolidBrush(new IntPtr(0xffff));
                  SelectObject(hDC, clr);
                  Ellipse(hDC,
                      x, y, 50 + x, 50 + y);
                  x++;
                  y++;
                  oldRect = rect;
              };

        }
        public class Snake
        {
            const int snakeLength = 10; // # cells
            Size snakeSegmentWidth = new Size(10, 10);
            WinRect[] snakeBody = new WinRect[snakeLength];

        }
    }
}
