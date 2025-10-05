
using System.Windows;
using System.Windows.Input;

namespace MarvinsAIRARefactored.Classes;

public static class HelpService
{
	public static string BaseUrl { get; set; } = "https://herboldracing.com/help/";

	public static readonly DependencyProperty HelpTopicProperty = DependencyProperty.RegisterAttached( "HelpTopic", typeof( string ), typeof( HelpService ), new FrameworkPropertyMetadata( null, FrameworkPropertyMetadataOptions.Inherits ) );

	public static void SetHelpTopic( DependencyObject element, string? value ) => element.SetValue( HelpTopicProperty, value );
	public static string? GetHelpTopic( DependencyObject element ) => (string?) element.GetValue( HelpTopicProperty );

	public static ICommand OpenHelpCommand { get; } = new RoutedUICommand( "OpenHelp", "OpenHelp", typeof( HelpService ) );

	public static void ExecuteOpenHelp( object? parameter )
	{
		var topic = parameter as string;

		if ( string.IsNullOrWhiteSpace( topic ) ) return;

		var url = topic!.EndsWith( ".html", StringComparison.OrdinalIgnoreCase ) ? $"{BaseUrl}{topic}" : $"{BaseUrl}{topic}.html";

		var helpWindow = new Windows.HelpWindow( url )
		{
			Owner = App.Instance!.MainWindow
		};

		helpWindow.Show();
	}
}
