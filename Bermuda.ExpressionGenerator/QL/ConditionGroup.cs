using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace Bermuda.ExpressionGeneration
{
    public partial class ConditionGroup : MultiNodeTree
    {
        internal enum JoiningType
        {
            And,
            Or
        }

        internal abstract class LogicalItem
        {
            public bool Negated { get; internal set; }
            public JoiningType JoiningType { get; internal set; }
            public SelectorTypes SingleSelector { get; internal set; }

            public abstract string ToString(bool ignoreParenthesis, bool ignoreSelector, bool ignoreCondition);

            public void AttachCondition(StringBuilder builder, bool ignoreCondition)
            {
                if (!ignoreCondition || JoiningType != JoiningType.And)
                {
                    switch (JoiningType)
                    {
                        case JoiningType.And:
                            builder.Append("AND");
                            break;
                        case JoiningType.Or:
                            builder.Append("OR");
                            break;
                    }
                    builder.Append(" ");
                }

                if (Negated)
                {
                    builder.Append("NOT ");
                    //builder.Append("NOT:");
                }
            }
        }

        internal sealed class SelectorGroup
        {
            public LogicalItem[] Children { get; private set; }
            public SelectorTypes? Selector { get; private set; }

            public SelectorGroup(IEnumerable<LogicalItem> children, SelectorTypes? selector)
            {
                Children = children.ToArray();
                Selector = selector;
            }

            public string ToString(bool ignoreSelector, bool ignoreCondition)
            {
                StringBuilder builder = new StringBuilder();

                if (Children.Length > 1 && Selector != null && !ignoreSelector)
                {
                    for (int i = 0; i < Children.Length; i++)
                    {
                        if (i == 0)
                        {
                            Children[i].AttachCondition(builder, true);
                            if (Selector != SelectorTypes.Unspecified)
                            {
                                builder.Append(Selector.ToString());
                                builder.Append(":");
                            }
                            builder.Append("(");
                        }

                        builder.Append(Children[i].ToString(false, true, i == 0));
                        if (i < Children.Length - 1)
                        {
                            builder.Append(" ");
                        }
                    }

                    builder.Append(")");
                }
                else
                {
                    for (int i = 0; i < Children.Length; i++)
                    {
                        builder.Append(Children[i].ToString(Children.Length == 1, ignoreSelector, i == 0 && ignoreCondition));
                        if (i < Children.Length - 1)
                        {
                            builder.Append(" ");
                        }
                    }
                }

                return builder.ToString();
            }
        }

        internal sealed class LogicalSelector : LogicalItem
        {
            public SelectorExpression Selector { get; private set; }

            public LogicalSelector(SelectorExpression selector)
            {
                Selector = selector;
                SingleSelector = selector.NodeType;
            }

            public override string ToString(bool ignoreParenthesis, bool ignoreSelector, bool ignoreCondition)
            {
                StringBuilder builder = new StringBuilder();
                AttachCondition(builder, ignoreCondition);
                builder.Append(Selector.ToString(ignoreSelector));
                return builder.ToString();
            }
        }

        internal sealed class LogicalGroup : LogicalItem
        {
            public bool IsSingleSelector { get; private set; }

            public LogicalItem[] SubItems { get; private set; }

            public ConditionGroup Group { get; private set; }

            public SelectorGroup[] SelectorGroups { get; private set; }

            public LogicalGroup(ConditionGroup group)
            {
                List<LogicalItem> subItems = new List<LogicalItem>();

                Group = group;

                foreach (ExpressionTreeBase current in group.Children)
                {
                    foreach (LogicalItem next in ConditionGroup.Convert(current))
                    {
                        subItems.Add(next);
                    }
                }

                SubItems = subItems.ToArray();

                IEnumerable<SelectorTypes> types = SubItems.Select(x => x.SingleSelector).Distinct();

                if (IsSingleSelector = types.Count() == 1 && !SubItems.OfType<LogicalGroup>().Any(x => !x.IsSingleSelector))
                {
                    SingleSelector = types.First();
                }

                List<List<LogicalItem>> chains = GetChains(subItems);

                List<SelectorGroup> groups = new List<SelectorGroup>();

                foreach (List<LogicalItem> chain in chains)
                {
                    List<LogicalItem> unknownItems = new List<LogicalItem>();

                    SelectorTypes[] newTypes = chain.Select(x => x is LogicalGroup ? (((LogicalGroup)x).IsSingleSelector ? x.SingleSelector : SelectorTypes.Invalid) : x.SingleSelector).Distinct().ToArray();

                    foreach (SelectorTypes current in newTypes)
                    {
                        List<LogicalItem> items = new List<LogicalItem>();

                        for (int i = chain.Count - 1; i >= 0; i--)
                        {
                            LogicalItem item = chain[i];
                            SelectorTypes? selector = item is LogicalGroup ? (((LogicalGroup)item).IsSingleSelector ? (SelectorTypes?)item.SingleSelector : null) : item.SingleSelector;

                            if (selector == null)
                            {
                                unknownItems.Insert(0, item);
                                chain.RemoveAt(i);
                            }
                            else if (selector == current)
                            {
                                items.Insert(0, item);
                            }
                        }

                        if (items.Count > 0)
                        {
                            groups.Add(new SelectorGroup(items, current));
                        }
                    }

                    if (unknownItems.Count > 0)
                    {
                        groups.Add(new SelectorGroup(unknownItems, null));
                    }
                }

                for (int i = groups.Count - 1; i >= 0; i--)
                {
                    if (i > 0)
                    {
                        if (groups[i].Selector == groups[i - 1].Selector)
                        {
                            SelectorGroup upperGroup = groups[i];
                            SelectorGroup lowerGroup = groups[i - 1];
                            groups.RemoveAt(i);
                            groups.RemoveAt(i - 1);

                            List<LogicalItem> items = new List<LogicalItem>();

                            items.AddRange(lowerGroup.Children);
                            items.AddRange(upperGroup.Children);

                            SelectorGroup joinedGroup = new SelectorGroup(items, upperGroup.Selector);

                            groups.Insert(i - 1, joinedGroup);
                        }
                    }
                }

                SelectorGroups = groups.ToArray();
            }

            private List<List<LogicalItem>> GetChains(List<LogicalItem> items)
            {
                List<List<LogicalItem>> chains = new List<List<LogicalItem>>();

                while (items.Count > 0)
                {
                    List<LogicalItem> chain = new List<LogicalItem>();

                    for (int i = items.Count - 1; i >= 0; i--)
                    {
                        LogicalItem item = items[i];

                        chain.Insert(0, item);
                        items.RemoveAt(i);

                        if (item.JoiningType == JoiningType.Or)
                        {
                            break;
                        }
                    }

                    chains.Insert(0, chain);
                }

                return chains;
            }

            public override string ToString(bool ignoreParenthesis, bool ignoreSelector, bool ignoreCondition)
            {
                StringBuilder builder = new StringBuilder();

                AttachCondition(builder, ignoreCondition);

                if (!ignoreParenthesis)
                {
                    builder.Append("(");
                }

                for (int i = 0; i < SelectorGroups.Length; i++)
                {
                    builder.Append(SelectorGroups[i].ToString(ignoreSelector, i == 0));
                    if (i < SelectorGroups.Length - 1)
                    {
                        builder.Append(" ");
                    }
                }

                if (!ignoreParenthesis)
                {
                    builder.Append(")");
                }

                return builder.ToString();
            }
        }

        private static JoiningType? GetJoiningType(ExpressionTreeBase item)
        {
            if (item is AndCondition)
            {
                return JoiningType.And;
            }
            else if (item is OrCondition)
            {
                return JoiningType.Or;
            }
            return null;
        }

        private static ExpressionTreeBase ExtractItem(ExpressionTreeBase item, out JoiningType? type, out bool negate, out ConditionalExpression lastParent)
        {
            lastParent = null;
            type = GetJoiningType(item);

            if (item is ConditionalExpression && !(item is NotCondition))
            {
                lastParent = (ConditionalExpression)item;
                item = ((ConditionalExpression)item).Child;
            }

            if (item is NotCondition)
            {
                negate = true;
                item = ((NotCondition)item).Child;
            }
            else
            {
                negate = false;
            }

            return item;
        }

        internal static IEnumerable<LogicalItem> Convert(ExpressionTreeBase item)
        {
            JoiningType? type;
            bool negate;
            ConditionalExpression lastParent;

            ExpressionTreeBase actualItem = ExtractItem(item, out type, out negate, out lastParent);

            LogicalItem result;

            if (actualItem is SelectorExpression)
            {
                result = new LogicalSelector((SelectorExpression)actualItem);
            }
            else if (actualItem is ConditionGroup)
            {
                result = new LogicalGroup((ConditionGroup)actualItem);
            }
            else
            {
                throw new Exception();
            }

            result.Negated = negate;
            result.JoiningType = type ?? JoiningType.And;

            yield return result;

            if (lastParent != null)
            {
                foreach (LogicalItem current in lastParent.AdditionalConditions.SelectMany(x => Convert(x)))
                {
                    yield return current;
                }
            }
        }

        public override string ToString()
        {
            return ToString(false);
        }

        public string ToString(bool ignoreParenthesis)
        {
            LogicalGroup group;

            if ((group = Convert(this).FirstOrDefault() as LogicalGroup) != null)
            {
                return group.ToString(Children.Count == 1, false, true);
            }

            if (ignoreParenthesis || Children.Count == 1)
            {
                return MultiToString(Children);
            }
            else
            {
                return String.Format("({0})", MultiToString(Children));
            }
        }

        public override Expression CreateExpression(object context)
        {
            foreach (ExpressionTreeBase current in Children)
            {
                return current.CreateExpression(null);
            }
            return null;
        }
    }
}
