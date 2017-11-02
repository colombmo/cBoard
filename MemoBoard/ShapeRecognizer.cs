using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MemoBoard {
    class ShapeRecognizer {
        // Recognize circles, rectangles and triangles and draw them on canvas. Return true if one of these shapes has been recognized, false otherwise
        public static bool recognizeShape(List<Point> points, Canvas c, Color color, double thickness, User u) {
            PointCollection ch = ConvexHull.CalculateContour(points);

            double xMin = double.MaxValue, yMin = double.MaxValue, xMax = 0, yMax = 0;
            double ls = 0;
            double Ach = 0;
            double Pch = 0;
            double width, height;
            double Arec = 0;
            double Atr = 0;

            Point p = ch[0];

            for (int i = 1; i < ch.Count; i++) {
                if (ch[i].X < xMin) xMin = ch[i].X;
                if (ch[i].Y < yMin) yMin = ch[i].Y;
                if (ch[i].X > xMax) xMax = ch[i].X;
                if (ch[i].Y > yMax) yMax = ch[i].Y;

                Ach += (ch[i].X * ch[i - 1].Y - ch[i].Y * ch[i - 1].X);
                Pch += Math.Sqrt(Math.Pow(ch[i].X - ch[i - 1].X, 2) + Math.Pow(ch[i].Y - ch[i - 1].Y, 2));
            }
            Ach += (ch[0].X * ch[ch.Count - 1].Y - ch[0].Y * ch[ch.Count - 1].X);
            Pch += Math.Sqrt(Math.Pow(ch[0].X - ch[ch.Count - 1].X, 2) + Math.Pow(ch[0].Y - ch[ch.Count - 1].Y, 2));

            Ach = Math.Abs(Ach / 2);
            width = xMax - xMin;
            height = yMax - yMin;

            // Calculate length of segments of the drawn shape
            for (int i = 1; i < points.Count; i++) {
                ls += Math.Sqrt(Math.Pow(points[i].X - points[i - 1].X, 2) + Math.Pow(points[i].Y - points[i - 1].Y, 2));
            }

            // Arec = b*h;
            List<Point> minRect = ConvexHull.minimumBoundingBox(ch);
            Arec = Math.Sqrt(Math.Pow(minRect[3].X - minRect[0].X, 2) + Math.Pow(minRect[3].Y - minRect[0].Y, 2)) * Math.Sqrt(Math.Pow(minRect[1].X - minRect[0].X, 2) + Math.Pow(minRect[1].Y - minRect[0].Y, 2));

            //Atr = b*h/2
            List<Point> maxTriangle = ConvexHull.maxEnclosedTriangle(ch);
            Atr = ConvexHull.triangleArea(maxTriangle[0], maxTriangle[1], maxTriangle[2]);

            // Recognize basic shapes similarly as in http://lib.tkk.fi/Dipl/2011/urn100500.pdf
                                                    // TODO: check these values here: V
            if (ls / Pch > 0.9 && ls / Pch < 1.1 && Math.Abs(points[0].X-points[points.Count-1].X)+ Math.Abs(points[0].Y - points[points.Count - 1].Y) < Pch/10) { // It's a closed shape
                if (Ach / Arec > 0.9 && Ach / Arec < 1.1) { // It's a rectangle
                    Polygon re = new Polygon();
                    re.Points = new PointCollection(minRect);
                    re.StrokeThickness = thickness;
                    re.Stroke = new SolidColorBrush(color);
                    re.StrokeLineJoin = PenLineJoin.Round;
                    c.Children.Add(re);
                    // Update undo/redo
                    u.updateUndo(re, c, Operation.Type.Create);
                } else if (Ach / Atr > 0.9 && Ach / Atr < 1.15) { // It's a triangle
                    Polygon tri = new Polygon();
                    tri.Points = new PointCollection(maxTriangle);
                    tri.StrokeThickness = thickness;
                    tri.Stroke = new SolidColorBrush(color);
                    tri.StrokeLineJoin = PenLineJoin.Round;
                    c.Children.Add(tri);
                    // Update undo/redo
                    u.updateUndo(tri, c, Operation.Type.Create);
                } else if (Math.Pow(Pch, 2) / Ach > 0.95 * 4 * Math.PI && Math.Pow(Pch, 2) / Ach < 1.05 * 4 * Math.PI) {
                    Ellipse circle = new Ellipse();                 // It's a circle
                    circle.StrokeThickness = thickness;
                    circle.Stroke = new SolidColorBrush(color);
                    double m = (width + height) / 2;
                    Canvas.SetLeft(circle, xMin);
                    Canvas.SetTop(circle, yMin);
                    circle.Width = m;
                    circle.Height = m;
                    c.Children.Add(circle);
                    // Update undo/redo
                    u.updateUndo(circle, c, Operation.Type.Create);
                } else {
                    return false;
                }
                return true;
            } else if (Math.Pow(Pch, 2) / Ach > 150) { // Recognized line 
                Line line = new Line();
                line.StrokeStartLineCap = PenLineCap.Round;
                line.StrokeEndLineCap = PenLineCap.Round;
                line.StrokeThickness = thickness;
                line.Stroke = new SolidColorBrush(color);

                line.X1 = points[0].X;
                line.Y1 = points[0].Y;
                line.X2 = points[points.Count - 1].X;
                line.Y2 = points[points.Count - 1].Y;
                c.Children.Add(line);

                // Update undo/redo
                u.updateUndo(line, c, Operation.Type.Create);

                return true;
            } else {                                      // No basic shape recognized
                return false;
            }
        }
    }
}
