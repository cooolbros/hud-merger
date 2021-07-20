using System;
using System.IO;
using System.Linq;
using System.Text.Json;
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
		public HUDPanel[] HUDPanels = JsonSerializer.Deserialize<HUDPanel[]>(File.ReadAllText("Resources\\Panels.json"));
		StackPanel OriginFilesList = new();
		StackPanel TargetFilesList = new();
		System.Windows.Forms.FolderBrowserDialog FolderBrowserDialog = new()
		{
			SelectedPath = $"{Properties.Settings.Default.Team_Fortress_Folder}\\tf\\custom\\"
		};

		public MainWindow()
		{
			InitializeComponent();

			// Updater
			bool Download = Properties.Settings.Default.Download_latest_HUD_file_definitions_file_on_start_up;
			bool Extract = Properties.Settings.Default.Extract_required_TF2_HUD_files_on_startup;
			Updater.Update(Download, Extract);
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
			foreach (UIElement Child in TargetFilesList.Children)
			{
				Child.Visibility = Visibility.Collapsed;
			}
			foreach (HUDPanel Panel in this.HUDPanels)
			{
				Panel.Armed = false;
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
			System.Windows.Forms.DialogResult Result = this.FolderBrowserDialog.ShowDialog();
			if (Result == System.Windows.Forms.DialogResult.OK)
			{
				ClearSelected();
				NewOriginHUD(this.FolderBrowserDialog.SelectedPath);
			}
			// Remember Folder
			this.FolderBrowserDialog.SelectedPath += "\\";
		}

		private void NewOriginHUD(string FolderPath)
		{
			OriginHUD = new HUD(FolderPath);

			OriginHUDStatusTitle.Content = OriginHUD.Name;
			OriginHUDStatusInfo.Content = OriginHUD.FolderPath;

			OriginHUDFilesContainer.Children.Clear();
			OriginHUDFilesContainer.ColumnDefinitions.Clear();
			OriginHUDFilesContainer.RowDefinitions.Clear();

			OriginHUDFilesContainer.ColumnDefinitions.Add(new ColumnDefinition());
			RowDefinition TitleRowDefinition = new()
			{
				Height = GridLength.Auto
			};
			OriginHUDFilesContainer.RowDefinitions.Add(TitleRowDefinition);
			OriginHUDFilesContainer.RowDefinitions.Add(new RowDefinition());

			Label TitleLabel = new()
			{
				Content = "Available Files",
				FontSize = 18
			};
			OriginHUDFilesContainer.Children.Add(TitleLabel);

			// Search Box
			TextBox SearchBox = new()
			{
				Style = (Style)Application.Current.Resources["SearchBox"]
			};
			SearchBox.TextChanged += (object sender, TextChangedEventArgs e) =>
			{
				string SearchText = SearchBox.Text.ToLower();
				foreach (Border PanelBorder in OriginFilesList.Children)
				{
					bool Contains = ((Label)PanelBorder.Child).Content.ToString().ToLower().Contains(SearchText);
					PanelBorder.Visibility = Contains ? Visibility.Visible : Visibility.Collapsed;
				}
			};

			Grid.SetColumn(SearchBox, 1);
			OriginHUDFilesContainer.Children.Add(SearchBox);

			ScrollViewer ScrollablePanel = new();
			Grid.SetRow(ScrollablePanel, 1);

			OriginFilesList.Margin = new Thickness(3);

			bool HUDIsValid = false;

			foreach (HUDPanel Panel in HUDPanels)
			{
				bool PanelExists = OriginHUD.TestPanel(Panel);

				Panel.OriginListItem = new Border()
				{
					Style = (Style)Application.Current.Resources["PanelBorder"],
					Child = new Label()
					{
						Content = Panel.Name,
						Style = (Style)Application.Current.Resources["PanelLabel"],
						Visibility = PanelExists ? Visibility.Visible : Visibility.Collapsed,
					}
				};

				Panel.OriginListItem.MouseEnter += (object sender, MouseEventArgs e) =>
				{
					if (!Panel.Armed)
					{
						Panel.OriginListItem.Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#E6E6E6");
					}
				};

				Panel.OriginListItem.MouseLeave += (object sender, MouseEventArgs e) =>
				{
					if (!Panel.Armed)
					{
						Panel.OriginListItem.Background = Brushes.Transparent;
					}
				};

				Panel.OriginListItem.MouseLeftButtonUp += (object sender, MouseButtonEventArgs e) =>
				{
					if (!Panel.Armed)
					{
						((Label)Panel.OriginListItem.Child).Foreground = Brushes.White;
						Panel.OriginListItem.Background = (Brush)Application.Current.Resources["_Blue"];

						if (TargetHUD != null)
						{
							Panel.TargetListItem.Visibility = Visibility.Visible;
						}

						Panel.Armed = true;
					}
					else
					{
						((Label)Panel.OriginListItem.Child).Foreground = Brushes.Black;
						Panel.OriginListItem.Background = Brushes.Transparent;

						if (TargetHUD != null)
						{
							Panel.TargetListItem.Visibility = Visibility.Collapsed;
						}

						Panel.Armed = false;
					}
				};

				OriginFilesList.Children.Add(Panel.OriginListItem);

				if (PanelExists)
				{
					HUDIsValid = true;
				}
			}

			if (!HUDIsValid)
			{
				TextBlock ErrorLabel = new()
				{
					Text = $"Could not find any HUD elements, are you sure {OriginHUD.Name} is a HUD?",
					FontSize = 16,
					TextAlignment = TextAlignment.Center,
					TextWrapping = TextWrapping.Wrap,
					Margin = new Thickness(25)
				};

				ScrollablePanel.Content = ErrorLabel;

				OriginHUD = null;
			}
			else
			{
				ScrollablePanel.Content = OriginFilesList;
			}

			OriginHUDFilesContainer.Children.Add(ScrollablePanel);

			UpdateFooterState();
		}

		private void NewTargetHUD_Click(object sender, RoutedEventArgs e)
		{
			System.Windows.Forms.DialogResult Result = this.FolderBrowserDialog.ShowDialog();
			if (Result == System.Windows.Forms.DialogResult.OK)
			{
				TargetFilesList.Children.Clear();
				NewTargetHUD(this.FolderBrowserDialog.SelectedPath);
			}
			// Remember Folder
			this.FolderBrowserDialog.SelectedPath += "\\";
		}

		private void NewTargetHUD(string FolderPath)
		{
			TargetHUD = new HUD(FolderPath);

			TargetHUDStatusTitle.Content = TargetHUD.Name;
			TargetHUDStatusInfo.Content = TargetHUD.FolderPath;

			TargetHUDFilesContainer.Children.Clear();
			TargetHUDFilesContainer.ColumnDefinitions.Clear();
			TargetHUDFilesContainer.RowDefinitions.Clear();

			RowDefinition TitleRowDefinition = new()
			{
				Height = GridLength.Auto
			};
			TargetHUDFilesContainer.RowDefinitions.Add(TitleRowDefinition);
			TargetHUDFilesContainer.RowDefinitions.Add(new RowDefinition());

			Label TitleLabel = new()
			{
				Content = "Files To Add",
				FontSize = 18
			};
			Grid.SetRow(TitleLabel, 0);
			TargetHUDFilesContainer.Children.Add(TitleLabel);

			ScrollViewer ScrollablePanel = new();
			Grid.SetRow(ScrollablePanel, 1);

			TargetFilesList.Margin = new Thickness(3);

			foreach (HUDPanel Panel in HUDPanels)
			{
				Panel.TargetListItem = new Border()
				{
					Style = (Style)Application.Current.Resources["PanelBorder"],
					Background = (Brush)Application.Current.Resources["_Blue"],
					Visibility = Panel.Armed ? Visibility.Visible : Visibility.Collapsed,
					Child = new Label()
					{
						Content = Panel.Name,
						Style = (Style)Application.Current.Resources["PanelLabel"],
						Foreground = Brushes.White,
					}
				};

				Panel.TargetListItem.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) =>
				{
					((Label)Panel.OriginListItem.Child).Foreground = Brushes.Black;
					Panel.OriginListItem.Background = Brushes.Transparent;
					Panel.TargetListItem.Visibility = Visibility.Collapsed;
					Panel.Armed = false;
				};

				TargetFilesList.Children.Add(Panel.TargetListItem);
			}
			ScrollablePanel.Content = TargetFilesList;
			TargetHUDFilesContainer.Children.Add(ScrollablePanel);

			UpdateFooterState();
		}

		private void MergeButton_Click(object sender, RoutedEventArgs e)
		{
			if (this.MergeButtonEnabled)
			{
				try
				{
					TargetHUD.Merge(OriginHUD, HUDPanels.Where((Panel) => Panel.Armed).ToArray());
					MessageBox.Show("Done!");
				}
				catch (Exception Error)
				{
					MessageBox.Show(Error.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{

		}
	}
}
