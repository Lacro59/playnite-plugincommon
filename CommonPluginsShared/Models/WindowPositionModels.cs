using System.Collections.Generic;

namespace CommonPluginsShared.Models
{
    /// <summary>
    /// Root document for plugin window geometry, aligned with Playnite <c>windowPositions.json</c>.
    /// </summary>
    public class WindowPositionsDocument
    {
        /// <summary>
        /// Gets or sets persisted window entries keyed by a stable non-localized id.
        /// </summary>
        public Dictionary<string, WindowPositionEntry> Positions { get; set; } = new Dictionary<string, WindowPositionEntry>();
    }

    /// <summary>
    /// Geometry for a single window key.
    /// </summary>
    public class WindowPositionEntry
    {
        /// <summary>
        /// Gets or sets the window top-left position in DIPs.
        /// </summary>
        public WindowPoint Position { get; set; }

        /// <summary>
        /// Gets or sets the window size in DIPs (<see cref="WindowPoint.X"/> = width, <see cref="WindowPoint.Y"/> = height).
        /// </summary>
        public WindowPoint Size { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="System.Windows.WindowState"/> value.
        /// </summary>
        public int State { get; set; }
    }

    /// <summary>
    /// Two-dimensional point used by Playnite-style window position JSON.
    /// </summary>
    public class WindowPoint
    {
        /// <summary>
        /// Gets or sets the X coordinate (or width when used as size).
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Gets or sets the Y coordinate (or height when used as size).
        /// </summary>
        public double Y { get; set; }
    }
}
