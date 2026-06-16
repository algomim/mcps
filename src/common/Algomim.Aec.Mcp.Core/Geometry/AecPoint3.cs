namespace Algomim.Aec.Mcp.Core.Geometry;

/// <summary>Host-neutral 3D point, always expressed in the host adapter's declared unit system.</summary>
public readonly record struct AecPoint3(double X, double Y, double Z);
