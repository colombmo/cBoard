using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoBoard {
    public class NewBodyDetectedEventArgs : EventArgs {
        public ulong[] BodyId { get; set; }
        public IList<Body> Bodies { get; set; }
    }
}
