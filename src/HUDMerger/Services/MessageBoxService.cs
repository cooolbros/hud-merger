using System;
using System.Windows;
using HUDMerger.Core.Services;

namespace HUDMerger.Services;

public class MessageBoxService : IMessageBoxService
{
	public void Show(string messageBoxText)
	{
		MessageBox.Show(messageBoxText);
	}

	public void ShowException(Exception exception, string caption)
	{
		MessageBox.Show(exception.Message, caption, MessageBoxButton.OK, MessageBoxImage.Error);
	}
}
