using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VDF.Models;

namespace HUDMerger.Extensions.Tests;

[TestClass()]
public class KeyValuesExtensionsTests
{
	[TestMethod()]
	public void HeaderTest()
	{
		// Header
		Assert.AreEqual([], new KeyValues([new KeyValue { Key = "Resource/HudLayout.res", Value = new KeyValues(), Conditional = null }]).Header());

		// Ignore conditional
		Assert.AreEqual([], new KeyValues([new KeyValue { Key = "Resource/HudLayout.res", Value = new KeyValues(), Conditional = "[$WIN32]" }]).Header());

		// Replace string
		Assert.AreEqual([], new KeyValues([new KeyValue { Key = "Resource/HudLayout.res", Value = "", Conditional = null }]).Header());

		// Return KeyValues
		Assert.AreEqual([], new KeyValues().Header());

		// Append Header
		KeyValues keyValues = [];
		Assert.AreEqual([], keyValues.Header("Resource/HudLayout.res"));
		Assert.IsTrue(keyValues.Any((kv) => kv.Key == "Resource/HudLayout.res" && kv.Value is KeyValues));

		// hudanimations_manifest (https://github.com/cooolbros/hud-merger/issues/3)
		Assert.AreEqual([], new KeyValues().Header("hudanimations_manifest"));
	}
}
