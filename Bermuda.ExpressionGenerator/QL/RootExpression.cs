using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Bermuda.ExpressionGeneration
{
    public abstract partial class RootExpression : SingleNodeTree
    {
        public RootExpression()
        {
            Root = this;
            Warnings = new List<string>();
        }

        public List<string> Warnings { get; private set; }

        public bool ContainsTextSearch = false;
        public System.Type ElementType;

        public void AddWarning(string text)
        {
            Debug.WriteLine("WARNING: " + text);
            Warnings.Add(text);
        }

        internal long[] GetTagLookup(string stringValue)
        {
            throw new System.NotImplementedException();
        }

        private List<object> _parameters;

        internal ReadOnlyCollection<object> Parameters { get; private set; }

        internal void AddParameter(object parameter)
        {
            _parameters.Add(parameter);
        }

        internal ObjectType SingleParameter<ObjectType>()
        {
            return _parameters.OfType<ObjectType>().FirstOrDefault();
        }

        internal void Init()
        {
            _parameters = new List<object>();
            Parameters = _parameters.AsReadOnly();
        }

    }
}
