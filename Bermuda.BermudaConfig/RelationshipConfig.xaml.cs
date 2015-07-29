using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Bermuda.Interface;
using System.Collections.ObjectModel;

namespace Bermuda.BermudaConfig
{
    /// <summary>
    /// Interaction logic for RelationshipConfig.xaml
    /// </summary>
    public partial class RelationshipConfig : Window
    {
        #region Variables and Properties

        /// <summary>
        /// the relationship
        /// </summary>
        public IRelationshipMetadata Relationship { get; set; }

        /// <summary>
        /// the original relationship name
        /// </summary>
        public string RelationshipOriginal { get; set; }

        /// <summary>
        /// tables for relationship
        /// </summary>
        public ObservableCollection<string> Tables { get; set; }

        /// <summary>
        /// the columns of the parent table
        /// </summary>
        public ObservableCollection<string> ParentColumns { get; set; }

        /// <summary>
        /// the columns of the child table
        /// </summary>
        public ObservableCollection<string> ChildColumns { get; set; }

        /// <summary>
        /// the columns of the relationship table
        /// </summary>
        public ObservableCollection<string> RelationColumns { get; set; }

        /// <summary>
        /// the bool Values
        /// </summary>
        public ObservableCollection<string> BoolValues { get; set; }

        /// <summary>
        /// the distinct field
        /// </summary>
        public string DistinctRelationship
        {
            get
            {
                if (Relationship.DistinctRelationship)
                    return "True";
                else
                    return "False";
            }
            set
            {
                if (value == "True")
                    Relationship.DistinctRelationship = true;
                else
                    Relationship.DistinctRelationship = false;
            }
        }

        #endregion


        #region Constructor

        /// <summary>
        /// constructor with relationship
        /// </summary>
        /// <param name="relationship"></param>
        public RelationshipConfig(IRelationshipMetadata copy, string original)
        {
            //init relationship
            Relationship = copy;
            RelationshipOriginal = original;

            //init properties
            Tables = new ObservableCollection<string>();
            Relationship.CatalogMetadata.Tables.Values.ToList().ForEach(t => Tables.Add(t.TableName));
            BoolValues = BermudaConfigUtil.GetBoolValues();
            ParentColumns = new ObservableCollection<string>();
            ChildColumns = new ObservableCollection<string>();
            RelationColumns = new ObservableCollection<string>();

            //init gui
            InitializeComponent();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// handle verification and close
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            //check empty name
            if (string.IsNullOrWhiteSpace(Relationship.RelationshipName))
            {
                MessageBox.Show("You must enter a name.");
                return;
            }
            //check unique name
            if (Relationship.CatalogMetadata.Relationships.Values.Any(r => r.RelationshipName != RelationshipOriginal && r.RelationshipName == Relationship.RelationshipName))
            {
                MessageBox.Show("You must have a unique name");
                return;
            }
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// cancel edit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// selection has changed for parent
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbParentTable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PopulateColumns(ParentColumns, e);
        }

        /// <summary>
        /// selection has changed for child
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbChildTable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PopulateColumns(ChildColumns, e);
        }

        /// <summary>
        /// selection has changed for relationship
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbRelationTable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PopulateColumns(RelationColumns, e);
        }

        #endregion

        #region Methods

        /// <summary>
        /// populate the columns collection based on selection change
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="e"></param>
        private void PopulateColumns(ObservableCollection<string> columns, SelectionChangedEventArgs e)
        {
            //init the columns
            columns.Clear();

            //validate the selection
            if (e.AddedItems.Count == 0)
                return;

            //get the table
            string name = e.AddedItems[0].ToString();

            //get the metadata for table
            ITableMetadata table = Relationship.CatalogMetadata.Tables[name];
            table.ColumnsMetadata.Values.ToList().ForEach(c => columns.Add(c.FieldMapping));
        }

        #endregion
    }
}
