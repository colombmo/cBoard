using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MemoBoard {
    class GUI {

        private String[] img = new String[]{ "undo", "save", "delete", "redo" };
        Canvas canvas;
        int top = 60;

        Dictionary<TouchDevice, bool> isLocked = new Dictionary<TouchDevice, bool>();

        public GUI(Canvas canvas) {
            this.canvas = canvas;
            double cx = ((Window)((Grid)canvas.Parent).Parent).Width / 2;
            Rectangle[] sq = new Rectangle[4];
            for (int i = 1; i < 3; i++) {
                sq[i] = new Rectangle { Width = 70, Height = 70, Fill = new SolidColorBrush(Colors.DarkSlateGray), StrokeThickness = 3, Stroke = new SolidColorBrush(Colors.White)};
                Canvas.SetTop(sq[i], top);
                canvas.Children.Add(sq[i]);
            }
            //Canvas.SetLeft(sq[0], cx - 55 - 60 - 70);
            Canvas.SetLeft(sq[1], cx - 55-60-3);
            Canvas.SetLeft(sq[2], cx + 45 + 3);
            //Canvas.SetLeft(sq[3], cx + 45 + 70);

            Ellipse el = new Ellipse { Width = 120, Height = 120, Fill = new SolidColorBrush(Colors.SteelBlue), StrokeThickness = 3, Stroke = new SolidColorBrush(Colors.White) };
            canvas.Children.Add(el);
            Canvas.SetLeft(el, cx - 60);
            Canvas.SetTop(el, top - 25);

            for (int i = 1; i < 3; i++) {
                Image icon = new Image { Source = new BitmapImage(new Uri(@"images\"+img[i]+".png", UriKind.Relative)), Width = 60, Height = 60, Name=img[i]};
                canvas.Children.Add(icon);
                Canvas.SetLeft(icon, Canvas.GetLeft(sq[i]) + 5);
                Canvas.SetTop(icon, Canvas.GetTop(sq[i]) + 5);
                icon.TouchDown += LockTouch;
                icon.TouchUp += Tap;
            }

            Image ic = new Image { Source = new BitmapImage(new Uri(@"images\old.png", UriKind.Relative)), Width = 60, Height = 60 };
            canvas.Children.Add(ic);
            Canvas.SetLeft(ic, Canvas.GetLeft(el) + 30);
            Canvas.SetTop(ic, Canvas.GetTop(el) + 30);
        }

        void LockTouch(object sender, TouchEventArgs e) {
            e.Handled = true;
        }

        void Tap(object sender, TouchEventArgs e) {
            Image s = (Image)sender;
            if (s.Name == img[0]) { //Undo
                //UndoRedo.undo();
            } else if (s.Name == img[1]) {
                // Save
                PDFSaver.generatePDF((Canvas)((Grid)canvas.Parent).FindName("MemoBoardCanvas"),
                                    new Uri(@"C:\Documents\MemoBoard\Exported\" + DateTime.Now.ToString("yyyyMMdd_hhmmss") + ".pdf", UriKind.Absolute));
            } else if (s.Name == img[2]) {
                // Delete all
                ((Canvas)((Grid)canvas.Parent).FindName("MemoBoardCanvas")).Children.Clear();
                ((Canvas)((Grid)canvas.Parent).FindName("MemoBoardCanvas")).Children.Add(
                       new Canvas { Name = "PostitLayer", VerticalAlignment = VerticalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Stretch });
                foreach(User u in MainWindow.users.Values) {
                    u.cancelUndo();
                    u.cancelRedo();
                }
            } else if(s.Name == img[3]) {
                // Redo
                //UndoRedo.redo();
            }
            e.Handled = false;
        }
    }
}
