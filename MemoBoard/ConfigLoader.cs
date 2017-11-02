using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoBoard {
    class ConfigLoader {
        public static string session = "session0";
        public static string shadowType = "none";

        public static void init() {
            string[] lines = System.IO.File.ReadAllLines(@"./config.cfg");

            session = lines[0];
            shadowType = lines[1];
        }
    }
}
