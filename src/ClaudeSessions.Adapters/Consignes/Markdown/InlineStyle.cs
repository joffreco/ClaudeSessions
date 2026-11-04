using System;

namespace ClaudeSessions.Adapters.Consignes.Markdown;

/// <summary>How a run of text is drawn.</summary>
[Flags]
public enum InlineStyle
{
    /// <summary>Plain text.</summary>
    None = 0,

    /// <summary>Strong emphasis.</summary>
    Bold = 1,

    /// <summary>Emphasis.</summary>
    Italic = 2,

    /// <summary>Inline code, in a fixed-width font.</summary>
    Code = 4,

    /// <summary>A link; the run carries its address.</summary>
    Link = 8,

    /// <summary>A line break inside a paragraph; the run has no text.</summary>
    LineBreak = 16,
}
