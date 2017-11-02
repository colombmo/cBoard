using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MemoBoard {
    class PointsReduction {
        public static IList<Point> ReducePoints(IList<Point> points) {
            var indexes = new List<Point>();

            indexes.Add(points[0]);
            Point last = points[0];

            for (int i = 1; i < points.Count - 1; i = i + 2) {
                /*if (Math.Abs(last.X - points[i].X) + Math.Abs(last.X - points[i].X) > 5) {
                    indexes.Add(points[i]);
                    last = points[i];
                }*/
                indexes.Add(points[i]);
            }
            indexes.Add(points[points.Count - 1]);

            return indexes;
        }

        public static IList<Point> Smooth(IList<Point> points, int limit) {
            var indexes = new List<Point>();

            indexes.Add(points[0]);
            Point last = points[0];

            for (int i = 1; i < points.Count - 1; i++) {
                if (keepMidPoint(last, points[i], points[i + 1], limit)) {
                    last = points[i];
                    indexes.Add(points[i]);
                }
            }
            indexes.Add(points[points.Count - 1]);

            return indexes;
        }

        private static bool keepMidPoint(Point p1, Point p2, Point p3, int limit) {
            Vector v1 = new Vector(p1.X, p1.Y);
            Vector v2 = new Vector(p2.X, p2.Y);
            Vector v3 = new Vector(p3.X, p3.Y);

            double a = Vector.Subtract(v2, v1).Length;
            double b = Vector.Subtract(v3, v1).Length;
            double c = Vector.Subtract(v3, v2).Length;

            double s = (a + b + c) / 2;
            double res = 2 * c * Math.Sqrt(s * (s - a) * (s - b) * (s - c));
            if (res > limit)
                return true;
            else
                return false;
        }

        public static List<Point> NoiseReduction(IList<Point> src, int severity = 1) {
            List<Point> res = new List<Point>();
            res.Add(src[0]);
            for (int i = 1; i < src.Count; i++) {
                //---------------------------------------------------------------avg
                var start = (i - severity > 0 ? i - severity : 0);
                var end = (i + severity < src.Count ? i + severity : src.Count);

                double sumX = 0;
                double sumY = 0;

                for (int j = start; j < end; j++) {
                    sumX += src[j].X;
                    sumY += src[j].Y;
                }

                var avgX = sumX / (end - start);
                var avgY = sumY / (end - start);

                //---------------------------------------------------------------
                res.Add(new Point(avgX, avgY));
            }
            res.Add(src[src.Count - 1]);
            return res;
        }

        public static List<Point> StraightLines(IList<Point> src, double slopeDiff = 0.5) {
            List<Point> res = new List<Point>();
            Point lastPoint = src[0];
            int count = 0;
            res.Add(src[0]);
            for (int i = 1; i < src.Count - 1; i++) {
                double tmp = (src[i + 1].Y - src[i].Y) / (src[i + 1].X - src[i].X);
                double tot = (src[i + 1].Y - lastPoint.Y) / (src[i + 1].X - lastPoint.X);
                if (Math.Abs(tmp - tot) >= slopeDiff) {
                    if (count > 1)
                        res.Add(src[i]);
                    res.Add(src[i]);
                    lastPoint = src[i];
                    count = 0;
                } else {
                    count++;
                }
            }
            res.Add(src[src.Count - 1]);
            res.Add(src[src.Count - 1]);
            return res;
        }
    }
}
