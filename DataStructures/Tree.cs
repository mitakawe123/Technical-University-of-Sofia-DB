namespace DataStructures
{
    /*public class Tree
    {
        private DKList<TreeNode> root = new();

        public void InsertOrUpdate(char[] key, long value)
        {
            var node = root.FirstOrDefault(n => n.Key.SequenceEqual(key));
            if (node == null)
                root.Add(new TreeNode(key, value));
            else
                if (!node.Values.Contains(value))
                    node.Values.Add(value);
        }

        public bool Delete(char[] key)
        {
            var node = root.FirstOrDefault(n => n.Key.SequenceEqual(key));
            if (node != null)
            {
                root.Remove(node);
                return true;
            }
            return false;
        }

        public DKList<long> Search(char[] key)
        {
            var node = root.FirstOrDefault(n => n.Key.SequenceEqual(key));
            return node?.Values;
        }

        private class TreeNode
        {
            public char[] Key { get; }
            public DKList<long> Values { get; }

            public TreeNode(char[] key, long value)
            {
                Key = key;
                Values = new DKList<long> { value };
            }
        }
    }*/
}
