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

			StackPanel checkBoxControlsContainer = new();
			StackPanel textBoxControlsContainer = new()
			{
				Margin = new Thickness(0, 5, 0, 0)
			};

			foreach (SettingsProperty setting in Properties.Settings.Default.Properties)
			{
				dynamic settingControl;
				dynamic value = Properties.Settings.Default[setting.Name];

				switch (setting.PropertyType.Name)
				{
					case "Boolean":
						settingControl = new CheckBox()
						{
							Content = setting.Name.Replace('_', ' '),
							Style = (Style)Application.Current.Resources["CheckBoxStyle1"],
							IsChecked = value
						};

						checkBoxControlsContainer.Children.Add(settingControl);
						break;
					case "String":
						StackPanel SettingContainer = new()
						{
							Margin = new Thickness(0, 0, 0, 10)
						};

						Label SettingLabel = new()
						{
							Content = setting.Name.Replace('_', ' '),
							FontSize = 15
						};

						settingControl = new TextBox()
						{
							Text = value,
							Style = (Style)Application.Current.Resources["TextBoxStyle1"],
							HorizontalAlignment = HorizontalAlignment.Left
						};

						SettingContainer.Children.Add(SettingLabel);
						SettingContainer.Children.Add(settingControl);
						textBoxControlsContainer.Children.Add(SettingContainer);
						break;
					default:
						throw new Exception($"Unrecognised type {setting.PropertyType.Name}!");
				}

				settingControl.Name = setting.Name;
				this.Controls.Add(settingControl);
			}


			// Button to force update files now
			WrapPanel updateContainer = new();
			updateContainer.Margin = new Thickness(0, 5, 0, 5);

			Button updateNow = new()
			{
				Content = "Update Files Now",
				Style = (Style)Application.Current.Resources["EnabledButton"],
				FontSize = 15,
				Padding = new Thickness(20, 5, 20, 5),
			};

			Label updateStatus = new()
			{
				FontSize = 15,
				VerticalAlignment = VerticalAlignment.Center
			};

			updateNow.Click += (object sender, RoutedEventArgs e) =>
			{
				Updater.Update(true, true);
				((MainWindow)Application.Current.MainWindow).HUDPanels = JsonSerializer.Deserialize<HUDPanel[]>(File.ReadAllText("Resources\\Panels.json"));
				updateStatus.Content = $"Last updated on {DateTime.Now}";
			};

			updateContainer.Children.Add(updateNow);
			updateContainer.Children.Add(updateStatus);

			checkBoxControlsContainer.Children.Add(updateContainer);

			SettingsContainer.Children.Add(checkBoxControlsContainer);
			SettingsContainer.Children.Add(textBoxControlsContainer);
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void ApplyButton_Click(object sender, RoutedEventArgs e)
		{
			foreach (dynamic settingsControl in this.Controls)
			{
				Properties.Settings.Default[settingsControl.Name] = settingsControl.GetType() == typeof(CheckBox) ? settingsControl.IsChecked : settingsControl.Text;
			}

			Properties.Settings.Default.Save();
			Close();
		}

		private void CheckBox_Checked(object sender, RoutedEventArgs e)
		{

		}
	}
}
