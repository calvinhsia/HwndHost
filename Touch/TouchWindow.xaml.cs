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

namespace Touch
{
    public partial class TouchWindow : Window
    {
        public TouchWindow()
        {
            InitializeComponent();
            this.Title = "Move, Size, and Rotate the Square";
            this.WindowState = WindowState.Maximized;
            var rect = new Rectangle() { Width = 200, Height = 200, Fill = Brushes.Blue, IsManipulationEnabled = true };
            var canv = new Canvas();
            canv.Children.Add(rect);
            this.Content = canv;
            this.ManipulationStarting += (o, e) =>
            {
                e.ManipulationContainer = this;
                e.Handled = true;
            };
            this.ManipulationDelta += (o, e) =>
            {
                // Get the Rectangle and its RenderTransform matrix.
                Rectangle rectToMove = e.OriginalSource as Rectangle;
                Matrix rectsMatrix = ((MatrixTransform)rectToMove.RenderTransform).Matrix;

                // Rotate the Rectangle.
                rectsMatrix.RotateAt(e.DeltaManipulation.Rotation,
                                     e.ManipulationOrigin.X,
                                     e.ManipulationOrigin.Y);

                // Resize the Rectangle.  Keep it square 
                // so use only the X value of Scale.
                rectsMatrix.ScaleAt(e.DeltaManipulation.Scale.X,
                                    e.DeltaManipulation.Scale.X,
                                    e.ManipulationOrigin.X,
                                    e.ManipulationOrigin.Y);

                // Move the Rectangle.
                rectsMatrix.Translate(e.DeltaManipulation.Translation.X,
                                      e.DeltaManipulation.Translation.Y);

                // Apply the changes to the Rectangle.
                rectToMove.RenderTransform = new MatrixTransform(rectsMatrix);

                Rect containingRect =
                    new Rect(((FrameworkElement)e.ManipulationContainer).RenderSize);

                Rect shapeBounds =
                    rectToMove.RenderTransform.TransformBounds(
                        new Rect(rectToMove.RenderSize));

                // Check if the rectangle is completely in the window.
                // If it is not and intertia is occuring, stop the manipulation.
                if (e.IsInertial && !containingRect.Contains(shapeBounds))
                {
                    e.Complete();
                }
                e.Handled = true;
            };
            this.ManipulationInertiaStarting += (o, e) =>
            {
                // Decrease the velocity of the Rectangle's movement by 
                // 10 inches per second every second.
                // (10 inches * 96 pixels per inch / 1000ms^2)
                e.TranslationBehavior.DesiredDeceleration = 10.0 * 96.0 / (1000.0 * 1000.0);

                // Decrease the velocity of the Rectangle's resizing by 
                // 0.1 inches per second every second.
                // (0.1 inches * 96 pixels per inch / (1000ms^2)
                e.ExpansionBehavior.DesiredDeceleration = 0.1 * 96 / (1000.0 * 1000.0);

                // Decrease the velocity of the Rectangle's rotation rate by 
                // 2 rotations per second every second.
                // (2 * 360 degrees / (1000ms^2)
                e.RotationBehavior.DesiredDeceleration = 720 / (1000.0 * 1000.0);

                e.Handled = true;
            };
        }
    }
}
