using AngleSharp.Dom;
using KristofferStrube.Blazor.SVGEditor;
using Microsoft.AspNetCore.Components.Web;
using System.Text.Json;

namespace KristofferStrube.Blazor.GraphEditor;

public class Node<TNodeData, TEdgeData> : Circle
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

    public override string Stroke => GraphEditor.NodeColorMapper(Data);

    public new double R => GraphEditor.NodeRadiusMapper(Data);

    public HashSet<Edge<TNodeData, TEdgeData>> Edges { get; } = new();

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
            Changed = SVG.UpdateInput,
            GraphEditor = graphEditor,
            Data = data
        };

        SVG.AddElement(node);
        return node;
    }
}