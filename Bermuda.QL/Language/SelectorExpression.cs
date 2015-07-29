using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bermuda.QL
{
    public partial class SelectorExpression : SingleNodeTree
    {
        public SelectorTypes Field { get; private set; }

        public ModifierTypes Modifier { get; private set; }

        public SelectorExpression(SelectorTypes field, ModifierTypes modifier)
        {
            Field = field;
            Modifier = modifier;
        }

        public string ToString(bool ignoreSelector)
        {
            if (Field == SelectorTypes.Unspecified || ignoreSelector)
            {
                return Child.ToString();
            }
            return String.Format("{0}{1}{2}", Field.ToString(), ConvertModifier(Modifier), Child.ToString());
        }

        public override string ToString()
        {
            return ToString(false);
        }

        private string ConvertModifier(ModifierTypes type)
        {
            switch (type)
            {
                case ModifierTypes.GreaterThan:
                    return ">";
                case ModifierTypes.LessThan:
                    return "<";
                default:
                    return ":";
            }
        }
    }

    public partial class SetterExpression : SingleNodeTree
    {
        public SetterTypes Field { get; private set; }

        public SetterExpression(SetterTypes field)
        {
            Field = field;
        }

        public string ToString(bool ignoreSetter)
        {
            if (ignoreSetter)
            {
                return Child.ToString();
            }
            return String.Format("{0}{1}{2}", Field.ToString(), ":", Child.ToString());
        }

        public override string ToString()
        {
            return ToString(false);
        }
    }
}
