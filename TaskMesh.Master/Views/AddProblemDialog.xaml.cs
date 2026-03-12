using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TaskMesh.Master.Views
{
    /// <summary>
    /// Interaction logic for AddProblemDialog.xaml
    /// </summary>
    public partial class AddProblemDialog : Window
    {
        public string ProblemTitle { get; private set; }
        public string ProblemDescription { get; private set; }
        public List<string> InputTestCases { get; private set; }
        public List<string> ExpectedOutputs { get; private set; }
        public int TimeLimit { get; private set; }

        public AddProblemDialog()
        {
            InitializeComponent();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            ProblemTitle = TitleBox.Text;
            ProblemDescription = DescriptionBox.Text;
            InputTestCases = new List<string>(
                InputBox.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries));
            ExpectedOutputs = new List<string>(
                OutputBox.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries));
            TimeLimit = int.TryParse(TimeLimitBox.Text, out int t) ? t : 5;
            DialogResult = true;
            Close();
        }
    }
}
