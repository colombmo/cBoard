using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace MemoBoard {
    public enum Type { Pencil=0, Eraser=1};

    public class Tool {
        public Type type;
        public Color color;
        public int size;

        public Tool(Type type, Color color, int size) {
            this.type = type;
            this.color = color;
            this.size = size;
        }

        public string name() {
            if (this.type == Type.Pencil)
                return "pencil";
            else
                return "eraser";
        }
    }
}
