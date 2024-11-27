using AngleSharp.Dom;
using KristofferStrube.Blazor.SVGEditor;
using Microsoft.AspNetCore.Components.Web;

namespace KristofferStrube.Blazor.GraphEditor;

/// <summary>
/// The edge between two <see cref="Node{TNodeData, TEdgeData}"/>s.
/// </summary>
/// <typeparam name="TNodeData">The type parameter for the data that backs the nodes in this graph.</typeparam>
/// <typeparam name="TEdgeData">The type parameter for the data that backs the nodes in this graph.</typeparam>
public class Edge<TNodeData, TEdgeData> : Line where TNodeData : IEquatable<TNodeData>
{
    /// <summary>
    /// Constructs an edge.
    /// </summary>
    /// <param name="element">The SVG element that will be used as the base backing element for changes to the position of the edge.</param>
    /// <param name="svg">The <see cref="SVGEditor.SVGEditor"/> that the edge resides in.</param>
    public Edge(IElement element, SVGEditor.SVGEditor svg) : base(element, svg)
    {
        UpdateLine();
    }

    /// <summary>
    /// The component type that will be used to edit this shape.
    /// </summary>
    public override Type Presenter => typeof(EdgeEditor<TNodeData, TEdgeData>);

    /// <summary>
    /// The <see cref="GraphEditor{TNode, TEdge}"/> that this edge belongs to.
    /// </summary>
    public required GraphEditor<TNodeData, TEdgeData> GraphEditor { get; set; }

    /// <summary>
    /// The data that this edge will get its characteristics from.
    /// </summary>
    public required TEdgeData Data { get; set; }

    /// <summary>
    /// The node that the edge goes from.
    /// </summary>
    public required Node<TNodeData, TEdgeData> From { get; set; }

    /// <summary>
    /// The node that the edge goes to.
    /// </summary>
    public required Node<TNodeData, TEdgeData> To { get; set; }

    /// <summary>
    /// The width of the edge mapped from its <see cref="Data"/>.
    /// </summary>
    public new string StrokeWidth => GraphEditor.EdgeWidthMapper(Data).AsString();

    /// <summary>
    /// The color of the edge mapped from its <see cref="Data"/>.
    /// </summary>
    public new string Stroke => GraphEditor.EdgeColorMapper(Data);

    /// <summary>
    /// Whether the edge should show an arrow head at its <see cref="To"/> end.
    /// </summary>
    public bool ShowArrow => GraphEditor.EdgeShowsArrow(Data);

    /// <summary>
    /// Handles when the shape is moved.
    /// </summary>
    /// <param name="eventArgs">The arguments from the pointer that is moved.</param>
    public override void HandlePointerMove(PointerEventArgs eventArgs)
    {
        if (SVG.EditMode is EditMode.Add)
        {
            (X2, Y2) = SVG.LocalDetransform((eventArgs.OffsetX, eventArgs.OffsetY));
            SetStart((X2, Y2));
        }
    }

    /// <summary>
    /// Handles when the shape stops moving.
    /// </summary>
    /// <param name="eventArgs">The arguments from the pointer that stops.</param>
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

    /// <summary>
    /// The method that is invoked when an edge finishes its creation process.
    /// </summary>
    public override void Complete()
    {
        if (To is null)
        {
            SVG.RemoveElement(this);
            Changed?.Invoke(this);
        }
    }

    /// <summary>
    /// Updates the coordinates of the start of the line.
    /// </summary>
    /// <param name="towards">The coordinates of where the edge will go towards.</param>
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

    /// <summary>
    /// Updates the coordinates of the start and end of the edge.
    /// </summary>
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

        if (distance < To.R + From.R + (ShowArrow ? GraphEditor.EdgeWidthMapper(Data) * 3 : 0))
        {
            (X1, Y1) = (X2, Y2);
        }
        else
        {
            SetStart((To.Cx, To.Cy));
            X2 = To.Cx - (differenceX / distance * (To.R + (ShowArrow ? GraphEditor.EdgeWidthMapper(Data) * 3 : 0)));
            Y2 = To.Cy - (differenceY / distance * (To.R + (ShowArrow ? GraphEditor.EdgeWidthMapper(Data) * 3 : 0)));
        }
    }

    /// <summary>
    /// Adds a new edge.
    /// </summary>
    /// <param name="SVG">The <see cref="SVGEditor.SVGEditor"/> that the shape will be create in.</param>
    /// <param name="graphEditor">The <see cref="GraphEditor{TNode, TEdge}"/> that the edge resides in.</param>
    /// <param name="data">The backing data that the edge will be created from.</param>
    /// <param name="from">The <see cref="Node{TNodeData, TEdgeData}"/> that the edge goes from.</param>
    /// <param name="to">The <see cref="Node{TNodeData, TEdgeData}"/> that the edge goes to.</param>
    /// <returns>The new edge.</returns>
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
            Id = graphEditor.NodeIdMapper(from.Data) + "-" + graphEditor.NodeIdMapper(to.Data),
            Changed = SVG.UpdateInput,
            GraphEditor = graphEditor,
            Data = data,
            From = from,
            To = to
        };
        from.Edges.Add(edge);
        to.Edges.Add(edge);
        from.NeighborNodes[graphEditor.NodeIdMapper(to.Data)] = edge;
        to.NeighborNodes[graphEditor.NodeIdMapper(from.Data)] = edge;

        SVG.Elements.Add(edge);
        return edge;
    }
}