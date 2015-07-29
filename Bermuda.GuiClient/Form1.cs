using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.ServiceModel;
using System.Linq.Expressions;
using System.Diagnostics;
using System.Threading.Tasks;
using Bermuda.Entities;
using ExpressionSerialization;
using Bermuda.GuiClient.ExternalAccess;
using System.Web.Script.Serialization;
using Bermuda.Core;
using Bermuda.Catalog;
using Bermuda.Core.Connection.External;
using Bermuda.Interface.Connection.External;
using Bermuda.Interface;

namespace Bermuda.GuiClient
{
    public partial class Form1 : Form
    {
        //string FallbackEndpoint = "net.tcp://192.168.1.197:13866/ExternalService.svc";
        //string FallbackEndpoint = "net.tcp://127.255.0.0:13866/ExternalService.svc";
        //string FallbackEndpoint = "net.tcp://184.73.253.140:13866/ExternalService.svc";
        //string FallbackEndpoint = "net.tcp://Bermuda-Load-Balancer-229347414.us-east-1.elb.amazonaws.com:13866/ExternalService.svc";
        string FallbackEndpoint = "net.tcp://bermudadev1.cloudapp.net:13866/ExternalService.svc";
        

        public Form1()
        {
            InitializeComponent();

            Load+=new EventHandler(Form1_Load);
        }

        void Form1_Load(object sender, EventArgs e)
        {
            //foreach (var i in typeof(GroupByTypes).GetEnumValues())
            //{
            //    comboBox1.Items.Add(i);
            //    comboBox2.Items.Add(i);
            //}

            textBox8.Text = FallbackEndpoint;

            //comboBox1.SelectedIndex = (int)GroupByTypes.None;
            //comboBox2.SelectedIndex = (int)GroupByTypes.Date;

            //foreach (var i in typeof(SelectTypes).GetEnumValues())
            //{
            //    comboBox3.Items.Add(i);
            //}

            //comboBox3.SelectedIndex = (int)SelectTypes.Count;

            //string[] seriesArray = { "Cats", "Dogs" };
            //int[] pointsArray = { 1, 2 };

            ////SeriesCollection seriesCol = new SeriesCollection();

            //chart1.Series.Clear();

            //for (int i = 0; i < seriesArray.Length; i++)
            //{
            //    // Add series.
            //    Series series = this.chart1.Series.Add(seriesArray[i]);

            //    series.XValueType = ChartValueType.Date;

            //    // Add point.
            //    series.Points.Add( new DataPoint(DateTime.Today.ToOADate(), pointsArray[i]) );
            //}

            
        }
        //private List<ThriftDatapoint> GetTagData(ExternalServiceClient client)
        //{
        //    object[] parameters = new object[] { textBox1.Text };

        //    Expression<Func<ThriftMention, object[], bool>> query = (x, p) => x.Description.Contains((string)p[0]);

        //    Expression<Func<IEnumerable<ThriftMention>, IEnumerable<ThriftDatapoint>>> mapreduce = x => x.SelectMany(y => y.Tags).GroupBy(y => y).Select(y => new ThriftDatapoint { Count = y.Count(), Value = y.Count(), EntityId = y.Key });

        //    Expression<Func<IEnumerable<ThriftDatapoint>, double>> merge = x => x.Sum(y => y.Value);

        //    var domain = "SmileySnuggle";
        //    var minDate = dateTimePicker1.Value;
        //    var maxDate = dateTimePicker2.Value;
        //    var datapoints = client.GetDatapointList(domain, query, mapreduce, merge, minDate, maxDate, parameters);

        //    var groups = datapoints.GroupBy(x => x.EntityId);

        //    chart1.Series.Clear();
        //    Series series = this.chart1.Series.Add("tags");
        //    series.XValueType = ChartValueType.String;

        //    foreach (var d in datapoints)
        //    {
        //        series.Points.Add(new DataPoint(d.EntityId, d.Value) { Label = "tag" + d.EntityId, });
        //    }

        //    return datapoints;
        //}

        //SelectTypes SelectedSelect;
        //GroupByTypes SelectedGroupBy;
        //GroupByTypes SelectedGroupBy2;
        private string Endpoint;

        private void GetData(ExternalServiceClient client)
        {
            InvokeOnFormThread(() =>
            {
                this.chart2.Series.Clear();
                var timeseries = this.chart2.Series.Add("timeseries");
                timeseries.XValueType = ChartValueType.Date;

                textBox2.Text = "Working...";

                label4.Text = "Working...";
            });

            string query = textBox1.Text;

            //object[] parameters = new object[] { textBox1.Text };

            //Expression<Func<Mention, object[], bool>> query = (x,p) => true;

            //if (!string.IsNullOrWhiteSpace(textBox1.Text))
            //{
            //    query = (x, p) => x.Name.Contains((string)p[0]) || x.Description.Contains((string)p[0]);
            //}

            //var query2 = EvoQLBuilder.GetLambda(textBox1.Text);

            //Expression<Func<IEnumerable<ThriftMention>, IEnumerable<ThriftDatapoint>>> mapreduce = mentions =>
            //    from mention in mentions
            //    from tag in mention.Tags
            //    group mention by new { mention.OccurredOn.Year, mention.OccurredOn.Month, mention.OccurredOn.Day, TagId = tag } into g
            //    select new ThriftDatapoint { Count = g.Count(), EntityId = g.Key.TagId, Value = g.Average(x => x.Sentiment), Timestamp = new DateTime(g.Key.Year, g.Key.Month, g.Key.Day).Ticks };

            //Expression<Func<IEnumerable<Mention>, IEnumerable<Datapoint>>> mapreduce1 = collection =>
            //    from mention in collection
            //    from tag in mention.Tags
            //    from datasource in mention.Datasources
            //    group mention by new MentionGroup{  Timestamp = mention.OccurredOnTicks, Id = tag, Id2 = datasource } into g
            //    select new Datapoint { EntityId = g.Key.Id, EntityId2 = g.Key.Id2, Timestamp = g.Key.Timestamp };

            //Expression<Func<IEnumerable<Mention>, IEnumerable<Datapoint>>> mapreduce = collection =>
            //    collection.SelectMany(m => m.Tags, (m, t) => new MentionMetadata { Mention = m, Id = t }).SelectMany(x => x.Mention.Datasources, (md, ds) => new MentionMetadata2 { Child = md, Id = ds }).GroupBy(md => new MentionGroup { Timestamp = md.Child.Mention.OccurredOnTicks, Id = md.Child.Id, Id2 = md.Id }).Select(x => new Datapoint { Timestamp = x.Key.Timestamp, EntityId = x.Key.Id, EntityId2 = x.Key.Id2 });

            //Expression<Func<IEnumerable<Mention>, IEnumerable<Datapoint>>> mapreduce = x => x.GroupBy(y => y.OccurredOnDayTicks).Select(g => new Datapoint { Count = g.Count(), Timestamp = g.Key });
            //Expression<Func<IEnumerable<Mention>, IEnumerable<Datapoint>>> mapreduce = x => x.GroupBy(y => new MentionGroup{ Timestamp = y.OccurredOnTicks - (y.OccurredOnTicks % 864000000000) }).Select(g => new Datapoint { Count = g.Count(), Value = g.Average(y => y.Sentiment), Timestamp = g.Key.Timestamp });

            //Expression<Func<IEnumerable<Mention>, IEnumerable<Datapoint>>> mapreduce = null; // ReduceExpressionGeneration.MakeExpression(null, SelectedSelect, SelectedGroupBy, SelectedGroupBy2);
            
            //Expression<Func<IEnumerable<Datapoint>, double>> merge = x => x.Sum(y => y.Value);
            //if (SelectedSelect == SelectTypes.Count) merge = x => x.Sum(y => (double)y.Count);

            var mapreduce = textBox9.Text;

            //Expression<Func<IEnumerable<Mention>, IEnumerable<Mention>>> paging = c => c.OrderByDescending(x => x.OccurredOnTicks).Take(25);

            ExpressionSerializer serializer = new ExpressionSerializer();

            //InvokeOnFormThread(() =>
            //{

            //    //ExpressionSerializer serializer = new ExpressionSerializer();
            //    //textBox6.Text = serializer.Serialize(mapreduce).ToString();
            //});

            var domain = textBox3.Text;
            //var minDate = dateTimePicker1.Value;
            //var maxDate = dateTimePicker2.Value;

            string command = textBox7.Text;

            Stopwatch sw = new Stopwatch();
            sw.Start();
            var result = client.GetData(domain, "__ql__" + query, "__ql__" + mapreduce, "__default__", null, command);
            sw.Stop();

            result.Metadata.OperationTime = sw.Elapsed;

            InvokeOnFormThread(() =>
            {
                var area = chart2.ChartAreas.First();
                var oldseries = chart2.Series.ToArray();
                string separator = "\r\n################################################################################################################################\r\n";

                HardcodedBermudaDatapoint[] datapoints = new HardcodedBermudaDatapoint[0];
                string jsonError = null;

                if (result.Data != null)
                {
                    try
                    {
                        datapoints = new JavaScriptSerializer().Deserialize<HardcodedBermudaDatapoint[]>(result.Data);
                        label4.Text = "Retrieved " + datapoints.Count() + "\r\nin " + sw.Elapsed;
                    }
                    catch (Exception ex)
                    {
                        jsonError = ex.ToString() + separator;
                    }
                }
                else
                {
                    datapoints = new HardcodedBermudaDatapoint[0];
                }


                StringBuilder sb = new StringBuilder();

                if (result.Metadata != null) ConvertStatsToString(sb, result.Metadata);
                textBox2.Text = result.CacheKey + separator + jsonError + result.Error + separator + sb.ToString() + separator + result.Data;

                var groups = datapoints.GroupBy(x => new { x.Id, x.Text } );
                
                chart2.SuspendLayout();

                foreach (var s in chart2.Series.Skip(1).ToArray())
                {
                    chart2.Series.Remove(s);
                }

                int i = 0;
                foreach (var g in groups)
                {
                    string name = g.Key.Text ?? g.Key.Id.ToString();
                    var timeseries = i == 0 ? chart2.Series.FirstOrDefault() : chart2.Series.Add(name);
                    timeseries.Points.Clear();
                    timeseries.Name = name;

                    timeseries.XValueType = g.Any(p => new DateTime(p.Id2).Year > 2000) ? ChartValueType.Date : ChartValueType.Int64;

                    foreach (var d in g)
                    {
                        timeseries.Points.Add(timeseries.XValueType == ChartValueType.Date ? new DataPoint(new DateTime(d.Id2).ToOADate(), d.Value) : new DataPoint(d.Id2, d.Value));
                    }

                    i++;
                }
                
                chart2.ResumeLayout(true);
                
                
                //textBox2.Text = sb.ToString().Trim() + separator + string.Join("\r\n", datapoints.Datapoints.Select(x => x.Id.ToString().PadLeft(32) + x.Id2.ToString().PadLeft(32) + ((int)x.Value).ToString().PadLeft(16) + x.Count.ToString().PadLeft(16) ));
                
                //var sb2 = new StringBuilder();
                //ConvertStatsToString(sb2, mentions.Metadata);
                //textBox5.Text = sb2.ToString().Trim() + separator + string.Join(separator, mentions.Mentions.Select(x => DateTime.FromBinary( x.OccurredOnTicks ) + " :: " + x.Name + "\r\n" + x.Description));

                //label4.Text = "Received " + (datapoints.Datapoints.Count()) + " datapoints\r\nin " + sw.Elapsed;
            });

            //return datapoints.Datapoints;
        }

        void ConvertStatsToString(StringBuilder sb, BermudaNodeStatistic stats, int depth = 0)
        {
            var spacing = string.Empty.PadLeft(8 * depth); 
            var padding =  "\r\n" + spacing;

            sb.Append(padding);
            sb.Append("[@" + stats.NodeId + "]");
            sb.Append(stats.Notes);

            if (stats.Error != null)
            {
                sb.Append(padding);
                sb.Append("    ERROR:");
                sb.Append(stats.Error);
            }

            if (stats.ChildNodes == null)
            {
                sb.Append(padding);

                sb.Append("    ");
                sb.Append(stats.TotalItems);
                sb.Append("  ==[Filtered]==>  ");
                sb.Append(stats.FilteredItems);
                sb.Append("  ==[Reduced]==>  ");
                sb.Append(stats.ReducedItems);
            }
            
            sb.Append(padding);
            sb.Append("    External Operation Time:");
            sb.Append(stats.OperationTime);

            sb.Append(padding);
            sb.Append("    Internal Operation Time:");
            sb.Append(stats.LinqExecutionTime);

            if (stats.ChildNodes != null)
            {
                foreach (var child in stats.ChildNodes)
                {
                    ConvertStatsToString(sb, child, depth + 1);
                }
            }
        }

        private void InvokeOnFormThread(Action behavior)
        {
            if (IsHandleCreated && InvokeRequired)
            {
                Invoke(behavior);
            }
            else
            {
                behavior();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            PrepareForQuery();
            
            Task.Factory.StartNew( MakeClientAndGetData );
        }

        private void PrepareForQuery()
        {
            //SelectedGroupBy = (GroupByTypes)comboBox1.SelectedIndex;
            //SelectedGroupBy2 = (GroupByTypes)comboBox2.SelectedIndex;
            //SelectedSelect = (SelectTypes)comboBox3.SelectedIndex;
            Endpoint = string.IsNullOrWhiteSpace(textBox8.Text) ? FallbackEndpoint : textBox8.Text;
        }

        private void MakeClientAndGetData()
        {
            try
            {
                using (var client = ExternalServiceClient.GetClient(Endpoint))
                {
                    GetData(client);
                }
            }
            catch (Exception ex)
            {
                textBox2.Invoke( new Action(() => textBox2.Text = ex.ToString() ) );
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Task.Factory.StartNew( GetStatus );
        }

        private void GetStatus()
        {
            PrepareForQuery();
            try
            {
                var client = ExternalServiceClient.GetClient(Endpoint);
                client.Open();
                var result = client.Ping("status");
                client.Close();

                textBox4.Invoke( new Action(() => textBox4.Text = result ));
            }
            catch (Exception ex)
            {
                textBox4.Invoke(new Action(() => textBox4.Text = ex.ToString()));
            }
        }

        private void textBox1_Enter(object sender, EventArgs e)
        {
            PrepareForQuery();
            Task.Factory.StartNew(MakeClientAndGetData);

        }

        private void textBox1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyValue == '\r')
            {
                PrepareForQuery();
                Task.Factory.StartNew(MakeClientAndGetData);

            }
        }

        private void textBox8_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
