using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace C2E4XML
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeForm();
            //throw new NotImplementedException();
        }

        private void InitializeForm()
        {
            txtFilePath.Text = "ファイルパスを入力";
            txtResult.Text = "結果が表示されます。";
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


        private void BtnConvert_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // ファイルパスの妥当性確認
                string path = txtFilePath.Text;
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                {
                    MessageBox.Show("有効なファイルパスを入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Excel 出力先パス設定
                string excelPath = System.IO.Path.ChangeExtension(path, ".xlsx");
                // 既に同名の Excel ファイルが存在する場合は上書き確認
                if (File.Exists(excelPath))
                {
                    var result =
                        MessageBox.Show(
                            $"同名の Excel ファイルが既に存在します。\n上書きしますか？\n\n{excelPath}",
                            "確認",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question
                            );
                    if (result != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }

                // XML ロード
                XmlLoader loader = new(path);
                loader.LoadXml();

                bool bDetail = tglDetailOut.IsChecked == true;
                if (bDetail)
                {
                    // テーブル構築
                    XmlTableBuilder builder = new(loader.ReadData);
                    var tables = builder.Build();

                    // Excel 出力
                    //ExcelExporter excel = new(tables);
                    ExcelExporter excel = new();
                    excel.Export(excelPath, tables);
                } else {
                    // Excel 出力
                    ExcelExporter excel = new();
                    excel.Export(excelPath, loader.ReadData);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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