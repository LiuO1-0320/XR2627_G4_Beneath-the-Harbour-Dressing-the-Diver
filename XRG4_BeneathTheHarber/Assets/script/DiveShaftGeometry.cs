/// <summary>Keep interaction reach separate from the narrow safe travel opening.</summary>
public static class DiveShaftGeometry
{
    // Platform inner edges: x +/-1, z +/-0.8. Reserve 0.35 for the visitor body.
    public const float HalfX = .65f, HalfZ = .45f, AxisX = .35f;
    public static bool Contains(float x, float z) => System.Math.Abs(x) <= HalfX && System.Math.Abs(z) <= HalfZ;
    public const float InteractionHalfX = 2.2f, InteractionHalfZ = 1.6f;
    public static bool CanInteract(float x, float z) => System.Math.Abs(x) <= InteractionHalfX && System.Math.Abs(z) <= InteractionHalfZ;
}
