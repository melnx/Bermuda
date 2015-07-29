namespace Bermuda.QL
{
    public abstract partial class RootExpression : SingleNodeTree
    {
        public RootExpression()
        {
            Root = this;
        }

        public bool ContainsTextSearch = false;

        internal long[] GetTagLookup(string stringValue)
        {
            throw new System.NotImplementedException();
        }
    }
}