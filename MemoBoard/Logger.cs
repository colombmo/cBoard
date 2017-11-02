using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace MemoBoard {
    public class Logger {
        private string filePath;
        StringBuilder sb = new StringBuilder();
        Dictionary<string, StringBuilder> logs = new Dictionary<string, StringBuilder>();
                
        public Logger(string colorName) {
            this.filePath = "../logs/" + ConfigLoader.session + "/" + ConfigLoader.shadowType + "/"+colorName+"/";
            (new FileInfo(this.filePath)).Directory.Create();
        }

        public void log(string type, string s) {
            if (!logs.ContainsKey(type))
                logs[type] = new StringBuilder();
            logs[type].Append(s+"\r\n");
            //File.AppendAllText(filePath + type +".csv", s);
        }

        public void saveFile() {
            MainWindow mw = (MainWindow)Application.Current.MainWindow;
            if (mw.interactingUser.Count == 0) {
                foreach (var log in logs) {
                    if (log.Value.Length >= 5) {
                        File.AppendAllText(filePath + log.Key + ".csv", log.Value.ToString());
                        log.Value.Clear();
                    }
                }
            }
            // File saved!
        }

        /*private async Task WriteTextAsync(string filePath, string text) {
            byte[] encodedText = Encoding.Unicode.GetBytes(text);

            using (FileStream sourceStream = new FileStream(filePath,
                FileMode.Append, FileAccess.Write, FileShare.None,
                bufferSize: 4096, useAsync: true)) {
                await sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
            };
        }*/
    }
}
