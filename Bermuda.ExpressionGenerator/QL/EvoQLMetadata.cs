using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;

namespace Bermuda.ExpressionGeneration
{
    public static class EvoQLMetadata
    {
        private static Dictionary<SelectorTypes, GetTypes> _selectorMappings = new Dictionary<SelectorTypes,GetTypes>();
        private static Dictionary<SelectorTypes, bool> _allowedModifiers = new Dictionary<SelectorTypes, bool>();

        public static SelectorTypes[] GetValidTypes(GetTypes getType)
        {
            var list = _selectorMappings.Where(x => x.Value == getType || ((int)getType > 0 && x.Value == GetTypes.Instance) || ((int)getType > 0 && ((int)x.Value & (int)getType) > 0) ).Select(x => x.Key).ToList();
            if (getType != GetTypes.SuggestedCommunication)
            {
                list.Add(SelectorTypes.Unspecified);
            }

            if (getType == GetTypes.Datapoint)
            {
                list.Add(SelectorTypes.FromDate);
                list.Add(SelectorTypes.ToDate);
                list.Add(SelectorTypes.Date);
                list.Add(SelectorTypes.Tag);
                list.Add(SelectorTypes.Dataset);
                list.Add(SelectorTypes.Minute);
                list.Add(SelectorTypes.Hour);
                list.Add(SelectorTypes.Day);
                list.Add(SelectorTypes.Month);
            }

            if (getType == GetTypes.SetDefinition)
            {
                list.Add(SelectorTypes.Tag);
            }

            if (getType == GetTypes.Keyword)
            {
                list.Add(SelectorTypes.Tag);
            }

            if (getType == GetTypes.Handle)
            {
                list.Add(SelectorTypes.Tag);
            }

            if (getType == GetTypes.HandleMetric)
            {
                list.Add(SelectorTypes.Date);
                list.Add(SelectorTypes.FromDate);
                list.Add(SelectorTypes.ToDate);
                list.Add(SelectorTypes.Handle);
                list.Add(SelectorTypes.Tag);
            }

            return list.ToArray();
        }

        public static GetTypes GetValidGetType(IEnumerable<SelectorTypes> selectors)
        {
            GetTypes? result = null;

            List<GetTypes> foundTypes = new List<GetTypes>();

            foreach (SelectorTypes selector in selectors)
            {
                if (selector == SelectorTypes.Unspecified)
                {
                    continue;
                }
                GetTypes currentType;
                if (!_selectorMappings.TryGetValue(selector, out currentType) || foundTypes.Contains(currentType))
                {
                    continue;
                }
                foundTypes.Add(currentType);
            }

            foundTypes = foundTypes.OrderBy(x => (int)x).ToList();

            foreach (GetTypes currentType in foundTypes)
            {
                if ((int)currentType < 0)
                {
                    if (result != null)
                    {
                        throw new Exception();
                    }
                    result = currentType;
                }
                else
                {
                    if (result != null && (int)result < 0)
                    {
                        throw new Exception();
                    }
                    if (result == null)
                    {
                        result = currentType;
                    }
                    else
                    {
                        //if (((int)currentType & (int)result) == (int)GetTypes.Instance)
                        //{
                        //    throw new Exception();
                        //}
                        //if ((int)currentType > (int)result)
                        if( result.Value.IsBaseOf(currentType) )
                        {
                            result = currentType;
                        }
                    }
                }
            }

            if (result != null)
            {
                return result.Value;
            }

            throw new Exception();
        }

        public static bool ModifiersAllowed(SelectorTypes type)
        {
            return _allowedModifiers.ContainsKey(type) ? _allowedModifiers[type] : false;
        }

        static EvoQLMetadata()
        {
            _selectorMappings.Add(SelectorTypes.Notes, GetTypes.Instance);
            _selectorMappings.Add(SelectorTypes.InstanceType, GetTypes.Instance);
            _selectorMappings.Add(SelectorTypes.Subject, GetTypes.Instance);
            _selectorMappings.Add(SelectorTypes.Name, GetTypes.Instance);
            _selectorMappings.Add(SelectorTypes.Description, GetTypes.Mention);
            _selectorMappings.Add(SelectorTypes.Body, GetTypes.Communication);
            _selectorMappings.Add(SelectorTypes.For, GetTypes.SuggestedCommunication);
            _selectorMappings.Add(SelectorTypes.From, GetTypes.Communication);
            _selectorMappings.Add(SelectorTypes.Initiator, GetTypes.Activity);
            _selectorMappings.Add(SelectorTypes.Target, GetTypes.WorkItem);
            _selectorMappings.Add(SelectorTypes.To, GetTypes.Communication);
            _selectorMappings.Add(SelectorTypes.AnyDirection, GetTypes.Communication);
            _selectorMappings.Add(SelectorTypes.Involves, GetTypes.Activity);
            _selectorMappings.Add(SelectorTypes.FromDate, GetTypes.Activity);
            _selectorMappings.Add(SelectorTypes.ToDate, GetTypes.Activity);
            _selectorMappings.Add(SelectorTypes.On, GetTypes.Activity);
            _selectorMappings.Add(SelectorTypes.Date, GetTypes.Activity);
            _selectorMappings.Add(SelectorTypes.Until, GetTypes.Activity);
            _selectorMappings.Add(SelectorTypes.Tag, GetTypes.Instance);
            _selectorMappings.Add(SelectorTypes.Source, GetTypes.Mention);
            _selectorMappings.Add(SelectorTypes.Author, GetTypes.Mention);
            _selectorMappings.Add(SelectorTypes.Type, GetTypes.Mention);
            _selectorMappings.Add(SelectorTypes.DataSource, GetTypes.Mention);
            _selectorMappings.Add(SelectorTypes.ReplyTo, GetTypes.Activity);
            _selectorMappings.Add(SelectorTypes.IgnoreDescription, GetTypes.Instance);
            _selectorMappings.Add(SelectorTypes.Parent, GetTypes.Activity);
            _selectorMappings.Add(SelectorTypes.Stage, GetTypes.Instance);
            _selectorMappings.Add(SelectorTypes.Keyword, GetTypes.Mention);
            _selectorMappings.Add(SelectorTypes.Filter, GetTypes.Mention);
            _selectorMappings.Add(SelectorTypes.IsComment, GetTypes.Mention);
            _selectorMappings.Add(SelectorTypes.Theme, GetTypes.Activity);
            _selectorMappings.Add(SelectorTypes.Handle, GetTypes.Activity);
            _selectorMappings.Add(SelectorTypes.Sentiment, GetTypes.Activity);
            _selectorMappings.Add(SelectorTypes.Hour, GetTypes.Activity);
            _selectorMappings.Add(SelectorTypes.Minute, GetTypes.Activity);
            _selectorMappings.Add(SelectorTypes.Month, GetTypes.Activity);
            _selectorMappings.Add(SelectorTypes.Day, GetTypes.Activity);
            _selectorMappings.Add(SelectorTypes.Year, GetTypes.Activity);
            _selectorMappings.Add(SelectorTypes.TagCount, GetTypes.Instance);
            _selectorMappings.Add(SelectorTypes.Importance, GetTypes.Activity);
            _selectorMappings.Add(SelectorTypes.IncludeComments, GetTypes.Activity);
            _selectorMappings.Add(SelectorTypes.ChildCount, GetTypes.Activity);
            _selectorMappings.Add(SelectorTypes.Created, GetTypes.Instance);
            _selectorMappings.Add(SelectorTypes.Influence, GetTypes.Mention);
            _selectorMappings.Add(SelectorTypes.Followers, GetTypes.Mention);
            _selectorMappings.Add(SelectorTypes.KloutScore, GetTypes.Mention);
            _selectorMappings.Add(SelectorTypes.Id, GetTypes.Instance);
            
            _allowedModifiers.Add(SelectorTypes.Sentiment, true);
            _allowedModifiers.Add(SelectorTypes.Hour, true);
            _allowedModifiers.Add(SelectorTypes.Minute, true);
            _allowedModifiers.Add(SelectorTypes.TagCount, true);
        }
    }
}
