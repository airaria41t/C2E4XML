using System;
using System.Collections.Generic;
using System.Linq;

namespace C2E4XML
{
    internal class XmlTableBuilder(XmlDataNode root)
    {
        private readonly XmlDataNode _root = root;

        public Dictionary<string, List<(string Title, List<Dictionary<string, string>> Table)>> Build()
        {
            var result = new Dictionary<string, List<(string, List<Dictionary<string, string>>)>>();

            // ルート直下のタグごとにシートを作成
            foreach (var child in _root.Children)
            {
                var tables = new List<(string, List<Dictionary<string, string>>)>();
                BuildSheet(child, tables, child.Name);
                result[child.Name] = tables;
            }

            return result;
        }

        /// <summary>
        /// シート単位で表を構築する
        /// </summary>
        private void BuildSheet(XmlDataNode node,
            List<(string Title, List<Dictionary<string, string>> Table)> tables,
            string path)
        {
            // --- 子タグを種類ごとにグループ化 ---
            var groups = node.Children.GroupBy(c => c.Name);

            foreach (var g in groups)
            {
                string tagName = g.Key;
                string fullPath = $"{path}.{tagName}"; // ★ タグパス（例：http.entry）

                // --- 同名タグは 1 表にまとめる ---
                var table = new List<Dictionary<string, string>>();

                foreach (var elem in g)
                    table.Add(Flatten(elem));

                tables.Add((fullPath, table));

                // --- 子ノードのシートは別シートで処理するため、ここでは再帰しない ---
            }
        }

        /// <summary>
        /// ノード以下の値をフラット化して Dictionary にする。
        /// ・属性は @属性名
        /// ・子要素は階層無視して展開
        /// </summary>
        private Dictionary<string, string> Flatten(XmlDataNode node)
        {
            var dict = new Dictionary<string, string>();

            // 属性
            foreach (var attr in node.Attributes)
                dict[$"@{attr.Key}"] = attr.Value;

            // 値
            if (node.Value != null)
                dict[node.Name] = node.Value;

            // 子要素
            foreach (var child in node.Children)
            {
                foreach (var kv in Flatten(child))
                    dict[kv.Key] = kv.Value;
            }

            return dict;
        }
    }
}
