namespace C2E4XML
{
    internal record FlattenedRow(IReadOnlyList<string> Path, string Value);

    internal class XmlFlattener
    {
        public IEnumerable<FlattenedRow> Flatten(XmlDataNode root)
        {
            var buffer = new List<FlattenedRow>();
            Traverse(root, new List<string>(), buffer);
            return buffer;
        }

        private void Traverse(XmlDataNode node, List<string> path, List<FlattenedRow> buffer)
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

}
