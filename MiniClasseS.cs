using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;


namespace C2E4XML
{
    /// <summary>
    /// ノード格納クラス
    /// </summary>
    internal class XmlDataNode
    {
        /// <summary>
        /// 要素名
        /// </summary>
        public string Name { get; set; } = "";
        /// <summary>
        /// 値
        /// </summary>
        public string? Value { get; set; }

        public Dictionary<string, string> Attributes { get; set; } = [];
        /// <summary>
        /// 子ノード
        /// </summary>
        public List<XmlDataNode> Children { get; set; } = [];
    }

    public class LogEntry
    {
        public string SourcePath { get; set; }
        public bool IsSuccess { get; set; }
        public string ResultText { get; set; }   // 出力パス or 失敗原因
        public ICommand ActionCommand { get; set; }
    }

    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (bool)value ? Brushes.LimeGreen : Brushes.Red;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class ConvertResult
    {
        public bool IsSuccess { get; set; }
        public string ExcelPath { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute();

        public void Execute(object parameter) => _execute();

        public event EventHandler CanExecuteChanged;
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
            => _canExecute == null || _canExecute((T)parameter);

        public void Execute(object parameter)
            => _execute((T)parameter);

        public event EventHandler CanExecuteChanged;
    }


}
