namespace Dictation.Api.Models;

public sealed class Letter
{
    public int Id { get; set; }
    public required string Character { get; set; }
    public int SortOrder { get; set; }
    public ICollection<Word> Words { get; } = new List<Word>();
    public ICollection<Sentence> Sentences { get; } = new List<Sentence>();
}

public sealed class Word
{
    public int Id { get; set; }
    public required string Text { get; set; }
    public int LetterId { get; set; }
    public Letter Letter { get; set; } = null!;
}

public sealed class Sentence
{
    public int Id { get; set; }
    public required string Text { get; set; }
    public int LetterId { get; set; }
    public Letter Letter { get; set; } = null!;
}

public static class Roles
{
    public const string Admin = "Admin";
    public const string User = "User";
}
