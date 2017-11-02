using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace MemoBoard {
    class ConvexHull {

        // Create a convex hull from a list of points, by using (On the identification of the convex hull of a finite set of points in the plane, R.A. Jarvis, 1972)
        public static PointCollection CalculateContour(List<Point> points) {
            // locate leftmost point
            int hull = 0;
            int i;

            for (i = 1; i < points.Count; i++) {
                if (points[i].X <= points[hull].X) {
                    hull = i;
                }
            }

            // wrap contour
            var outIndices = new int[points.Count];
            int endPt;
            i = 0;
            do {
                outIndices[i++] = hull;
                endPt = 0;
                for (int j = 1; j < points.Count; j++)
                    if (hull == endPt || IsLeft(points[hull], points[endPt], points[j]))
                        endPt = j;
                hull = endPt;
            } while (endPt != outIndices[0] && i < points.Count);

            // build countour points
            var contourPoints = new PointCollection(points.Capacity);
            int results = i;
            for (i = 0; i < results; i++)
                contourPoints.Add(points[outIndices[i]]);
            return contourPoints;
        }

        private static bool IsLeft(Point hull, Point endPt, Point pt) {
            Vector h = new Vector(hull.X, hull.Y);
            Vector e = new Vector(endPt.X, endPt.Y);
            Vector p = new Vector(pt.X, pt.Y);

            double angle = Vector.AngleBetween(Vector.Subtract(e, h), Vector.Subtract(p, h));
            if (0 < angle && angle < 180) return true;
            else return false;
        }

        public static List<Point> ConcaveHull(List<Point> points) {
            PointCollection convex = CalculateContour(points);
            List<Point> res = new List<Point>();
            for (int i = 0; i < convex.Count; i++) {
                List<Point> r = concave(convex[i], convex[(i + 1) % convex.Count],
                                        distance(convex[i], convex[(i + 1) % convex.Count]), points);
                for(int j=0;j<r.Count-1;j++) {
                    res.Add(r[j]);
                }
            }
            return res;
        }

        public static List<Point> concave(Point p1, Point p2, double di, List<Point> points) {
            // Stop condition
            if (di <= 60) {
                return new List<Point> { p1, p2 };
            } else {
                Point pm = new Point { X = (p1.X + p2.X) / 2, Y = (p1.Y + p2.Y) / 2 };
                Point pc = FindClosest(pm, points);
                double dip1 = distance(p1, pc);
                double dip2 = distance(pc, p2);
                if (dip1 < 0.8*di && dip2 < 0.8*di) {
                    List<Point> res = new List<Point>();
                    foreach (Point p in concave(p1, pc, dip1, points)) {
                        res.Add(p);
                    }
                    foreach (Point p in concave(pc, p2, dip2, points)) {
                        res.Add(p);
                    }
                    return res;
                } else {
                    return new List<Point> {p1, p2};
                }
            }
        }

        private static Point FindClosest(Point p, List<Point> points) {
            double min = double.MaxValue;
            Point res = points[0];
            foreach (Point pt in points) {
                double dist = distance(pt, p);
                if (dist < min) {
                    res = pt;
                    min = dist;
                }
            }
            return res;
        }

        // Find the minimum bounding box of a convex hull (as seen in http://delivery.acm.org/10.1145/370000/360919/p409-freeman.pdf?ip=134.21.147.199&id=360919&acc=ACTIVE%20SERVICE&key=FC66C24E42F07228%2E60246931B3350ED0%2E4D4702B0C3E38B35%2E4D4702B0C3E38B35&CFID=928896437&CFTOKEN=11972912&__acm__=1493205020_cb9319633c33cbf7999848c580bff611)
        public static List<Point> minimumBoundingBox(PointCollection ch) {
            List<Point> minBox = new List<Point>();
            double minArea = double.MaxValue;
            double minAngle = 0;

            //foreach edge of the convex hull
            for (var i = 0; i < ch.Count; i++) {
                var nextIndex = i + 1;

                var current = ch[i];
                var next = ch[nextIndex % ch.Count];

                Vector segment = new Vector { X = next.X - current.X, Y = next.Y - current.Y };

                //min / max points
                var top = double.MaxValue;
                var bottom = double.MinValue;
                var left = double.MaxValue;
                var right = double.MinValue;

                //get angle of segment to x axis
                var angle = AngleToXAxis(segment);

                //rotate every point and get min and max values for each direction
                foreach (var p in ch) {
                    Point rotatedPoint = RotateToXAxis(p, angle);

                    top = Math.Min(top, rotatedPoint.Y);
                    bottom = Math.Max(bottom, rotatedPoint.Y);

                    left = Math.Min(left, rotatedPoint.X);
                    right = Math.Max(right, rotatedPoint.X);
                }

                double area = Math.Abs((right - left) * (top - bottom));

                if (minBox.Count == 0 || area < minArea) {
                    minBox = new List<Point>();
                    minBox.Add(new Point(left, bottom));
                    minBox.Add(new Point(left, top));
                    minBox.Add(new Point(right, top));
                    minBox.Add(new Point(right, bottom));
                    minAngle = angle;
                    minArea = area;
                }
            }

            //rotate axis aligned box back
            for (int i = 0; i < minBox.Count; i++) {
                minBox[i] = RotateToXAxis(minBox[i], -minAngle);
            }

            return minBox;
        }


        // Calculates the angle to the X axis.
        private static double AngleToXAxis(Vector s) {
            return -Math.Atan(s.Y / s.X);
        }

        // Rotates vector by an angle to the x-Axis
        private static Point RotateToXAxis(Point p, double angle) {
            var newX = p.X * Math.Cos(angle) - p.Y * Math.Sin(angle);
            var newY = p.X * Math.Sin(angle) + p.Y * Math.Cos(angle);

            return new Point(newX, newY);
        }

        //Find the maximum area triangle enclosed in the convex hull (as seen in http://ieeexplore.ieee.org/stamp/stamp.jsp?arnumber=4567996)
        public static List<Point> maxEnclosedTriangle(PointCollection ch) {
            if (ch.Count >= 5) {
                int A = 0, B = 1, C = 2;
                int bA = A, bB = B, bC = C;     // Best picks for triangle vertices
                while (true) {                  // Loop A
                    while (true) {              // Loop B
                        while (triangleArea(ch[A], ch[B], ch[C]) <= triangleArea(ch[A], ch[B], ch[(C + 1) % ch.Count])) {
                            // Loop C
                            C = (C + 1) % ch.Count;
                        }
                        if (triangleArea(ch[A], ch[B], ch[C]) <= triangleArea(ch[A], ch[(B + 1) % ch.Count], ch[C])) {
                            B = (B + 1) % ch.Count;
                            continue;
                        } else break;
                    }
                    if (triangleArea(ch[A], ch[B], ch[C]) > triangleArea(ch[bA], ch[bB], ch[bC])) {
                        bA = A; bB = B; bC = C;
                    }
                    A = (A + 1) % ch.Count;
                    if (A == B) B = (B + 1) % ch.Count;
                    if (B == C) C = (C + 1) % ch.Count;
                    if (A == 0) break;
                }
                return new List<Point>(new Point[] { ch[bA], ch[bB], ch[bC] });
            }
            return new List<Point>(new Point[] { new Point(0, 0), new Point(0, 0), new Point(0, 0) });
        }

        // triangleArea |1/2(x1(y2−y3)+x2(y3−y1)+x3(y1−y2))|
        public static double triangleArea(Point p1, Point p2, Point p3) {
            return Math.Abs(0.5 * (p1.X * (p2.Y - p3.Y) + p2.X * (p3.Y - p1.Y) + p3.X * (p1.Y - p2.Y)));
        }

        public static double distance(Point p1, Point p2) {
            return Math.Abs(p1.X - p2.X) + Math.Abs(p1.Y - p2.Y);
        }
    }
}
