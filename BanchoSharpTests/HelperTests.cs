using BanchoSharp.Multiplayer;

namespace BanchoSharpTests;

public class HelperTests
{
	private Dictionary<Mods, string> _modsMap;

	[SetUp]
	public void Setup() => _modsMap = new Dictionary<Mods, string>
	{
		{ Mods.None, "None" },
		{ Mods.NoFail, "NoFail" },
		{ Mods.Easy, "Easy" },
		{ Mods.HalfTime, "HalfTime" },
		{ Mods.Hidden, "Hidden" },
		{ Mods.FadeIn, "FadeIn" },
		{ Mods.HardRock, "HardRock" },
		{ Mods.Flashlight, "Flashlight" },
		{ Mods.DoubleTime, "DoubleTime" },
		{ Mods.Nightcore, "Nightcore" },
		{ Mods.SuddenDeath, "SuddenDeath" },
		{ Mods.SpunOut, "SpunOut" },
		{ Mods.Relax, "Relax" },
		{ Mods.Autopilot, "Relax2" },
		{ Mods.Freemod, "Freemod" }
	};

	[Test]
	public void TestModsString()
	{
		// First, validate single mods
		foreach (var kvPair in _modsMap)
		{
			Assert.That(kvPair.Key, Is.EqualTo(ModsUtilities.GetModsFromString(kvPair.Value)));
			
			// Validate all combinations
			foreach (var secondaryKvPair in _modsMap)
			{
				if ((secondaryKvPair.Key & Mods.Freemod) != 0 || secondaryKvPair.Key == Mods.None)
				{
					continue;
				}
			
				Mods applied = kvPair.Key;
				applied |= secondaryKvPair.Key;
				
				Assert.That(applied, Is.EqualTo(kvPair.Key | ModsUtilities.GetModsFromString(secondaryKvPair.Value)));
			}
		}
	}
}