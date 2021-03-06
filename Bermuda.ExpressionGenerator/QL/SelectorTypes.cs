﻿namespace Bermuda.ExpressionGeneration
{
    public enum SetterTypes
    {
        None,
        AnyField,
        Tag,
        Sentiment,
        Delete,
        Influence,
        Name,
        Unknown
    }

    public enum SelectorTypes
    {
        Unspecified,
        AnyField,
        Name,
        Type,
        Subject,
        Notes,
        Body,
        Location,
        FromDate,
        Date,
        On,
        ToDate,
        Until,
        From,
        To,
        Cc,
        AnyDirection,
        Tag,
        For,
        Sentiment,
        Source,
        Author,
        Stage,
        Keyword,
        Importance,
        Initiator,
        Target,
        ReplyTo,
        Involves,
        TagCount,
        Handle,
        Description,
        Parent,
        Theme,
        Hour,
        Minute,
        Month,
        Day,
        Invalid,
        ChildCount,
        Dataset,
        IsComment,
        IncludeComments,
        Created,
        DataSource,
        Filter,
        KloutScore,
        Influence,
        Followers,
        Year,
        Unknown,
        InstanceType,
        IgnoreDescription,
        Id,
        Domain,
        Field,
        Function
    }
}