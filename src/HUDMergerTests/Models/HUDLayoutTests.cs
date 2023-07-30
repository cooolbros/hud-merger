using System;
using HUDMerger.Models;
using HUDMergerVDF;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HUDMerger.Models.Tests;

[TestClass()]
public class HUDLayoutTests
{
	[TestMethod()]
	public void HUDLayoutTest1()
	{
		HUDLayout hudLayout = new HUDLayout(new HUD("Models\\HUDLayoutTests\\HUDLayoutTest1"));

		Assert.AreEqual("HudPlayerStatus", hudLayout["HudPlayerStatus"]["fieldName"]);
		Assert.AreEqual("1", hudLayout["HudPlayerStatus"]["visible"]);
		Assert.AreEqual("1", hudLayout["HudPlayerStatus"]["enabled"]);
		Assert.AreEqual("0", hudLayout["HudPlayerStatus"]["xpos"]);
		Assert.AreEqual("0", hudLayout["HudPlayerStatus"]["ypos"]);
		Assert.AreEqual("f0", hudLayout["HudPlayerStatus"]["wide"]);
		Assert.AreEqual("480", hudLayout["HudPlayerStatus"]["tall"]);

		Assert.AreEqual("HudWeaponAmmo", hudLayout["HudWeaponAmmo"]["fieldName"]);
		Assert.AreEqual("1", hudLayout["HudWeaponAmmo"]["visible"]);
		Assert.AreEqual("1", hudLayout["HudWeaponAmmo"]["enabled"]);
		Assert.AreEqual("r95", hudLayout["HudWeaponAmmo"]["xpos^[$WIN32]"]);
		Assert.AreEqual("r85", hudLayout["HudWeaponAmmo"]["xpos_minmode^[$WIN32]"]);
		Assert.AreEqual("r55", hudLayout["HudWeaponAmmo"]["ypos^[$WIN32]"]);
		Assert.AreEqual("r36", hudLayout["HudWeaponAmmo"]["ypos_minmode^[$WIN32]"]);
		Assert.AreEqual("r131", hudLayout["HudWeaponAmmo"]["xpos^[$X360]"]);
		Assert.AreEqual("r77", hudLayout["HudWeaponAmmo"]["ypos^[$X360]"]);
		Assert.AreEqual("94", hudLayout["HudWeaponAmmo"]["wide"]);
		Assert.AreEqual("45", hudLayout["HudWeaponAmmo"]["tall"]);
	}

	[TestMethod()]
	public void HUDLayoutTest2()
	{
		HUDLayout hudLayout = new HUDLayout(new HUD("Models\\HUDLayoutTests\\HUDLayoutTest2"));
		Assert.AreEqual("255 0 0 255", hudLayout["TestPanel"]["bgcolor_override"]);
	}

	[TestMethod()]
	public void HUDLayoutTest3()
	{
		HUDLayout hudLayout = new HUDLayout(new HUD("Models\\HUDLayoutTests\\HUDLayoutTest3"));
		Assert.AreEqual("255 0 0 255", hudLayout["TestPanel"]["bgcolor_override"]);
	}
}
