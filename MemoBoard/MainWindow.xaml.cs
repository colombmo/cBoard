using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MemoBoard {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public static AssTouch touch_association;
        
        public static Dictionary<ulong, User> users;
        public static Dictionary<User, Toolbar> toolbars;
        Dictionary<User, Polygon> oldPol = new Dictionary<User, Polygon>();

        public Dictionary<TouchDevice, User> interactingUser = new Dictionary<TouchDevice, User>();
        Dictionary<TouchDevice, Point> touchPosition = new Dictionary<TouchDevice, Point>();
        Dictionary<TouchDevice, Canvas> touchedCanvas = new Dictionary<TouchDevice, Canvas>();
        Dictionary<TouchDevice, Tool> tools = new Dictionary<TouchDevice, Tool>();
        Dictionary<TouchDevice, Polyline> lines = new Dictionary<TouchDevice, Polyline>();
        Dictionary<TouchDevice, PointCollection> points = new Dictionary<TouchDevice, PointCollection>();
        Dictionary<TouchDevice, bool> moving = new Dictionary<TouchDevice, bool>();
        Dictionary<TouchDevice, string> closestHand = new Dictionary<TouchDevice, string>();
        Dictionary<TouchDevice, StringBuilder> logs = new Dictionary<TouchDevice, StringBuilder>();
        
        private static Color[] colors = new Color[] { (Color)ColorConverter.ConvertFromString("#1abc9c"), (Color)ColorConverter.ConvertFromString("#e74c3c"),
            (Color)ColorConverter.ConvertFromString("#f1c40f"), (Color)ColorConverter.ConvertFromString("#3498db"),
            (Color)ColorConverter.ConvertFromString("#9b59b6"), (Color)ColorConverter.ConvertFromString("#34495e") };
        private static string[] colorNames = new string[] { "green", "red", "yellow", "blue", "purple", "grey" };

        public MainWindow() {
            // Initialize component
            InitializeComponent();

            // Set up ViewModel, assign to DataContext etc.
            Closing += OnWindowClosing;

            // Load configuration
            ConfigLoader.init();

            // Start touch association library
            touch_association = new AssTouch(window);

            users = new Dictionary<ulong, User>();
            toolbars = new Dictionary<User, Toolbar>();

            // Event handler to get new detected bodies
            touch_association.NewBodyDetected += newBodyDetected;

            // Eventhandler to update GUI (show toolbar in the correct position), each 60 ms
            DispatcherTimer t = new DispatcherTimer();
            t.Tick += updateGUI;
            t.Interval = new TimeSpan(0, 0, 0, 0, 120);
            t.Start();

            // Eventhandler to log user position
            DispatcherTimer t1 = new DispatcherTimer();
            t1.Tick += logPosition;
            t1.Interval = new TimeSpan(0, 0, 0, 5, 0);
            t1.Start();

            // Timer to update canvas content to reduce flickering
            DispatcherTimer t2 = new DispatcherTimer();
            t2.Tick += removeFlickering;
            t2.Interval = new TimeSpan(0, 0, 0, 0, 20);
            t2.Start();

            // Create global GUI
            GUI g = new GUI(GUICanvas);
        }

        // Save log files on window closing
        public void OnWindowClosing(object sender, CancelEventArgs e) {
            // Handle closing logic, set e.Cancel as needed
            foreach (var u in users.Values) {
                u.logger.saveFile();
            }
        }

        private void newBodyDetected(object sender, NewBodyDetectedEventArgs e) {
            // Add new user to user's list with BodyId as identifier
            // Create new user
            foreach(ulong id in e.BodyId) {
                // If user is not yet registered, handle returning user
                if (!users.ContainsKey(id)) {
                    // Easy check to handle occlusions
                    double distance = double.MaxValue;
                    ulong closest = ulong.MinValue;
                    foreach (Body b in e.Bodies) {
                        if (b.TrackingId == id) {
                            foreach (User u in users.Values) {
                                double newDist = Math.Sqrt(3*Math.Pow(u.getPosition().X - b.Joints[JointType.SpineShoulder].Position.X,2) + Math.Pow(u.getPosition().Z - b.Joints[JointType.SpineShoulder].Position.Z, 2))/2;
                                if (!u.getDetectedState() && newDist < distance && newDist < 2) {
                                    distance = newDist;
                                    closest = u.getTrackingId();
                                }
                            }
                        }
                    }
                    if (closest != ulong.MinValue && id != closest) {
                        users[closest].setTrackingId(id);
                        users[id] = users[closest];
                        users.Remove(closest);
                    } else { // The user that has been detected is a new user
                        users[id] = new User(id,colors[users.Count%colors.Count()], colorNames[users.Count % colors.Count()]);
                        toolbars[users[id]] = new Toolbar(GUICanvas, users[id]);
                    }
                }
            }
        }

        private void logPosition(object sender, EventArgs e) {
            foreach (User u in users.Values) {
                double dist = touch_association.getDistanceFromScreen(u.getTrackingId());
                Point position = touch_association.getBodyPosition(u.getTrackingId());
                // Log distance from screen and x,y position
                u.logger.log("bodyPosition", (-dist) + ", " + position.X + ", " + position.Y);
            }
        }

        private void updateGUI(object sender, EventArgs e) {
            
            // Keep post-it layer to front
            Panel.SetZIndex((Canvas)LogicalTreeHelper.FindLogicalNode(MemoBoardCanvas, "PostitLayer"), MemoBoardCanvas.Children.Count + 3);
            var t = GUICanvas.Children.OfType<FrameworkElement>().ToList();
            foreach (var a in t) {
                if (a.Name == "body")
                    GUICanvas.Children.Remove(a);
            }

            // Move/hide/show toolbar in front of each user
            foreach (User u in users.Values) {
                // Update user position
                u.update(touch_association.getBodyPositionInCameraCoord(u.getTrackingId()));
                double dist = touch_association.getDistanceFromScreen(u.getTrackingId());
                Point position = touch_association.getBodyPosition(u.getTrackingId());

                if (dist > -1.4) {
                    // Update position of GUI (hide GUI when interacting with the screen)
                    if (interactingUser.ContainsValue(u)) {
                        toolbars[u].GUI.Visibility = Visibility.Hidden;
                    } else {
                        toolbars[u].GUI.Visibility = Visibility.Visible;
                        toolbars[u].update(position);
                    }

                    // Add all joints to canvas
                    if (ConfigLoader.shadowType == "dots") {
                        Ellipse el;
                        foreach (Point j in touch_association.getJoints(u.getTrackingId())) {
                            el = new Ellipse { Name = "body", Fill = u.getBodyBrush(), Width = 10, Height = 10, IsHitTestVisible = false };
                            Canvas.SetLeft(el, j.X - 5);
                            Canvas.SetTop(el, j.Y - 5);
                            GUICanvas.Children.Add(el);
                        }
                    } else if (ConfigLoader.shadowType == "shadow") {
                        Polygon p = touch_association.getShadow(u.getTrackingId(), u.getBodyBrush());
                        if (p.Points != null && p.Points.Count < 400) {
                            oldPol[u] = p;
                        } else if(oldPol.ContainsKey(u)) {
                            p = oldPol[u];
                        }
                        Panel.SetZIndex(p, 0);
                        GUICanvas.Children.Add(p);
                    }
                } else {
                    toolbars[u].GUI.Visibility = Visibility.Hidden;
                }
            }
        }

        private void removeFlickering(object sender, EventArgs e) {
            // Repaint canvas
            MemoBoardCanvas.InvalidateVisual();
            GUICanvas.InvalidateVisual();
        }

        // Handle multitouch drawing
        // For each new touch, generate a new initial point
        private void TouchStart(object sender, TouchEventArgs e) {
            e.TouchDevice.Capture((Canvas)sender);
            e.Handled = true;
            TouchPoint point = e.GetTouchPoint((Canvas)sender);
            touchedCanvas[e.TouchDevice] = (Canvas)sender;
            touchPosition[e.TouchDevice] = point.Position;
            moving[e.TouchDevice] = false;

            // Move around or edit if it's a post-it, else draw on the canvas
            if (touchedCanvas[e.TouchDevice].Name == "Postit" || touchedCanvas[e.TouchDevice].Name == "PostitBorder") {
                try {
                    ulong bodyId = touch_association.getClosestUser(e.GetTouchPoint(MemoBoardCanvas).Position, users);
                    interactingUser[e.TouchDevice] = users[bodyId];
                    closestHand[e.TouchDevice] = touch_association.getClosestHand(e.GetTouchPoint(MemoBoardCanvas).Position, bodyId);
                } catch (Exception) {}
                Canvas can = touchedCanvas[e.TouchDevice].Name != "Postit" ? ((Canvas)((Canvas)touchedCanvas[e.TouchDevice].Parent).Parent) : touchedCanvas[e.TouchDevice];
                Canvas pit = (Canvas)LogicalTreeHelper.FindLogicalNode(MemoBoardCanvas, "PostitLayer");
                touchPosition[e.TouchDevice] = e.GetTouchPoint(pit).Position;
                tools[e.TouchDevice] = null;
                int max = 0;
                foreach (UIElement ch in pit.Children) {
                    max = Panel.GetZIndex(ch) > max ? Panel.GetZIndex(ch) : max;
                }
                Panel.SetZIndex(can, max+1);
            } else {
                try {
                    ulong bodyId = touch_association.getClosestUser(e.GetTouchPoint(MemoBoardCanvas).Position, users);
                    closestHand[e.TouchDevice] = touch_association.getClosestHand(e.GetTouchPoint(MemoBoardCanvas).Position, bodyId);
                    Tool tool = users[bodyId].getTool();
                    tools[e.TouchDevice] = tool;
                    interactingUser[e.TouchDevice] = users[bodyId];
                    // Empty redo stack
                    users[bodyId].cancelRedo();
                } catch (Exception) {
                    tools[e.TouchDevice] = new Tool(Type.Pencil, Colors.Black, 4);
                }
                if (tools[e.TouchDevice].type == Type.Pencil) {
                    (lines[e.TouchDevice] = new Polyline {
                        StrokeThickness = tools[e.TouchDevice].size, Stroke = new SolidColorBrush(tools[e.TouchDevice].color),
                        StrokeLineJoin = PenLineJoin.Round, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round,
                        Points = new PointCollection()
                    }).Points.Add(point.Position);

                    touchedCanvas[e.TouchDevice].Children.Add(lines[e.TouchDevice]);
                }

                // Log touches on screen
                if (closestHand.ContainsKey(e.TouchDevice)) {
                    Point pos = e.GetTouchPoint(MemoBoardCanvas).Position;
                    logs[e.TouchDevice] = new StringBuilder();
                    logs[e.TouchDevice].Append("started, " + pos.X + ", " + pos.Y + ", " + closestHand[e.TouchDevice] + ", "
                        + tools[e.TouchDevice].name() + ", " + tools[e.TouchDevice].size + ", " + tools[e.TouchDevice].color + "\r\n");
                }
            }
        }

        // When leaving screen, transform hand drawn lines to basic shapes, or smooth out hand drawn shapes
        private void TouchEnd(object sender, TouchEventArgs e) {
            e.Handled = true;
            if (touchedCanvas.ContainsKey(e.TouchDevice) && e.TouchDevice.Captured == touchedCanvas[e.TouchDevice]) {
                Canvas can = touchedCanvas[e.TouchDevice];
                can.ReleaseTouchCapture(e.TouchDevice);
            
                if (tools[e.TouchDevice] == null) {
                    if (!moving[e.TouchDevice] && can.Name == "Postit") {
                        // Edit Postit
                        can.Name = "editingPostit";
                        can.RenderTransform = null;
                        Canvas cmd = createPostitCommands();
                        can.Children.Add(cmd);
                        cmd.Width = can.Width;
                    } else if (moving[e.TouchDevice]) {
                        // Log postit moved
                        interactingUser[e.TouchDevice].logger.log("postit", "moved, " + CustomPostitId.GetCustomId(can)+", "+closestHand[e.TouchDevice]);
                    }
                } else if (tools[e.TouchDevice].type == Type.Pencil) {
                    List<Point> temp = PointsReduction.NoiseReduction(lines[e.TouchDevice].Points);

                    if (!ShapeRecognizer.recognizeShape(temp, can, tools[e.TouchDevice].color, tools[e.TouchDevice].size, interactingUser[e.TouchDevice])) {
                        //CreatePolyBezierSegment(new PointCollection(temp), can, tools[e.TouchDevice].color, tools[e.TouchDevice].size, interactingUser[e.TouchDevice]);
                        lines[e.TouchDevice].Points = new PointCollection(temp);
                        interactingUser[e.TouchDevice].updateUndo(lines[e.TouchDevice], can, Operation.Type.Create);
                    } else {
                        can.Children.Remove(lines[e.TouchDevice]);
                    }
                    //can.Children.Remove(lines[e.TouchDevice]);

                    // Log touches on screen
                    if (closestHand.ContainsKey(e.TouchDevice)) {
                        Point pos = e.GetTouchPoint(MemoBoardCanvas).Position;
                        logs[e.TouchDevice].Append("ended, " + pos.X + ", " + pos.Y + ", " + closestHand[e.TouchDevice] + ", "
                            + tools[e.TouchDevice].name() + ", " + tools[e.TouchDevice].size + ", " + tools[e.TouchDevice].color + "\r\n");
                        interactingUser[e.TouchDevice].logger.log("touches", logs[e.TouchDevice].ToString());
                    }

                    if (can.Name == "editingPostit") {
                        // Log postit edited
                        interactingUser[e.TouchDevice].logger.log("postit", "edited, " + CustomPostitId.GetCustomId(can)+ ", "+closestHand[e.TouchDevice]);
                    }

                } else if (tools[e.TouchDevice].type == Type.Eraser) {
                    try {
                        // Perform the hit test against a given portion of the visual object tree.
                        FrameworkElement result = VisualTreeHelper.HitTest(can, e.TouchDevice.GetTouchPoint(can).Position).VisualHit as FrameworkElement;
                        if (result != null) {
                            if (result.Name != "MemoBoardCanvas" && result.Name != "PostitLayer" && result.Name != "editingPostit") {
                                if (interactingUser.ContainsKey(e.TouchDevice)) {
                                    // Update undo and remove touched element
                                    interactingUser[e.TouchDevice].updateUndo(result, (Panel)result.Parent, Operation.Type.Delete);
                                    ((Panel)result.Parent).Children.Remove(result);

                                    // Log postit deletion
                                    if (result.Name == "Postit")
                                        interactingUser[e.TouchDevice].logger.log("postit", "deleted, " + CustomPostitId.GetCustomId(can) + ", " + closestHand[e.TouchDevice]);
                                }
                            }
                        }
                        // Log touches on screen
                        if (closestHand.ContainsKey(e.TouchDevice)) {
                            Point pos = e.GetTouchPoint(MemoBoardCanvas).Position;
                            logs[e.TouchDevice].Append("ended, " + pos.X + ", " + pos.Y + ", " + closestHand[e.TouchDevice] + ", "
                                + tools[e.TouchDevice].name() + ", " + tools[e.TouchDevice].size + ", " + tools[e.TouchDevice].color + "\r\n");
                            interactingUser[e.TouchDevice].logger.log("touches", logs[e.TouchDevice].ToString());
                        }
                    } catch (Exception) { }
                } /*else if (lines.ContainsKey(e.TouchDevice)) {
                    interactingUser[e.TouchDevice].updateUndo(lines[e.TouchDevice], can, Operation.Type.Create);
                } */

                interactingUser.Remove(e.TouchDevice);
                touchedCanvas.Remove(e.TouchDevice);
                lines.Remove(e.TouchDevice);
                tools.Remove(e.TouchDevice);
                moving.Remove(e.TouchDevice);
                closestHand.Remove(e.TouchDevice);
                logs.Remove(e.TouchDevice);

            }
        }
       
        // When finger moves on screen, draw a new line
        private void TouchMoved(object sender, TouchEventArgs e) {
            e.Handled = true;
            if (touchedCanvas.ContainsKey(e.TouchDevice) && e.TouchDevice.Captured == touchedCanvas[e.TouchDevice]) {
                Canvas can = touchedCanvas[e.TouchDevice];
                TouchPoint point = e.GetTouchPoint(can);

                if (tools[e.TouchDevice] != null) {
                    // Prevent touch from exiting from left and right of postit
                    if (point.Position.X < touchedCanvas[e.TouchDevice].MaxWidth && point.Position.X > 0
                        && point.Position.Y > 0 && point.Position.Y < touchedCanvas[e.TouchDevice].MaxHeight) {
                        if (tools[e.TouchDevice].type == Type.Pencil) {
                            // Add Points to polyline
                            Point last = lines[e.TouchDevice].Points.Last();
                            if (Math.Abs(point.Position.X - last.X) + Math.Abs(point.Position.Y - last.Y) > 3 && Math.Abs(point.Position.X - last.X) + Math.Abs(point.Position.Y - last.Y) < 200) {
                                lines[e.TouchDevice].Points.Add(point.Position);

                                // Change size of postit if writing on top of it
                                if (point.Position.Y > can.ActualHeight && point.Position.Y < can.MaxHeight) {
                                    try {
                                        can.Height = point.Position.Y;
                                        Rectangle temp = (Rectangle)LogicalTreeHelper.FindLogicalNode(can, "Border");
                                        temp.Height = can.Height;
                                    } catch (Exception) { }
                                }
                            }
                        } else if (tools[e.TouchDevice].type == Type.Eraser) {
                            // Perform the hit test against a given portion of the visual object tree.
                            try {
                                FrameworkElement result = VisualTreeHelper.HitTest(can, point.Position).VisualHit as FrameworkElement;
                                if (result != null) {
                                    if (result.Name != "MemoBoardCanvas" && result.Name != "PostitLayer" && result.Name != "editingPostit"
                                        && result.Name != "Border" && result.Name != "Commands" && result.Name != "PostitBorder"
                                        && result.Name != "btn") {
                                        if (interactingUser.ContainsKey(e.TouchDevice)) {
                                            // Update undo and remove touched element
                                            interactingUser[e.TouchDevice].updateUndo(result, (Panel)result.Parent, Operation.Type.Delete);
                                            ((Panel)result.Parent).Children.Remove(result);
                                        }
                                    }
                                }
                            } catch (Exception) { }
                        }
                        // Log touches on screen
                        if (closestHand.ContainsKey(e.TouchDevice)) {
                            Point pos = e.GetTouchPoint(MemoBoardCanvas).Position;
                            logs[e.TouchDevice].Append("moved, " + pos.X + ", " + pos.Y + ", " + closestHand[e.TouchDevice] + ", "
                                + tools[e.TouchDevice].name() + ", " + tools[e.TouchDevice].size + ", " + tools[e.TouchDevice].color+"\r\n");
                        }
                    }
                } else {
                    try {
                        can = touchedCanvas[e.TouchDevice].Name != "Postit" ? ((Canvas)((Canvas)touchedCanvas[e.TouchDevice].Parent).Parent) : touchedCanvas[e.TouchDevice];
                        Canvas pit = (Canvas)LogicalTreeHelper.FindLogicalNode(MemoBoardCanvas, "PostitLayer");
                        double tx = e.GetTouchPoint(pit).Position.X - touchPosition[e.TouchDevice].X;
                        double ty = e.GetTouchPoint(pit).Position.Y - touchPosition[e.TouchDevice].Y;
                        // Move postit around with touch
                        if (moving[e.TouchDevice] || Math.Abs(tx) + Math.Abs(ty) > 20) {
                            touchPosition[e.TouchDevice] = e.GetTouchPoint(pit).Position;
                            Canvas.SetLeft(can, Canvas.GetLeft(can) + tx);
                            Canvas.SetTop(can, Canvas.GetTop(can) + ty);
                            moving[e.TouchDevice] = true;
                        }
                    } catch (Exception) { }
                }
            }
        }

        public void createPostit(User user, string usedHand) {
            // Create postit
            double width = 700;
            double minHeight = 400;
            Point pos = touch_association.getBodyPosition(user.getTrackingId());
            Color c = Colors.LightYellow;
            c.A = 230;
            Canvas can = new Canvas { MaxWidth = width, Width = width, MinHeight = minHeight,
                    Background = new SolidColorBrush(c), Name = "editingPostit" };
            Canvas.SetLeft(can, pos.X - width / 2);
            Canvas.SetTop(can, pos.Y - minHeight / 2);
            can.MaxHeight = MemoBoardCanvas.ActualHeight - Canvas.GetTop(can);
            can.TouchDown += TouchStart;
            can.TouchMove += TouchMoved;
            can.TouchUp += TouchEnd;

            // Insert borders
            Rectangle border = new Rectangle { Width = can.Width, Height = can.MinHeight, StrokeThickness = 4,
                                                Stroke = new SolidColorBrush(Colors.DarkSlateGray), Name="Border"};
            can.Children.Add(border);

            // Show commands
            Canvas cmd = createPostitCommands();
            can.Children.Add(cmd);
            cmd.Width = can.Width;

            Canvas pit = (Canvas)LogicalTreeHelper.FindLogicalNode(MemoBoardCanvas, "PostitLayer");
            pit.Children.Add(can);
            
            // Keep on top
            int max = 0;
            foreach (UIElement ch in pit.Children) {
                max = Panel.GetZIndex(ch) > max ? Panel.GetZIndex(ch) : max;
            }
            Panel.SetZIndex(can, max);

            // Log postit creation
            user.logger.log("postit", "created, "+can.GetHashCode() + ", "+ usedHand);
            CustomPostitId.SetCustomId(can, can.GetHashCode());

            user.cancelRedo();
            user.updateUndo(can, pit, Operation.Type.Create);
        }

        private Canvas createPostitCommands() {
            Canvas c = new Canvas { Name = "Commands"};
            Color col = Colors.DimGray;
            col.A = 30;
            Canvas r = new Canvas { Background = new SolidColorBrush(col), Height = 50, Width = 700, Name="PostitBorder" };
            Panel.SetZIndex(r, 1);
            c.Children.Add(r);
            r.TouchDown += TouchStart;
            r.TouchMove += TouchMoved;
            r.TouchUp += TouchEnd;

            Ellipse bg = new Ellipse { Width = 40, Height = 40, Fill = new SolidColorBrush(Colors.Green), Name = "btn" };
            Canvas.SetRight(bg, 30);
            c.Children.Add(bg);
            bg = new Ellipse { Width = 40, Height = 40, Fill = new SolidColorBrush(Colors.Red), Name = "btn" };
            Canvas.SetRight(bg, 100);
            c.Children.Add(bg);
            Image im = new Image{ Source = new BitmapImage(new Uri(@"images\check.png", UriKind.Relative)), Width = 30, Height = 30, Name = "btn"};
            Panel.SetZIndex(im, Panel.GetZIndex(r)+1);
            Canvas.SetRight(im, 35); Canvas.SetTop(im, 5);
            c.Children.Add(im);
            im.TouchDown += LockTouchImg;
            im.TouchUp += AcceptPostit;
            Image im2 = new Image { Source = new BitmapImage(new Uri(@"images\delete.png", UriKind.Relative)), Width = 30, Height = 30, Name = "btn" };
            Panel.SetZIndex(im, Panel.GetZIndex(r)+1);
            Canvas.SetRight(im2, 105); Canvas.SetTop(im2, 5);
            c.Children.Add(im2);
            im2.TouchDown += LockTouchImg;
            im2.TouchUp += RemovePostit;

            return c;
        }

        private void removePostitCommands(Canvas c) {
            Canvas temp = (Canvas)LogicalTreeHelper.FindLogicalNode(c, "Commands");
            c.Children.Remove(temp);
        }

        void LockTouchImg(object sender, TouchEventArgs e) {
            e.Handled = true;
        }

        void AcceptPostit(object sender, TouchEventArgs e) {
            Canvas postit = (Canvas)((Canvas)((Image)sender).Parent).Parent;
            removePostitCommands(postit);
            postit.Name = "Postit";
            ScaleTransform trans = new ScaleTransform(0.3, 0.3);
            postit.RenderTransform = trans;
        }

        void RemovePostit(object sender, TouchEventArgs e) {
            Canvas postit = (Canvas)((Canvas)((Image)sender).Parent).Parent;
            try {
                User user = users[touch_association.getClosestUser(e.GetTouchPoint(MemoBoardCanvas).Position, users)];
                string hand = touch_association.getClosestHand(e.GetTouchPoint(MemoBoardCanvas).Position, user.getTrackingId());
                // Log postit deletion
                user.logger.log("postit", "deleted, " + CustomPostitId.GetCustomId(postit) + ", " + hand);
            } catch (Exception){ }
            
            removePostitCommands(postit);
            postit.Name = "Postit";
            ScaleTransform trans = new ScaleTransform(0.3, 0.3);
            postit.RenderTransform = trans;
            try {
                Canvas pit = (Canvas)LogicalTreeHelper.FindLogicalNode(MemoBoardCanvas, "PostitLayer");
                User u = users[touch_association.getClosestUser(e.GetTouchPoint(MemoBoardCanvas).Position, users)];
                u.updateUndo(postit, pit, Operation.Type.Delete);
                pit.Children.Remove(postit);
            } catch (Exception) { }
        }

        /*
         *  Create a bezier curve between some points, to have smoothed out pen lines 
         */
        private void CreatePolyBezierSegment(PointCollection p, Canvas can, Color c, int thickness, User u) {
            PathFigure pthFigure = new PathFigure();
            pthFigure.StartPoint = p[0];

            int count = p.Count;
            for (int i = count % 3; i%3 != 0 ; i++) {
                p.Add(p[p.Count - 1]);
            }

            PolyBezierSegment pbzSeg = new PolyBezierSegment();
            pbzSeg.Points = p;

            PathSegmentCollection myPathSegmentCollection = new PathSegmentCollection();
            myPathSegmentCollection.Add(pbzSeg);

            pthFigure.Segments = myPathSegmentCollection;

            PathFigureCollection pthFigureCollection = new PathFigureCollection();
            pthFigureCollection.Add(pthFigure);

            PathGeometry pthGeometry = new PathGeometry();
            pthGeometry.Figures = pthFigureCollection;

            Path arcPath = new Path();
            arcPath.StrokeLineJoin = PenLineJoin.Round;
            arcPath.StrokeStartLineCap = PenLineCap.Round;
            arcPath.StrokeEndLineCap = PenLineCap.Round;

            arcPath.Stroke = new SolidColorBrush(c);
            arcPath.StrokeThickness = thickness;
            arcPath.Data = pthGeometry;

            can.Children.Add(arcPath);

            // Update Undo/redo
            u.updateUndo(arcPath, can, Operation.Type.Create);
        }
    }
}
