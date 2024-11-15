using System;
using System.IO;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HUDAnimations.Models;

namespace HUDAnimations.Tests;

[TestClass()]
public class HUDAnimationsSerializerTests
{
	[TestMethod()]
	public void DeserializeTest()
	{
		foreach (string path in Directory.EnumerateFiles("HUDAnimationsSerializerTests/scripts"))
		{
			string text = File.ReadAllText(path);

			HUDAnimationsSerializer.Deserialize(text);

			CultureInfo currentCulture = CultureInfo.CurrentCulture;

			foreach (CultureInfo culture in CultureInfo.GetCultures(CultureTypes.AllCultures))
			{
				CultureInfo.CurrentCulture = culture;

				HUDAnimationsSerializer.Deserialize(text);
			}

			CultureInfo.CurrentCulture = currentCulture;
		}
	}

	[TestMethod()]
	public void SerializeTest()
	{
		HUDAnimationsFile animations = HUDAnimationsSerializer.Deserialize(File.ReadAllText("HUDAnimationsSerializerTests/scripts/hudanimations_tf.txt"));
		string text = HUDAnimationsSerializer.Serialize(animations);

		CultureInfo currentCulture = CultureInfo.CurrentCulture;

		foreach (CultureInfo culture in CultureInfo.GetCultures(CultureTypes.AllCultures))
		{
			CultureInfo.CurrentCulture = culture;

			string result = HUDAnimationsSerializer.Serialize(animations);
			Assert.AreEqual(text, result, CultureInfo.CurrentCulture.Name);
			HUDAnimationsSerializer.Deserialize(result);
		}

		CultureInfo.CurrentCulture = currentCulture;
	}
}
