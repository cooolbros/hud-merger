using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace hud_merger
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private HUD OriginHUD;
		private HUD TargetHUD;
		private bool MergeButtonEnabled = false;
		public static HUDPanel[] HUDPanels = JsonSerializer.Deserialize<HUDPanel[]>(File.ReadAllText("Resources\\Panels.json"));
		StackPanel OriginFilesList = new();
		StackPanel TargetFilesList = new();
		System.Windows.Forms.FolderBrowserDialog FolderBrowserDialog = new()
		{
			SelectedPath = $"{Properties.Settings.Default.Team_Fortress_2_Folder}\\tf\\custom\\"
		};

		public MainWindow()
		{
			InitializeComponent();

			Properties.Settings.Default.Upgrade();
			Properties.Settings.Default.Save();

			// Updater
			bool download = Properties.Settings.Default.Download_latest_HUD_file_definitions_file_on_start_up;
			bool extract = Properties.Settings.Default.Extract_required_TF2_HUD_files_on_startup;
			Task.WhenAll(Updater.Update(download, extract)).ContinueWith((Task task) =>
			{
				if (task.Exception != null)
				{
					foreach (Exception error in task.Exception.InnerExceptions)
					{
						System.Diagnostics.Debug.WriteLine($"({error.GetType().Name}): {error.Message}");
					}
				}
			});
		}

		private void MenuItem_LoadOriginHUD(object sender, RoutedEventArgs e)
		{
			NewOriginHUD_Click(sender, e);
		}

		private void MenuItem_LoadTargetHUD(object sender, RoutedEventArgs e)
		{
			NewTargetHUD_Click(sender, e);
		}

		private void MenuItem_Settings(object sender, RoutedEventArgs e)
		{
			SettingsWindow settingsWindow = new();
			settingsWindow.Owner = this;
			settingsWindow.Show();
		}

		private void MenuItem_Quit(object sender, RoutedEventArgs e)
		{
			Application.Current.Shutdown();
		}

		private void MenuItem_About(object sender, RoutedEventArgs e)
		{
			AboutWindow aboutWindow = new();
			aboutWindow.Owner = this;
			aboutWindow.Show();
		}

		private void ClearSelected()
		{
			OriginFilesList.Children.Clear();
			foreach (UIElement child in TargetFilesList.Children)
			{
				child.Visibility = Visibility.Collapsed;
			}
			foreach (HUDPanel panel in HUDPanels)
			{
				panel.Armed = false;
			}
		}

		private void UpdateFooterState()
		{
			if (OriginHUD != null && TargetHUD != null)
			{
				MergeButton.IsEnabled = true;
				this.MergeButtonEnabled = true;
			}
			else
			{
				MergeButton.IsEnabled = false;
				this.MergeButtonEnabled = false;
			}
		}

		private void NewOriginHUD_Click(object sender, RoutedEventArgs e)
		{
			System.Windows.Forms.DialogResult result = this.FolderBrowserDialog.ShowDialog();
			if (result == System.Windows.Forms.DialogResult.OK)
			{
				ClearSelected();
				NewOriginHUD(this.FolderBrowserDialog.SelectedPath);
			}
		}

		private void NewOriginHUD(string folderPath)
		{
			OriginHUD = new HUD(folderPath);

			OriginHUDStatusTitle.Content = OriginHUD.Name;
			OriginHUDStatusInfo.Content = OriginHUD.FolderPath;

			OriginHUDFilesContainer.Children.Clear();
			OriginHUDFilesContainer.ColumnDefinitions.Clear();
			OriginHUDFilesContainer.RowDefinitions.Clear();

			OriginHUDFilesContainer.ColumnDefinitions.Add(new ColumnDefinition());
			RowDefinition titleRowDefinition = new()
			{
				Height = GridLength.Auto
			};
			OriginHUDFilesContainer.RowDefinitions.Add(titleRowDefinition);
			OriginHUDFilesContainer.RowDefinitions.Add(new RowDefinition());

			Label titleLabel = new()
			{
				Content = "Available Files",
				FontSize = 18
			};
			OriginHUDFilesContainer.Children.Add(titleLabel);

			// Search Box
			TextBox searchBox = new()
			{
				Style = (Style)Application.Current.Resources["SearchBox"]
			};
			searchBox.TextChanged += (object sender, TextChangedEventArgs e) =>
			{
				string searchText = searchBox.Text.ToLower();
				foreach (Border PanelBorder in OriginFilesList.Children)
				{
					bool contains = ((Label)PanelBorder.Child).Content.ToString().ToLower().Contains(searchText);
					PanelBorder.Visibility = contains ? Visibility.Visible : Visibility.Collapsed;
				}
			};

			Grid.SetColumn(searchBox, 1);
			OriginHUDFilesContainer.Children.Add(searchBox);

			ScrollViewer scrollablePanel = new();
			Grid.SetRow(scrollablePanel, 1);

			OriginFilesList.Margin = new Thickness(3);

			bool hudIsValid = false;

			foreach (HUDPanel panel in HUDPanels)
			{
				bool panelExists = OriginHUD.TestPanel(panel);

				panel.OriginListItem = new Border()
				{
					Style = (Style)Application.Current.Resources["PanelBorder"],
					Child = new Label()
					{
						Content = panel.Name,
						Style = (Style)Application.Current.Resources["PanelLabel"],
						Visibility = panelExists ? Visibility.Visible : Visibility.Collapsed,
					}
				};

				panel.OriginListItem.MouseEnter += (object sender, MouseEventArgs e) =>
				{
					if (!panel.Armed)
					{
						panel.OriginListItem.Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#E6E6E6");
					}
				};

				panel.OriginListItem.MouseLeave += (object sender, MouseEventArgs e) =>
				{
					if (!panel.Armed)
					{
						panel.OriginListItem.Background = Brushes.Transparent;
					}
				};

				panel.OriginListItem.MouseLeftButtonUp += (object sender, MouseButtonEventArgs e) =>
				{
					if (!panel.Armed)
					{
						((Label)panel.OriginListItem.Child).Foreground = Brushes.White;
						panel.OriginListItem.Background = (Brush)Application.Current.Resources["_Blue"];

						if (TargetHUD != null)
						{
							panel.TargetListItem.Visibility = Visibility.Visible;
						}

						panel.Armed = true;
					}
					else
					{
						((Label)panel.OriginListItem.Child).Foreground = Brushes.Black;
						panel.OriginListItem.Background = Brushes.Transparent;

						if (TargetHUD != null)
						{
							panel.TargetListItem.Visibility = Visibility.Collapsed;
						}

						panel.Armed = false;
					}
				};

				OriginFilesList.Children.Add(panel.OriginListItem);

				if (panelExists)
				{
					hudIsValid = true;
				}
			}

			if (!hudIsValid)
			{
				TextBlock errorLabel = new()
				{
					Text = $"Could not find any HUD elements, are you sure {OriginHUD.Name} is a HUD?",
					FontSize = 16,
					TextAlignment = TextAlignment.Center,
					TextWrapping = TextWrapping.Wrap,
					Margin = new Thickness(25)
				};

				scrollablePanel.Content = errorLabel;

				OriginHUD = null;
			}
			else
			{
				scrollablePanel.Content = OriginFilesList;
			}

			OriginHUDFilesContainer.Children.Add(scrollablePanel);

			UpdateFooterState();
		}

		private void NewTargetHUD_Click(object sender, RoutedEventArgs e)
		{
			System.Windows.Forms.DialogResult result = this.FolderBrowserDialog.ShowDialog();
			if (result == System.Windows.Forms.DialogResult.OK)
			{
				TargetFilesList.Children.Clear();
				NewTargetHUD(this.FolderBrowserDialog.SelectedPath);
			}
		}

		private void NewTargetHUD(string folderPath)
		{
			TargetHUD = new HUD(folderPath);

			TargetHUDStatusTitle.Content = TargetHUD.Name;
			TargetHUDStatusInfo.Content = TargetHUD.FolderPath;

			TargetHUDFilesContainer.Children.Clear();
			TargetHUDFilesContainer.ColumnDefinitions.Clear();
			TargetHUDFilesContainer.RowDefinitions.Clear();

			RowDefinition titleRowDefinition = new()
			{
				Height = GridLength.Auto
			};
			TargetHUDFilesContainer.RowDefinitions.Add(titleRowDefinition);
			TargetHUDFilesContainer.RowDefinitions.Add(new RowDefinition());

			Label titleLabel = new()
			{
				Content = "Files To Add",
				FontSize = 18
			};
			Grid.SetRow(titleLabel, 0);
			TargetHUDFilesContainer.Children.Add(titleLabel);

			ScrollViewer scrollablePanel = new();
			Grid.SetRow(scrollablePanel, 1);

			TargetFilesList.Margin = new Thickness(3);

			foreach (HUDPanel panel in HUDPanels)
			{
				panel.TargetListItem = new Border()
				{
					Style = (Style)Application.Current.Resources["PanelBorder"],
					Background = (Brush)Application.Current.Resources["_Blue"],
					Visibility = panel.Armed ? Visibility.Visible : Visibility.Collapsed,
					Child = new Label()
					{
						Content = panel.Name,
						Style = (Style)Application.Current.Resources["PanelLabel"],
						Foreground = Brushes.White,
					}
				};

				panel.TargetListItem.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) =>
				{
					((Label)panel.OriginListItem.Child).Foreground = Brushes.Black;
					panel.OriginListItem.Background = Brushes.Transparent;
					panel.TargetListItem.Visibility = Visibility.Collapsed;
					panel.Armed = false;
				};

				TargetFilesList.Children.Add(panel.TargetListItem);
			}
			scrollablePanel.Content = TargetFilesList;
			TargetHUDFilesContainer.Children.Add(scrollablePanel);

			UpdateFooterState();
		}

		private void MergeButton_Click(object sender, RoutedEventArgs e)
		{
			if (this.MergeButtonEnabled)
			{
				try
				{
					TargetHUD.Merge(OriginHUD, HUDPanels.Where((panel) => panel.Armed).ToArray());
					MessageBox.Show("Done!");
				}
				catch (Exception Error)
				{
					MessageBox.Show(Error.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}
	}
}
