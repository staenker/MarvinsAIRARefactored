
using System.Windows;
using System.Windows.Input;

using UserControl = System.Windows.Controls.UserControl;

namespace MarvinsAIRARefactored.Controls;

public partial class MairaTextBox : UserControl
{
	public MairaTextBox()
	{
		InitializeComponent();
	}

	#region User Control Events

	private void InnerTextBox_KeyDown( object sender, System.Windows.Input.KeyEventArgs e )
	{
		if ( e.Key == Key.Enter )
		{
			// Force binding update (in case UpdateSourceTrigger isn’t PropertyChanged)
			var textBox = (TextBox) sender;
			var binding = textBox.GetBindingExpression( TextBox.TextProperty );
			binding?.UpdateSource();

			// Remove focus from the TextBox
			textBox.MoveFocus( new TraversalRequest( FocusNavigationDirection.Next ) );

			e.Handled = true; // stop ding sound
		}
	}

	#endregion

	#region Dependency Properties

	public static readonly DependencyProperty LabelProperty = DependencyProperty.Register( nameof( Label ), typeof( string ), typeof( MairaTextBox ), new PropertyMetadata( string.Empty ) );

	public string Label
	{
		get => (string) GetValue( LabelProperty );
		set => SetValue( LabelProperty, value );
	}

	public static readonly DependencyProperty ValueProperty = DependencyProperty.Register( nameof( Value ), typeof( string ), typeof( MairaTextBox ), new PropertyMetadata( string.Empty ) );

	public string Value
	{
		get => (string) GetValue( ValueProperty );
		set => SetValue( ValueProperty, value );
	}

	public static readonly DependencyProperty IsNumericOnlyProperty = DependencyProperty.Register( nameof( IsNumericOnly ), typeof( bool), typeof( MairaTextBox ) );

	public bool IsNumericOnly
	{
		get => (bool) GetValue( ValueProperty );
		set => SetValue( ValueProperty, value );
	}

	#endregion

	#region Dependency Property Changed Events

	#endregion

	#region Logic

	#endregion
}
