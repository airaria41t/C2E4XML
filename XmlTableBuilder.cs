using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace C2E4XML
{

    /// <summary>
    /// XmlDataNode ツリーを解析し、
    /// 「シート名 → 複数の表（List<List<Dictionary<string,string>>>）」
    /// に変換する汎用クラス。
    /// </summary>
    internal class XmlTableBuilder(XmlDataNode root)
    {
        private readonly XmlDataNode _root = root;

        /// <summary>
        /// ルート直下のノードごとに 1 シートを作成する。
        /// </summary>
        public Dictionary<string, List<List<Dictionary<string, string>>>> Build()
        {
            var result = new Dictionary<string, List<List<Dictionary<string, string>>>>();

            foreach (var child in _root.Children)
            {
                string sheetName = child.Name;
                var tables = new List<List<Dictionary<string, string>>>();

                BuildTablesRecursive(child, tables);

                result[sheetName] = tables;
            }

            return result;
        }

        /// <summary>
        /// 任意のノードを解析し、必要に応じて表を生成する。
        /// </summary>
        private static void BuildTablesRecursive(XmlDataNode node, List<List<Dictionary<string, string>>> tables)
        {
            var children = node.Children;

            // --- 値だけのノード（例：<topn>100</topn>） ---
            if (children.Count == 0 && node.Value != null)
            {
                tables.Add(
            [
                new Dictionary<string, string> { [node.Name] = node.Value }
            ]);
                return;
            }

            // --- 同名タグが複数ある場合（リスト扱い） ---
            var groups = children.GroupBy(c => c.Name);
            foreach (var g in groups)
            {
                if (g.Count() > 1)
                {
                    var table = new List<Dictionary<string, string>>();

                    foreach (var elem in g)
                    {
                        var row = Flatten(elem);
                        table.Add(row);
                    }

                    tables.Add(table);
                }
            }

            // --- 子要素が複数ある場合（1 表にまとめる） ---
            if (children.Count > 1)
            {
                var table = new List<Dictionary<string, string>>();

                foreach (var child in children)
                {
                    var row = new Dictionary<string, string>
                    {
                        ["type"] = child.Name
                    };

                    foreach (var kv in Flatten(child))
                        row[kv.Key] = kv.Value;

                    table.Add(row);
                }

                tables.Add(table);
            }

            // --- 再帰処理 ---
            foreach (var child in children)
                BuildTablesRecursive(child, tables);
        }

        /// <summary>
        /// ノード以下の値をフラット化して Dictionary にする。
        /// ・属性は @属性名
        /// ・子要素は階層無視して展開
        /// </summary>
        private static Dictionary<string, string> Flatten(XmlDataNode node)
        {
            var dict = new Dictionary<string, string>();

            // 属性
            foreach (var attr in node.Attributes)
                dict[$"@{attr.Key}"] = attr.Value;

            // 自身の値
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
