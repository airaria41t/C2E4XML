using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
                Title = "ファイルを選択",
                Filter = "すべてのファイル (*.*)|*.*"
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

                var tb = sender as TextBox;
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
                string path = txtFilePath.Text;
                XmlLoader cls = new(path);
                cls.LoadXml();
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