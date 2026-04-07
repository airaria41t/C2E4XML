using System;
using System.Collections.Generic;
using System.Linq;

namespace C2E4XML
{
    /// <summary>
    /// XmlDataNode ツリーを解析し、
    /// 「シート名 → 複数の表（List<List<Dictionary<string,string>>>）」
    /// に変換するクラス。
    /// ・各ノードにつき 1 回だけ表を作成する
    /// ・属性・値・子要素を正しく出力
    /// ・重複出力を防ぐ
    /// </summary>
    internal class XmlTableBuilder(XmlDataNode root)
    {
        private readonly XmlDataNode _root = root;

        public Dictionary<string, List<List<Dictionary<string, string>>>> Build()
        {
            var result = new Dictionary<string, List<List<Dictionary<string, string>>>>();

            // ★ ルート自身をシートとして追加
            {
                string sheetName = _root.Name;
                var tables = new List<List<Dictionary<string, string>>>();
                BuildNode(_root, tables);
                result[sheetName] = tables;
            }

            // ★ ルートの子ノードもシートとして追加
            foreach (var child in _root.Children)
            {
                string sheetName = child.Name;
                var tables = new List<List<Dictionary<string, string>>>();

                BuildNode(child, tables);

                result[sheetName] = tables;
            }

            return result;
        }

        /// <summary>
        /// ノード自身の表を 1 回だけ作り、次に子ノードを処理する
        /// </summary>
        private void BuildNode(XmlDataNode node, List<List<Dictionary<string, string>>> tables)
        {
            // --- ノード自身の表（属性＋値）を作る ---
            var selfTable = CreateSelfTable(node);
            if (selfTable != null)
                tables.Add(selfTable);

            // --- 同名タグのグループを取得 ---
            var groups = node.Children
                .GroupBy(c => c.Name)
                .Where(g => g.Count() > 1)
                .ToList();

            var groupedNames = new HashSet<string>(groups.Select(g => g.Key));

            // --- 親でまとめるべきグループはここで 1 回だけ表にする ---
            foreach (var g in groups)
            {
                var table = new List<Dictionary<string, string>>();

                foreach (var elem in g)
                    table.Add(Flatten(elem));

                tables.Add(table);
            }

            // --- 子ノードを再帰処理（ただし groupedNames はスキップ） ---
            foreach (var child in node.Children)
            {
                if (groupedNames.Contains(child.Name))
                    continue; // ★ 親でまとめたので重複禁止

                BuildNode(child, tables);
            }
        }

        /// <summary>
        /// ノード自身の属性＋値だけの 1 行の表を作る
        /// （子要素があっても必ず作る）
        /// </summary>
        private List<Dictionary<string, string>>? CreateSelfTable(XmlDataNode node)
        {
            if (node.Attributes.Count == 0 && node.Value == null)
                return null;

            var row = new Dictionary<string, string>();

            // 属性
            foreach (var attr in node.Attributes)
                row[$"@{attr.Key}"] = attr.Value;

            // 値
            if (node.Value != null)
                row[node.Name] = node.Value;

            return [row];
        }

        /// <summary>
        /// ノード以下の値をフラット化して Dictionary にする。
        /// ・属性は @属性名
        /// ・子要素は階層無視して展開
        /// </summary>
        private Dictionary<string, string> Flatten(XmlDataNode node)
        {
            var dict = new Dictionary<string, string>();

            foreach (var attr in node.Attributes)
                dict[$"@{attr.Key}"] = attr.Value;

            if (node.Value != null)
                dict[node.Name] = node.Value;

            foreach (var child in node.Children)
            {
                foreach (var kv in Flatten(child))
                    dict[kv.Key] = kv.Value;
            }

            return dict;
        }
    }
}
