namespace NorthwindOps.Components.Guide.Demos.VariableHeight;

/// <summary>
/// The list heights measured on the repro in docs/research/field-guide-audit/repros/virtualize-variable-height,
/// built for .NET 10 (SDK 10.0.102) and for .NET 11 (SDK 11.0.100-rc.2), with
/// tools/guide-shots/probes/virtualize-variable-height-series.mjs: the height at the top, then after each of 40
/// scroll steps of 400px.
/// </summary>
public static class Measured
{
    public static readonly int[] Net10 =
    [
        38741, 38741, 39795, 33745, 38965, 37845, 33745, 45607, 39795, 35833, 42460, 35833, 37797, 41842, 39843, 44506,
        35156, 43888, 37845, 36885, 37845, 37197, 33685, 34415, 34415, 33057, 44364, 33057, 41889, 33685, 33685, 39843,
        39891, 37797, 44506, 39843, 39795, 43888, 41889, 33685, 36869,
    ];

    public static readonly int[] Net11 =
    [
        33589, 33589, 33589, 33589, 33589, 33589, 33589, 34738, 34738, 34738, 34738, 34738, 34738, 35812, 35812, 35812,
        35812, 35812, 35812, 35829, 35829, 35829, 35829, 35400, 35400, 35400, 35267, 35267, 35267, 35517, 35517, 35517,
        35517, 35492, 35492, 35492, 35492, 35396, 35396, 35396, 35396,
    ];
}
