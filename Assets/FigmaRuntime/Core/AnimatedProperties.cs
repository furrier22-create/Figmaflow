namespace FigmaRuntime
{
    [System.Flags]
    public enum AnimatedProperties
    {
        None = 0,
        Position = 1,
        Size = 2,
        Opacity = 4,

        /// <summary>
        /// v0.5 (ADR-023): az elforgatás animálása (F8 zárás).
        /// </summary>
        Rotation = 8
    }
}
