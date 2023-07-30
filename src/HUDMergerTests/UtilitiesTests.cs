using System;
using System.IO;
using HUDMerger;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HUDMerger.Tests;

[TestClass()]
public class UtilitiesTests
{
	[TestMethod()]
	public void PathContainsPathTest()
	{
		string customFolder = @"C:\Program Files (x86)\Steam\steamapps\common\Team Fortress 2\tf\custom";

		Assert.IsTrue(Utilities.PathContainsPath(customFolder, Path.Join(customFolder, "HUD")));
		Assert.IsFalse(Utilities.PathContainsPath(customFolder, @"C:\Users\user\Downloads\HUD"));
	}
}
