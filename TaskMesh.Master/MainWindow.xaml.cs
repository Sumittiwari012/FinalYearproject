using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using TaskMesh.Master.Helpers;
using TaskMesh.Master.ViewModels;

namespace TaskMesh.Master
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;
        private List<string> _addedColumns = new List<string>();

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            // Subscribe after DataContext is set
            _viewModel.ScoreboardColumnsChanged += RefreshScoreboardColumns;

            _viewModel.Problems.CollectionChanged += (s, e) =>
            {
                App.Current.Dispatcher.Invoke(() => RefreshScoreboardColumns());
            };
            _viewModel.OnResultUpdated += () =>
        App.Current.Dispatcher.Invoke(() => ScoreboardGrid.Items.Refresh());
        }

        private void RefreshScoreboardColumns()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                foreach (var problem in _viewModel.Problems)
                {
                    if (_addedColumns.Contains(problem.ProblemName)) continue;
                    _addedColumns.Add(problem.ProblemName);

                    string problemName = problem.ProblemName;
                    var column = new DataGridTemplateColumn
                    {
                        Header = problemName,
                        Width = new DataGridLength(100)
                    };

                    var cellTemplate = new DataTemplate();
                    var factory = new FrameworkElementFactory(typeof(TextBlock));

                    var multiBinding = new MultiBinding
                    {
                        Converter = new ScoreboardConverter(),
                        ConverterParameter = problemName
                    };
                    multiBinding.Bindings.Add(new Binding("."));

                    factory.SetBinding(TextBlock.TextProperty, multiBinding);
                    factory.SetValue(TextBlock.HorizontalAlignmentProperty,
                        HorizontalAlignment.Center);
                    factory.SetValue(TextBlock.FontSizeProperty, 16.0);

                    cellTemplate.VisualTree = factory;
                    column.CellTemplate = cellTemplate;

                    ScoreboardGrid.Columns.Add(column);
                }

                ScoreboardGrid.Items.Refresh();
            });
        }

        private void ListBox_SelectionChanged(object sender,
            SelectionChangedEventArgs e)
        { }
    }
}