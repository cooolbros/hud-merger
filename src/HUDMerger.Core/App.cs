using System;
using System.Text.RegularExpressions;

namespace HUDMerger.Core;

public partial class App
{
	[GeneratedRegex(@"[/\\]+", RegexOptions.Compiled)]
	public static partial Regex PathSeparatorRegex();
}
