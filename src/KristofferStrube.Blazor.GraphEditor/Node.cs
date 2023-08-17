using AngleSharp.Dom;
using KristofferStrube.Blazor.SVGEditor;
using Microsoft.AspNetCore.Components.Web;

namespace KristofferStrube.Blazor.GraphEditor;

public class Node : Circle
{
    public Node(IElement element, SVGEditor.SVGEditor svg) : base(element, svg)
    {
        string? id = element.GetAttribute("id");
        if (id is null || svg.Elements.Any(e => e.Id == id))
        {
            Id = Guid.NewGuid().ToString();
        }
    }

    public override Type Presenter => typeof(NodeEditor);

    public override string Fill
    {
        get
        {
            int[] parts = Stroke[1..].Chunk(2).Select(part => int.Parse(part, System.Globalization.NumberStyles.HexNumber)).ToArray();
            return "#" + string.Join("", parts.Select(part => Math.Min(255, part + 50).ToString("X2")));
        }
    }

    public HashSet<Edge> Edges { get; } = new();

    public override void HandlePointerMove(PointerEventArgs eventArgs)
    {
        base.HandlePointerMove(eventArgs);
        if (SVG.EditMode is EditMode.Move)
        {
            foreach (Edge edge in Edges)
            {
                edge.UpdateLine();
            }
        }
    }

    public override void BeforeBeingRemoved()
    {
        foreach (Edge edge in Edges)
        {
            SVG.RemoveElement(edge);
        }
    }

    public static new void AddNew(SVGEditor.SVGEditor SVG)
    {
        IElement element = SVG.Document.CreateElement("CIRCLE");
        element.SetAttribute("data-elementtype", "node");

        Node node = new(element, SVG)
        {
            Changed = SVG.UpdateInput,
            Stroke = "#28B6F6",
            R = 50
        };

        (node.Cx, node.Cy) = SVG.LocalDetransform(SVG.LastRightClick);

        SVG.ClearSelectedShapes();
        SVG.SelectShape(node);
        SVG.AddElement(node);
    }
}