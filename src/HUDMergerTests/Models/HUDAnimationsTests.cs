using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HUDMerger.Models.Tests;

[TestClass()]
public class HUDAnimationsTests
{
	private readonly string Animations = String.Join("\r\n", new string[]
	{
			"event TestEvent",
			"{",
			"\tAnimate TestElement TestProperty TestValue Linear 0.0 0.0",
			"\tAnimate TestElement TestProperty TestValue Gain 0.0 0.0 0.0",
			"\tAnimate TestElement TestProperty TestValue Pulse 0.0 0.0 0.0",
			"\tRunEvent TestEvent 0.0",
			"\tStopEvent TestEvent 0.0",
			"\tSetVisible TestElement 1 0.0",
			"\tFireCommand 0.0 TestCommand",
			"\tRunEventChild TestElement TestElementEvent 0.0",
			"\tSetInputEnabled TestElement 1 0.0",
			"\tPlaySound 0.0 TestSound",
			"\tStopPanelAnimations TestElement 0.0",
			"\tAnimate TestElement TestProperty TestValue Linear 0.0 0.0 [$WIN32]",
			"}",
			""
	});

	[TestMethod()]
	public void ParseTest1()
	{
		Dictionary<string, List<HUDAnimation>> testAnimations = HUDAnimations.Parse(Animations);
		List<HUDAnimation> testEvent = testAnimations["TestEvent"];

		Animate animate1 = (Animate)testEvent[0];
		Assert.AreEqual(nameof(Animate), animate1.Type);
		Assert.AreEqual("TestElement", animate1.Element);
		Assert.AreEqual("TestProperty", animate1.Property);
		Assert.AreEqual("TestValue", animate1.Value);
		Assert.AreEqual("Linear", animate1.Interpolator);
		Assert.AreEqual(null, animate1.Bias);
		Assert.AreEqual(null, animate1.Frequency);
		Assert.AreEqual("0.0", animate1.Delay);
		Assert.AreEqual("0.0", animate1.Duration);
		Assert.AreEqual(null, animate1.OSTag);

		Animate animate2 = (Animate)testEvent[1];
		Assert.AreEqual(nameof(Animate), animate2.Type);
		Assert.AreEqual("TestElement", animate2.Element);
		Assert.AreEqual("TestProperty", animate2.Property);
		Assert.AreEqual("TestValue", animate2.Value);
		Assert.AreEqual("Gain", animate2.Interpolator);
		Assert.AreEqual("0.0", animate2.Bias);
		Assert.AreEqual(null, animate2.Frequency);
		Assert.AreEqual("0.0", animate2.Delay);
		Assert.AreEqual("0.0", animate2.Duration);

		Animate animate3 = (Animate)testEvent[2];
		Assert.AreEqual(nameof(Animate), animate3.Type);
		Assert.AreEqual("TestElement", animate3.Element);
		Assert.AreEqual("TestProperty", animate3.Property);
		Assert.AreEqual("TestValue", animate3.Value);
		Assert.AreEqual("Pulse", animate3.Interpolator);
		Assert.AreEqual(null, animate3.Bias);
		Assert.AreEqual("0.0", animate3.Frequency);
		Assert.AreEqual("0.0", animate3.Delay);
		Assert.AreEqual("0.0", animate3.Duration);

		RunEvent runEvent1 = (RunEvent)testEvent[3];
		Assert.AreEqual(nameof(RunEvent), runEvent1.Type);
		Assert.AreEqual("TestEvent", runEvent1.Event);
		Assert.AreEqual("0.0", runEvent1.Delay);

		StopEvent stopEvent1 = (StopEvent)testEvent[4];
		Assert.AreEqual(nameof(StopEvent), stopEvent1.Type);
		Assert.AreEqual("TestEvent", stopEvent1.Event);
		Assert.AreEqual("0.0", stopEvent1.Delay);

		SetVisible setVisible1 = (SetVisible)testEvent[5];
		Assert.AreEqual(nameof(SetVisible), setVisible1.Type);
		Assert.AreEqual("TestElement", setVisible1.Element);
		Assert.AreEqual(true, setVisible1.Visible);
		Assert.AreEqual("0.0", setVisible1.Delay);

		FireCommand fireCommand1 = (FireCommand)testEvent[6];
		Assert.AreEqual(nameof(FireCommand), fireCommand1.Type);
		Assert.AreEqual("0.0", fireCommand1.Delay);
		Assert.AreEqual("TestCommand", fireCommand1.Command);

		RunEventChild runEventChild1 = (RunEventChild)testEvent[7];
		Assert.AreEqual(nameof(RunEventChild), runEventChild1.Type);
		Assert.AreEqual("TestElement", runEventChild1.Element);
		Assert.AreEqual("TestElementEvent", runEventChild1.Event);
		Assert.AreEqual("0.0", runEventChild1.Delay);

		SetInputEnabled setInputEnabled1 = (SetInputEnabled)testEvent[8];
		Assert.AreEqual(nameof(SetInputEnabled), setInputEnabled1.Type);
		Assert.AreEqual("TestElement", setInputEnabled1.Element);
		Assert.AreEqual(true, setInputEnabled1.Enabled);
		Assert.AreEqual("0.0", setInputEnabled1.Delay);

		PlaySound playSound1 = (PlaySound)testEvent[9];
		Assert.AreEqual(nameof(PlaySound), playSound1.Type);
		Assert.AreEqual("0.0", playSound1.Delay);
		Assert.AreEqual("TestSound", playSound1.Sound);

		StopPanelAnimations stopPanelAnimations1 = (StopPanelAnimations)testEvent[10];
		Assert.AreEqual(nameof(StopPanelAnimations), stopPanelAnimations1.Type);
		Assert.AreEqual("TestElement", stopPanelAnimations1.Element);
		Assert.AreEqual("0.0", stopPanelAnimations1.Delay);

		Animate animate4 = (Animate)testEvent[11];
		Assert.AreEqual(nameof(Animate), animate4.Type);
		Assert.AreEqual("TestElement", animate4.Element);
		Assert.AreEqual("TestProperty", animate4.Property);
		Assert.AreEqual("TestValue", animate4.Value);
		Assert.AreEqual("Linear", animate4.Interpolator);
		Assert.AreEqual(null, animate4.Bias);
		Assert.AreEqual(null, animate4.Frequency);
		Assert.AreEqual("0.0", animate4.Delay);
		Assert.AreEqual("0.0", animate4.Duration);
		Assert.AreEqual("[$WIN32]", animate4.OSTag);
	}

	[TestMethod()]
	public void ParseTest2()
	{
		Dictionary<string, List<HUDAnimation>> hudanimations_tf = HUDAnimations.Parse(File.ReadAllText("Models\\HUDAnimationsTests\\ParseTest\\hudanimations_tf.txt"));

		// LevelInit
		Assert.ReferenceEquals(hudanimations_tf["LevelInit"], hudanimations_tf["levelinit"]);

		// OpenWeaponSelectionMenu
		List<HUDAnimation> openWeaponSelectionMenu = hudanimations_tf["OpenWeaponSelectionMenu"];

		StopEvent stopEvent1 = (StopEvent)openWeaponSelectionMenu[0];
		Assert.AreEqual(nameof(StopEvent), stopEvent1.Type);
		Assert.AreEqual("CloseWeaponSelectionMenu", stopEvent1.Event);
		Assert.AreEqual("0.0", stopEvent1.Delay);

		StopEvent stopEvent2 = (StopEvent)openWeaponSelectionMenu[1];
		Assert.AreEqual(nameof(StopEvent), stopEvent2.Type);
		Assert.AreEqual("WeaponPickup", stopEvent2.Event);
		Assert.AreEqual("0.0", stopEvent2.Delay);

		Animate animate1 = (Animate)openWeaponSelectionMenu[2];
		Assert.AreEqual(nameof(Animate), animate1.Type);
		Assert.AreEqual("HudWeaponSelection", animate1.Element);
		Assert.AreEqual("Alpha", animate1.Property);
		Assert.AreEqual("128", animate1.Value);
		Assert.AreEqual("Linear", animate1.Interpolator);
		Assert.AreEqual("0.0", animate1.Delay);
		Assert.AreEqual("0.1", animate1.Duration);

		Animate animate2 = (Animate)openWeaponSelectionMenu[3];
		Assert.AreEqual(nameof(Animate), animate2.Type);
		Assert.AreEqual("HudWeaponSelection", animate2.Element);
		Assert.AreEqual("SelectionAlpha", animate2.Property);
		Assert.AreEqual("255", animate2.Value);
		Assert.AreEqual("Linear", animate2.Interpolator);
		Assert.AreEqual("0.0", animate2.Delay);
		Assert.AreEqual("0.1", animate2.Duration);

		Animate animate3 = (Animate)openWeaponSelectionMenu[4];
		Assert.AreEqual(nameof(Animate), animate3.Type);
		Assert.AreEqual("HudWeaponSelection", animate3.Element);
		Assert.AreEqual("FgColor", animate3.Property);
		Assert.AreEqual("FgColor", animate3.Value);
		Assert.AreEqual("Linear", animate3.Interpolator);
		Assert.AreEqual("0.0", animate3.Delay);
		Assert.AreEqual("0.1", animate3.Duration);

		Animate animate4 = (Animate)openWeaponSelectionMenu[5];
		Assert.AreEqual(nameof(Animate), animate4.Type);
		Assert.AreEqual("HudWeaponSelection", animate4.Element);
		Assert.AreEqual("TextScan", animate4.Property);
		Assert.AreEqual("1", animate4.Value);
		Assert.AreEqual("Linear", animate4.Interpolator);
		Assert.AreEqual("0.0", animate4.Delay);
		Assert.AreEqual("0.1", animate4.Duration);
	}

	[TestMethod()]
	public void StringifyTest()
	{
		Assert.AreEqual(
			Animations,
			HUDAnimations.Stringify(new Dictionary<string, List<HUDAnimation>>()
			{
				["TestEvent"] = new List<HUDAnimation>()
				{
						new Animate()
						{
							Element = "TestElement",
							Property = "TestProperty",
							Value = "TestValue",
							Interpolator = "Linear",
							Delay = "0.0",
							Duration = "0.0"
						},
						new Animate()
						{
							Element = "TestElement",
							Property = "TestProperty",
							Value = "TestValue",
							Interpolator = "Gain",
							Bias = "0.0",
							Delay = "0.0",
							Duration = "0.0"
						},
						new Animate()
						{
							Element = "TestElement",
							Property = "TestProperty",
							Value = "TestValue",
							Interpolator = "Pulse",
							Frequency = "0.0",
							Delay = "0.0",
							Duration = "0.0"
						},
						new RunEvent()
						{
							Event = "TestEvent",
							Delay = "0.0"
						},
						new StopEvent()
						{
							Event = "TestEvent",
							Delay = "0.0"
						},
						new SetVisible()
						{
							Element = "TestElement",
							Visible = true,
							Delay = "0.0"
						},
						new FireCommand()
						{
							Delay = "0.0",
							Command = "TestCommand"
						},
						new RunEventChild()
						{
							Element = "TestElement",
							Event = "TestElementEvent",
							Delay = "0.0"
						},
						new SetInputEnabled()
						{
							Element = "TestElement",
							Enabled = true,
							Delay = "0.0"
						},
						new PlaySound()
						{
							Delay = "0.0",
							Sound = "TestSound"
						},
						new StopPanelAnimations()
						{
							Element = "TestElement",
							Delay = "0.0"
						},
						new Animate()
						{
							Element = "TestElement",
							Property = "TestProperty",
							Value = "TestValue",
							Interpolator = "Linear",
							Delay = "0.0",
							Duration = "0.0",
							OSTag = "[$WIN32]"
						}
				}
			})
		);
	}
}
