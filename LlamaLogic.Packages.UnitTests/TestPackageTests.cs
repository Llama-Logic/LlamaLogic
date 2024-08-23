namespace LlamaLogic.Packages.UnitTests;

public abstract class TestPackageTests
{
    protected string tempPackagePath = string.Empty;

    [TestInitialize]
    public void Initialize()
    {
        tempPackagePath = Path.GetTempFileName();
        using var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("LlamaLogic.Packages.UnitTests.TestPackage.package");
        using var tempPathStream = new FileStream(tempPackagePath, FileMode.Create, FileAccess.Write, FileShare.None);
        resourceStream!.CopyTo(tempPathStream);
    }

    [TestCleanup]
    public void Cleanup() =>
        File.Delete(tempPackagePath);
}
