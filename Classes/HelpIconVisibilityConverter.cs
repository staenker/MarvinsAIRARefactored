
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MarvinsAIRARefactored.Classes;

public sealed class HelpIconVisibilityConverter : IMultiValueConverter
{
	public object? Convert( object[] values, Type targetType, object? parameter, CultureInfo culture )
	{
		var isOver = values.Length > 0 && values[ 0 ] is bool b && b;
		var topic = values.Length > 1 ? values[ 1 ] as string : null;

		return ( isOver && !string.IsNullOrWhiteSpace( topic ) ) ? Visibility.Visible : Visibility.Collapsed;
	}

	public object[] ConvertBack( object value, Type[] targetTypes, object? parameter, CultureInfo culture ) => throw new NotSupportedException();
}
