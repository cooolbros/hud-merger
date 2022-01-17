using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HUDMerger.Views
{
	/// <summary>
	/// Interaction logic for BackupsWindowView.xaml
	/// </summary>
	public partial class BackupsWindowView : UserControl
	{
		public static readonly DependencyProperty HUDNameClickProperty = DependencyProperty.Register("HUDNameClick", typeof(ICommand), typeof(BackupsWindowView), new PropertyMetadata(null));

		public ICommand HUDNameClick
		{
			get => (ICommand)GetValue(HUDNameClickProperty);
			set => SetValue(HUDNameClickProperty, value);
		}

		public static readonly DependencyProperty FileNameClickProperty = DependencyProperty.Register("FileNameClick", typeof(ICommand), typeof(BackupsWindowView), new PropertyMetadata(null));

		public ICommand FileNameClick
		{
			get => (ICommand)GetValue(FileNameClickProperty);
			set => SetValue(FileNameClickProperty, value);
		}

		public static readonly DependencyProperty CreationTimeClickProperty = DependencyProperty.Register("CreationTimeClick", typeof(ICommand), typeof(BackupsWindowView), new PropertyMetadata(null));

		public ICommand CreationTimeClick
		{
			get => (ICommand)GetValue(CreationTimeClickProperty);
			set => SetValue(CreationTimeClickProperty, value);
		}

		public BackupsWindowView()
		{
			InitializeComponent();
		}

		private void HUDName_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			HUDNameClick?.Execute(sender);
		}

		private void FileName_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			FileNameClick?.Execute(sender);
		}

		private void CreationTime_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			CreationTimeClick?.Execute(sender);
		}
	}
}
