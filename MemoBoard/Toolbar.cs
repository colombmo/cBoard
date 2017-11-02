using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MemoBoard {
    public class Toolbar {
        Canvas canvas;
        public Canvas GUI;
        Canvas GUIBackground;

        int s = 5;
        Canvas[] items = new Canvas[5];
        User user;
        Ellipse el;
        Image img;

        public double xDiff = 0, yDiff = 0;
        double oldXDiff = 0, oldYDiff = 0;

        Dictionary<TouchDevice, bool> isLocked = new Dictionary<TouchDevice, bool>();
        Dictionary<TouchDevice, Point> startingPos = new Dictionary<TouchDevice, Point>();

        bool moving = false;

        public Toolbar(Canvas canvas, User user) {
            this.user = user;
            this.canvas = canvas;

            GUI = new Canvas();
            canvas.Children.Add(GUI);
            
            GUIBackground = new Canvas();
            GUI.Children.Add(GUIBackground);
            for (int i = 0; i < s; i++) {
                items[i] = new Canvas();
                GUIBackground.Children.Add(items[i]);
            }

            int r = 80;

            for (int i = 0; i < s; i++) {
                Ellipse tool = new Ellipse { Width = 60, Height = 60, Fill = new SolidColorBrush(user.getBodyColor()), Name = "Circle"+i};

                if (i == 0)
                    tool.Fill = new SolidColorBrush(Colors.LightCoral);

                Canvas.SetLeft(tool, r + r * Math.Sin(2 * Math.PI / s * i) - 30);
                Canvas.SetTop(tool, r - r * Math.Cos(2 * Math.PI / s * i) - 30);
                GUIBackground.Children.Add(tool);

                Image icon = null;

                // Add icons
                switch (i) {
                    case 0: // Pencil
                        icon = new Image { Source = new BitmapImage(new Uri(@"images\pencil.png", UriKind.Relative)), Width = 50, Height = 50, Name = "Button" + i };
                        items[0].Children.Add(createColorPalette(0, 0));
                        items[0].Children.Add(createSizeSelect(0, 50));
                        Canvas.SetLeft(items[0], Canvas.GetLeft(tool) + 60 + (items[0].ActualWidth / 2));
                        Canvas.SetTop(items[0], Canvas.GetTop(tool) - 30 + (items[0].ActualHeight / 2));
                        items[0].Visibility = Visibility.Hidden;
                        break;
                    case 1: // Eraser
                        icon = new Image { Source = new BitmapImage(new Uri(@"images\eraser.png", UriKind.Relative)), Width = 50, Height = 50, Name = "Button" + i };
                        /*items[1].Children.Add(createSizeSelect(0, 50));
                        Canvas.SetLeft(items[1], Canvas.GetLeft(items[0]) + 30);
                        Canvas.SetTop(items[1], Canvas.GetTop(items[0]));
                        items[1].Visibility = Visibility.Hidden;
                        */
                        break;
                    case 2: // Post-it
                        icon = new Image { Source = new BitmapImage(new Uri(@"images\postit.png", UriKind.Relative)), Width = 50, Height = 50, Name = "Button" + i }; break;
                    case 3: // Redo
                        icon = new Image { Source = new BitmapImage(new Uri(@"images\redo.png", UriKind.Relative)), Width = 50, Height = 50, Name = "Button" + i }; break;
                    case 4: // Undo
                        icon = new Image { Source = new BitmapImage(new Uri(@"images\undo.png", UriKind.Relative)), Width = 50, Height = 50, Name = "Button" + i }; break;
                }

                if (icon != null) {
                    Canvas.SetLeft(icon, Canvas.GetLeft(tool) + 5);
                    Canvas.SetTop(icon, Canvas.GetTop(tool) + 5);
                    GUIBackground.Children.Add(icon);
                    icon.TouchDown += LockTouch;
                    if (i == 2) {
                        icon.TouchLeave += CreatePostit;
                    } else if(i == 3) {
                        icon.TouchLeave += Redo;
                    } else if (i == 4) {
                        icon.TouchLeave += Undo;
                    } else {
                        icon.TouchLeave += ShowSubMenus;
                    }
                }
            }

            GUIBackground.Visibility = Visibility.Hidden;

            el = new Ellipse { Width = 50, Height = 50, Fill = new SolidColorBrush(Colors.Black), Stroke = new SolidColorBrush(Colors.Black), StrokeThickness = 3};
            Canvas.SetLeft(el, r - 25);
            Canvas.SetTop(el, r - 25);
            img = new Image { Source = new BitmapImage(new Uri(@"images\pencil.png", UriKind.Relative)), Width = 40, Height = 40, Name = "CenterBtn" };
            Canvas.SetLeft(img, r - 20);
            Canvas.SetTop(img, r - 20);

            img.TouchDown += LockTouch;
            img.TouchMove += MoveBtn;
            img.TouchUp += ShowFirstMenu;

            GUI.Children.Add(el);
            GUI.Children.Add(img);
            GUI.Visibility = Visibility.Hidden;
            Panel.SetZIndex(GUI, 10);
        }

        private void LockTouch(object sender, TouchEventArgs e) {
            TouchPoint point = e.GetTouchPoint(canvas);
            ulong bodyId = MainWindow.touch_association.getClosestUser(point.Position, MainWindow.users);
            if (bodyId == this.user.getTrackingId()) {
                e.Handled = true;
                e.TouchDevice.Capture((UIElement)sender);
                isLocked[e.TouchDevice] = true;
                startingPos[e.TouchDevice] = point.Position;
                oldXDiff = xDiff;
                oldYDiff = yDiff;
            }
        }

        void MoveBtn(object sender, TouchEventArgs e) {
            if (isLocked.ContainsKey(e.TouchDevice) && isLocked[e.TouchDevice]) {
                double tx = e.GetTouchPoint(canvas).Position.X - startingPos[e.TouchDevice].X;
                double ty = e.GetTouchPoint(canvas).Position.Y - startingPos[e.TouchDevice].Y;

                if (moving || Math.Abs(tx) + Math.Abs(ty) > 20) {
                    xDiff = tx+oldXDiff>300 ? 300 : tx+oldXDiff;
                    yDiff = ty+oldYDiff>300 ? 300 : ty + oldYDiff;
                    xDiff = xDiff < -300 ? -300 : xDiff;
                    yDiff = yDiff < -400 ? -400 : yDiff;
                    moving = true;
                }
            }
        }

        void ShowFirstMenu(object sender, TouchEventArgs e) {
            if (isLocked.ContainsKey(e.TouchDevice) && isLocked[e.TouchDevice]) {
                canvas.ReleaseTouchCapture(e.TouchDevice);
                if (moving) {
                    moving = false;

                    string usedHand = MainWindow.touch_association.getClosestHand(e.GetTouchPoint(canvas).Position, this.user.getTrackingId());
                    // Log new position of toolbar if ti has been moved
                    this.user.logger.log("toolbarPosition", xDiff+", "+yDiff+", "+usedHand);
                } else {
                    if (!GUIBackground.IsVisible)
                        GUIBackground.Visibility = Visibility.Visible;
                    else
                        GUIBackground.Visibility = Visibility.Hidden;
                }
                isLocked.Remove(e.TouchDevice);
                startingPos.Remove(e.TouchDevice);
            }
        }

        void ShowSubMenus(object sender, TouchEventArgs e) {
            if (isLocked.ContainsKey(e.TouchDevice) && isLocked[e.TouchDevice]) {
                canvas.ReleaseTouchCapture(e.TouchDevice);
                int id = int.Parse(((Image)sender).Name.Replace("Button", ""));
                bool wasVisible = items[id].IsVisible;

                // Update selected tool
                this.user.selectedTool = id;

                foreach (Canvas it in items) {
                    it.Visibility = Visibility.Hidden;
                }
                if (!wasVisible)
                    items[id].Visibility = Visibility.Visible;

                // Update center circle color
                el.Fill = new SolidColorBrush(this.user.getTool().color);

                // Show selected tool in center element
                ImageSource imsrc = ((Image)LogicalTreeHelper.FindLogicalNode(GUIBackground, "Button" + this.user.selectedTool)).Source;
                img.Source = imsrc;

                isLocked.Remove(e.TouchDevice);

                // Show selected item
                for (int i = 0; i < s; i++) {
                    if (i == this.user.selectedTool) {
                        ((Ellipse)LogicalTreeHelper.FindLogicalNode(GUIBackground, "Circle" + i)).Fill = new SolidColorBrush(Colors.LightCoral);
                    } else {
                        ((Ellipse)LogicalTreeHelper.FindLogicalNode(GUIBackground, "Circle" + i)).Fill = new SolidColorBrush(Colors.DarkSlateGray);
                    }
                }

                // Log selected tool
                string usedHand = MainWindow.touch_association.getClosestHand(e.GetTouchPoint(canvas).Position, this.user.getTrackingId());
                this.user.logger.log("selectedTool", "selectedTool, " + this.user.getTool().name()+", "+usedHand);
            }
        }

        void UpdateColor(object sender, TouchEventArgs e) {
            if (isLocked.ContainsKey(e.TouchDevice) && isLocked[e.TouchDevice]) {
                canvas.ReleaseTouchCapture(e.TouchDevice);
                Color col = ((SolidColorBrush)((Ellipse)sender).Fill).Color;

                this.user.getTool().color = col;
                el.Fill = new SolidColorBrush(col);
                isLocked.Remove(e.TouchDevice);
                // Log changed color
                string usedHand = MainWindow.touch_association.getClosestHand(e.GetTouchPoint(canvas).Position, this.user.getTrackingId());
                this.user.logger.log("selectedTool", "colorChanged, "+ col + ", " + usedHand);
            }
        }

        void UpdateSize(object sender, TouchEventArgs e) {
            if (isLocked.ContainsKey(e.TouchDevice) && isLocked[e.TouchDevice]) {
                canvas.ReleaseTouchCapture(e.TouchDevice);
                int size = int.Parse(((Rectangle)sender).Name.Replace("Btn", ""));

                // Update selected tool's size
                this.user.getTool().size = size;
                isLocked.Remove(e.TouchDevice);
                // Log changed size
                string usedHand = MainWindow.touch_association.getClosestHand(e.GetTouchPoint(canvas).Position, this.user.getTrackingId());
                this.user.logger.log("selectedTool", "sizeChanged, "+size + ", " + usedHand);
            }
        }

        void CreatePostit(object sender, TouchEventArgs e) {
            // Log createPostit 
            string usedHand = MainWindow.touch_association.getClosestHand(e.GetTouchPoint(canvas).Position, this.user.getTrackingId());
            this.user.logger.log("selectedTool", "postitCreate, postit, " + usedHand);
            // Create postit
            MainWindow mw = (MainWindow)Application.Current.MainWindow;
            mw.createPostit(this.user, usedHand);
        }

        void Undo(object sender, TouchEventArgs e) {
            if (isLocked.ContainsKey(e.TouchDevice) && isLocked[e.TouchDevice]) {
                canvas.ReleaseTouchCapture(e.TouchDevice);
                this.user.undo();
                // Log undo tap
                string usedHand = MainWindow.touch_association.getClosestHand(e.GetTouchPoint(canvas).Position, this.user.getTrackingId());
                this.user.logger.log("selectedTool", "undo, undo, " + usedHand);
            }
        }

        void Redo(object sender, TouchEventArgs e) {
            if (isLocked.ContainsKey(e.TouchDevice) && isLocked[e.TouchDevice]) {
                canvas.ReleaseTouchCapture(e.TouchDevice);
                this.user.redo();
                // Log redo tap
                string usedHand = MainWindow.touch_association.getClosestHand(e.GetTouchPoint(canvas).Position, this.user.getTrackingId());
                this.user.logger.log("selectedTool", "redo, redo, " + usedHand);
            }
        }

        Canvas createColorPalette(double x, double y) {
            Canvas c = new Canvas();

            Color[] colors = new Color[] { Colors.Black, (Color)ColorConverter.ConvertFromString("#cc6155"), (Color)ColorConverter.ConvertFromString("#f5cb62"),
            (Color)ColorConverter.ConvertFromString("#7bcabb"), (Color)ColorConverter.ConvertFromString("#165f99"), (Color)ColorConverter.ConvertFromString("#38367d") };

            for (int i = 0; i < 6; i++) {
                Ellipse col = new Ellipse { Width = 30, Height = 30, Fill = new SolidColorBrush(colors[i]) };
                Canvas.SetLeft(col, x + i * 40 + 15);
                Canvas.SetTop(col, y + 15);
                c.Children.Add(col);
                col.TouchDown += LockTouch;
                col.TouchLeave += UpdateColor;
            }

            return c;
        }

        Canvas createSizeSelect(double x, double y) {
            Canvas c = new Canvas();

            int[] sizes = new int[] { 2, 4, 8, 12, 24, 30 };

            for (int i = 0; i < 6; i++) {
                Rectangle rect = new Rectangle { Width = 40, Height = 40, Fill = new SolidColorBrush(Colors.Transparent), Name = "Btn"+sizes[i], StrokeThickness=2};
                Canvas.SetLeft(rect, x + i * 40 + 15);
                Canvas.SetTop(rect, y + 15);
                
                Ellipse col = new Ellipse { Width = sizes[i], Height = sizes[i], Fill = new SolidColorBrush(Colors.Black) };
                Canvas.SetLeft(col, x + i * 40 + 15 + (30 - sizes[i]) / 2);
                Canvas.SetTop(col, y + 15 + (30 - sizes[i]) / 2);
                c.Children.Add(col);
                c.Children.Add(rect);

                rect.TouchDown += LockTouch;
                rect.TouchLeave += UpdateSize;

            }

            return c;
        }

        public void update(Point pos) {
            double newX = pos.X - GUI.ActualWidth / 2 - 60 + xDiff;
            double newY = pos.Y - GUI.ActualHeight / 2 + yDiff;
            Canvas.SetLeft(GUI, newX);
            Canvas.SetTop(GUI, newY);
        }
    }
}
