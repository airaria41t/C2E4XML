namespace C2E4XML
{
    internal class XmlTableBuilder(XmlDataNode root)
    {
        private readonly XmlDataNode _root = root;
        private readonly HashSet<string> _created = [];

        public Dictionary<string, List<Dictionary<string, string>>> Build()
        {
            var result = new Dictionary<string, List<Dictionary<string, string>>>();

            // ✔ config も必ず出力される（属性のみの 1 行表）
            AddAttributeTable(_root.Name, _root, result);

            // ✔ シートは「ルート直下のタグ」 → ここでは path の先頭要素になる
            foreach (var child in _root.Children)
            {
                BuildNode(child, $"{child.Name}", result);
            }

            return result;
        }

        private void BuildNode(XmlDataNode node, string path,
            Dictionary<string, List<Dictionary<string, string>>> result)
        {
            if (_created.Contains(path))
                return;

            var children = node.Children;

            // ✔ 属性・値・子がなくても表を出す（空表）
            // ✔ 値が無い場合だけ空表を作る
            if (node.Attributes.Count == 0 &&
                children.Count == 0 &&
                string.IsNullOrEmpty(node.Value))
            {
                AddEmptyTable(path, result);
                return;
            }

            // ✔ 属性だけ → 1 行表
            if (node.Attributes.Count > 0 && children.Count == 0)
            {
                AddAttributeTable(path, node, result);
                return;
            }

            // 子ノードの構造を調べる
            var sameNameGroups = children.GroupBy(c => c.Name).ToList();

            // entry の兄弟まとめが可能なら structureGroups は無効化
            if (sameNameGroups.Any(g => g.Count() > 1))
            {
                // ✔ 同じタグ名の兄弟（entry など）は 1 表にまとめる（行＝兄弟）
                foreach (var g in sameNameGroups.Where(g => g.Count() > 1))
                {
                    string tablePath = $"{path}.{g.Key}";
                    if (_created.Contains(tablePath)) continue;

                    var rows = new List<Dictionary<string, string>>();
                    foreach (var n in g)
                    {
                        var row = new Dictionary<string, string>();

                        // 属性
                        foreach (var attr in n.Attributes)
                            row[$"@{attr.Key}"] = attr.Value;

                        // 値と子ノードは flatten（ただし entry の子は flatten しない）
                        foreach (var child in n.Children)
                            Flatten(child, child.Name, row);

                        rows.Add(row);
                    }

                    _created.Add(tablePath);
                    result[tablePath] = rows;

                    // その子ノード配下は表にしない（重複防止）
                    continue;
                }
            }

            // structureGroups は「名前が異なる兄弟」ではなく
            // 「同じ親の子の中で構造が似ているもの」だけに限定する
            var structureGroups = children
                .GroupBy(c => GetSignature(c))
                .Where(g =>
                    g.Count() > 1 &&
                    g.Select(x => x.Name).Distinct().Count() > 1 &&
                    g.All(x => x.Children.Count > 0) &&
                    // ★ 追加：entry の兄弟まとめが可能なら structureGroups を無効化
                    !sameNameGroups.Any(sg => sg.Count() > 1)
                )
                .ToList();

            if (structureGroups.Count > 0)
            {
                // このノード自身が表の単位になる
                if (!_created.Contains(path))
                {
                    var rows = new List<Dictionary<string, string>>();
                    foreach (var s in children)
                    {
                        var row = new Dictionary<string, string>
                        {
                            ["type"] = s.Name // ✔ type 列は先頭
                        };
                        Flatten(s, "", row);
                        rows.Add(row);
                    }

                    _created.Add(path);
                    result[path] = rows;
                }

                // 子ノード配下は表にしない（重複防止）
                return;
            }

            // ★★★ 修正版：属性 + 子複数 → 1 行表（customer を正しく処理する）★★★
            if (node.Attributes.Count > 0 &&
    children.Count > 1 &&
    children.All(c => c.Children.Count == 0))   // ★ 追加：子が葉ノードのときだけ
            {
                string tablePath = path;
                if (!_created.Contains(tablePath))
                {
                    var row = new Dictionary<string, string>();

                    // 属性
                    foreach (var attr in node.Attributes)
                        row[$"@{attr.Key}"] = attr.Value;

                    // 子ノード（値）を列にする
                    foreach (var child in children)
                    {
                        var value = child.Value ?? string.Empty;
                        row[child.Name] = value;
                    }

                    _created.Add(tablePath);
                    result[tablePath] = [row];
                }

                return;
            }

            // ★★★ 値だけを持つ単純要素（DeliveryNotes など）を 1 行表にする分岐 ★★★
            if (node.Attributes.Count == 0 &&
                children.Count == 0 &&
                !string.IsNullOrEmpty(node.Value))
            {
                string tablePath = path;
                if (!_created.Contains(tablePath))
                {
                    var row = new Dictionary<string, string>
                    {
                        [path.Split('.').Last()] = node.Value
                    };

                    _created.Add(tablePath);
                    result[tablePath] = [row];
                }

                return;
            }

            // ✔ 子が 1 つだけ → 1 行表
            if (children.Count == 1)
            {
                var only = children[0];

                // ★ その子の直下に「同名の兄弟」が複数あるなら、
                //    ここで 1 行表にせず、後続のロジック（兄弟まとめ）に任せる
                bool onlyHasSameNameGrandChildren =
                    only.Children
                        .GroupBy(c => c.Name)
                        .Any(g => g.Count() > 1);
                // ★★★ 既存判定が false のときだけ deeper を探索する ★★★
                if (!onlyHasSameNameGrandChildren)
                {
                    if (HasSameNameDescendants(only))
                        onlyHasSameNameGrandChildren = true;
                }
                if (!onlyHasSameNameGrandChildren)
                {
                    string tablePath = $"{path}.{only.Name}";
                    if (!_created.Contains(tablePath))
                    {
                        var row = new Dictionary<string, string>();
                        Flatten(only, "", row);

                        _created.Add(tablePath);
                        result[tablePath] = [row];
                    }

                    return;
                }
            }

            // ここまでで表にならなかったノードは、さらに下の階層を探索
            foreach (var child in children)
            {
                BuildNode(child, $"{path}.{child.Name}", result);
            }
        }

        private void AddEmptyTable(string path,
            Dictionary<string, List<Dictionary<string, string>>> result)
        {
            if (_created.Add(path))
                result[path] = [];
        }

        private void AddAttributeTable(string path, XmlDataNode node,
            Dictionary<string, List<Dictionary<string, string>>> result)
        {
            if (!_created.Add(path)) return;

            var row = new Dictionary<string, string>();
            foreach (var attr in node.Attributes)
                row[$"@{attr.Key}"] = attr.Value;

            result[path] = [row];
        }

        private static string GetSignature(XmlDataNode node)
        {
            // 自身の属性名
            var attrNames = node.Attributes.Keys
                .OrderBy(n => n);

            // 子ノードの構造を含めた signature
            var childSigs = node.Children
                .Select(c =>
                    $"{c.Name}:" +
                    $"{string.Join(",", c.Attributes.Keys.OrderBy(a => a))}:" +
                    $"{c.Children.Count}"
                )
                .OrderBy(s => s);

            return $"{string.Join(",", attrNames)}|{string.Join(",", childSigs)}";
        }

        private static bool HasSameNameDescendants(XmlDataNode node)
        {
            // 子孫に同名兄弟がいるか？
            foreach (var child in node.Children)
            {
                // 直下に同名兄弟
                if (child.Children
                        .GroupBy(c => c.Name)
                        .Any(g => g.Count() > 1))
                    return true;

                // さらに深い階層を探索
                if (HasSameNameDescendants(child))
                    return true;
            }

            return false;
        }


        private static void Flatten(XmlDataNode node, string prefix, Dictionary<string, string> row)
        {
            if (!string.IsNullOrEmpty(node.Value))
            {
                string col = prefix.Trim('.');
                row[col] = node.Value;
            }

            foreach (var child in node.Children)
            {
                string nextPrefix = string.IsNullOrEmpty(prefix)
                    ? child.Name
                    : $"{prefix}.{child.Name}";

                Flatten(child, nextPrefix, row);
            }
        }
    }
}