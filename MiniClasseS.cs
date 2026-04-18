using System.Globalization;
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

    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isSuccess = value is bool b && b;

            return isSuccess
                ? Brushes.SeaGreen : Brushes.Crimson;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class BoolToMarkConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isSuccess = value is bool b && b;

            return isSuccess ? "✔" : "✖";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class BoolToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isSuccess = value is bool b && b;

            // 成功 → フォルダアイコン（E8A1）
            // 失敗 → 警告アイコン（E783）
            return isSuccess ? "\uE8A1" : "\uE783";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class BoolToToolTipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isSuccess = value is bool b && b;

            return isSuccess
                ? "Excel起動" : "メッセージ表示";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }


    public class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
    {
        private readonly Action _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        private readonly Func<bool>? _canExecute = canExecute;

        public bool CanExecute(object? parameter)
            => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter)
            => _execute();

        public event EventHandler? CanExecuteChanged;

        public void RaiseCanExecuteChanged()
            => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    public class RelayCommand<T>(Action<T> execute, Func<T, bool>? canExecute = null) : ICommand
    {
        private readonly Action<T> _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        private readonly Func<T, bool>? _canExecute = canExecute;

        public bool CanExecute(object? parameter)
        {
            if (_canExecute == null) return true;
            if (parameter == null && default(T) != null)
                return _canExecute(default!);
            return _canExecute((T)parameter!);
        }

        public void Execute(object? parameter)
            => _execute((T)parameter!);

        public event EventHandler? CanExecuteChanged;

        public void RaiseCanExecuteChanged()
            => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

}
