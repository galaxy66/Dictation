using System.ComponentModel.DataAnnotations;

namespace Dictation.Api.Endpoints;

/// <summary>A letter and its position in the learning sequence.</summary>
public sealed record LetterResponse(int Id, string Character, int SortOrder);

/// <summary>Payload for adding a letter.</summary>
public sealed record CreateLetterRequest
{
    /// <summary>The displayed letter or letter group.</summary>
    [Required, StringLength(32)]
    public required string Character { get; init; }

    /// <summary>The zero-based position used to order letters.</summary>
    [Range(0, int.MaxValue)]
    public int SortOrder { get; init; }
}

/// <summary>Payload for changing a letter.</summary>
public sealed record UpdateLetterRequest
{
    /// <summary>The displayed letter or letter group.</summary>
    [Required, StringLength(32)]
    public required string Character { get; init; }

    /// <summary>The zero-based position used to order letters.</summary>
    [Range(0, int.MaxValue)]
    public int SortOrder { get; init; }
}

/// <summary>A word associated with a letter.</summary>
public sealed record WordResponse(int Id, string Text, int LetterId);

/// <summary>Payload for adding a word.</summary>
public sealed record CreateWordRequest
{
    /// <summary>The word text.</summary>
    [Required, StringLength(200)]
    public required string Text { get; init; }

    /// <summary>The associated letter ID.</summary>
    [Range(1, int.MaxValue)]
    public int LetterId { get; init; }
}

/// <summary>Payload for changing a word.</summary>
public sealed record UpdateWordRequest
{
    /// <summary>The word text.</summary>
    [Required, StringLength(200)]
    public required string Text { get; init; }

    /// <summary>The associated letter ID.</summary>
    [Range(1, int.MaxValue)]
    public int LetterId { get; init; }
}

/// <summary>A sentence associated with a letter.</summary>
public sealed record SentenceResponse(int Id, string Text, int LetterId);

/// <summary>Payload for adding a sentence.</summary>
public sealed record CreateSentenceRequest
{
    /// <summary>The sentence text.</summary>
    [Required, StringLength(1000)]
    public required string Text { get; init; }

    /// <summary>The associated letter ID.</summary>
    [Range(1, int.MaxValue)]
    public int LetterId { get; init; }
}

/// <summary>Payload for changing a sentence.</summary>
public sealed record UpdateSentenceRequest
{
    /// <summary>The sentence text.</summary>
    [Required, StringLength(1000)]
    public required string Text { get; init; }

    /// <summary>The associated letter ID.</summary>
    [Range(1, int.MaxValue)]
    public int LetterId { get; init; }
}
