using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Xml.Linq;
using System.Xml;

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
        /// <summary>
        /// 子ノード
        /// </summary>
        public List<XmlDataNode> Children { get; set; } = [];
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

        XmlDataNode Data { get; set; } = new XmlDataNode();

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
            var rslt = new XmlDataNode{Name = node.Name,};

            if (node.Attributes != null)
            {
                foreach (XmlAttribute attr in node.Attributes)
                {
                    rslt.Children.Add(new XmlDataNode{Name = $"@{attr.Name}", Value = attr.Value});
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
                        rslt.Value = child.Value;
                        break;

                    case XmlNodeType.CDATA:
                        break;
                }
            }
            return rslt;
        }


    }
}
