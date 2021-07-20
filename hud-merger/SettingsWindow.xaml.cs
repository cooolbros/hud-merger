using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text.Json;
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
						SettingControl = new CheckBox()
						{
							Content = Setting.Name.Replace('_', ' '),
							Style = (Style)Application.Current.Resources["CheckBoxStyle1"],
							IsChecked = Value
						};

						CheckBoxControlsContainer.Children.Add(SettingControl);
						break;
					case "String":
						StackPanel SettingContainer = new()
						{
							Margin = new Thickness(0, 0, 0, 10)
						};

						Label SettingLabel = new()
						{
							Content = Setting.Name.Replace('_', ' '),
							FontSize = 15
						};

						SettingControl = new TextBox()
						{
							Text = Value,
							Style = (Style)Application.Current.Resources["TextBoxStyle1"],
							HorizontalAlignment = HorizontalAlignment.Left
						};

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
			WrapPanel UpdateContainer = new();
			UpdateContainer.Margin = new Thickness(0, 5, 0, 5);

			Button UpdateNow = new()
			{
				Content = "Update Files Now",
				Style = (Style)Application.Current.Resources["EnabledButton"],
				FontSize = 15,
				Padding = new Thickness(20, 5, 20, 5),
			};

			Label UpdateStatus = new()
			{
				FontSize = 15,
				VerticalAlignment = VerticalAlignment.Center
			};

			UpdateNow.Click += (object sender, RoutedEventArgs e) =>
			{
				Updater.Update(true, true);
				((MainWindow)Application.Current.MainWindow).HUDPanels = JsonSerializer.Deserialize<HUDPanel[]>(File.ReadAllText("Resources\\Panels.json"));
				UpdateStatus.Content = $"Last updated on {DateTime.Now}";
			};

			UpdateContainer.Children.Add(UpdateNow);
			UpdateContainer.Children.Add(UpdateStatus);

			CheckBoxControlsContainer.Children.Add(UpdateContainer);

			SettingsContainer.Children.Add(CheckBoxControlsContainer);
			SettingsContainer.Children.Add(TextBoxControlsContainer);
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

		private void CheckBox_Checked(object sender, RoutedEventArgs e)
		{

		}
	}
}
