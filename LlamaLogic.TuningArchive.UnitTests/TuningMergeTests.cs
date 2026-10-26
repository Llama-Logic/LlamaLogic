namespace LlamaLogic.TuningArchive.UnitTests;

[TestClass]
public class TuningMergeTests
{
    static string Wrap(string body, string tuningClass = "Snippet") =>
        TuningXml.GetCanonicalText(TuningXml.Parse($"<I c=\"{tuningClass}\" i=\"snippet\" m=\"snippets\" n=\"Thing\" s=\"5\">{body}</I>"));

    static TuningMergeResult Merge(string baseBody, string modBody, string gameBody, Func<TuningMergeConflict, TuningMergeChoice>? resolve = null) =>
        TuningMerge.Merge(Wrap(baseBody), Wrap(modBody), Wrap(gameBody), resolve);

    static string List(params int[] values) =>
        $"<L n=\"items\">{string.Concat(values.Select(value => $"<T>{value}</T>"))}</L>";

    static string Tuple(int a, int b, int c) =>
        $"<U><T n=\"a\">{a}</T><T n=\"b\">{b}</T><T n=\"c\">{c}</T></U>";

    [TestMethod]
    public void AChangeTheGameDidNotMakeIsKept()
    {
        var result = Merge("<T n=\"a\">1</T><T n=\"b\">2</T>", "<T n=\"a\">5</T><T n=\"b\">2</T>", "<T n=\"a\">1</T><T n=\"b\">3</T>");
        Assert.AreEqual(0, result.Conflicts.Count);
        Assert.AreEqual(Wrap("<T n=\"a\">5</T><T n=\"b\">3</T>"), result.Text);
    }

    [TestMethod]
    public void AChangeBothMadeTheSameWayIsNotAConflict()
    {
        var result = Merge("<T n=\"a\">1</T><T n=\"b\">2</T>", "<T n=\"a\">5</T><T n=\"b\">2</T>", "<T n=\"a\">5</T><T n=\"b\">3</T>");
        Assert.AreEqual(0, result.Conflicts.Count);
        Assert.AreEqual(Wrap("<T n=\"a\">5</T><T n=\"b\">3</T>"), result.Text);
    }

    [TestMethod]
    public void AValueBothChangedDifferentlyIsAConflict()
    {
        var result = Merge("<T n=\"a\">1</T>", "<T n=\"a\">5</T>", "<T n=\"a\">7</T>");
        var conflict = result.Conflicts.Single();
        Assert.AreEqual(TuningMergeConflictKind.BothChanged, conflict.Kind);
        Assert.AreEqual("a", conflict.Path);
        Assert.AreEqual("<T n=\"a\">1</T>", conflict.Base);
        Assert.AreEqual("<T n=\"a\">5</T>", conflict.Mod);
        Assert.AreEqual("<T n=\"a\">7</T>", conflict.Game);
        Assert.AreEqual(Wrap("<T n=\"a\">7</T>"), result.Text);
    }

    [TestMethod]
    public void AConflictResolvedAsTheModsTakesTheModsValue()
    {
        var result = Merge("<T n=\"a\">1</T>", "<T n=\"a\">5</T>", "<T n=\"a\">7</T>", _ => TuningMergeChoice.Mod);
        Assert.AreEqual(1, result.Conflicts.Count);
        Assert.AreEqual(Wrap("<T n=\"a\">5</T>"), result.Text);
    }

    [TestMethod]
    public void AnItemTheModAddsLandsInTheGamesList()
    {
        var result = Merge(List(10, 20), List(10, 20, 30), List(10, 15, 20));
        Assert.AreEqual(0, result.Conflicts.Count);
        Assert.AreEqual(Wrap(List(10, 15, 20, 30)), result.Text);
    }

    [TestMethod]
    public void AnItemTheModRemovesIsRemovedFromTheGamesList()
    {
        var result = Merge(List(10, 20, 30), List(10, 30), List(10, 20, 30, 40));
        Assert.AreEqual(0, result.Conflicts.Count);
        Assert.AreEqual(Wrap(List(10, 30, 40)), result.Text);
    }

    [TestMethod]
    public void ItemsBothAddAtTheSamePlaceAreKeptTheGamesFirst()
    {
        var result = Merge(List(10), List(10, 20), List(10, 30));
        Assert.AreEqual(0, result.Conflicts.Count);
        Assert.AreEqual(Wrap(List(10, 30, 20)), result.Text);
    }

    [TestMethod]
    public void AnItemBothAddIsKeptOnce()
    {
        var result = Merge(List(10), List(10, 20, 25), List(10, 20));
        Assert.AreEqual(0, result.Conflicts.Count);
        Assert.AreEqual(Wrap(List(10, 20, 25)), result.Text);
    }

    [TestMethod]
    public void AnItemBothAddInDifferentPlacesIsKeptOnceWhereTheModPutIt()
    {
        var result = Merge(List(10, 20, 30), List(5, 10, 20, 30), List(10, 20, 30, 5));
        Assert.AreEqual(0, result.Conflicts.Count);
        Assert.AreEqual(Wrap(List(5, 10, 20, 30)), result.Text);
    }

    [TestMethod]
    public void ChangesToDifferentItemsOfAListAreBothKept()
    {
        var result = Merge(
            $"<L n=\"items\">{Tuple(1, 2, 3)}{Tuple(4, 5, 6)}</L>",
            $"<L n=\"items\">{Tuple(1, 2, 9)}{Tuple(4, 5, 6)}</L>",
            $"<L n=\"items\">{Tuple(1, 2, 3)}{Tuple(4, 5, 7)}</L>");
        Assert.AreEqual(0, result.Conflicts.Count);
        Assert.AreEqual(Wrap($"<L n=\"items\">{Tuple(1, 2, 9)}{Tuple(4, 5, 7)}</L>"), result.Text);
    }

    [TestMethod]
    public void AnItemTheModChangedAndTheGameRemovedIsAConflict()
    {
        var baseBody = $"<L n=\"items\">{Tuple(1, 2, 3)}{Tuple(4, 5, 6)}</L>";
        var modBody = $"<L n=\"items\">{Tuple(1, 2, 9)}{Tuple(4, 5, 6)}</L>";
        var gameBody = $"<L n=\"items\">{Tuple(4, 5, 6)}</L>";
        var result = Merge(baseBody, modBody, gameBody);
        var conflict = result.Conflicts.Single();
        Assert.AreEqual(TuningMergeConflictKind.GameRemovedModChanged, conflict.Kind);
        Assert.AreEqual("items[0]", conflict.Path);
        Assert.IsNull(conflict.Game);
        Assert.AreEqual(Wrap(gameBody), result.Text);
        Assert.AreEqual(Wrap(modBody), Merge(baseBody, modBody, gameBody, _ => TuningMergeChoice.Mod).Text);
    }

    [TestMethod]
    public void AnItemTheModRemovedAndTheGameChangedIsAConflict()
    {
        var baseBody = $"<L n=\"items\">{Tuple(1, 2, 3)}{Tuple(4, 5, 6)}</L>";
        var modBody = $"<L n=\"items\">{Tuple(4, 5, 6)}</L>";
        var gameBody = $"<L n=\"items\">{Tuple(1, 2, 7)}{Tuple(4, 5, 6)}</L>";
        var result = Merge(baseBody, modBody, gameBody);
        var conflict = result.Conflicts.Single();
        Assert.AreEqual(TuningMergeConflictKind.ModRemovedGameChanged, conflict.Kind);
        Assert.IsNull(conflict.Mod);
        Assert.AreEqual(Wrap(gameBody), result.Text);
        Assert.AreEqual(Wrap(modBody), Merge(baseBody, modBody, gameBody, _ => TuningMergeChoice.Mod).Text);
    }

    [TestMethod]
    public void ARootAttributeTheModChangedIsKept()
    {
        var result = TuningMerge.Merge(Wrap("<T n=\"a\">1</T>"), Wrap("<T n=\"a\">1</T>", "Special"), Wrap("<T n=\"a\">2</T>"));
        Assert.AreEqual(0, result.Conflicts.Count);
        Assert.AreEqual(Wrap("<T n=\"a\">2</T>", "Special"), result.Text);
    }

    [TestMethod]
    public void ARootAttributeBothChangedDifferentlyIsAConflict()
    {
        var result = TuningMerge.Merge(Wrap("<T n=\"a\">1</T>"), Wrap("<T n=\"a\">1</T>", "Special"), Wrap("<T n=\"a\">2</T>", "Other"));
        var conflict = result.Conflicts.Single();
        Assert.AreEqual("@c", conflict.Path);
        Assert.AreEqual("Snippet", conflict.Base);
        Assert.AreEqual("Special", conflict.Mod);
        Assert.AreEqual("Other", conflict.Game);
        Assert.AreEqual(Wrap("<T n=\"a\">2</T>", "Other"), result.Text);
    }

    [TestMethod]
    public void ALongListWhoseChangesAreNearItsEndsIsMerged()
    {
        var values = Enumerable.Range(1, 3000).ToArray();
        var game = (int[])values.Clone();
        game[1500] = -1;
        var result = Merge(List(values), List([.. values, 5000]), List(game));
        Assert.AreEqual(0, result.Conflicts.Count);
        Assert.AreEqual(Wrap(List([.. game, 5000])), result.Text);
    }

    [TestMethod]
    public void AListTooLargeToAlignIsAConflict()
    {
        var values = Enumerable.Range(1, 2100).ToArray();
        var mod = Enumerable.Reverse(values).ToArray();
        var game = (int[])values.Clone();
        game[0] = -1;
        var result = Merge(List(values), List(mod), List(game));
        var conflict = result.Conflicts.Single();
        Assert.AreEqual(TuningMergeConflictKind.TooLarge, conflict.Kind);
        Assert.AreEqual("items", conflict.Path);
        Assert.AreEqual(Wrap(List(game)), result.Text);
        Assert.AreEqual(Wrap(List(mod)), Merge(List(values), List(mod), List(game), _ => TuningMergeChoice.Mod).Text);
    }

    [TestMethod]
    public void ConflictsAreTheSameHoweverTheyAreResolved()
    {
        const string baseBody = "<T n=\"a\">1</T><T n=\"b\">1</T>";
        const string modBody = "<T n=\"a\">2</T><T n=\"b\">2</T>";
        const string gameBody = "<T n=\"a\">3</T><T n=\"b\">3</T>";
        var byGame = Merge(baseBody, modBody, gameBody);
        var byMod = Merge(baseBody, modBody, gameBody, _ => TuningMergeChoice.Mod);
        CollectionAssert.AreEqual(byGame.Conflicts.ToList(), byMod.Conflicts.ToList());
        CollectionAssert.AreEqual(new[] { 0, 1 }, byGame.Conflicts.Select(conflict => conflict.Index).ToArray());
        Assert.AreEqual(Wrap("<T n=\"a\">2</T><T n=\"b\">3</T>"), Merge(baseBody, modBody, gameBody, conflict => conflict.Index == 0 ? TuningMergeChoice.Mod : TuningMergeChoice.Game).Text);
    }
}