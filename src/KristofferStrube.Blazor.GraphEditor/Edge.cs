using AngleSharp.Dom;
using KristofferStrube.Blazor.GraphEditor.Extensions;
using KristofferStrube.Blazor.SVGEditor;
using Microsoft.AspNetCore.Components.Web;

namespace KristofferStrube.Blazor.GraphEditor;

public class Edge : Line
{
    public Edge(IElement element, SVGEditor.SVGEditor svg) : base(element, svg)
    {
        UpdateLine();
    }

    public override Type Presenter => typeof(EdgeEditor);

    public Node? From
    {
        get
        {
            var from = (Node?)SVG.Elements.FirstOrDefault(e => e is Node && e.Id == Element.GetAttribute("data-from"));
            _ = from?.Edges.Add(this);
            return from;
        }
        set
        {
            if (From is { } from)
            {
                _ = from.Edges.Remove(this);
            }
            if (value is null)
            {
                _ = Element.RemoveAttribute("data-from");
            }
            else
            {
                Element.SetAttribute("data-from", value.Id);
                _ = value.Edges.Add(this);
            }
            Changed?.Invoke(this);
        }
    }

    public Node? To
    {
        get
        {
            var to = (Node?)SVG.Elements.FirstOrDefault(e => e is Node && e.Id == Element.GetAttribute("data-to"));
            _ = to?.Edges.Add(this);
            return to;
        }
        set
        {
            if (To is { } to)
            {
                _ = to.Edges.Remove(this);
            }
            if (value is null)
            {
                _ = Element.RemoveAttribute("data-to");
            }
            else
            {
                Element.SetAttribute("data-to", value.Id);
                _ = value.Edges.Add(this);
            }
            Changed?.Invoke(this);
        }
    }

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
            && SVG.SelectedShapes.FirstOrDefault(s => s is Node node && node != From) is Node { } to)
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

    public static void AddNew(SVGEditor.SVGEditor SVG, Node from)
    {
        IElement element = SVG.Document.CreateElement("LINE");
        element.SetAttribute("data-elementtype", "edge");

        Edge edge = new(element, SVG)
        {
            Changed = SVG.UpdateInput,
            Stroke = "black",
            StrokeWidth = "5",
            From = from
        };
        SVG.EditMode = EditMode.Add;

        SVG.ClearSelectedShapes();
        SVG.SelectShape(edge);
        SVG.AddElement(edge);
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

        if (distance < 100)
        {
            (X1, Y1) = (X2, Y2);
        }
        else
        {
            SetStart((To.Cx, To.Cy));
            X2 = To.Cx - (differenceX / distance * (To.R + double.Parse(StrokeWidth) * 3));
            Y2 = To.Cy - (differenceY / distance * (To.R + double.Parse(StrokeWidth) * 3));
        }
    }
}