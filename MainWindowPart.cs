using DocumentFormat.OpenXml.Drawing.Charts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace C2E4XML
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private async Task<bool> ProcExecution()
        {
            try
            {
                // ファイルパス設定
                string path = txtFilePath.Text;

                // ProgressBar 初期化
                //InitializeProgressBar(true);

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
                        //await ConvertXmlToExcel(
                        //    progress, status,
                        //    excelPath =>
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
                string log = string.IsNullOrEmpty(ex.Message) ? "キャンセルされました。" : ex.Message;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    LastResultMessage = log;

                    ResultActionCommand = new RelayCommand(() =>
                    {
                        MessageBox.Show(log, "キャンセル",
                            MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    });
                });
                MessageBox.Show(log, "キャンセル", MessageBoxButton.OK, MessageBoxImage.Exclamation);

                return false;
            }

            catch (IOException ex) when (((int)ex.HResult & 0xFFFF) == 0x20)
            {
                string log = "Excel ファイルが開いているため、上書きできません。\n閉じてから再実行してください。";
                Application.Current.Dispatcher.Invoke(() =>
                {
                    LastResultMessage = log.Replace("\n", "");

                    ResultActionCommand = new RelayCommand(() =>
                    {
                        MessageBox.Show(log, "エラー",
                            MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    });
                });
                MessageBox.Show(log, "エラー", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    LastResultMessage = ex.Message;

                    ResultActionCommand = new RelayCommand(() =>
                    {
                        MessageBox.Show(ex.Message, "エラー",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                });

                MessageBox.Show(ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
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

        //private async Task ConvertXmlToExcel(string path, bool bDetail, IProgress<int> progress, IProgress<string> status, Action<string> onSuccess, CancellationToken token)
        //{
        //    token.ThrowIfCancellationRequested();
        //    progress.Report(0);

        //    //
        //    // ① XML 読み込み
        //    //
        //    status.Report("XML 読み込み中...");
        //    XmlLoader loader = new(path);
        //    loader.LoadXml();
        //    progress.Report(20);
        //    token.ThrowIfCancellationRequested();

        //    string excelPath;

        //    //
        //    // ② トグルチェックによる処理分岐（テーブル構築の有無）
        //    //
        //    if (!bDetail)
        //    {
        //        // 詳細出力オフの場合はテーブル構築をスキップして直接 Excel 出力

        //        //
        //        // ③ パス設定
        //        //
        //        status.Report("Excel 出力先パス設定中...");
        //        excelPath = Path.ChangeExtension(path, ".xlsx");
        //        progress.Report(40);
        //        token.ThrowIfCancellationRequested();

        //        //
        //        // ④ ファイル既存チェック（上書き確認）
        //        //
        //        if (!ChkFileExists(excelPath))
        //            throw new OperationCanceledException("同名ファイルが既にあります。"); // キャンセル扱い
        //        progress.Report(50);
        //        token.ThrowIfCancellationRequested();

        //        //
        //        // ⑤ Excel 出力
        //        //
        //        status.Report("Excel 出力中...");
        //        _ = new ExcelExporter();
        //        ExcelExporter.Export(excelPath, loader.ReadData);

        //    }
        //    else
        //    {
        //        // 詳細出力オンの場合はテーブル構築してから Excel 出力

        //        //
        //        // ③ テーブル構築
        //        //
        //        status.Report("テーブル構築中...");
        //        XmlTableBuilder builder = new(loader.ReadData);
        //        var tables = builder.Build();
        //        progress.Report(30);
        //        token.ThrowIfCancellationRequested();

        //        //
        //        // ④ パス設定
        //        //
        //        status.Report("Excel 出力先パス設定中...");
        //        excelPath = Path.ChangeExtension(path, ".xlsx");
        //        progress.Report(40);
        //        token.ThrowIfCancellationRequested();

        //        //
        //        // ⑤ ファイル既存チェック（上書き確認）
        //        //
        //        if (!ChkFileExists(excelPath))
        //            throw new OperationCanceledException("同名ファイルが既にあります。"); // キャンセル扱い
        //        progress.Report(50);
        //        token.ThrowIfCancellationRequested();

        //        //
        //        // ⑥ Excel 出力
        //        //
        //        status.Report("Excel 出力中...");
        //        _ = new ExcelExporter();
        //        ExcelExporter.Export(excelPath, tables);

        //    }

        //    progress.Report(100);
        //    status.Report("完了");

        //    //  成功時だけパスを外に返す
        //    onSuccess(excelPath);
        //}

        private async Task ConvertXmlToExcel(string path, bool bDetail, IProgress<int> progress, IProgress<string> status, Action<string> onSuccess, CancellationToken token)
        {
            StatusData sd = new();
            XmlLoader? loader = null;
            Dictionary<string, List<Dictionary<string, string>>> ? excelSource = null;
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
                        //if (!bDetail)
                        //    excelSource = loader.ReadData;
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
                        } else {
                            ExcelExporter.Export(excelPath!, loader!.ReadData);
                        }
                        
                        break;

                    case 6: // 完了（処理なし）
                        break;
                }
            }
            //token.ThrowIfCancellationRequested();
            //progress.Report(0);

            ////
            //// ① XML 読み込み
            ////
            //status.Report("XML 読み込み中...");
            //XmlLoader loader = new(path);
            //loader.LoadXml();
            //progress.Report(20);
            //token.ThrowIfCancellationRequested();

            //string excelPath;

            ////
            //// ② トグルチェックによる処理分岐（テーブル構築の有無）
            ////
            //if (!bDetail)
            //{
            //    // 詳細出力オフの場合はテーブル構築をスキップして直接 Excel 出力

            //    //
            //    // ③ パス設定
            //    //
            //    status.Report("Excel 出力先パス設定中...");
            //    excelPath = Path.ChangeExtension(path, ".xlsx");
            //    progress.Report(40);
            //    token.ThrowIfCancellationRequested();

            //    //
            //    // ④ ファイル既存チェック（上書き確認）
            //    //
            //    if (!ChkFileExists(excelPath))
            //        throw new OperationCanceledException("同名ファイルが既にあります。"); // キャンセル扱い
            //    progress.Report(50);
            //    token.ThrowIfCancellationRequested();

            //    //
            //    // ⑤ Excel 出力
            //    //
            //    status.Report("Excel 出力中...");
            //    _ = new ExcelExporter();
            //    ExcelExporter.Export(excelPath, loader.ReadData);

            //}
            //else
            //{
            //    // 詳細出力オンの場合はテーブル構築してから Excel 出力

            //    //
            //    // ③ テーブル構築
            //    //
            //    status.Report("テーブル構築中...");
            //    XmlTableBuilder builder = new(loader.ReadData);
            //    var tables = builder.Build();
            //    progress.Report(30);
            //    token.ThrowIfCancellationRequested();

            //    //
            //    // ④ パス設定
            //    //
            //    status.Report("Excel 出力先パス設定中...");
            //    excelPath = Path.ChangeExtension(path, ".xlsx");
            //    progress.Report(40);
            //    token.ThrowIfCancellationRequested();

            //    //
            //    // ⑤ ファイル既存チェック（上書き確認）
            //    //
            //    if (!ChkFileExists(excelPath))
            //        throw new OperationCanceledException("同名ファイルが既にあります。"); // キャンセル扱い
            //    progress.Report(50);
            //    token.ThrowIfCancellationRequested();

            //    //
            //    // ⑥ Excel 出力
            //    //
            //    status.Report("Excel 出力中...");
            //    _ = new ExcelExporter();
            //    ExcelExporter.Export(excelPath, tables);

            //}

            //progress.Report(100);
            //status.Report("完了");

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
            var chk = txtCheck;
            bool checker;
            if (string.IsNullOrEmpty(path))
            {
                if (flag)
                {
                    chk.Visibility = Visibility.Hidden;
                    StatusText = "ファイルパスを入力";
                    return true;
                }
                checker = false;
            }
            else if (!File.Exists(path))
            {
                checker = false;
            } else {
                checker = true;
            }

            if (checker)
            {
                chk.Visibility = Visibility.Visible;                
                chk.Text = "✔";                
                chk.Foreground = new SolidColorBrush(Colors.SeaGreen);
                StatusText = "準備完了";
                return true;
            }
            else
            {
                chk.Visibility = Visibility.Visible;
                chk.Text = "✖";
                chk.Foreground = new SolidColorBrush(Colors.Crimson);
                return false;
            }
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
