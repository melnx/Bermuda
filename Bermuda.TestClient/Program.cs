using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Linq.Expressions;
using System.Diagnostics;
using System.ServiceModel;
using Bermuda.ExpressionGeneration;
using Bermuda.Core;
using ExpressionSerialization;
using System.Text.RegularExpressions;
using Bermuda.Catalog;
using System.Collections.Concurrent;
using System.Web.Script.Serialization;
using Bermuda.Interface;
using System.Reflection;
using System.Reflection.Emit;

namespace Bermuda.TestClient
{
    class Program
    {


        private static string GetJsonField(string s, string field)
        {
            var result = new StringBuilder();

            var i = s.IndexOf("\"" + field + "\"");

            if (i == -1) return null;

            int quotes = 0;

            for (; i < s.Length; i++)
            {
                var c = s[i];
                result.Append(s[i]);
                if (s[i] == '\"') quotes++;
                if (quotes == 4) 
                    break;
                if (s[i] == ':') result.Clear();
            }

            var finalresult = result.ToString().Trim('"');

            return finalresult;
        }

        struct Lolz
        {
            public int a;
            public string b;
        }

        public static Expression UnionEnumerableExpressions(IEnumerable<Expression> expressions)
        {
            var count = expressions.Count();
            if (count == 1) return expressions.FirstOrDefault();
            else if(count >= 2)
            {
        

                var expressionGroups = expressions.GroupBy(x => x.Type);

                if( expressionGroups.Count() != 1 ) throw new Exception("All expressions must be of the same type");

                var elementType = ReduceExpressionGeneration.GetTypeOfEnumerable( expressionGroups.First().Key );

                var unionInfos = typeof(Enumerable).GetMethods().Where(x => x.Name == "Union");
                var unionInfo = unionInfos.FirstOrDefault();
                var mi = unionInfo.MakeGenericMethod(elementType);

                var first = expressions.FirstOrDefault();
                var result = UnionEnumerableExpressions(expressions.Skip(1));

                return Expression.Call
                (
                    method: mi,
                    arg0: first,
                    arg1: result
                );
            }
            else
            {
                return null;
            }
        }

        private static object UnionEnumerables(IEnumerable<object> collections)
        {
            var count = collections.Count();
            if (count == 0) return null;
            if (count == 1) return collections.First();

            var expressionGroups = collections.GroupBy(x => x.GetType());

            if (expressionGroups.Count() != 1) throw new Exception("All collections must be of the same type");

            var elementType = ReduceExpressionGeneration.GetTypeOfEnumerable(expressionGroups.First().Key);

            var last = collections.First();

            var unionInfos = typeof(Enumerable).GetMethods().Where(x => x.Name == "Union");
            var unionInfo = unionInfos.FirstOrDefault();
            var mi = unionInfo.MakeGenericMethod(elementType);

            foreach(var c in collections.Skip(1))
            {
                last = mi.Invoke(null, new object[] { last, c });                
            }

            return last;
        }

        private static object ToArrayCollection(object collection, Type elementType)
        {
            var genericToArrayInfos = typeof(Enumerable).GetMethods().Where(x => x.Name == "ToArray" && x.IsGenericMethod && x.GetParameters().Length == 1);
            var genericToArrayInfo = genericToArrayInfos.FirstOrDefault();
            var toArrayInfo = genericToArrayInfo.MakeGenericMethod(elementType);

            var res = toArrayInfo.Invoke(null, new object[] { collection });

            return res;
        }

        static void MakeCtor(IEnumerable<FieldInfo> fields, ILGenerator g)
        {

            foreach (FieldInfo m in fields)
            {
                if( m.FieldType.IsGenericType && m.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var elementType = m.FieldType.GetGenericArguments().FirstOrDefault();

                    var listctor = typeof(List<>).MakeGenericType(elementType).GetConstructor(new Type[0]);
                    g.Emit(OpCodes.Ldarg_0);
                    g.Emit(OpCodes.Call, listctor);
                    g.Emit(OpCodes.Ldarg_0);
                    g.Emit(OpCodes.Stfld, m);
                }
            }

            g.Emit(OpCodes.Ret);
        }

        static void Main(string[] args)
        {
            AssemblyBuilder _assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("MyBuilder"),AssemblyBuilderAccess.Run);
            ModuleBuilder _moduleBuilder = _assemblyBuilder.DefineDynamicModule("MyModule");
            var typeBuilder = _moduleBuilder.DefineType("lulz43", TypeAttributes.Class | TypeAttributes.Public);
            var fieldBuilder = typeBuilder.DefineField("zomglist", typeof(List<long>), FieldAttributes.Public);
            var cctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard | CallingConventions.HasThis, new Type[0]);
            var ilGenerator = cctor.GetILGenerator();
            MakeCtor(new FieldBuilder[] { fieldBuilder }, ilGenerator);

            var type32 = typeBuilder.CreateType();


            //var lolzfda = Activator.CreateInstance(type32);

            var param = Expression.Parameter(typeof(Mention), "x");
            var tags = Expression.MakeMemberAccess(param, typeof(Mention).GetField("Tags"));
            var ds = Expression.MakeMemberAccess(param, typeof(Mention).GetField("Datasources"));

            var a = new List<int> { 1, 2 };
            var b = new List<int> { 3, 4 };
            var c = new List<int> { 5, 6 };

            var joinedlistsexpressions = UnionEnumerableExpressions(new Expression[] { tags, ds, tags, ds });

            var joinedlists = ToArrayCollection( UnionEnumerables(new object[] { a, b, c }), typeof(int) );

            //var lol = Convert.ChangeType("5", typeof(string));

            var listlol = DataHelper.GetMentions(10000);

            //var sqlexpr = new EvoSQLExpression("SELECT Type, Color, Count() FROM Cats GROUP BY Color, Fur");
            //Console.WriteLine(sqlexpr.Tree.ToString());

            

            var tzs = TimeZoneInfo.GetSystemTimeZones();

            var query22 = "SELECT Type FROM (SELECT Type GROUP BY Type)";
            var query23 = "SELECT Sentiment FROM (SELECT Sentiment, Id FROM Mentions)";

            var query24 = @"
SELECT SUM({fn CONVERT(1, SQL_BIGINT)}) AS ""sum_Number_of_Records_qk""
,
{
    fn TIMESTAMPADD
    (
        SQL_TSI_MONTH, 
        CAST
        (
            {fn TRUNCATE((3 * (CAST({fn TRUNCATE({fn QUARTER(""Mentions"".""Date"")},0)} AS INTEGER) - 1)),0)} AS INTEGER),
            {fn TIMESTAMPADD(SQL_TSI_DAY,CAST({fn TRUNCATE(-({fn DAYOFYEAR(""Mentions"".""Date"")} - 1),0)} AS INTEGER), CAST(""Mentions"".""Date"" AS DATE))}
        )
} AS ""tqr_Date_qk""
FROM ""Bermuda"".""Mentions"" ""Mentions""
GROUP BY ""tqr_Date_qk""";



            var query24mini = @" SELECT 
SUM({fn CONVERT(1, SQL_BIGINT)}) AS ""sum_Number_of_Records_qk"", 
{ fn TIMESTAMPADD( SQL_TSI_MONTH,  CAST({fn TRUNCATE(3,3)} as sql_bigint), Date ) } AS ""tqr_Date_qk""
FROM ""Bermuda"".""Mentions"" ""Mentions""
GROUP BY ""tqr_Date_qk""";

            var reduceexpr1 = EvoQLBuilder.GetReduceExpression("SELECT Average(NulNum), * GROUP BY IsDisabled, Interval(OccurredOn, \"Year\", 0)", typeof(MentionTest2), true);
            var reduceexpr11 = EvoQLBuilder.GetReduceExpression("SELECT Count() WHERE IsDisabled NulNum>5 Tags:5", typeof(MentionTest2), true);

            var sellol52 = EvoQLBuilder.GetReduceExpression("SELECT Count() as Value, * GROUP BY TOP 5 Tags as Id VIA Count(), Interval(Date,Day,-5) as Id2", typeof(Mention), true);

            //var whereexpr2 = EvoQLBuilder.GetWhereExpression("GET Activity WHERE Whateverz:50 DONCAER:OK jkdsadasjd DATE>\"2012/06/04 16:19:58\"", typeof(Mention));

            var sellol3 = EvoQLBuilder.GetReduceExpression(query24, typeof(Mention), true);
            var sellol4 = EvoQLBuilder.GetMergeExpression(query24, typeof(Mention), null);

            //var sellol5 = EvoQLBuilder.GetReduceExpression("SELECT SUM({fn CONVERT(CONVERT(Sentiment, SQL_BIGINT), SQL_BIGINT)}) \"sum_Number_of_Records_qk\" FROM Nulz", typeof(Mention), true);
            var sellol7 = EvoQLBuilder.GetReduceExpression("SELECT -Sentiment+Influence as Ten", typeof(Mention), true);
            var sellol6 = EvoQLBuilder.GetReduceExpression("SELECT Sentiment+Influence as Ten, -Interval(Date, Day,-5) as DLOL , -Sentiment as neg, cat cat, Sentiment as S FROM Lulzcol Lulzforsur", typeof(Mention), true);
            var sellol = EvoQLBuilder.GetReduceExpression("SELECT \"Mentions\".\"Name\", Sentiment, {fn DatePart(Date,Hour,-5)} as Hour FROM Whatever as Lol", typeof(Mention), true);
            var kdsadsa = EvoQLBuilder.GetCollections(null, "SELECT Sentiment FROM Mentions metn", null, null);
            //var whereexpr = EvoQLBuilder.GetWhereExpression("Themes:lol NOT:(lol or hi or bye) (Tag:((@52 OR @75) AND NOT:(@53)) ) Tags:5 DATE:-13.551235..-12.551235 (Sentiment:(-100..100 OR -100..100) ) ", typeof(Mention));
            var mergeexpr = EvoQLBuilder.GetMergeExpression("SELECT Average(Sentiment) as C GROUP BY Interval(Date, Day, -5) as IntervalTheKilla FROM sigh", typeof(Mention), null);


            var zomg = "SELECT 5+5 as lol FROM Mentions";
            var red = EvoQLBuilder.GetReduceExpression(zomg, typeof(Mention), true);
            var red2 = EvoQLBuilder.GetMergeExpression(zomg, typeof(Mention), null);
            

            var cols = EvoQLBuilder.GetCollections("GET Mention DATE:\"2012/04/09 04:00:00\"..\"2012/04/10 04:00:00\" ( ( (Tag:@2 )))", "SELECT Sum(Followers) AS Count, * FROM Catz", null, null);
            //var reduceexpr = EvoQLBuilder.GetReduceExpression("SELECT Count(*) as Value, Average(Sentiment) as Retard GROUP BY TOP 5 Type VIA Count(), OccurredOn as X INTERVAL Day", typeof(MentionTest));

            //var reducestring = "SELECT Count() AS Value GROUP BY TOP 10 Tags VIA Count() AS Id, Datepart(OccurredOn, QuarterHour, -5) AS Id2";
            var reducestring = "SELECT Sentiment FROM (SELECT Sentiment, Id FROM Mentions)";
            var reduceexpr = EvoQLBuilder.GetReduceExpression(reducestring, typeof(MentionTest));
            
            
            //var mergeexpr = EvoQLBuilder.GetMergeExpression("SELECT Average(Sentiment), Count() as Lulz GROUP BY Tags, OccurredOn INTERVAL Day WHERE Name:no", typeof(Mention), null);
            
            var pagingexpr = EvoQLBuilder.GetPagingExpression("ORDERED BY Date LIMIT 5,25", typeof(Mention)); 
            //var reduceexpr = EvoQLBuilder.GetReduceExpression("CHART Count BY TOP 11 Tags VIA Sentiment OVER Date INTERVAL Day WHERE cats TAG:@1");
            //var reduceexpr = ReduceExpressionGeneration.MakeExpression
            //(
            //    null,
            //    "Count",
            //    new ReduceDimension[]
            //    {
            //        new ReduceDimension{ GroupBy = "Type" },
            //        new ReduceDimension{ GroupBy = "OccurredOn", Interval = IntervalTypes.Day }
            //    }
            //);


            var mytype = LinqRuntimeTypeBuilder.GetDynamicType(new Dictionary<string, Type> { { "Count", typeof(long) }, { "Value", typeof(double)}, {"Zomg", typeof(string)} });
            var ctor = Activator.CreateInstance(mytype, false);
            var type = LinqRuntimeTypeBuilder.GetTypeKey(listlol.GetType().GetGenericArguments().FirstOrDefault());
            var json = LinqRuntimeTypeBuilder.SerializeObject(listlol.Take(5).ToList());
            var res = LinqRuntimeTypeBuilder.DeserializeJson(json, type, true);
            var type2 = LinqRuntimeTypeBuilder.GetTypeKey( ReduceExpressionGeneration.GetTypeOfEnumerable(res.GetType()) );

            var type_consistent = type == type2;

            //var listlol = DataHelper.GetPremadeMentions();

            var dsadsadas = listlol.GroupBy(x => x).Select(g => g.FirstOrDefault());

            //Console.WriteLine(reduceexpr);
            Console.WriteLine(mergeexpr);

            //int serverCount = 20;
            //int bucketCount = 131;
            //ServerId == BucketId % ServerCount
            //var parititions = Enumerable.Range(0, bucketCount).Split(serverCount);
            //foreach (var s in parititions){ foreach (var b in s) Console.Write(b + "\t"); Console.WriteLine();}
            //Console.WriteLine();
            //parititions = Enumerable.Range(0, bucketCount).Split(serverCount + 3);
            //foreach (var s in parititions) { foreach (var b in s) Console.Write(b + "\t"); Console.WriteLine(); }

            //########################################################################
            //## TEST REDUCE EXPRESSION
            //########################################################################


            //var poitsad = listlol.AsParallel().GroupBy(m => m.Type).Select(g => new EnumMetadata<IGrouping<string, MentionTest>>() { Enum = g }).Select(gmd => new { Type = gmd.Enum.Key, Type_Hash = (long)(gmd.Enum.Key.GetHashCode()), _Count = gmd.Enum.LongCount() }).ToArray();

            var compileMethod = reduceexpr.GetType().GetMethods().FirstOrDefault(x => x.Name == "Compile" && x.GetParameters().Length == 0);
            var mapreduceFunc = compileMethod.Invoke(reduceexpr, new object[0]);
            var mapReduceFuncInvoke = mapreduceFunc.GetType().GetMethod("Invoke");
            var pointenum = mapReduceFuncInvoke.Invoke(mapreduceFunc, new object[] { listlol });

            var genericToArrayInfos = typeof(Enumerable).GetMethods().Where(x => x.Name == "ToArray" && x.IsGenericMethod && x.GetParameters().Length == 1);
            var genericToArrayInfo = genericToArrayInfos.FirstOrDefault();
            var enumType = pointenum.GetType();
            var pointType = enumType.GetGenericArguments().Last();
            var toArrayInfo = genericToArrayInfo.MakeGenericMethod(pointType);


            Stopwatch sw2 = new Stopwatch();
            sw2.Start();

            var data = toArrayInfo.Invoke(null, new object[] { pointenum });

            sw2.Stop();

            var jsonz = new JavaScriptSerializer().Serialize(pointenum); 

            Console.WriteLine(sw2.Elapsed);


            //var filter = EvoQLBuilder.GetFilterLambda("(b and a) or 23");
            //var filtered = listlol.Where(filter.Compile());
            //var result = filtered.ToArray();
            
            Console.ReadKey();

            //########################################################################
            //## BENCHMARK
            //########################################################################
            #region playing with expressions
            //var expr = EvoQLBuilder.GetReduceExpression("CHART Count BY Type OVER Date WHERE cat");

            /*
            var validTags = new long[] { 1, 2 };

            Expression<Func<IEnumerable<Mention>, IEnumerable<Datapoint>>> reduce = collection =>
                from mention in collection
                from tag in mention.Tags
                from datasource in mention.Datasources
                group mention by new MentionGroup { Id = tag, Id2 = datasource } into g
                select new Datapoint { Id = g.Key.Id, Id2 = g.Key.Id2, Timestamp = g.Key.Timestamp };

            Expression<Func<IEnumerable<Mention>, IEnumerable<Tag>, IEnumerable<Datapoint>>> reduce2 = (mentions, tags) =>
                from mention in mentions
                join tag in tags on mention.Id equals tag.MentionId
                group mention by new MentionGroup { Timestamp = mention.OccurredOnTicks, Id = tag.TagId } into g
                select new Datapoint { Id = g.Key.Id, Timestamp = g.Key.Timestamp };

            var dayTicks = TimeSpan.FromDays(1).Ticks;

            Expression<Func<IEnumerable<Mention>, IEnumerable<Datapoint>>> reduce34 = mentions =>
             from mention in mentions
             group mention by mention.OccurredOnTicks - mention.OccurredOnTicks % dayTicks into g
             select new Datapoint { Timestamp = g.Key };

            Expression<Func<IEnumerable<Mention>, IEnumerable<Datapoint>>> reduce33 = mentions =>
             from mention in mentions
             group mention by mention.OccurredOn.Date.Ticks into g
             select new Datapoint { Timestamp = g.Key };

            
            
            Expression<Func<IEnumerable<Mention>, IEnumerable<Datapoint>>> reduce3 = mentions =>
               from mention in mentions
               from tag in mention.Tags
               where validTags.Contains(tag)
               group mention by new MentionGroup { Timestamp = mention.OccurredOnTicks, Id = tag } into g
               select new Datapoint { Id = g.Key.Id, Timestamp = g.Key.Timestamp };


            Expression<Func<IEnumerable<Mention>, IEnumerable<Datapoint>>> mapreduce3 = collection =>
                collection
                .SelectMany(m => m.Tags, (m, t) => new MentionMetadata2 { Mention = m, Id = t })
                .SelectMany(md => md.Mention.Datasources, (md, ds) => new MentionMetadata2 { Child = md, Id = ds })
                .GroupBy(md => new MentionGroup { Timestamp = md.Child.Mention.OccurredOnTicks, Id = md.Child.Id, Id2 = md.Id })
                .Select(x => new Datapoint { Timestamp = x.Key.Timestamp, Id = x.Key.Id, Id2 = x.Key.Id2 });

            Expression<Func<IEnumerable<Mention>, IEnumerable<Datapoint>>> mapreduce4 = collection =>
                collection
                .SelectMany(m => m.Tags, (m, t) => new MentionMetadata2 { Mention = m, Id = t })
                .GroupBy(mt => mt.Id)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .SelectMany(x => x)
                .GroupBy(md => new MentionGroup { Timestamp = md.Child.Mention.OccurredOnTicks, Id = md.Child.Id, Id2 = md.Id })
                .Select(x => new Datapoint { Timestamp = x.Key.Timestamp, Id = x.Key.Id, Id2 = x.Key.Id2 });



            Expression<Func<IEnumerable<Mention>, IEnumerable<Datapoint>>> mapreduce5 = collection =>
                collection
                .SelectMany(m => m.Tags, (m, t) => new MentionMetadata { Item = m, Id = t })
                .GroupBy(md => md.Id)
                .Select(g => new { Group = g, Value = g.Count() })
                .OrderByDescending(md => md.Value)
                .Take(5)
                .Select(x => new Datapoint());

            Expression<Func<IEnumerable<Mention>, IEnumerable<Datapoint>>> mapreduce9 = collection =>
                collection
                .AsParallel()
                .SelectMany(m => m.Themes, (m, t) => new MentionMetadata { Item = m, Id = t })

                .GroupBy(md => md.Id, md => md.Item)
                .Select(g => new GroupMetadata { Group = g, Value = g.Count() })
                .OrderByDescending(g => g.Value)
                .Take(5)

                .SelectMany
                (
                    gmd =>

                    gmd.Group.GroupBy(md => md.OccurredOnTicks)
                    .Select(g => new GroupMetadata { Group = g })
                    .Select(gmd2 => new Datapoint { Id = gmd.Group.Key, Id2 = gmd2.Group.Key, CountValue = gmd.Value })
                );

            Expression<Func<IEnumerable<Mention>, IEnumerable<Datapoint>>> mapreduce6 = collection =>

                collection
                .AsParallel()
                .SelectMany(m => m.Themes, (m, t) => new MentionMetadata { Item = m, Id = t })
                .GroupBy(md => md.Id, md => md.Item)
                .Select(g => new GroupMetadata { Group = g, Value = g.Count() })
                .OrderByDescending(g => g.Value)
                .Take(5)

                .SelectMany
                (
                    gmd =>

                    gmd.Group
                    .SelectMany(m => m.Tags, (m, t) => new MentionMetadata { Item = m, Id = t })
                    .GroupBy(md => md.Id, md => md.Item)
                    .Select(g => new GroupMetadata { Group = g, Value = gmd.Value })
                    .OrderByDescending(g => g.Value)
                    .Take(2)

                    .Select(gmd2 => new Datapoint { Id = gmd.Group.Key, Id2 = gmd2.Group.Key, CountValue = gmd2.Value })

                    
                );
            */
            //ExpressionSerializer serializer = new ExpressionSerializer();
            //var xml = serializer.Serialize(mapreduce6);

            //Expression<Func<IEnumerable<Mention>, IEnumerable<Datapoint>>> dsajkdsa = collection => collection.SelectMany(m => m.Ngrams, (m, t) => new MentionMetadata() { Mention = m, Id = t }).GroupBy(md => new MentionGroup() { Timestamp = md.Mention.OccurredOnTicks, Id2 = md.Id }).Select(x => new Datapoint() { Timestamp = x.Key.Timestamp, Id2 = x.Key.Id2 });

            /*
            Expression<Func<IEnumerable<Mention>, IEnumerable<Datapoint>>> mapreduce11 = collection =>
              collection
              .SelectMany(m => m.Themes, (m, t) => new MentionMetadata { Item = m, Id = t })
              .GroupBy(md => new MentionGroup { Timestamp = md.Item.OccurredOnTicks, Id2 = md.Id })
              .Select(x => new Datapoint { Timestamp = x.Key.Timestamp, Id2 = x.Key.Id2 });*/
            #endregion

            var exprs = new Expression<Func<IEnumerable<Mention>, IEnumerable<Datapoint>>>[] {  };
            var sw = new Stopwatch();

            foreach (var expr in exprs)
            {
                Console.WriteLine(expr);
                var func = expr.Compile();

                sw.Restart();

                for (int i = 0; i < 100; i++)
                {
                   //var dsadqw = func(listlol).ToArray();
                }

                sw.Stop();
                Console.WriteLine(sw.Elapsed);
            }

            

            
            Console.ReadKey();

            /*
            Console.WriteLine("WAITING");


            //http://localhost:13866/Connection.External/ExternalService.svc

            var client = GetServiceClient();
            

            Random rng = new Random();

            while (true)
            {
                char randomChar = (char)(rng.Next(0, 10) + '0');

                //ParameterExpression param = Expression.Parameter(typeof(ThriftMention));

                object[] parameters = new object[] { randomChar };

                Expression<Func<ThriftMention, object[], bool>> query = (x, p) => x.Description.Contains((char)p[0]);

                Expression<Func<IEnumerable<ThriftMention>, IEnumerable<ThriftDatapoint>>> mapreduce = x => x.SelectMany(y => y.Tags).GroupBy(y => y).Select(y => new ThriftDatapoint { Count = y.Count(), Value = y.Count(), EntityId = y.Key });

                Expression<Func<IEnumerable<ThriftDatapoint>, double>> merge = x => x.Sum(y => y.Value);

                var domain = "evoapp";
                var minDate = DateTime.Today.AddDays(-600);
                var maxDate = DateTime.Today.AddDays(1);

                Stopwatch sw = new Stopwatch();
                sw.Start();
                var datapoints = client.GetDatapointList(domain, query, mapreduce, merge, minDate, maxDate, parameters);
                sw.Stop();

                Console.WriteLine("QUERY: " + randomChar);
                Console.WriteLine(sw.Elapsed.ToString());

                Console.WriteLine(string.Join(" ", datapoints.Select(x => "tag" + x.EntityId + ": " + x.Value + " (" + x.Count + ")")));
                Console.WriteLine(string.Empty);

                Thread.Sleep(1000);
            }
             */
        }

     

    //    static ExternalServiceClient cachedClient = null;

    //    public static ExternalServiceClient GetServiceClient()
    //    {
    //        ExternalServiceClient client = null;

    //        if (cachedClient != null)
    //        {
    //            client = cachedClient;
    //        }
    //        else
    //        {
    //            var url = "net.tcp://127.255.0.5:13866/ExternalService.svc";
    //            var binding = new NetTcpBinding(SecurityMode.None);
    //            var endpoint = new EndpointAddress(new Uri(url));
    //            binding.ReaderQuotas.MaxBytesPerRead *= 10;
    //            binding.ReaderQuotas.MaxStringContentLength *= 10;
    //            client = new ExternalServiceClient(binding, endpoint);

    //            cachedClient = client;
    //        }

    //        return client;
    //    }



        void lol()
        {
            IEnumerable<Datapoint> c = null;

            var groups = c.GroupBy(x => new { x.Id });

            var result = groups.OrderByDescending(x => x.Sum(y => y.Value)).SelectMany(x => x); 

        }

    }


    public class MentionTest2
    {
        public long Id;
        public string Name;
        public string Description;
        public string Type;
        public List<long> Tags;
        public List<long> Datasources;
        public List<string> Ngrams;
        public List<long> Themes;
        public double Sentiment;
        public long Influence;
        public bool IsDisabled;

        public long OccurredOnTicks;
        public long CreatedOnTicks;
        public long UpdatedOnTicks;

        public DateTime OccurredOn;
        public DateTime CreatedOn;
        public DateTime UpdatedOn;
        public string Guid;

        public string Author;
        public long Followers;
        public long Klout;
        public long Comments;

        public double? NulNum;
        public bool IsLol;

        public int Year;
    }
}
