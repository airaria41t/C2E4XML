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

        public Dictionary<string, string> Attributes { get; set; } = [];
        /// <summary>
        /// 子ノード
        /// </summary>
        public List<XmlDataNode> Children { get; set; } = [];
    }
}
