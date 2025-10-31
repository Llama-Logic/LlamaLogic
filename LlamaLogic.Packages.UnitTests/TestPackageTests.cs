namespace LlamaLogic.Packages.UnitTests;

public abstract class TestPackageTests
{
    protected string tempPackagePath = string.Empty;
    protected string tempTS2PackagePath = string.Empty;
    protected string tempTS3PackagePage = string.Empty;

    [TestInitialize]
    public void Initialize()
    {
        tempPackagePath = Path.GetTempFileName();
        using (var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("LlamaLogic.Packages.UnitTests.TestPackage.package"))
        using (var tempPathStream = new FileStream(tempPackagePath, FileMode.Create, FileAccess.Write, FileShare.None))
            resourceStream!.CopyTo(tempPathStream);
        tempTS2PackagePath = Path.GetTempFileName();
        using (var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("LlamaLogic.Packages.UnitTests.TS2.package"))
        using (var tempPathStream = new FileStream(tempTS2PackagePath, FileMode.Create, FileAccess.Write, FileShare.None))
            resourceStream!.CopyTo(tempPathStream);
        tempTS3PackagePage = Path.GetTempFileName();
        using (var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("LlamaLogic.Packages.UnitTests.TS3.package"))
        using (var tempPathStream = new FileStream(tempTS3PackagePage, FileMode.Create, FileAccess.Write, FileShare.None))
            resourceStream!.CopyTo(tempPathStream);
    }

    [TestCleanup]
    public void Cleanup()
    {
        File.Delete(tempPackagePath);
        File.Delete(tempTS2PackagePath);
        File.Delete(tempTS3PackagePage);
    }
}
