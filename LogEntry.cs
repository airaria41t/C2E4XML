using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace C2E4XML
{
    public class LogEntry
    {
        public string SourcePath { get; set; }
        public bool IsSuccess { get; set; }
        public string ResultText { get; set; }   // 出力パス or 失敗原因
        public ICommand ActionCommand { get; set; }
    }
}
