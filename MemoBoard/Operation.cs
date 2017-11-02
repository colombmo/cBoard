using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MemoBoard {
   public class Operation {

        public enum Type { Create = 0, Delete = 1, Move = 2 };

        public UIElement element;
        public Panel parent;
        public Type type;

        public Operation(UIElement element, Panel parent, Type type) {
            this.element = element;
            this.parent = parent;
            this.type = type;
        }
    }
}
