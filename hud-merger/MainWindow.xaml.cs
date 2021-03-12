using System;
using System.Collections.Generic;
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
		HUDPanel[] HUDPanels = JsonSerializer.Deserialize<List<HUDPanel>>(File.ReadAllText("Resources\\Panels.json")).ToArray();
		StackPanel OriginFilesList = new();
		StackPanel TargetFilesList = new();

		public MainWindow()
		{
			InitializeComponent();

			MergeButtonTextBlock.MouseEnter += (object sender, MouseEventArgs e) =>
			{
				if (MergeButtonEnabled)
				{
					MergeButtonTextBlock.Background = (Brush)Application.Current.Resources["_BlueHover"];
				}
			};
			MergeButtonTextBlock.MouseLeave += (object sender, MouseEventArgs e) =>
			{
				if (MergeButtonEnabled)
				{
					MergeButtonTextBlock.Background = (Brush)Application.Current.Resources["_Blue"];
				}
			};
		}

		private void MenuItem_LoadOriginHUD(object sender, RoutedEventArgs e)
		{
			ClearState();
			NewOriginHUD(sender, e);

		}

		private void MenuItem_LoadTargetHUD(object sender, RoutedEventArgs e)
		{
			TargetFilesList.Children.Clear();
			NewTargetHUD(sender, e);
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

		private string FolderBrowserDialog()
		{
			using (System.Windows.Forms.FolderBrowserDialog fbd = new())
			{
				fbd.ShowDialog();
				return fbd.SelectedPath;
			}
		}

		private void ClearState()
		{
			OriginFilesList.Children.Clear();
			TargetFilesList.Children.Clear();
			foreach (HUDPanel Panel in this.HUDPanels)
			{
				Panel.Armed = false;
			}
		}

		private void UpdateFooterState()
		{
			if (OriginHUD != null && TargetHUD != null)
			{
				MergeButtonTextBlock.Cursor = Cursors.Hand;
				MergeButtonTextBlock.Background = (Brush)Application.Current.Resources["_Blue"];
				this.MergeButtonEnabled = true;
			}
			else
			{
				MergeButtonTextBlock.Cursor = Cursors.Arrow;
				MergeButtonTextBlock.Background = (Brush)Application.Current.Resources["_MergeButtonBackground"];
			}
		}

		private void NewOriginHUD(object sender, RoutedEventArgs e)
		{
			string Result = FolderBrowserDialog();
			if (Result == "") return;
			OriginHUD = new HUD(Result);

			OriginHUDStatusTitle.Content = OriginHUD.Name;
			OriginHUDStatusInfo.Content = OriginHUD.FolderPath;

			OriginHUDFilesContainer.Children.Clear();
			OriginHUDFilesContainer.ColumnDefinitions.Clear();
			OriginHUDFilesContainer.RowDefinitions.Clear();


			OriginHUDFilesContainer.ColumnDefinitions.Add(new ColumnDefinition());
			RowDefinition TitleRowDefinition = new();
			TitleRowDefinition.Height = GridLength.Auto;
			OriginHUDFilesContainer.RowDefinitions.Add(TitleRowDefinition);
			OriginHUDFilesContainer.RowDefinitions.Add(new RowDefinition());

			Label TitleLabel = new();
			TitleLabel.Content = "Available Files";
			TitleLabel.FontSize = 18;
			OriginHUDFilesContainer.Children.Add(TitleLabel);

			// Search Box
			TextBox SearchBox = new();
			SearchBox.Style = (Style)Application.Current.Resources["SearchBox"];
			SearchBox.TextChanged += (object sender, TextChangedEventArgs e) =>
			{
				string SearchText = SearchBox.Text.ToLower();
				foreach (Label PanelLabel in OriginFilesList.Children)
				{
					if (PanelLabel.Content.ToString().ToLower().Contains(SearchText))
					{
						PanelLabel.Visibility = Visibility.Visible;
					}
					else
					{
						PanelLabel.Visibility = Visibility.Collapsed;
					}
				}
			};

			Grid.SetColumn(SearchBox, 1);
			OriginHUDFilesContainer.Children.Add(SearchBox);

			ScrollViewer ScrollablePanel = new();
			Grid.SetRow(ScrollablePanel, 1);

			OriginFilesList.Margin = new Thickness(3);

			foreach (HUDPanel Panel in HUDPanels)
			{
				Label PanelLabel = new();
				PanelLabel.Content = Panel.Name;
				PanelLabel.Style = (Style)Application.Current.Resources["PanelLabel"];
				// The killfeed doesnt have a file, only check if a required file is specified
				PanelLabel.Visibility = Panel.Main.FilePath != null ? (OriginHUD.TestPanel(Panel) ? Visibility.Visible : Visibility.Collapsed) : Visibility.Visible;
				OriginFilesList.Children.Add(PanelLabel);

				PanelLabel.MouseEnter += (object sender, MouseEventArgs e) =>
				{
					if (!Panel.Armed)
					{
						PanelLabel.Background = Brushes.LightGray;
					}
				};
				PanelLabel.MouseLeave += (object sender, MouseEventArgs e) =>
				{
					if (!Panel.Armed)
					{
						PanelLabel.Background = Brushes.White;
					}
				};
				PanelLabel.MouseLeftButtonUp += (object sender, MouseButtonEventArgs e) =>
				{
					if (!Panel.Armed)
					{
						PanelLabel.Foreground = Brushes.White;
						PanelLabel.Background = (Brush)Application.Current.Resources["_Blue"];

						if (TargetHUD != null)
						{
							Panel.TargetListItem.Visibility = Visibility.Visible;
						}

						Panel.Armed = true;
					}
					else
					{
						PanelLabel.Foreground = Brushes.Black;
						PanelLabel.Background = Brushes.White;

						if (TargetHUD != null)
						{
							Panel.TargetListItem.Visibility = Visibility.Collapsed;
						}

						Panel.Armed = false;
					}
				};
			}
			ScrollablePanel.Content = OriginFilesList;

			OriginHUDFilesContainer.Children.Add(ScrollablePanel);

			UpdateFooterState();
		}

		private void NewTargetHUD(object sender, RoutedEventArgs e)
		{
			string Result = FolderBrowserDialog();
			if (Result == "") return;
			TargetHUD = new HUD(Result);

			TargetHUDStatusTitle.Content = TargetHUD.Name;
			TargetHUDStatusInfo.Content = TargetHUD.FolderPath;

			TargetHUDFilesContainer.Children.Clear();
			TargetHUDFilesContainer.ColumnDefinitions.Clear();
			TargetHUDFilesContainer.RowDefinitions.Clear();

			RowDefinition TitleRowDefinition = new();
			TitleRowDefinition.Height = GridLength.Auto;
			TargetHUDFilesContainer.RowDefinitions.Add(TitleRowDefinition);
			TargetHUDFilesContainer.RowDefinitions.Add(new RowDefinition());

			Label TitleLabel = new();
			TitleLabel.Content = "Files To Add";
			TitleLabel.FontSize = 18;
			Grid.SetRow(TitleLabel, 0);
			TargetHUDFilesContainer.Children.Add(TitleLabel);

			ScrollViewer ScrollablePanel = new();
			Grid.SetRow(ScrollablePanel, 1);

			foreach (HUDPanel Panel in HUDPanels)
			{
				Panel.TargetListItem = new Label();
				Panel.TargetListItem.Content = Panel.Name;
				Panel.TargetListItem.Style = (Style)Application.Current.Resources["PanelLabel"];
				Panel.TargetListItem.Foreground = Brushes.White;
				Panel.TargetListItem.Background = (Brush)Application.Current.Resources["_Blue"];
				Panel.TargetListItem.Visibility = Panel.Armed ? Visibility.Visible : Visibility.Collapsed;
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
					System.Windows.MessageBox.Show("Done!");
				}
				catch (Exception Error)
				{
					System.Windows.MessageBox.Show(Error.ToString());
				}
			}
		}
	}
}
