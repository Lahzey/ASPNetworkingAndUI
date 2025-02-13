using Godot;

[GlobalClass, Tool]
public partial class PolygonInfo : Resource {
    private static readonly Vector2[] DEFAULT_SHAPE = { Vector2.Zero, Vector2.Right, Vector2.One, Vector2.Down };
    
    private Color backgroundColor;
    [Export] public Color BackgroundColor { get => backgroundColor; set { backgroundColor = value; EmitChanged(); } }

    private Color borderColor;
    [Export] public Color BorderColor { get => borderColor; set { borderColor = value; EmitChanged(); } }

    private float borderThickness = 2f;
    [Export] public float BorderThickness { get => borderThickness; set { borderThickness = value; EmitChanged(); } }

    public int[] PolygonIndices { get; private set; } = Geometry2D.TriangulatePolygon(DEFAULT_SHAPE);
    private Vector2[] polygonPoints = DEFAULT_SHAPE;
    [Export] public Vector2[] PolygonPoints {
        get => polygonPoints;
        set {
            polygonPoints = value;
            if (!IsValid()) {
                GD.PrintErr($"Points {PointsToString(value)} do not form a valid polygon.");
                PolygonIndices = null;
            } else {
                PolygonIndices = Geometry2D.TriangulatePolygon(value);
            }
            EmitChanged();
        }
    }

    public bool IsValid() {
        int n = polygonPoints.Length;

        for (int i = 0; i < n; i++) {
            Vector2 a1 = polygonPoints[i];
            Vector2 a2 = polygonPoints[(i + 1) % n];

            for (int j = i + 1; j < n; j++) {
                // Skip adjacent edges and the same edge
                if (Mathf.Abs(i - j) <= 1 || (i == 0 && j == n - 1))
                    continue;

                Vector2 b1 = polygonPoints[j];
                Vector2 b2 = polygonPoints[(j + 1) % n];

                if (DoSegmentsIntersect(a1, a2, b1, b2)) {
                    return false;
                }
            }
        }

        return true;
    }

    // Check if two line segments (p1-p2) and (q1-q2) intersect
    private static bool DoSegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2) {
        float d1 = (q2 - q1).Cross(p1 - q1);
        float d2 = (q2 - q1).Cross(p2 - q1);
        float d3 = (p2 - p1).Cross(q1 - p1);
        float d4 = (p2 - p1).Cross(q2 - p1);

        // General case: segments straddle each other
        if ((d1 * d2 < 0) && (d3 * d4 < 0))
            return true;

        // Special cases: check for colinearity and overlap
        if (IsPointOnSegment(q1, q2, p1)) return true;
        if (IsPointOnSegment(q1, q2, p2)) return true;
        if (IsPointOnSegment(p1, p2, q1)) return true;
        if (IsPointOnSegment(p1, p2, q2)) return true;

        return false;
    }

    // Check if point r lies on the segment (p-q)
    private static bool IsPointOnSegment(Vector2 p, Vector2 q, Vector2 r) {
        return Mathf.IsEqualApprox((q - p).Cross(r - p), 0, 0.0001f) &&
               Mathf.Min(p.X, q.X) - 0.0001f <= r.X && r.X <= Mathf.Max(p.X, q.X) + 0.0001f &&
               Mathf.Min(p.Y, q.Y) - 0.0001f <= r.Y && r.Y <= Mathf.Max(p.Y, q.Y) + 0.0001f;
    }

    private static string PointsToString(Vector2[] points) {
        return "[" + points.Join(", ") + "]";
    }
    
}