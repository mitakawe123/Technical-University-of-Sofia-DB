namespace DataStructures
{
    public class BPlusTree<TKey> where TKey : IComparable<TKey>
    {
        private BPlusTreeNode<TKey, int>? root;
        private int order;

        public BPlusTree(int order)
        {
            if(order < 3)
                throw new ArgumentException("Order must be at least 3", nameof(order));
            
            this.order = order;
        }

        public void Insert(TKey key, int pageNumber)
        {
            
        }

        public void Delete(TKey key)
        {

        }

    }
    public class BPlusTreeNode<TKey, TValue> where TKey : IComparable<TKey> 
    {
        public List<TKey> Keys { get; set; }
        public List<TValue> Values { get; set; } // This would be a list of page numbers
        public bool IsLeaf { get; set; }
        public BPlusTreeNode<TKey, TValue> Parent { get; set; }
        public List<BPlusTreeNode<TKey, TValue>> Children { get; set; }
    }
}
