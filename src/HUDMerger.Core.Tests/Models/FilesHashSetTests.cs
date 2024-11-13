using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HUDMerger.Core.Models;

namespace HUDMerger.Core.Tests.Models;

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
