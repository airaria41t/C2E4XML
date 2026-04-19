using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace C2E4XML
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region フィールド
        private readonly Lock _procLock = new();
        private bool _isProcessing = false;
        private CancellationTokenSource? _cts;

        private bool _isPathCheck;
        public bool IsPathCheck
        {
            get => _isPathCheck;
            set
            {
                _isPathCheck = value;
                OnPropertyChanged(nameof(IsPathCheck));
            }
        }

        private bool _isSuccess;
        public bool IsSuccess
        {
            get => _isSuccess;
            set
            {
                _isSuccess = value;
                OnPropertyChanged(nameof(IsSuccess));
            }
        }

        private double _progressValue;
        public double ProgressValue
        {
            get => _progressValue;
            set
            {
                _progressValue = value;
                OnPropertyChanged(nameof(ProgressValue));
            }
        }

        private string _statusText = "";
        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                OnPropertyChanged(nameof(StatusText));
            }
        }

        // ステータスバー表示用（成功パス／失敗メッセージ）
        private string _lastResultMessage = "";
        public string LastResultMessage
        {
            get => _lastResultMessage;
            set
            {
                _lastResultMessage = value;
                OnPropertyChanged(nameof(LastResultMessage));
            }
        }

        // ステータスバー横ボタンの動作
        public ICommand? ResultActionCommand { get; private set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        #endregion

        #region コンストラクタ・初期化
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;

            DataContext = this;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeForm();
        }

        private void InitializeForm()
        {
            txtFilePath.Text = "ファイルパスを入力";
            StatusText = "ファイルパスを入力";
            txtResultMark.Text = "";
            LastResultMessage = "";
            prgConvert.Visibility = Visibility.Hidden;
            btnExplorer.Visibility = Visibility.Hidden;
            txtCheck.Visibility = Visibility.Hidden;
        }

        private void InitializeProgressBar(bool flag)
        {
            if (flag)
            {
                prgConvert.Visibility = Visibility.Visible;
                ProgressValue = 0;
                btnExplorer.Visibility = Visibility.Hidden;
                LastResultMessage = "";
            }
            else
            {
                prgConvert.Visibility = Visibility.Hidden;
            }
            //StatusText = "準備完了";
            PathChecker(txtFilePath.Text);
            txtResultMark.Text = "";
        }
        #endregion

        #region イベント
        /// <summary>
        /// オープンファイルダイアログ表示
        /// 変換ファイルパス取得
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnFilePath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "XML ファイルを選択",
                Filter = "XML ファイル (*.xml;*.XML)|*.xml;*.XML|すべてのファイル (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                txtFilePath.Text = dialog.FileName;
            }
        }

        /// <summary>
        /// テキストボックスドロップイベント発火用ドラッグオーバーイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtFilePath_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        /// <summary>
        /// テキストボックスドロップイベント
        /// ファイルパス取得
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtFilePath_PreviewDrop(object sender, DragEventArgs e)
        {
            try
            {
                if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                    return;

                if (e.Data.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0)
                    return;

                string path = files[0];

                if (sender is not TextBox tb)
                    return;

                tb.Text = path;
            }
            catch (Exception ex)
            {
                // Drop 内で例外が外に出ると DragOver が壊れるためここで止める
                MessageBox.Show(
                    ex.Message,
                    "Drop エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private async void BtnConvert_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                InitializeProgressBar(true);

                btnConvert.Content = "\uEE95";   // StopSolid
                lock (_procLock)
                {
                    if (_isProcessing)
                    {
                        // 既に処理中の場合はキャンセルを試みる
                        _cts?.Cancel();
                        return;
                    }
                    _isProcessing = true;
                }
                // ファイルパスの妥当性確認
                if (!PathChecker(txtFilePath.Text, false))
                {
                    IsSuccess = false;
                    MessageBox.Show("有効なファイルパスを入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusText = "パスエラー";
                    return;
                }

                IsSuccess = await ProcExecution();

                StatusText = IsSuccess ? "変換成功" : "変換失敗";
                if (!string.IsNullOrEmpty(LastResultMessage))
                {
                    btnExplorer.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                IsSuccess = false;
                StatusText = "変換失敗";
                // ここで例外を完全に吸収
                MessageBox.Show(ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                lock (_procLock)
                {
                    _isProcessing = false;
                }
                btnConvert.Content = "\uEE4A";   // StopSolid
                StartClearResultTimer(TimeSpan.FromSeconds(3)); // 3秒後に消す
            }
        }

        private async void CopyBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (string.IsNullOrEmpty(LastResultMessage))
                return;
            if (!File.Exists(LastResultMessage))
                return;

            // クリップボードへコピー
            Clipboard.SetText(LastResultMessage);

            // クリック視覚効果（Opacity を一瞬下げる）
            var border = sender as Border;
            border?.Opacity = 0.5;
            await Task.Delay(120);
            border?.Opacity = 1.0;

            // 一時メッセージ表示
            await ShowCopiedMessageAsync();
        }

        //   ResultActionCommand が設定されていればそれを使う
        private void BtnExplorer_Click(object sender, RoutedEventArgs e)
        {
            if (ResultActionCommand != null && ResultActionCommand.CanExecute(null))
            {
                ResultActionCommand.Execute(null);
            }
            else if (!string.IsNullOrEmpty(LastResultMessage) && File.Exists(LastResultMessage))
            {
                // 念のためパスとして直接実行もフォールバック
                Process.Start(new ProcessStartInfo(LastResultMessage)
                {
                    UseShellExecute = true
                });
            }
            else
            {
                MessageBox.Show("実行可能なパスがありません。", "情報",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnExplorer_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            // ファイルパスが存在する場合のみフォルダを開く
            if (!string.IsNullOrEmpty(LastResultMessage) && File.Exists(LastResultMessage))
            {
                // Explorer でファイルを選択状態で開く
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{LastResultMessage}\"",
                    UseShellExecute = true
                });
            }
        }

        private void TxtFilePath_TextChanged(object sender, TextChangedEventArgs e)
        {
            PathChecker(txtFilePath.Text);
        }

        private void TxtFilePath_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox tb && !tb.IsKeyboardFocusWithin)
            {
                e.Handled = true;
                tb.Focus();
                tb.SelectAll();
            }
        }

        #endregion

    }
}