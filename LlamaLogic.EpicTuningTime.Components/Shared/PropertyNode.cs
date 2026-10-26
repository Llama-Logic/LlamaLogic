namespace LlamaLogic.EpicTuningTime.Components.Shared;

/// <summary>
/// Represents a tunable in the property tree of a tuning: an element of the document, or a tunable its tuning descriptions declare that the document does not set
/// </summary>
public sealed class PropertyNode
{
    /// <summary>
    /// Gets the nodes within this one
    /// </summary>
    public IReadOnlyList<PropertyNode> Children =>
        ChildList;

    internal List<PropertyNode> ChildList { get; } = [];

    /// <summary>
    /// Gets the depth of the node in the tree (0 for the tuning's own tunables)
    /// </summary>
    public int Depth { get; internal set; }

    /// <summary>
    /// Gets the tunable description of the node, if one was found
    /// </summary>
    public TunableDescription? Description { get; internal init; }

    /// <summary>
    /// Gets what the value refers to, such as the name of a referenced tuning or the text of a string, if anything
    /// </summary>
    public string? Detail { get; internal init; }

    /// <summary>
    /// Gets the position of the node in the tree's document order
    /// </summary>
    public int Index { get; internal set; }

    /// <summary>
    /// Gets whether the value refers to something that could not be found
    /// </summary>
    public bool IsMissing { get; internal init; }

    /// <summary>
    /// Gets whether the document sets the tunable (otherwise, it takes its default)
    /// </summary>
    public bool IsSet { get; internal init; } = true;

    /// <summary>
    /// Gets the name of the node: the tunable's name, or its position in a list
    /// </summary>
    public string Label { get; internal init; } = string.Empty;

    /// <summary>
    /// Gets the 1-based line of the document on which the node starts (for a tunable the document does not set, the line of the element that would contain it)
    /// </summary>
    public int Line { get; internal init; }

    /// <summary>
    /// Gets the node containing this one, if any
    /// </summary>
    public PropertyNode? Parent { get; internal set; }

    /// <summary>
    /// Gets the path of the node from the root, such as <c>loot_list[2].stat</c>
    /// </summary>
    public string Path { get; internal init; } = string.Empty;

    /// <summary>
    /// Gets the element of the document the node stands for, if the document sets it
    /// </summary>
    public TuningNode? Source { get; internal init; }

    /// <summary>
    /// Gets the summary shown for a node with children, such as the number of items in a list
    /// </summary>
    public string? Summary { get; internal set; }

    /// <summary>
    /// Gets the tuning the value refers to, if it is in the snapshot
    /// </summary>
    public (ResourceType type, ulong instance)? Target { get; internal init; }

    /// <summary>
    /// Gets the value of the node (for a tunable the document does not set, its default), if it has one
    /// </summary>
    public string? Value { get; internal init; }
}