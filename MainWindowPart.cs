using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace C2E4XML
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private async Task<bool> ProcExecution()
        {
            string? log = null;
            string? title = null;
            MessageBoxImage icon = MessageBoxImage.None;

            try
            {
                // ファイルパス設定
                string path = txtFilePath.Text;

                // キャンセル用
                _cts = new CancellationTokenSource();
                var token = _cts.Token;

                var progress = new Progress<int>(v => ProgressValue = v);
                var status = new Progress<string>(msg => StatusText = msg);

                bool bDetail = tglDetailOut.IsChecked == true;

                await Task.Run(async () =>
                {
                    await ConvertXmlToExcel(
                        path, bDetail, progress, status,
                        excelPath =>
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                LastResultMessage = excelPath;// 成功時：ステータスバーに表示
                                // 成功時：ファイル実行
                                ResultActionCommand = new RelayCommand(() =>
                                {
                                    Process.Start(new ProcessStartInfo(excelPath)
                                    {
                                        UseShellExecute = true
                                    });
                                });

                            });
                        }, token);
                });

                MessageBox.Show("Excel 出力が完了しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);

                return true;
            }
            catch (OperationCanceledException ex)
            {
                log = string.IsNullOrEmpty(ex.Message) ? "キャンセルされました。" : ex.Message;
                title = "キャンセル";
                icon = MessageBoxImage.Exclamation;
                return false;
            }
            catch (IOException ex) when (((int)ex.HResult & 0xFFFF) == 0x20)
            {
                log = "Excel ファイルが開いているため、上書きできません。\n閉じてから再実行してください。";
                title = "エラー";
                icon = MessageBoxImage.Exclamation;
                return false;
            }
            catch (Exception ex)
            {
                log = ex.Message;
                title = "エラー";
                icon = MessageBoxImage.Error;
                return false;
            }
            finally
            {
                if (log != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        LastResultMessage = log.Replace("\n", ""); // ステータスバーに表示
                        ResultActionCommand = new RelayCommand(() =>
                        {
                            MessageBox.Show(log, title, MessageBoxButton.OK, icon);
                        });
                    });
                    MessageBox.Show(log, title, MessageBoxButton.OK, icon);
                }
            }
        }

        private void StartClearResultTimer(TimeSpan delay)
        {
            var timer = new DispatcherTimer
            {
                Interval = delay
            };

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
            StatusData sd = new();
            XmlLoader? loader = null;
            Dictionary<string, List<Dictionary<string, string>>>? excelSource = null;
            string? excelPath = null;
            for (int i = 0; i <= sd.MaxIndex; i++)
            {
                status.Report(sd[i]);
                progress.Report(i);
                token.ThrowIfCancellationRequested();

                switch (i)
                {
                    case 1: // XML 読み込み
                        loader = new XmlLoader(path);
                        loader.LoadXml();
                        break;

                    case 2: // パス設定
                        excelPath = Path.ChangeExtension(path, ".xlsx");
                        break;

                    case 3: // ファイルチェック
                        if (!ChkFileExists(excelPath!))
                            throw new OperationCanceledException("同名ファイルが既にあります。");
                        break;

                    case 4: // テーブル構築（bDetail のときだけ）
                        if (bDetail)
                        {
                            XmlTableBuilder? builder = new(loader!.ReadData);
                            excelSource = builder.Build();
                        }
                        break;

                    case 5: // Excel 出力
                        if (bDetail)
                        {
                            ExcelExporter.Export(excelPath!, excelSource!);
                        }
                        else
                        {
                            ExcelExporter.Export(excelPath!, loader!.ReadData);
                        }

                        break;

                    case 6: // 完了（処理なし）
                        break;
                }
            }
            //  成功時だけパスを外に返す
            onSuccess(excelPath!);
        }


        private async Task ShowCopiedMessageAsync()
        {
            string original = LastResultMessage;

            LastResultMessage = "コピーしました";
            await Task.Delay(1500); // 1.5秒表示
            LastResultMessage = original;
        }

        private bool PathChecker(string path, bool flag = true)
        {
            //var chk = txtCheck;
            if (string.IsNullOrEmpty(path) || txtFilePath.Text == "ファイルパスを入力")
            {
                if (flag)
                {
                    txtCheck.Visibility = Visibility.Hidden;
                    StatusText = "ファイルパスを入力";
                    return true;
                }
                IsPathCheck = false;
            }
            //else if (!File.Exists(path))
            //{
            //    IsPathCheck = false;
            //}
            else
            {
                //IsPathCheck = true;
                IsPathCheck = File.Exists(path);
            }

            txtCheck.Visibility = Visibility.Visible;

            //if (IsPathCheck)
            //{
            //    StatusText = "準備完了";
            //    return true;
            //} else {
            //    return false;
            //}
            //if (IsPathCheck) StatusText = "準備完了";
            StatusText = IsPathCheck ? "準備完了" : "ファイルが見つかりません";
            return IsPathCheck;
        }

        public class StatusData
        {
            private readonly Dictionary<int, string> _data
                = new()
                {
                    [0] = (""),
                    [1] = ("XML 読み込み中..."),
                    [2] = ("Excel 出力先パス設定中..."),
                    [3] = ("Excel ファイルチェック中..."),
                    [4] = ("Excel テーブル構築中..."),
                    [5] = ("Excel 出力中..."),
                    [6] = ("完了")
                };

            public string this[int index] => _data[index];
            public int MaxIndex => _data.Count - 1;
        }

    }
}
