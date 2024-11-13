using System;

namespace HUDMerger.Core.Services;

public interface IMessageBoxService
{
	public void Show(string messageBoxText);
	public void ShowException(Exception exception, string caption);
}
