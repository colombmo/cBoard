using System;
using System.Collections.Generic;
using Microsoft.Kinect;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Controls;
using System.Linq;
using System.Windows.Media.Effects;

namespace MemoBoard {
    public class AssTouch
    {
        private bool isKinectRunning = false;

        private Dictionary<string, Vector3D> coordsys;
        private Dictionary<string, double> measures;

        private KinectSensor kinectSensor = null;
        private MultiSourceFrameReader reader;
        private IList<Body> bodies;

        private byte[] bodyIndices;
        private ushort[] depth;
        
        private Window canvas;

        private int oldBodiesCount = 0;

        /*
         *  Initialize Kinect and start reading data from it 
         *  Note: canvas is used as a reference for computing width and height of screen
         */
        public AssTouch(Window canvas) {
            this.canvas = canvas;
            this.oldBodiesCount = 0;

            // Init coordinate system from configuration file
            this.coordsys = new Dictionary<string, Vector3D>();
            this.measures = new Dictionary<string, double>();

            try {
                using (var file = System.IO.File.OpenText("master.cfg")) {
                    int i = 0;
                    string line;
                    while ((line = file.ReadLine()) != null) {
                        string[] parts = line.Split(',');

                        if (i < 4) {
                            coordsys[parts[0]] = new Vector3D(float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]));
                        } else {
                            measures[parts[0]] = double.Parse(parts[1]);
                        }
                        i++;
                    }
                }
            } catch (Exception e) {
                Console.WriteLine(e.StackTrace);
                Console.WriteLine("Create a configuration file first!");
                return;
            }

            // Init and open kinect sensor
            this.kinectSensor = KinectSensor.GetDefault();
            if (this.kinectSensor != null)
            {
                this.kinectSensor.Open();
            }
            // Init multisource reader
            this.reader = this.kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.BodyIndex | FrameSourceTypes.Depth);
            this.reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

            this.isKinectRunning = true;
        }

        /*
         * Get the body which is the closest to a touch point on the screen
         * Params:  touch -> The touch point we want to associate to someone
         */
        public Body getClosestBody(Point touch) {
            double minDist = double.MaxValue;
            Body closest = null;
            foreach (var body in bodies) {
                if (body != null && body.IsTracked) { 
                    foreach (Joint j in body.Joints.Values) {
                        if (j.TrackingState == TrackingState.Tracked || j.TrackingState == TrackingState.Inferred) {
                            double d = dist(touch, new Vector3D(j.Position.X, j.Position.Y, j.Position.Z));
                            if (d < minDist) {
                                minDist = d;
                                closest = body;
                            }
                        }
                    }
                }
            }
            return closest;
        }

        /*
         * Get the user which is the closest to a touch point on the screen
         * Params:  touch -> the touch point position on the screen
         *          Dictionary<ulong, User> -> The users registered to the screen
         */
        public ulong getClosestUser(Point touch, Dictionary<ulong, User> users) {
            double minDist = double.MaxValue;
            ulong closest = ulong.MinValue;
            foreach (var body in bodies) {
                // Don't consider bodies further than 1.4m from screen
                if (body != null && body.IsTracked && getDistanceFromScreen(body.TrackingId) < 1.4) {
                    // If forearms and hands are visible, use just them to associate touch, otherwise use all body
                    if (body.Joints[JointType.HandLeft].TrackingState == TrackingState.Tracked &&
                       body.Joints[JointType.WristLeft].TrackingState == TrackingState.Tracked &&
                       body.Joints[JointType.ElbowLeft].TrackingState == TrackingState.Tracked &&
                       body.Joints[JointType.HandRight].TrackingState == TrackingState.Tracked &&
                       body.Joints[JointType.WristRight].TrackingState == TrackingState.Tracked &&
                       body.Joints[JointType.ElbowRight].TrackingState == TrackingState.Tracked) {
                        foreach (JointType t in new JointType[] { JointType.HandLeft, JointType.WristLeft, JointType.ElbowLeft, JointType.HandRight, JointType.WristRight, JointType.ElbowRight }) {
                            Joint j = body.Joints[t];
                            double d = dist(touch, new Vector3D(j.Position.X, j.Position.Y, j.Position.Z));
                            if (d < minDist) {
                                minDist = d;
                                closest = body.TrackingId;
                            }
                        }
                    } else {
                        foreach (Joint j in body.Joints.Values) {
                            if (j.TrackingState != TrackingState.NotTracked) {
                                double d = dist(touch, new Vector3D(j.Position.X, j.Position.Y, j.Position.Z));
                                if (d < minDist) {
                                    minDist = d;
                                    closest = body.TrackingId;
                                }
                            }
                        }
                    }
                }
            }
            // Consider also last_position to associate touch, to handle when someone occluded tries to draw something
            foreach (User u in users.Values) {
                if (!u.isDetected) {
                    double d = dist(touch, u.getPosition());
                    if (d < minDist) {
                        minDist = d;
                        closest = u.getTrackingId();
                    }
                }
            }
            return closest;
        }

        /*
         * Get hand used to interact with screen
         */
        public string getClosestHand(Point touch, ulong closestUser) {
            double minDist = double.MaxValue;
            string closest = "unknown";
            foreach (var body in bodies) {
                // Don't consider bodies further than 1.4m from screen
                if (body != null && body.TrackingId == closestUser) {
                    if (body.Joints[JointType.HandLeft].TrackingState != TrackingState.NotTracked) {
                        Joint j = body.Joints[JointType.HandLeft];
                        double d = dist(touch, new Vector3D(j.Position.X, j.Position.Y, j.Position.Z));
                        Console.WriteLine(d);
                        if (d < minDist && d < 0.6) {
                            minDist = d;
                            closest = "right";
                        }else {
                            closest = "left";
                        }
                    }
                    if (body.Joints[JointType.HandRight].TrackingState != TrackingState.NotTracked) {
                        Joint j = body.Joints[JointType.HandRight];
                        double d = dist(touch, new Vector3D(j.Position.X, j.Position.Y, j.Position.Z));
                        if (d < minDist && d < 0.6) {
                            minDist = d;
                            closest = "left";
                        }else {
                            closest = "right";
                        }
                    }
                }
                break;
            }
            Console.WriteLine(closest);
            return closest;
        }

        /*
         * Get distance from screen (in meters) for a specific body identifier 
         */
        public double getDistanceFromScreen(ulong bodyId) {
            foreach (var body in bodies) {
                if (body != null && body.IsTracked && body.TrackingId == bodyId) {
                    CameraSpacePoint p = body.Joints[JointType.SpineShoulder].Position;

                    return Vector3D.DotProduct(Vector3D.Subtract(new Vector3D { X = p.X, Y = p.Y, Z = p.Z }, coordsys["origin"]), coordsys["z"]);
                }
            }
            return double.MinValue;
        }

        /*
         * Get user chest's position in screen coordinates 
         */
        public Point getBodyPosition(ulong bodyId) {
            foreach (var body in bodies) {
                if (body != null && body.IsTracked && body.TrackingId == bodyId) {
                    CameraSpacePoint p = body.Joints[JointType.SpineShoulder].Position;

                    return new Point {
                        X = Vector3D.DotProduct(Vector3D.Subtract(new Vector3D { X = p.X, Y = p.Y, Z = p.Z }, coordsys["origin"]), coordsys["x"]) / measures["width"] * canvas.ActualWidth,
                        Y = Vector3D.DotProduct(Vector3D.Subtract(new Vector3D { X = p.X, Y = p.Y, Z = p.Z }, coordsys["origin"]), coordsys["y"]) / measures["height"] * canvas.ActualHeight
                    };
                }
            }
            return new Point { X = -100, Y = -100};
        }

        /*
         * Get user's chest position in camera coordinates
         */
        public Vector3D getBodyPositionInCameraCoord(ulong bodyId) {
            foreach (var body in bodies) {
                if (body != null && body.IsTracked && body.TrackingId == bodyId) {
                    return new Vector3D {
                        X = body.Joints[JointType.SpineShoulder].Position.X,
                        Y = body.Joints[JointType.SpineShoulder].Position.Y,
                        Z = body.Joints[JointType.SpineShoulder].Position.Z
                    };
                }
            }
            return new Vector3D { X = double.MinValue, Y = double.MinValue, Z = double.MinValue};
        }

        /*
         *  Get joints position in screen coordinates 
         */
        public List<Point> getJoints(ulong bodyId) {
            foreach (var body in bodies) {
                if (body != null && body.IsTracked && body.TrackingId == bodyId) {
                    List<Point> joints = new List<Point>();
                    foreach (Joint j in body.Joints.Values) {
                        CameraSpacePoint p = j.Position;
                        joints.Add(new Point {
                            X = Vector3D.DotProduct(Vector3D.Subtract(new Vector3D { X = p.X, Y = p.Y, Z = p.Z }, coordsys["origin"]), coordsys["x"]) / measures["width"] * canvas.ActualWidth,
                            Y = Vector3D.DotProduct(Vector3D.Subtract(new Vector3D { X = p.X, Y = p.Y, Z = p.Z }, coordsys["origin"]), coordsys["y"]) / measures["height"] * canvas.ActualHeight
                        });
                    }
                    return joints;
                }
            }
            return new List<Point>();
        }

        /*
         *  Get shadow 
         */
        public Polygon getShadow(ulong bodyId, Brush b) {
            Polygon pol = new Polygon { Fill = b, IsHitTestVisible = false, Name = "body"};
            foreach (var body in bodies) {
                if (body != null && body.IsTracked && body.TrackingId == bodyId) {
                    try {
                        // Get bodyId
                        Joint j = body.Joints[JointType.SpineMid];
                        DepthSpacePoint pt = kinectSensor.CoordinateMapper.MapCameraPointToDepthSpace(j.Position);
                        byte id = bodyIndices[(int)(Math.Floor(pt.Y) * kinectSensor.BodyIndexFrameSource.FrameDescription.Width + Math.Floor(pt.X))];
                        CameraSpacePoint[] csp = new CameraSpacePoint[kinectSensor.DepthFrameSource.FrameDescription.LengthInPixels];
                        kinectSensor.CoordinateMapper.MapDepthFrameToCameraSpace(depth, csp);

                        List<Point> top = new List<Point>();
                        List<Point> bot = new List<Point>();

                        for (int x = 0; x < kinectSensor.BodyIndexFrameSource.FrameDescription.Width; x=x+2) {
                            int min = int.MaxValue;
                            int max = -1;
                            for (int y = 0; y < 7*kinectSensor.BodyIndexFrameSource.FrameDescription.Height/10; y=y+2) {
                                if (bodyIndices[y * kinectSensor.BodyIndexFrameSource.FrameDescription.Width + x] == id) {
                                    min = y < min ? y : min;
                                    max = y > max ? y : max;
                                }
                            }
                            if (min != int.MaxValue)
                                top.Add(new Point { X = x, Y = min });
                            if (max != -1)
                                bot.Add(new Point { X = x, Y = max });
                        }
                        
                        List<Point> points = new List<Point>();
                        foreach (Point l in top) {
                            CameraSpacePoint p = csp[(int)(l.Y * kinectSensor.BodyIndexFrameSource.FrameDescription.Width + l.X)];
                            points.Add(new Point {
                                X = Vector3D.DotProduct(Vector3D.Subtract(new Vector3D { X = p.X, Y = p.Y, Z = p.Z }, coordsys["origin"]), coordsys["x"]) / measures["width"] * canvas.ActualWidth,
                                Y = Vector3D.DotProduct(Vector3D.Subtract(new Vector3D { X = p.X, Y = p.Y, Z = p.Z }, coordsys["origin"]), coordsys["y"]) / measures["height"] * canvas.ActualHeight
                            });
                        }
                        bot.Reverse();
                        foreach (Point r in bot) {
                            CameraSpacePoint p = csp[(int)(r.Y * kinectSensor.BodyIndexFrameSource.FrameDescription.Width + r.X)];
                            points.Add(new Point {
                                X = Vector3D.DotProduct(Vector3D.Subtract(new Vector3D { X = p.X, Y = p.Y, Z = p.Z }, coordsys["origin"]), coordsys["x"]) / measures["width"] * canvas.ActualWidth,
                                Y = Vector3D.DotProduct(Vector3D.Subtract(new Vector3D { X = p.X, Y = p.Y, Z = p.Z }, coordsys["origin"]), coordsys["y"]) / measures["height"] * canvas.ActualHeight
                            });
                        }
                        
                        BlurEffect blur = new BlurEffect();
                        blur.Radius = 90;
                        
                        pol.Effect = blur;

                        pol.Points = new PointCollection(PointsReduction.NoiseReduction(points, 3));
                        
                    } catch (Exception) { }
                }
            }
            return pol;
        }
        
        /*
         *  Create event to be fired each time a new body is detected by the kinect sensor 
         */
        protected virtual void OnNewBodyDetected(NewBodyDetectedEventArgs e) {
            EventHandler<NewBodyDetectedEventArgs> handler = NewBodyDetected;
            if (handler != null) {
                handler(this, e);
            }
        }
        public event EventHandler<NewBodyDetectedEventArgs> NewBodyDetected;

        /*
         * Handle frames from kinect sensor
         */
        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e) {

            var reference = e.FrameReference.AcquireFrame();
            
            // Handle body data from kinect sensor
            using (var frame = reference.BodyFrameReference.AcquireFrame()) {
                if (frame != null) {
                    bodies = new Body[frame.BodyFrameSource.BodyCount];
                    frame.GetAndRefreshBodyData(bodies);

                    int bodiesNumber = 0;
                    NewBodyDetectedEventArgs args = new NewBodyDetectedEventArgs();
                    List<ulong> tempIds = new List<ulong>();

                    foreach (Body b in bodies) {
                        if (b.IsTracked) {
                            bodiesNumber++;
                            tempIds.Add(b.TrackingId);
                        }
                    }

                    if (bodiesNumber > oldBodiesCount) {
                        args.BodyId = tempIds.ToArray();
                        args.Bodies = bodies;
                        OnNewBodyDetected(args);
                    }
                    oldBodiesCount = bodiesNumber;
                }
            }
            
            using (var frame = reference.DepthFrameReference.AcquireFrame()) {
                using (var bodyId = reference.BodyIndexFrameReference.AcquireFrame()) {
                    if (frame != null && bodyId != null) {
                        depth = new ushort[frame.FrameDescription.LengthInPixels];
                        bodyIndices = new byte[frame.FrameDescription.LengthInPixels];
                        frame.CopyFrameDataToArray(depth);
                        bodyId.CopyFrameDataToArray(bodyIndices);
                    }
                }
            }
        }

        /*
         * Compute euclidean distance touch point-joint position (in meters, and considering x,y and z coordinates)
         */
        private double dist(Point t, Vector3D j) {
            // Joint position with respect to screen coordinates (in meters)
            double x = Vector3D.DotProduct(Vector3D.Subtract(j, coordsys["origin"]), coordsys["x"]);
            double y = Vector3D.DotProduct(Vector3D.Subtract(j, coordsys["origin"]), coordsys["y"]);
            double z = Vector3D.DotProduct(Vector3D.Subtract(j, coordsys["origin"]), coordsys["z"]);
            
            Vector3D j_screen = new Vector3D { X = x, Y = y, Z = z };
            
            // Compute distance between joint and touch-point in meters (euclidean distance)
            return Math.Sqrt(Math.Pow(t.X / canvas.ActualWidth * measures["width"] - j_screen.X, 2) +
                            Math.Pow(t.Y / canvas.ActualHeight * measures["height"] - j_screen.Y, 2) +//);
                            Math.Pow((j_screen.Z+0.2 < 0?j_screen.Z+0.2:0), 2));
        }

        /*
         * 
         */
        private Point ptTransform(CameraSpacePoint p, float offsetX = 0, float offsetY = 0) {
            return new Point {
                X = Vector3D.DotProduct(Vector3D.Subtract(new Vector3D { X = p.X, Y = p.Y, Z = p.Z }, coordsys["origin"]), coordsys["x"]) / measures["width"] * canvas.ActualWidth + offsetX,
                Y = Vector3D.DotProduct(Vector3D.Subtract(new Vector3D { X = p.X, Y = p.Y, Z = p.Z }, coordsys["origin"]), coordsys["y"]) / measures["height"] * canvas.ActualHeight + offsetY
            };
        }
    }
}
