using System;
using System.Collections.Generic;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace hud_merger
{
	/// <summary>
	/// Interaction logic for SettingsWindow.xaml
	/// </summary>
	public partial class SettingsWindow : Window
	{
		private List<Control> Controls = new();

		public SettingsWindow()
		{
			InitializeComponent();

			StackPanel CheckBoxControlsContainer = new();
			StackPanel TextBoxControlsContainer = new()
			{
				Margin = new Thickness(0, 5, 0, 0)
			};

			foreach (SettingsProperty Setting in Properties.Settings.Default.Properties)
			{
				dynamic SettingControl;
				dynamic Value = Properties.Settings.Default[Setting.Name];

				switch (Setting.PropertyType.Name)
				{
					case "Boolean":
						SettingControl = new CheckBox();
						SettingControl.Content = Setting.Name.Replace('_', ' ');
						SettingControl.IsChecked = Value;
						CheckBoxControlsContainer.Children.Add(SettingControl);
						break;
					case "String":
						StackPanel SettingContainer = new();
						Label SettingLabel = new()
						{
							Content = Setting.Name.Replace('_', ' ')
						};

						SettingControl = new TextBox();
						SettingControl.Text = Value;

						SettingContainer.Children.Add(SettingLabel);
						SettingContainer.Children.Add(SettingControl);
						TextBoxControlsContainer.Children.Add(SettingContainer);
						break;
					default:
						throw new Exception($"Unrecognised type {Setting.PropertyType.Name}!");
				}

				SettingControl.Name = Setting.Name;
				this.Controls.Add(SettingControl);
			}


			// Button to force update files now
			Button UpdateNow = new()
			{
				Content = "Update Files Now",
				FontSize = 12,
				Padding = new Thickness(5),
				BorderThickness = new Thickness(0),
				HorizontalAlignment = HorizontalAlignment.Left
			};
			UpdateNow.Click += (object sender, RoutedEventArgs e) =>
			{
				Updater.Update(true, true);
			};

			CheckBoxControlsContainer.Children.Add(UpdateNow);

			SettingsContainer.Children.Add(CheckBoxControlsContainer);
			SettingsContainer.Children.Add(TextBoxControlsContainer);

			ApplyButtonTextBlock.MouseEnter += (object sender, MouseEventArgs e) =>
			{
				ApplyButtonTextBlock.Background = (Brush)Application.Current.Resources["_BlueHover"];
			};
			ApplyButtonTextBlock.MouseLeave += (object sender, MouseEventArgs e) =>
			{
				ApplyButtonTextBlock.Background = (Brush)Application.Current.Resources["_Blue"];
			};
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void ApplyButton_Click(object sender, RoutedEventArgs e)
		{
			foreach (dynamic SettingsControl in this.Controls)
			{
				Properties.Settings.Default[SettingsControl.Name] = SettingsControl.GetType() == typeof(CheckBox) ? SettingsControl.IsChecked : SettingsControl.Text;
			}

			Properties.Settings.Default.Save();
			Close();
		}
	}
}
