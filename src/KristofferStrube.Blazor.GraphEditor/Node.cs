using AngleSharp.Dom;
using AngleSharp.Svg.Dom;
using KristofferStrube.Blazor.SVGEditor;
using Microsoft.AspNetCore.Components.Web;
using System.Xml.Linq;

namespace KristofferStrube.Blazor.GraphEditor;

public class Node<TNodeData, TEdgeData> : Circle where TNodeData : IEquatable<TNodeData>
{
    public Node(IElement element, SVGEditor.SVGEditor svg) : base(element, svg)
    {
        GraphEditor = default!;
        Data = default!;
    }

    public override Type Presenter => typeof(NodeEditor<TNodeData, TEdgeData>);

    public GraphEditor<TNodeData, TEdgeData> GraphEditor { get; set; }

    public TNodeData Data { get; set; }

    public override string Fill
    {
        get
        {
            int[] parts = Stroke[1..].Chunk(2).Select(part => int.Parse(part, System.Globalization.NumberStyles.HexNumber)).ToArray();
            return "#" + string.Join("", parts.Select(part => Math.Min(255, part + 50).ToString("X2")));
        }
    }

    public override string? Id { get; set; }

    public override string Stroke => GraphEditor.NodeColorMapper(Data);

    public override string StateRepresentation => base.StateRepresentation + Stroke;

    public new double R => GraphEditor.NodeRadiusMapper(Data);

    public HashSet<Edge<TNodeData, TEdgeData>> Edges { get; } = new();

    public Dictionary<Node<TNodeData, TEdgeData>, Edge<TNodeData, TEdgeData>> NeighborNodes { get; } = new();

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

    public override void BeforeBeingRemoved()
    {
        foreach (Edge<TNodeData, TEdgeData> edge in Edges)
        {
            SVG.RemoveElement(edge);
        }
    }

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

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        return obj is Node<TNodeData, TEdgeData> node && Equals(node);
    }

    public bool Equals(Node<TNodeData, TEdgeData> obj)
    {
        return obj.Id?.Equals(Id) ?? false;
    }
}