namespace Algomim.Aec.Mcp.Core.Geometry;

/// <summary>Host-neutral axis-aligned bounding box DTO.</summary>
public readonly record struct AecBoundingBox(AecPoint3 Min, AecPoint3 Max);
