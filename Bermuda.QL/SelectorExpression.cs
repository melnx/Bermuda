using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Bermuda.DomainLayer;
using Bermuda.Entities;

namespace Bermuda.QL
{
    public partial class SetterExpression
    {
        public override Expression CreateExpression(object context)
        {
            return null;
        }
    }

    public partial class SelectorExpression
    {
        private Expression CreateTagExpression(object value, bool isId)
        {
            if (!isId)
            {
                string stringValue = (string)value;

                long[] ids = Root.GetTagLookup(stringValue);

                if (ids.Length == 1)
                {
                    var id = ids[0];
                    return GetExpression<Mention>(x => x.Tags != null && x.Tags.Contains(id));
                }
                else if (ids.Length == 0)
                {
                    return Expression.Constant(false);
                }
                else
                {
                    return GetExpression<Mention>(x => x.Tags != null && x.Tags.Any(y => ids.Contains(y)));
                }
            }
            else
            {
                int intValue = (int)value;
                //Root.AddIdToTagLookup(intValue);
                return GetExpression<Mention>(x => x.Tags != null && x.Tags.Contains(intValue));
            }
        }

        //private Expression CreateDatasourceExpression(object value, bool isId)
        //{
        //    if (!isId)
        //    {
        //        string stringValue = (string)value;

        //        long[] ids = Root.GetDatasourceLookup(stringValue);

        //        if (ids.Length == 1)
        //        {
        //            long id = ids[0];
        //            return GetExpression<Mention>(x => x.DatasourceMentions.Any(y => y.DatasourceId == id));
        //        }
        //        else if (ids.Length == 0)
        //        {
        //            return Expression.Constant(false);
        //        }
        //        else
        //        {
        //            return GetExpression<Mention>(x => x.DatasourceMentions.Any(y => ids.Contains(y.DatasourceId)));
        //        }
        //    }
        //    else
        //    {
        //        int intValue = (int)value;
        //        Root.AddIdToDatasourceLookup(intValue);
        //        return GetExpression<Mention>(x => x.DatasourceMentions.Any(y => y.DatasourceId == intValue));
        //    }
        //}

        //private Expression CreatePhraseExpression(object value, bool isId)
        //{
        //    if (!isId)
        //    {
        //        string stringValue = (string)value;

        //        int[] ids = Root.GetPhraseLookup(stringValue);

        //        if (ids.Length == 1)
        //        {
        //            int id = ids[0];
        //            return GetExpression<Activity>(x => x.PhraseInstances.Any(y => y.PhraseId == id));
        //        }
        //        else if (ids.Length == 0)
        //        {
        //            return Expression.Constant(false);
        //        }
        //        else
        //        {
        //            return GetExpression<Activity>(x => x.PhraseInstances.Any(y => ids.Contains(y.PhraseId)));
        //        }
        //    }
        //    else
        //    {
        //        int intValue = (int)value;
        //        Root.AddIdToPhraseLookup(intValue);
        //        return GetExpression<Activity>(x => x.PhraseInstances.Any(y => y.PhraseId == intValue));
        //    }
        //}

        public override Expression CreateExpression(object context)
        {
            LiteralExpression child = Child as LiteralExpression;
            ValueExpression valueChild = Child as ValueExpression;
            if (child == null && valueChild == null)
            {
                throw new Exception();
            }
            object value = child != null ? (object)child.Value : (object)valueChild.Value;
            Expression childExpression = Child.CreateExpression(Field);

            var getExpression = (GetExpression)Root;

            switch (Field)
            {
                case SelectorTypes.Domain:
                    getExpression.Domain = child.Value;
                    return null;
                case SelectorTypes.Notes:
                    Root.ContainsTextSearch = true;
                    return GetExpression<Mention>(x => x.Description.Contains(child.Value));
                case SelectorTypes.Subject:
                case SelectorTypes.Name:
                    Root.ContainsTextSearch = true;
                    return GetExpression<Mention>(x => x.Name == child.Value);
                case SelectorTypes.Unspecified:
                    Root.ContainsTextSearch = true;
                    return GetExpression<Mention>(x => (x.Name != null && x.Name.Contains(child.Value)) || (x.Description != null && x.Description.Contains(child.Value)));
                case SelectorTypes.Description:
                    return GetExpression<Mention>(x => x.Description.Contains(child.Value));
                case SelectorTypes.FromDate:
                    return Expression.GreaterThanOrEqual(GetExpression<Mention, DateTime>(x => x.OccurredOn), child.ConvertExpression<DateTime>());
                case SelectorTypes.Type:
                    return GetExpression<Mention>(x => x.Type == child.Value);
                case SelectorTypes.Id:
                    int id = (int)value;
                    return GetExpression<Mention>(x => x.Id == id);
                case SelectorTypes.Created:
                case SelectorTypes.Date:
                case SelectorTypes.On:
                {
                    var results = child.Convert<DateTime[]>("DateTimeRange");

                    Expression exp;

                    if (Field == SelectorTypes.Created)
                    {
                        exp = GetExpression<Mention, DateTime>(x => x.CreatedOn);
                    }
                    else
                    {
                        exp = GetExpression<Mention, DateTime>(x => x.OccurredOn); 
                    }

                    switch (Modifier)
                    {
                        case ModifierTypes.GreaterThan:
                            return Expression.GreaterThan(exp, Expression.Constant(results[1]));
                        case ModifierTypes.LessThan:
                            return Expression.LessThan(exp, Expression.Constant(results[0]));
                        default:
                            if (results[0] == results[1]) return Expression.Equal(exp, Expression.Constant(results[0]));
                            else return Expression.AndAlso(Expression.GreaterThanOrEqual(exp, Expression.Constant(results[0])), Expression.LessThan(exp, Expression.Constant(results[1])));
                    }
                }    

                case SelectorTypes.ToDate:
                case SelectorTypes.Until:
                
                    return Expression.LessThanOrEqual(GetExpression<Mention, DateTime>(x => x.OccurredOn), child.ConvertExpression<DateTime>());
                
                case SelectorTypes.Tag:
                
                    return CreateTagExpression(value, valueChild != null);
                

                
                case SelectorTypes.DataSource:
                    return Expression.Constant(false);
                    //return CreateDatasourceExpression(value, valueChild != null);

                case SelectorTypes.Theme:
                    return Expression.Constant(false);
                    //return CreatePhraseExpression(value, valueChild != null);

                case SelectorTypes.Influence:
                case SelectorTypes.KloutScore:
                case SelectorTypes.Followers:
                case SelectorTypes.Sentiment:
                {
                    double[] results = child.Convert<double[]>("Sentiment");
                    Expression exp = null;

                    switch(Field )
                    {
                        case SelectorTypes.Sentiment: exp = GetExpression<Mention, double>(x => x.Sentiment); break;
                    }

                    switch (Modifier)
                    {
                        case ModifierTypes.GreaterThan:
                            return Expression.GreaterThan(exp, Expression.Constant(results[1]));
                        case ModifierTypes.LessThan:
                            return Expression.LessThan(exp, Expression.Constant(results[0]));
                        default:
                            if (results[0] == results[1])
                            {
                                return Expression.Equal(exp, Expression.Constant(results[0]));
                            }
                            else
                            {
                                return Expression.AndAlso(Expression.GreaterThanOrEqual(exp, Expression.Constant(results[0])), Expression.LessThanOrEqual(exp, Expression.Constant(results[1])));
                            }
                    }
                }

                case SelectorTypes.InstanceType:
                case SelectorTypes.Hour:
                case SelectorTypes.Month:
                case SelectorTypes.Minute:
                case SelectorTypes.Year:
                case SelectorTypes.Day:
                {
                    var results = child.Convert<double[]>("NumberRange").Select(x => (int)x).ToArray();

                    Expression exp = null;

                    if (Field == SelectorTypes.Year)
                    {
                        exp = GetExpression<Mention, int>(x => x.OccurredOn.Year); 
                    }
                    else if (Field == SelectorTypes.Day)
                    {
                        exp = GetExpression<Mention, int>(x => x.OccurredOn.Day); 
                    }
                    else if (Field == SelectorTypes.Month)
                    {
                        exp = GetExpression<Mention, int>(x => x.OccurredOn.Month); 
                    }
                    else if (Field == SelectorTypes.Minute)
                    {
                        exp = GetExpression<Mention, int>(x => x.OccurredOn.Minute); 
                    }
                    else if (Field == SelectorTypes.Hour)
                    {
                        exp = GetExpression<Mention, int>(x => x.OccurredOn.Hour); 
                    }
                    
                    switch (Modifier)
                    {
                        case ModifierTypes.GreaterThan:
                            return Expression.GreaterThan(exp, Expression.Constant(results[1]));
                        case ModifierTypes.LessThan:
                            return Expression.LessThan(exp, Expression.Constant(results[0]));
                        default:
                            if (results[0] == results[1])
                            {
                                return Expression.Equal(exp, Expression.Constant(results[0]));
                            }
                            else
                            {
                                return Expression.AndAlso(Expression.GreaterThanOrEqual(exp, Expression.Constant(results[0])), Expression.LessThanOrEqual(exp, Expression.Constant(results[1])));
                            }
                    }
                }

                case SelectorTypes.TagCount:
                {
                    var results = child.Convert<double[]>("NumberRange").Select(x => (int)x).ToArray();
               
                    Expression exp;
                   
                    exp = GetExpression<Mention, int>(x => x.Tags.Count());
                    
                    switch (Modifier)
                    {
                        case ModifierTypes.GreaterThan:
                            return Expression.GreaterThan(exp, Expression.Constant(results[1]));
                        case ModifierTypes.LessThan:
                            return Expression.LessThan(exp, Expression.Constant(results[0]));
                        default:
                            return Expression.Equal(exp, Expression.Constant(results[0]));
                    }
                }

                default:
                    return Expression.Constant(true);
            }
        }

        
    }
}
