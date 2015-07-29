using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Bermuda.Entities;
using System.Linq.Expressions;
using ExpressionSerialization;
using Bermuda.Entities.Thrift;
using System.Web.Script.Serialization;
using Bermuda.Entities.ExpressionGeneration;

namespace Bermuda.Core.BermudaPeer 
{
    //public partial class BermudaServiceClient
    //{

    //}

    //public partial class BermudaResult
    //{
    //    public long CreatedOn;

    //    BermudaNodeStatistic _metadataObject;
    //    public BermudaNodeStatistic MetadataObject
    //    {
    //        get
    //        {
    //            return _metadataObject ?? (this.Metadata != null ? _metadataObject = new JavaScriptSerializer().Deserialize<BermudaNodeStatistic>(this.Metadata) : null );
    //        }

    //        set
    //        {
    //            _metadataObject = value;
    //            Metadata = new JavaScriptSerializer().Serialize(value);
    //        }
    //    }

    //    object _dataObject;
    //    public object DataObject
    //    {
    //        get
    //        {
    //            return _dataObject ?? (this.Data != null  && this.DataType != null ? _dataObject = LinqRuntimeTypeBuilder.DeserializeJson(this.Data, this.DataType, true) : null );
    //        }
    //        set
    //        {
    //            _dataObject = value;
    //            Data = new JavaScriptSerializer().Serialize(value);
    //        }
    //    }
    //}

}