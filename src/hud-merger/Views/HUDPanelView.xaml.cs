using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HUDMerger.Views
{
	/// <summary>
	/// Interaction logic for SourceHUDPanelView.xaml
	/// </summary>
	public partial class HUDPanelView : UserControl
	{
		public static readonly DependencyProperty ToggleSelectedProperty = DependencyProperty.Register("ToggleSelected", typeof(ICommand), typeof(HUDPanelView), new PropertyMetadata(null));

		public ICommand ToggleSelected
		{
			get => (ICommand)GetValue(ToggleSelectedProperty);
			set => SetValue(ToggleSelectedProperty, value);
		}

		public HUDPanelView()
		{
			InitializeComponent();
		}

		private void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			ToggleSelected?.Execute(sender);
		}
	}
}
