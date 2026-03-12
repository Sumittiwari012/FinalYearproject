using System;
using System.Globalization;
using System.Windows.Data;
using TaskMesh.Master.ViewModels;

namespace TaskMesh.Master.Helpers
{
    public class ScoreboardConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (values[0] is ScoreboardEntry entry && parameter is string problemName)
                return entry.GetResult(problemName);
            return "⏳";
        }

        public object[] ConvertBack(object value, Type[] targetTypes,
            object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}