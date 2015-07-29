using System;
using System.Collections.Generic;

namespace Bermuda.QL
{
    public enum location
    {
        anyField, type, name, notes, fromdate, todate, location, from, to, cc, anyDirection, tag
        , NULL
    }
    public class LocationWrapper
    {
        public location inn = location.anyField;
    }
    public enum logic
    {
        and, or, not
    }
    public class LogicWrapper
    {
        public logic inn = logic.and;
    }
    public class TreeElement
    {
        public List<TreeElement> children = new List<TreeElement>();
        public bool includeNotExclude = true;
        public logic logic;
        public location searchIn;
        public string searchFor;
    }
    public class TreeLink
    {
        public bool IncludeNotExclude = true;
        public logic Logic;
        public location SearchIn;
        public string SearchFor;
        public TreeLink Inner;
        public TreeLink Next;
    }
}
