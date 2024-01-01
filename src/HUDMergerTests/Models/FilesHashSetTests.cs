using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HUDMerger.Models.Tests;

[TestClass()]
public class FilesHashSetTests
{
	[TestMethod()]
	public void FilesHashSetTest()
	{
		FilesHashSet set1 = ["resource/ui/hudplayerhealth.res"];
		Assert.IsTrue(set1.Contains("resource\\ui\\hudplayerhealth.res"));
		Assert.IsFalse(set1.Add("resource/ui\\hudplayerhealth.res"));
	}
}
