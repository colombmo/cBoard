using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace MemoBoard {
    public class User {

        /*
         * Parameters
         */
        private ulong trackingId;
        private Tool[] tools = new Tool[2];
        public int selectedTool = 0;
        private Queue<double> lastZ = new Queue<double>();
        private Vector3D position;
        private Color bodyColor;
        private Brush bodyBrush;
        private string colorName;

        private double meanHeight = 0;
        private double iterations = 0;

        public bool isDetected;

        private Stack<Operation> undoStack;
        private Stack<Operation> redoStack;

        public Logger logger;
        
        // Contructor of new user
        public User(ulong trackingId, Color bodyColor, string colorName) {
            this.trackingId = trackingId;
            tools[0] = new Tool(Type.Pencil, Colors.Black, 4);
            tools[1] = new Tool(Type.Eraser, Colors.White, 4);

            undoStack = new Stack<Operation>();
            redoStack = new Stack<Operation>();

            this.bodyColor = bodyColor;
            this.bodyBrush = new SolidColorBrush(new Color { R = bodyColor.R, G = bodyColor.G, B = bodyColor.B, A = 30 });

            this.colorName = colorName;

            this.logger = new Logger(colorName);
        }

        /*
         * Get body color
         */
        public Color getBodyColor() {
            return this.bodyColor;
        }

        /*
         * Get body color name
         */
        public string getColorName() { return this.colorName; }

        /*
         * Set body color name 
         */
        public void setColorName(string name) { this.colorName = name; }


        /*
         * Get body brush
         */
        public Brush getBodyBrush() {
            return this.bodyBrush;
        }

        /*
         * Get selected tool
         */
        public Tool getTool() {
            return this.tools[selectedTool];
        }

        /*
         * Get trackingId of body
         */
        public ulong getTrackingId() {
            return this.trackingId;
        }

        /*
         * Set trackingId of body
         */
        public void setTrackingId(ulong trackingId) {
            this.trackingId = trackingId;
        }

        /*
         * Get position
         */
        public Vector3D getPosition() {
            return this.position;
        }

        /*
         * 
         */
        public bool getDetectedState() {
            return this.isDetected;
        }

        /*
         * Update user
         */
        public void update(Vector3D p) {
            if (p.X != double.MinValue && p.Y > -1.25) {
                this.isDetected = true;
                this.position = p;
                if (lastZ.Count >= 5)
                    this.position.Z = lastZ.Dequeue();
                else if(lastZ.Count>0)
                    this.position.Z = lastZ.Peek();
                this.meanHeight = (this.iterations * this.meanHeight + p.Y) / (++this.iterations);
                lastZ.Enqueue(p.Z);
            } else {
                // Keep correct height of user's position
                this.position = new Vector3D(position.X, this.meanHeight, position.Z);
                this.isDetected = false;
            }
        }

        /**
         *  Handle undo/redo 
         * 
         */
        public void updateUndo(UIElement el, Panel parent, Operation.Type type) {
            undoStack.Push(new Operation(el, parent, type));
        }

        public void undo() {
            if (undoStack.Count > 0) {
                Operation prev = undoStack.Pop();

                if (prev.type == Operation.Type.Create) {
                    prev.parent.Children.Remove(prev.element);
                } else if (prev.type == Operation.Type.Delete) {
                    prev.parent.Children.Add(prev.element);
                }
                redoStack.Push(prev);
            }
        }

        public void redo() {
            if (redoStack.Count > 0) {
                Operation prev = redoStack.Pop();

                if (prev.type == Operation.Type.Create) {
                    prev.parent.Children.Add(prev.element);
                } else if (prev.type == Operation.Type.Delete) {
                    prev.parent.Children.Remove(prev.element);
                }
                // Move??
                undoStack.Push(prev);
            }
        }

        public void cancelRedo() {
            redoStack.Clear();
        }

        public void cancelUndo() {
            undoStack.Clear();
        }
    }
}
