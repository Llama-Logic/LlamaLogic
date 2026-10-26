namespace LlamaLogic.TuningArchive.UnitTests;

[TestClass]
public class GameInstallationTests
{
    static void WriteInstallation(TemporaryDirectory directory, string root)
    {
        directory.WriteFile($"{root}/Game/Bin/Default.ini", "[Other]\ngameversion = 0.0.0.0\n[Version]\ngameversion = 1.2.3.4\n");
        directory.WriteFile($"{root}/Data/Simulation/Resource.cfg", "Priority 100\nPackedFile SimulationPreload.package\nPriority 50\nPackedFile SimulationDeltaBuild*.package\nSelect Never\nPriority 999\nPackedFile SimulationFullBuild0.package\nEnd\nPriority 0\nPackedFile SimulationFullBuild*.package\n");
        directory.WriteFile($"{root}/Data/Simulation/SimulationFullBuild0.package", string.Empty);
        directory.WriteFile($"{root}/Data/Simulation/SimulationDeltaBuild0.package", string.Empty);
        directory.WriteFile($"{root}/Data/Simulation/SimulationPreload.package", string.Empty);
        directory.WriteFile($"{root}/Data/Client/Strings_ENG_US.package", string.Empty);
        directory.WriteFile($"{root}/EP01/SimulationFullBuild0.package", string.Empty);
        directory.WriteFile($"{root}/EP01/Strings_ENG_US.package", string.Empty);
        directory.WriteFile($"{root}/Delta/EP01/SimulationDeltaBuild0.package", string.Empty);
        directory.WriteFile($"{root}/Delta/EP01/SimulationPreload.package", string.Empty);
        directory.WriteFile($"{root}/Delta/EP01/Version.ini", "[Version]\npackversion = 5\n");
        directory.WriteFile($"{root}/EP02/Strings_ENG_US.package", string.Empty);
        directory.WriteFile($"{root}/Delta/EP03/SimulationDeltaBuild0.package", string.Empty);
    }

    [TestMethod]
    public void AFolderWithoutTheGameIsRejected()
    {
        using var directory = new TemporaryDirectory();
        Assert.ThrowsExactly<DirectoryNotFoundException>(() => GameInstallation.Open(directory.Path));
    }

    [TestMethod]
    public void AnInstallationIsOpened()
    {
        using var directory = new TemporaryDirectory();
        WriteInstallation(directory, "The Sims 4");
        var installation = GameInstallation.Open(directory.GetPath("The Sims 4"));
        Assert.AreEqual("1.2.3.4", installation.GameVersion);
        Assert.AreEqual(directory.GetPath("The Sims 4"), installation.ContentRoot);
        CollectionAssert.AreEqual(new[] { "BG", "EP01" }, installation.Packs.Select(pack => pack.Code).ToArray());
        CollectionAssert.AreEqual(new[] { "ENG_US" }, installation.Locales.ToArray());
        var baseGame = installation.Packs[0];
        Assert.IsNull(baseGame.Version);
        CollectionAssert.AreEqual
        (
            new[] { ("SimulationPreload.package", 100), ("SimulationDeltaBuild0.package", 50), ("SimulationFullBuild0.package", 0) },
            baseGame.SimulationPackages.Select(package => (Path.GetFileName(package.Path), package.Priority)).ToArray()
        );
        var baseGameStrings = baseGame.GetStringTablePackages("eng_us").Single();
        Assert.AreEqual(directory.GetPath("The Sims 4/Data/Client/Strings_ENG_US.package"), baseGameStrings.Path);
        var pack = installation.Packs[1];
        Assert.AreEqual("5", pack.Version);
        CollectionAssert.AreEqual
        (
            new[] { (directory.GetPath("The Sims 4/Delta/EP01/SimulationPreload.package"), -14), (directory.GetPath("The Sims 4/Delta/EP01/SimulationDeltaBuild0.package"), -20), (directory.GetPath("The Sims 4/EP01/SimulationFullBuild0.package"), -30) },
            pack.SimulationPackages.Select(package => (package.Path, package.Priority)).ToArray()
        );
        Assert.AreEqual(0, pack.GetStringTablePackages("FRE_FR").Count);
    }

    [TestMethod]
    public void ClientPackagesFollowTheirFoldersConfigurations()
    {
        using var directory = new TemporaryDirectory();
        directory.WriteFile("The Sims 4/Data/Simulation/SimulationFullBuild0.package", string.Empty);
        directory.WriteFile("The Sims 4/Data/Client/Resource.cfg", "Priority -20\nPackedFile ClientDeltaBuild*.package\nPriority -30\nPackedFile ClientFullBuild*.package\nPackedFile Strings_*.package\nPriority -40\nPackedFile UI*.package\n");
        directory.WriteFile("The Sims 4/Data/Client/ClientDeltaBuild0.package", string.Empty);
        directory.WriteFile("The Sims 4/Data/Client/ClientFullBuild0.package", string.Empty);
        directory.WriteFile("The Sims 4/Data/Client/Strings_ENG_US.package", string.Empty);
        directory.WriteFile("The Sims 4/Data/Client/UI.package", string.Empty);
        directory.WriteFile("The Sims 4/EP01/ResourceClient.cfg", "Priority -30\nPackedFile ClientFullBuild*.package\n");
        directory.WriteFile("The Sims 4/EP01/SimulationFullBuild0.package", string.Empty);
        directory.WriteFile("The Sims 4/EP01/ClientFullBuild0.package", string.Empty);
        directory.WriteFile("The Sims 4/Delta/EP01/ResourceClient.cfg", "Priority -20\nPackedFile ClientDeltaBuild*.package\n");
        directory.WriteFile("The Sims 4/Delta/EP01/ClientDeltaBuild0.package", string.Empty);
        directory.WriteFile("The Sims 4/EP02/SimulationFullBuild0.package", string.Empty);
        directory.WriteFile("The Sims 4/EP02/ClientFullBuild0.package", string.Empty);
        directory.WriteFile("The Sims 4/EP02/thumbnails.package", string.Empty);
        var installation = GameInstallation.Open(directory.GetPath("The Sims 4"));
        CollectionAssert.AreEqual
        (
            new[] { (directory.GetPath("The Sims 4/Data/Client/ClientDeltaBuild0.package"), -20, false), (directory.GetPath("The Sims 4/Data/Client/ClientFullBuild0.package"), -30, false), (directory.GetPath("The Sims 4/Data/Client/UI.package"), -40, false) },
            installation.Packs[0].ClientPackages.Select(package => (package.Path, package.Priority, package.IsPatch)).ToArray()
        );
        CollectionAssert.AreEqual
        (
            new[] { (directory.GetPath("The Sims 4/Delta/EP01/ClientDeltaBuild0.package"), -20, true), (directory.GetPath("The Sims 4/EP01/ClientFullBuild0.package"), -30, false) },
            installation.Packs[1].ClientPackages.Select(package => (package.Path, package.Priority, package.IsPatch)).ToArray()
        );
        CollectionAssert.AreEqual
        (
            new[] { (directory.GetPath("The Sims 4/EP02/ClientFullBuild0.package"), -30, false) },
            installation.Packs[2].ClientPackages.Select(package => (package.Path, package.Priority, package.IsPatch)).ToArray()
        );
    }

    [TestMethod]
    public void DeltaAndInstalledFoldersEachUseTheirOwnConfiguration()
    {
        using var directory = new TemporaryDirectory();
        directory.WriteFile("The Sims 4/Data/Simulation/SimulationFullBuild0.package", string.Empty);
        directory.WriteFile("The Sims 4/EP01/ResourceSimulation.cfg", "Priority -15\nPackedFile SimulationPreload.package\nPriority -30\nPackedFile SimulationFullBuild*.package\n");
        directory.WriteFile("The Sims 4/EP01/ResourceClient.cfg", "Priority -30\nPackedFile Strings_*.package\n");
        directory.WriteFile("The Sims 4/EP01/SimulationPreload.package", string.Empty);
        directory.WriteFile("The Sims 4/EP01/SimulationFullBuild0.package", string.Empty);
        directory.WriteFile("The Sims 4/EP01/Strings_ENG_US.package", string.Empty);
        directory.WriteFile("The Sims 4/Delta/EP01/ResourceSimulation.cfg", "Priority -14\nPackedFile SimulationPreload.package\nPriority -15\nPriority -20\nPackedFile SimulationDeltaBuild*.package\nPriority -29\nPackedFile SimulationFullBuild*.package\n");
        directory.WriteFile("The Sims 4/Delta/EP01/ResourceClient.cfg", "Priority -9\nPackedFile Strings_*.package\n");
        directory.WriteFile("The Sims 4/Delta/EP01/SimulationPreload.package", string.Empty);
        directory.WriteFile("The Sims 4/Delta/EP01/SimulationDeltaBuild0.package", string.Empty);
        directory.WriteFile("The Sims 4/Delta/EP01/Strings_ENG_US.package", string.Empty);
        var pack = GameInstallation.Open(directory.GetPath("The Sims 4")).Packs.Single(candidate => candidate.Code == "EP01");
        CollectionAssert.AreEqual
        (
            new[] { (directory.GetPath("The Sims 4/Delta/EP01/SimulationPreload.package"), -14), (directory.GetPath("The Sims 4/EP01/SimulationPreload.package"), -15), (directory.GetPath("The Sims 4/Delta/EP01/SimulationDeltaBuild0.package"), -20), (directory.GetPath("The Sims 4/EP01/SimulationFullBuild0.package"), -30) },
            pack.SimulationPackages.Select(package => (package.Path, package.Priority)).ToArray()
        );
        CollectionAssert.AreEqual
        (
            new[] { (directory.GetPath("The Sims 4/Delta/EP01/Strings_ENG_US.package"), -9), (directory.GetPath("The Sims 4/EP01/Strings_ENG_US.package"), -30) },
            pack.GetStringTablePackages("ENG_US").Select(package => (package.Path, package.Priority)).ToArray()
        );
    }

    [TestMethod]
    public void TheContentsOfAMacApplicationBundleAreFound()
    {
        using var directory = new TemporaryDirectory();
        directory.WriteFile("Applications/The Sims 4.app/Contents/Data/Simulation/SimulationFullBuild0.package", string.Empty);
        directory.WriteFile("Applications/The Sims 4.app/Contents/Resources/Default.ini", "[Version]\ngameversion = 1.2.3.4\n");
        var installation = GameInstallation.Open(directory.GetPath("Applications"));
        Assert.AreEqual(directory.GetPath("Applications/The Sims 4.app/Contents"), installation.ContentRoot);
        Assert.AreEqual("1.2.3.4", installation.GameVersion);
        Assert.AreEqual("BG", installation.Packs.Single().Code);
    }
}