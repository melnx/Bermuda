using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Bermuda.DomainLayer;
using System.Collections.ObjectModel;
using Bermuda.Entities;

namespace Bermuda.QL
{
    public partial class RootExpression
    {
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
