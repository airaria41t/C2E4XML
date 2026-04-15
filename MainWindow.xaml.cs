using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office2013.Drawing.ChartStyle;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace C2E4XML
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly object _procLock = new object();
        private bool _isProcessing = false;
        private CancellationTokenSource? _cts;

        public ObservableCollection<LogEntry> LogEntries { get; }
        = new ObservableCollection<LogEntry>();
        public ICommand CopyResultCommand { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;

            CopyResultCommand = new RelayCommand<string>(text =>
            {
                Clipboard.SetText(text);
            });

            DataContext = this;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeForm();
            //throw new NotImplementedException();
        }

        private void InitializeForm()
        {
            txtFilePath.Text = "ファイルパスを入力";
            txtLog.Text = "結果が表示されます。";
            txtStatus.Text = "";
            txtResultMark.Text = "";
            txtResultMarkB.Text = "";
            stkProgressBar.Visibility = Visibility.Hidden;
        }

        private void InitializeProgressBar(bool flag)
        {
            if (flag)
            {
                stkProgressBar.Visibility = Visibility.Visible;
                prgConvert.Minimum = 0;
                prgConvert.Maximum = 100;
                prgConvert.Value = 0;
            }
            else
            {
                stkProgressBar.Visibility = Visibility.Hidden;
            }
            txtStatus.Text = "";
            txtResultMark.Text = "";
            txtResultMarkB.Text = "";
        }

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

                var files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files == null || files.Length == 0)
                    return;

                string path = files[0];

                if (sender is not TextBox tb) return;
                tb.Text = path;
            }
            catch
            {
                // Drop 内で例外が出ると 2 回目以降 DragOver が壊れるため必須
            }
        }

        private async Task<bool> ProcExecution()
        {
            try
            {
                // ファイルパスの妥当性確認
                string path = txtFilePath.Text;
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                {
                    MessageBox.Show("有効なファイルパスを入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                //// Excel 出力先パス設定
                //string excelPath = System.IO.Path.ChangeExtension(path, ".xlsx");
                //// 既に同名の Excel ファイルが存在する場合は上書き確認
                //if (File.Exists(excelPath))
                //{
                //    var result =
                //        MessageBox.Show(
                //            $"同名の Excel ファイルが既に存在します。\n上書きしますか？\n\n{excelPath}",
                //            "確認",
                //            MessageBoxButton.YesNo,
                //            MessageBoxImage.Question
                //            );
                //    if (result != MessageBoxResult.Yes)
                //    {
                //        return;
                //    }
                //}

                // ProgressBar 初期化
                InitializeProgressBar(true);

                // キャンセル用
                _cts = new CancellationTokenSource();
                var token = _cts.Token;

                var progress = new Progress<int>(v => prgConvert.Value = v);
                var status = new Progress<string>(msg => txtStatus.Text = msg);

                bool bDetail = tglDetailOut.IsChecked == true;

                await Task.Run(async () =>
                {
                    await ConvertXmlToExcel(
                        path, bDetail, progress, status,
                        excelPath => {
                            // 成功ログ
                            LogEntries.Insert(0, new LogEntry {
                                SourcePath = path,
                                IsSuccess = true,
                                ResultText = excelPath,
                                ActionCommand = new RelayCommand(() => {
                                    Process.Start(new ProcessStartInfo(excelPath) { UseShellExecute = true });
                                })
                            });
                        }, token);
                });

                MessageBox.Show("Excel 出力が完了しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);

                //// XML ロード
                //XmlLoader loader = new(path);
                //loader.LoadXml();

                //bool bDetail = tglDetailOut.IsChecked == true;
                //if (bDetail)
                //{
                //    // テーブル構築
                //    XmlTableBuilder builder = new(loader.ReadData);
                //    var tables = builder.Build();

                //    // Excel 出力
                //    //ExcelExporter excel = new(tables);
                //    ExcelExporter excel = new();
                //    excel.Export(excelPath, tables);
                //}
                //else
                //{
                //    // Excel 出力
                //    ExcelExporter excel = new();
                //    excel.Export(excelPath, loader.ReadData);
                //}
                return true;
            }
            catch (OperationCanceledException ex)
            {
                string log = string.IsNullOrEmpty(ex.Message) ? "キャンセルされました。" : ex.Message;
                LogEntries.Insert(0, new LogEntry
                {
                    SourcePath = txtFilePath.Text,
                    IsSuccess = false,
                    ResultText = log,
                    ActionCommand = new RelayCommand(() =>
                    {
                        MessageBox.Show(log, "キャンセル", MessageBoxButton.OK, MessageBoxImage.Error);
                    })
                });
                return false;
            }
            catch (IOException ex) when (((int)ex.HResult & 0xFFFF) == 0x20)
            {
                string log = "Excel ファイルが開いているため、上書きできません。閉じてから再実行してください。";
                LogEntries.Insert(0, new LogEntry
                {
                    SourcePath = txtFilePath.Text,
                    IsSuccess = false,
                    ResultText = log,
                    ActionCommand = new RelayCommand(() =>
                    {
                        MessageBox.Show(log, "変換失敗", MessageBoxButton.OK, MessageBoxImage.Error);
                    })
                });
                MessageBox.Show(log, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            catch (Exception ex)
            {
                LogEntries.Insert(0, new LogEntry
                {
                    SourcePath = txtFilePath.Text,
                    IsSuccess = false,
                    ResultText = ex.Message,
                    ActionCommand = new RelayCommand(() =>
                    {
                        MessageBox.Show(ex.Message, "変換失敗", MessageBoxButton.OK, MessageBoxImage.Error);
                    })
                });

                MessageBox.Show(ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private async void BtnConvert_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnConvert.FontFamily = new FontFamily("Segoe Fluent Icons");
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

                bool success = await ProcExecution();

                if (success)
                {
                    // 成功時の処理（必要に応じて追加）
                    txtStatus.Text = "変換成功";
                    txtResultMark.Text = "✔";
                    txtResultMark.Foreground = new SolidColorBrush(Colors.SeaGreen);
                    txtResultMarkB.Text = "✔";
                    txtResultMarkB.Foreground = new SolidColorBrush(Colors.SeaGreen);
                }
                else
                {
                    // 失敗時の処理（必要に応じて追加）
                    txtStatus.Text = "変換失敗";
                    txtResultMark.Text = "✖";
                    txtResultMark.Foreground = new SolidColorBrush(Colors.Crimson);
                    txtResultMarkB.Text = "✖";
                    txtResultMarkB.Foreground = new SolidColorBrush(Colors.Crimson);
                }
                StartClearResultTimer(TimeSpan.FromSeconds(3)); // 3秒後に消す
            }
            catch (Exception ex)
            {
                // ここで例外を完全に吸収
                MessageBox.Show(ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                txtResultMark.Text = "✖";
                txtResultMark.Foreground = new SolidColorBrush(Colors.Crimson);
            }
            finally
            {
                lock (_procLock)
                {
                    _isProcessing = false;
                }
                btnConvert.Content = "\uEE4A";   // StopSolid

            }
        }

        private void StartClearResultTimer(TimeSpan delay)
        {
            var timer = new DispatcherTimer();
            timer.Interval = delay;

            timer.Tick += (s, e) =>
            {
                timer.Stop();
                InitializeProgressBar(false);
            };

            timer.Start();
        }


        private bool ChkFileExists(string path)
        {
            if (File.Exists(path))
            {
                bool overwrite = false;

                Dispatcher.Invoke(() =>
                {
                    var result = MessageBox.Show(
                        $"同名の Excel ファイルが既に存在します。\n上書きしますか？\n\n{path}",
                        "確認",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question
                    );
                    overwrite = (result == MessageBoxResult.Yes);
                });
                if (!overwrite)
                {
                    return false; // キャンセル扱い
                }
            }
            return true;
        }

        private async Task ConvertXmlToExcel(string path, bool bDetail, IProgress<int> progress, IProgress<string> status, Action<string> onSuccess, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            progress.Report(0);

            //
            // ① XML 読み込み
            //
            status.Report("XML 読み込み中...");
            XmlLoader loader = new(path);
            loader.LoadXml();
            progress.Report(20);
            token.ThrowIfCancellationRequested();

            string excelPath;

            //
            // ② トグルチェックによる処理分岐（テーブル構築の有無）
            //
            if (!bDetail)
            {
                // 詳細出力オフの場合はテーブル構築をスキップして直接 Excel 出力

                //
                // ③ パス設定
                //
                status.Report("Excel 出力先パス設定中...");
                excelPath = Path.ChangeExtension(path, ".xlsx");
                progress.Report(40);
                token.ThrowIfCancellationRequested();

                //
                // ④ ファイル既存チェック（上書き確認）
                //
                if (!ChkFileExists(excelPath)) 
                    throw new OperationCanceledException("同名ファイルが既にあります。"); // キャンセル扱い
                progress.Report(50);
                token.ThrowIfCancellationRequested();

                //
                // ⑤ Excel 出力
                //
                status.Report("Excel 出力中...");
                ExcelExporter excel = new();
                excel.Export(excelPath, loader.ReadData);

            }
            else 
            {
                // 詳細出力オンの場合はテーブル構築してから Excel 出力

                //
                // ③ テーブル構築
                //
                status.Report("テーブル構築中...");
                XmlTableBuilder builder = new(loader.ReadData);
                var tables = builder.Build();
                progress.Report(30);
                token.ThrowIfCancellationRequested();

                //
                // ④ パス設定
                //
                status.Report("Excel 出力先パス設定中...");
                excelPath = Path.ChangeExtension(path, ".xlsx");
                progress.Report(40);
                token.ThrowIfCancellationRequested();

                //
                // ⑤ ファイル既存チェック（上書き確認）
                //
                if (!ChkFileExists(excelPath)) 
                    throw new OperationCanceledException("同名ファイルが既にあります。"); // キャンセル扱い
                progress.Report(50);
                token.ThrowIfCancellationRequested();

                //
                // ⑥ Excel 出力
                //
                status.Report("Excel 出力中...");
                ExcelExporter excel = new();
                excel.Export(excelPath, tables);

            }

            progress.Report(100);
            status.Report("完了");

            // ★ 成功時だけパスを外に返す
            onSuccess(excelPath);
        }

        private void BtnExplorer_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnExcel_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnCopy_Click(object sender, RoutedEventArgs e)
        {

        }


        #endregion

    }
}