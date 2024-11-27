using AngleSharp.Dom;
using KristofferStrube.Blazor.SVGEditor;
using Microsoft.AspNetCore.Components.Web;

namespace KristofferStrube.Blazor.GraphEditor;

/// <summary>
/// The nodes of the graph.
/// </summary>
/// <typeparam name="TNodeData">The type parameter for the data that backs the nodes in this graph.</typeparam>
/// <typeparam name="TEdgeData">The type parameter for the data that backs the nodes in this graph.</typeparam>
public class Node<TNodeData, TEdgeData> : Circle where TNodeData : IEquatable<TNodeData>
{
    /// <summary>
    /// Constructs a node.
    /// </summary>
    /// <param name="element">The SVG element that will be used as the base backing element for changes to the position of the node.</param>
    /// <param name="svg">The <see cref="SVGEditor.SVGEditor"/> that the node resides in.</param>
    public Node(IElement element, SVGEditor.SVGEditor svg) : base(element, svg)
    {
        GraphEditor = default!;
        Data = default!;
    }

    /// <summary>
    /// The component type that will be used to edit this shape.
    /// </summary>
    public override Type Presenter => typeof(NodeEditor<TNodeData, TEdgeData>);

    /// <summary>
    /// The <see cref="GraphEditor{TNode, TEdge}"/> that this node belongs to.
    /// </summary>
    public GraphEditor<TNodeData, TEdgeData> GraphEditor { get; set; }

    /// <summary>
    /// The data that this node will get its characteristics from.
    /// </summary>
    public TNodeData Data { get; set; }

    /// <summary>
    /// The fill color of the node mapped from the stroke color defined by <see cref="Stroke"/>.
    /// </summary>
    public override string Fill
    {
        get
        {
            int[] parts = Stroke[1..].Chunk(2).Select(part => int.Parse(part, System.Globalization.NumberStyles.HexNumber)).ToArray();
            return "#" + string.Join("", parts.Select(part => Math.Min(255, part + 50).ToString("X2")));
        }
    }

    /// <summary>
    /// The unique of the node.
    /// </summary>
    public override string? Id { get; set; }

    /// <summary>
    /// The color of the nodes stroke mapped from its <see cref="Data"/>.
    /// </summary>
    public override string Stroke => GraphEditor.NodeColorMapper(Data);

    /// <summary>
    /// Used to detect whether the node has changed any characteristics that will need a re-render.
    /// </summary>
    public override string StateRepresentation => base.StateRepresentation + Stroke + R.ToString() + (Image ?? "");

    private double r;
    /// <summary>
    /// The radius of the node.
    /// </summary>
    public new double R { 
        get {
            var currentRadius = GraphEditor.NodeRadiusMapper(Data);
            if (currentRadius != r)
            {
                base.R = currentRadius;
                r = currentRadius;
            }
            return currentRadius;
        }
    }

    /// <summary>
    /// The optional image that will be shown in the middle of the node mapped from its <see cref="Data"/>.
    /// </summary>
    public string? Image => GraphEditor.NodeImageMapper(Data);

    /// <summary>
    /// All edges that connect to this node.
    /// </summary>
    public HashSet<Edge<TNodeData, TEdgeData>> Edges { get; } = [];

    /// <summary>
    /// All nodes that are connected via <see cref="Edges"/> to this node.
    /// </summary>
    public Dictionary<string, Edge<TNodeData, TEdgeData>> NeighborNodes { get; } = [];

    /// <summary>
    /// Handles when the shape is moved.
    /// </summary>
    /// <param name="eventArgs">The arguments from the pointer that is moved.</param>
    public override void HandlePointerMove(PointerEventArgs eventArgs)
    {
        base.HandlePointerMove(eventArgs);
        if (SVG.EditMode is EditMode.Move)
        {
            foreach (Edge<TNodeData, TEdgeData> edge in Edges)
            {
                edge.UpdateLine();
            }
        }
    }

    /// <summary>
    /// Handles when the shape is removed.
    /// </summary>
    public override void BeforeBeingRemoved()
    {
        foreach (Edge<TNodeData, TEdgeData> edge in Edges)
        {
            SVG.RemoveElement(edge);
        }
    }

    /// <summary>
    /// Adds a new node.
    /// </summary>
    /// <param name="SVG">The <see cref="SVGEditor.SVGEditor"/> that the shape will be create in.</param>
    /// <param name="graphEditor">The <see cref="GraphEditor{TNode, TEdge}"/> that the node resides in.</param>
    /// <param name="data">The backing data that the node will be created from.</param>
    /// <returns>The new node.</returns>
    public static Node<TNodeData, TEdgeData> AddNew(SVGEditor.SVGEditor SVG, GraphEditor<TNodeData, TEdgeData> graphEditor, TNodeData data)
    {
        IElement element = SVG.Document.CreateElement("CIRCLE");
        element.SetAttribute("data-elementtype", "node");

        Node<TNodeData, TEdgeData> node = new(element, SVG)
        {
            Id = graphEditor.NodeIdMapper(data),
            Changed = null,
            GraphEditor = graphEditor,
            Data = data
        };

        SVG.Elements.Add(node);
        SVG.Document.GetElementsByTagName("BODY")[0].AppendElement(element);
        return node;
    }

    /// <summary>
    /// Creates a new node without adding it to the <paramref name="SVG"/> <see cref="SVGEditor.SVGEditor"/>.
    /// </summary>
    /// <param name="SVG">The <see cref="SVGEditor.SVGEditor"/> that the shape will be create in.</param>
    /// <param name="graphEditor">The <see cref="GraphEditor{TNode, TEdge}"/> that the node resides in.</param>
    /// <param name="data">The backing data that the node will be created from.</param>
    /// <returns>The new node.</returns>
    public static Node<TNodeData, TEdgeData> CreateNew(SVGEditor.SVGEditor SVG, GraphEditor<TNodeData, TEdgeData> graphEditor, TNodeData data)
    {
        IElement element = SVG.Document.CreateElement("CIRCLE");
        element.SetAttribute("data-elementtype", "node");

        Node<TNodeData, TEdgeData> node = new(element, SVG)
        {
            Id = graphEditor.NodeIdMapper(data),
            Changed = null,
            GraphEditor = graphEditor,
            Data = data
        };

        return node;
    }

    /// <summary>
    /// A unique hashcode that builds on the fact that the id must be unique.
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode()
    {
        return Id?.GetHashCode() ?? 0;
    }

    /// <summary>
    /// How this node can be equiated to any other object.
    /// </summary>
    /// <param name="obj">The other object.</param>
    /// <returns>Whether they are the same node.</returns>
    public override bool Equals(object? obj)
    {
        return obj is Node<TNodeData, TEdgeData> node && Equals(node);
    }

    /// <summary>
    /// Checks whether two nodes have the same id.
    /// </summary>
    /// <param name="obj">The other node.</param>
    /// <returns>Whether they are the same node.</returns>
    public bool Equals(Node<TNodeData, TEdgeData> obj)
    {
        return obj.Id?.Equals(Id) ?? false;
    }
}