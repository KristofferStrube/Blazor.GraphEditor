using AngleSharp.Dom;
using KristofferStrube.Blazor.SVGEditor;
using Microsoft.AspNetCore.Components.Web;

namespace KristofferStrube.Blazor.GraphEditor;

public class Edge<TNodeData, TEdgeData> : Line where TNodeData : IEquatable<TNodeData>
{
    public Edge(IElement element, SVGEditor.SVGEditor svg) : base(element, svg)
    {
        UpdateLine();
    }

    public override Type Presenter => typeof(EdgeEditor<TNodeData, TEdgeData>);

    public GraphEditor<TNodeData, TEdgeData> GraphEditor { get; set; }

    public TEdgeData Data { get; set; }

    public Node<TNodeData, TEdgeData> From { get; set; }

    public Node<TNodeData, TEdgeData> To { get; set; }

    public new string StrokeWidth => GraphEditor.EdgeWidthMapper(Data).AsString();

    public new string Stroke => "black";

    public override void HandlePointerMove(PointerEventArgs eventArgs)
    {
        if (SVG.EditMode is EditMode.Add)
        {
            (X2, Y2) = SVG.LocalDetransform((eventArgs.OffsetX, eventArgs.OffsetY));
            SetStart((X2, Y2));
        }
    }

    public override void HandlePointerUp(PointerEventArgs eventArgs)
    {
        if (SVG.EditMode is EditMode.Add
            && SVG.SelectedShapes.FirstOrDefault(s => s is Node<TNodeData, TEdgeData> node && node != From) is Node<TNodeData, TEdgeData> { } to)
        {
            if (to.Edges.Any(c => c.To == From || c.From == From))
            {
                Complete();
            }
            else
            {
                To = to;
                SVG.EditMode = EditMode.None;
                UpdateLine();
            }
        }
    }

    public override void Complete()
    {
        if (To is null)
        {
            SVG.RemoveElement(this);
            Changed?.Invoke(this);
        }
    }

    public void SetStart((double x, double y) towards)
    {
        double differenceX = towards.x - From!.Cx;
        double differenceY = towards.y - From!.Cy;
        double distance = Math.Sqrt((differenceX * differenceX) + (differenceY * differenceY));

        if (distance > 0)
        {
            X1 = From!.Cx + (differenceX / distance * From.R);
            Y1 = From!.Cy + (differenceY / distance * From.R);
        }
    }

    public void UpdateLine()
    {
        if (From is null || To is null)
        {
            (X1, Y1) = (X2, Y2);
            return;
        }

        double differenceX = To.Cx - From.Cx;
        double differenceY = To.Cy - From.Cy;
        double distance = Math.Sqrt((differenceX * differenceX) + (differenceY * differenceY));

        if (distance < To.R + From.R)
        {
            (X1, Y1) = (X2, Y2);
        }
        else
        {
            SetStart((To.Cx, To.Cy));
            X2 = To.Cx - (differenceX / distance * (To.R + GraphEditor.EdgeWidthMapper(Data) * 3));
            Y2 = To.Cy - (differenceY / distance * (To.R + GraphEditor.EdgeWidthMapper(Data) * 3));
        }
    }

    public static Edge<TNodeData, TEdgeData> AddNew(
        SVGEditor.SVGEditor SVG,
        GraphEditor<TNodeData, TEdgeData> graphEditor,
        TEdgeData data,
        Node<TNodeData, TEdgeData> from,
        Node<TNodeData, TEdgeData> to)
    {
        IElement element = SVG.Document.CreateElement("LINE");
        element.SetAttribute("data-elementtype", "edge");

        Edge<TNodeData, TEdgeData> edge = new(element, SVG)
        {
            Changed = SVG.UpdateInput,
            GraphEditor = graphEditor,
            Data = data,
            From = from,
            To = to
        };
        from.Edges.Add(edge);
        to.Edges.Add(edge);

        SVG.AddElement(edge);
        return edge;
    }
}