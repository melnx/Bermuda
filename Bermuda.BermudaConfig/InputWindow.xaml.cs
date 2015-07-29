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
using System.Collections.ObjectModel;

namespace Bermuda.BermudaConfig
{
    /// <summary>
    /// Interaction logic for InputWindow.xaml
    /// </summary>
    public partial class InputWindow : Window
    {
        #region Variables and Properties

        /// <summary>
        /// the question and answers for input
        /// </summary>
        public ObservableCollection<Question> Questions { get; set; }

        
        #endregion

        #region Constructor

        /// <summary>
        /// input window default constructor
        /// </summary>
        /// <param name="title"></param>
        public InputWindow(string title)
        {
            Title = title;
            Questions = new ObservableCollection<Question>();
            InitializeComponent();
        }

        /// <summary>
        /// constructor with one question prompt
        /// </summary>
        /// <param name="title"></param>
        /// <param name="prompt"></param>
        public InputWindow(string title, string prompt)
        {
            Title = title;
            Questions = new ObservableCollection<Question>();
            Questions.Add(new Question(prompt));
            InitializeComponent();
        }

        /// <summary>
        /// constructor with one prompt and default answer
        /// </summary>
        /// <param name="title"></param>
        /// <param name="prompt"></param>
        /// <param name="answer"></param>
        public InputWindow(string title, string prompt, string answer)
        {
            Title = title;
            Questions = new ObservableCollection<Question>();
            Questions.Add(new Question(prompt, answer));
            InitializeComponent();
        }

        /// <summary>
        /// constructor with a list of questions
        /// </summary>
        /// <param name="title"></param>
        /// <param name="questions"></param>
        public InputWindow(string title, IEnumerable<Question> questions)
        {
            Title = title;
            Questions = new ObservableCollection<Question>();
            questions.ToList().ForEach(q => Questions.Add(q));
            InitializeComponent();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// check answers and close
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btOk_Click(object sender, RoutedEventArgs e)
        {
            //parse the answers
            foreach (var question in Questions)
            {
                if (string.IsNullOrWhiteSpace(question.Answer))
                {
                    MessageBox.Show(
                        string.Format("The question: \"{0}\" needs to be answered.", question.Prompt),
                        "Input Required",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }
            }
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// cancel input
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        #endregion

        #region Classes

        /// <summary>
        /// a question and answer for input
        /// </summary>
        public class Question
        {
            public string Prompt { get; set; }
            public string Answer { get; set; }
            public Question()
            {

            }
            public Question(string prompt)
            {
                Prompt = prompt;
            }
            public Question(string prompt, string answer)
            {
                Prompt = prompt;
                Answer = answer;
            }
        }

        #endregion
    }
}
