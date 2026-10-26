namespace LlamaLogic.TuningArchive.UnitTests;

[TestClass]
public class TuningSemanticDiffTests
{
    static IReadOnlyList<TuningSemanticChange> Compare(string olderBody, string newerBody, string olderName = "Thing", string newerName = "Thing") =>
        TuningSemanticDiff.Compare(Wrap(olderBody, olderName), Wrap(newerBody, newerName));

    static string Wrap(string body, string name) =>
        TuningXml.GetCanonicalText(TuningXml.Parse($"<I c=\"Snippet\" i=\"snippet\" m=\"snippets\" n=\"{name}\" s=\"5\">{body}</I>"));

    [TestMethod]
    public void AChangedValueIsFound()
    {
        var change = Compare("<T n=\"a\">1</T><T n=\"b\">2</T>", "<T n=\"a\">1</T><T n=\"b\">3</T>").Single();
        Assert.AreEqual(TuningSemanticChangeKind.Changed, change.Kind);
        Assert.AreEqual("b", change.Path);
        Assert.AreEqual("2", change.OldValue);
        Assert.AreEqual("3", change.NewValue);
        Assert.AreEqual(4, change.OldLine);
        Assert.AreEqual(4, change.NewLine);
    }

    [TestMethod]
    public void AChangedVariantOptionIsOneChange()
    {
        var change = Compare("<V n=\"option\" t=\"disabled\" />", "<V n=\"option\" t=\"enabled\"><T n=\"value\">1</T></V>").Single();
        Assert.AreEqual(TuningSemanticChangeKind.Changed, change.Kind);
        Assert.AreEqual("option", change.Path);
    }

    [TestMethod]
    public void AnInsertedListItemIsTheOnlyChange()
    {
        var change = Compare("<L n=\"items\"><T>10</T><T>20</T><T>30</T></L>", "<L n=\"items\"><T>10</T><T>15</T><T>20</T><T>30</T></L>").Single();
        Assert.AreEqual(TuningSemanticChangeKind.Added, change.Kind);
        Assert.AreEqual("items[1]", change.Path);
        Assert.IsNull(change.OldValue);
        Assert.AreEqual("15", change.NewValue);
    }

    [TestMethod]
    public void AddedAndRemovedMembersAreFound()
    {
        var changes = Compare("<T n=\"a\">1</T><T n=\"b\">2</T>", "<T n=\"a\">1</T><T n=\"c\">3</T>");
        Assert.AreEqual(2, changes.Count);
        var added = changes.Single(change => change.Kind is TuningSemanticChangeKind.Added);
        Assert.AreEqual("c", added.Path);
        Assert.AreEqual("3", added.NewValue);
        var removed = changes.Single(change => change.Kind is TuningSemanticChangeKind.Removed);
        Assert.AreEqual("b", removed.Path);
        Assert.AreEqual("2", removed.OldValue);
    }

    [TestMethod]
    public void IdenticalTuningHasNoChanges() =>
        Assert.AreEqual(0, Compare("<T n=\"a\">1</T><L n=\"items\"><T>1</T></L>", "<T n=\"a\">1</T><L n=\"items\"><T>1</T></L>").Count);

    [TestMethod]
    public void MappingsAreMatchedByKey()
    {
        var change = Compare
        (
            "<L n=\"map\"><U><T n=\"key\">A</T><T n=\"value\">1</T></U><U><T n=\"key\">B</T><T n=\"value\">2</T></U></L>",
            "<L n=\"map\"><U><T n=\"key\">B</T><T n=\"value\">3</T></U><U><T n=\"key\">A</T><T n=\"value\">1</T></U></L>"
        ).Single();
        Assert.AreEqual(TuningSemanticChangeKind.Changed, change.Kind);
        Assert.AreEqual("map[0].value", change.Path);
        Assert.AreEqual("2", change.OldValue);
        Assert.AreEqual("3", change.NewValue);
    }

    [TestMethod]
    public void RootAttributeChangesAreFound()
    {
        var change = Compare("<T n=\"a\">1</T>", "<T n=\"a\">1</T>", "Old_Name", "New_Name").Single();
        Assert.AreEqual(TuningSemanticChangeKind.Changed, change.Kind);
        Assert.AreEqual("@n", change.Path);
        Assert.AreEqual("Old_Name", change.OldValue);
        Assert.AreEqual("New_Name", change.NewValue);
    }
}