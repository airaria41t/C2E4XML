using System.IO;
using System.Xml;

namespace C2E4XML
{
    internal record FlattenedRow(IReadOnlyList<string> Path, string Value);

    internal class XmlFlattener
    {
        public static IEnumerable<FlattenedRow> Flatten(XmlDataNode root)
        {
            var buffer = new List<FlattenedRow>();
            Traverse(root, [], buffer);
            return buffer;
        }

        private static void Traverse(XmlDataNode node, List<string> path, List<FlattenedRow> buffer)
        {
            var currentPath = new List<string>(path) { node.Name };

            if (node.Value is not null)
            {
                buffer.Add(new FlattenedRow(currentPath, node.Value));
            }

            foreach (var child in node.Children)
            {
                Traverse(child, currentPath, buffer);
            }
        }
    }


    /// <summary>
    /// XML読み込みクラス
    /// </summary>
    /// <param name="path">読み込みファイルのパス</param>
    internal class XmlLoader(string path)
    {
        /// <summary>
        /// 読み込むXMLファイルのパス
        /// </summary>
        public string Path { get; } = path;

        private XmlDataNode Data { get; set; } = new XmlDataNode();

        public XmlDataNode ReadData => Data;

        /// <summary>
        /// ファイルからXMLを読み込む関数
        /// </summary>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        public void LoadXml()
        {
            if (!File.Exists(Path))
                throw new FileNotFoundException("XMLファイルが見つかりません。", Path);

            var doc = new XmlDocument();
            doc.Load(Path);

            Data = Convert(doc.DocumentElement!);
        }

        /// <summary>
        /// 指定された XML ノードおよびそのすべての子ノードを再帰的に変換し、対応する XmlDataNode オブジェクトを作成します。
        /// </summary>
        /// <remarks>このメソッドはノード階層全体を再帰的に処理します。ノードのテキスト内容は Value プロパティに格納され、子ノードは Children
        /// コレクションに追加されます。</remarks>
        /// <param name="node">変換する XML ノード。ノードとその子ノードが XmlDataNode 構造にマッピングされます。null であってはなりません。</param>
        /// <returns>指定されたノードおよびその子ノードを表す XmlDataNode オブジェクト。</returns>
        private static XmlDataNode Convert(XmlNode node)
        {
            var rslt = new XmlDataNode { Name = node.Name, };

            if (node.Attributes != null)
            {
                foreach (XmlAttribute attr in node.Attributes)
                {
                    rslt.Attributes[attr.Name] = attr.Value;
                    //rslt.Children.Add(new XmlDataNode { Name = $"@{attr.Name}", Value = attr.Value });
                }
            }

            foreach (XmlNode child in node.ChildNodes)
            {
                switch (child.NodeType)
                {
                    case XmlNodeType.Element:
                        rslt.Children.Add(Convert(child));
                        break;

                    case XmlNodeType.Text:
                        // 空白・改行の Text ノードは無視する
                        if (!string.IsNullOrWhiteSpace(child.Value))
                            rslt.Value = child.Value.Trim();
                        break;

                    case XmlNodeType.CDATA:
                        rslt.Value = child.Value?.Trim();
                        break;
                }
            }
            return rslt;
        }


    }
}
