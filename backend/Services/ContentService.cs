using Dictation.Api.Data;
using Dictation.Api.Endpoints;
using Dictation.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Dictation.Api.Services;

public interface IContentService
{
    Task<IReadOnlyList<LetterResponse>> GetLettersAsync(CancellationToken ct);
    Task<LetterResponse?> GetLetterAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<WordResponse>> GetWordsAsync(int? letterId, CancellationToken ct);
    Task<WordResponse?> GetWordAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<SentenceResponse>> GetSentencesAsync(int? letterId, CancellationToken ct);
    Task<SentenceResponse?> GetSentenceAsync(int id, CancellationToken ct);
    Task<LetterResponse?> CreateLetterAsync(CreateLetterRequest request, CancellationToken ct);
    Task<UpdateLetterResult> UpdateLetterAsync(int id, UpdateLetterRequest request, CancellationToken ct);
    Task<DeleteResult> DeleteLetterAsync(int id, CancellationToken ct);
    Task<WordResponse?> CreateWordAsync(CreateWordRequest request, CancellationToken ct);
    Task<WordResponse?> UpdateWordAsync(int id, UpdateWordRequest request, CancellationToken ct);
    Task<bool> DeleteWordAsync(int id, CancellationToken ct);
    Task<SentenceResponse?> CreateSentenceAsync(CreateSentenceRequest request, CancellationToken ct);
    Task<SentenceResponse?> UpdateSentenceAsync(int id, UpdateSentenceRequest request, CancellationToken ct);
    Task<bool> DeleteSentenceAsync(int id, CancellationToken ct);
}

public enum DeleteResult
{
    NotFound,
    HasChildren,
    Deleted,
}

public enum UpdateLetterResultKind
{
    NotFound,
    CharacterConflict,
    Updated,
}

public sealed record UpdateLetterResult(UpdateLetterResultKind Kind, LetterResponse? Letter);

public sealed class ContentService(DictationDbContext db) : IContentService
{
    public async Task<IReadOnlyList<LetterResponse>> GetLettersAsync(CancellationToken ct) =>
        await db.Letters.AsNoTracking()
            .OrderBy(letter => letter.SortOrder)
            .ThenBy(letter => letter.Id)
            .Select(letter => new LetterResponse(letter.Id, letter.Character, letter.SortOrder))
            .ToListAsync(ct);

    public async Task<LetterResponse?> GetLetterAsync(int id, CancellationToken ct) =>
        await db.Letters.AsNoTracking()
            .Where(letter => letter.Id == id)
            .Select(letter => new LetterResponse(letter.Id, letter.Character, letter.SortOrder))
            .SingleOrDefaultAsync(ct);

    public async Task<IReadOnlyList<WordResponse>> GetWordsAsync(int? letterId, CancellationToken ct)
    {
        var query = db.Words.AsNoTracking();
        if (letterId.HasValue)
        {
            query = query.Where(word => word.LetterId == letterId.Value);
        }

        return await query.OrderBy(word => word.Id)
            .Select(word => new WordResponse(word.Id, word.Text, word.LetterId))
            .ToListAsync(ct);
    }

    public async Task<WordResponse?> GetWordAsync(int id, CancellationToken ct) =>
        await db.Words.AsNoTracking()
            .Where(word => word.Id == id)
            .Select(word => new WordResponse(word.Id, word.Text, word.LetterId))
            .SingleOrDefaultAsync(ct);

    public async Task<IReadOnlyList<SentenceResponse>> GetSentencesAsync(int? letterId, CancellationToken ct)
    {
        var query = db.Sentences.AsNoTracking();
        if (letterId.HasValue)
        {
            query = query.Where(sentence => sentence.LetterId == letterId.Value);
        }

        return await query.OrderBy(sentence => sentence.Id)
            .Select(sentence => new SentenceResponse(sentence.Id, sentence.Text, sentence.LetterId))
            .ToListAsync(ct);
    }

    public async Task<SentenceResponse?> GetSentenceAsync(int id, CancellationToken ct) =>
        await db.Sentences.AsNoTracking()
            .Where(sentence => sentence.Id == id)
            .Select(sentence => new SentenceResponse(sentence.Id, sentence.Text, sentence.LetterId))
            .SingleOrDefaultAsync(ct);

    public async Task<LetterResponse?> CreateLetterAsync(CreateLetterRequest request, CancellationToken ct)
    {
        if (await db.Letters.AnyAsync(letter => letter.Character == request.Character, ct))
        {
            return null;
        }

        var letter = new Letter { Character = request.Character, SortOrder = request.SortOrder };
        db.Letters.Add(letter);
        await db.SaveChangesAsync(ct);
        return new LetterResponse(letter.Id, letter.Character, letter.SortOrder);
    }

    public async Task<UpdateLetterResult> UpdateLetterAsync(int id, UpdateLetterRequest request, CancellationToken ct)
    {
        var letter = await db.Letters.FindAsync([id], ct);
        if (letter is null)
        {
            return new UpdateLetterResult(UpdateLetterResultKind.NotFound, null);
        }

        if (await db.Letters.AnyAsync(other => other.Id != id && other.Character == request.Character, ct))
        {
            return new UpdateLetterResult(UpdateLetterResultKind.CharacterConflict, null);
        }

        letter.Character = request.Character;
        letter.SortOrder = request.SortOrder;
        await db.SaveChangesAsync(ct);
        return new UpdateLetterResult(
            UpdateLetterResultKind.Updated,
            new LetterResponse(letter.Id, letter.Character, letter.SortOrder));
    }

    public async Task<DeleteResult> DeleteLetterAsync(int id, CancellationToken ct)
    {
        var letter = await db.Letters.FindAsync([id], ct);
        if (letter is null)
        {
            return DeleteResult.NotFound;
        }

        if (await db.Words.AnyAsync(word => word.LetterId == id, ct) ||
            await db.Sentences.AnyAsync(sentence => sentence.LetterId == id, ct))
        {
            return DeleteResult.HasChildren;
        }

        db.Letters.Remove(letter);
        await db.SaveChangesAsync(ct);
        return DeleteResult.Deleted;
    }

    public async Task<WordResponse?> CreateWordAsync(CreateWordRequest request, CancellationToken ct)
    {
        if (!await db.Letters.AnyAsync(letter => letter.Id == request.LetterId, ct))
        {
            return null;
        }

        var word = new Word { Text = request.Text, LetterId = request.LetterId };
        db.Words.Add(word);
        await db.SaveChangesAsync(ct);
        return new WordResponse(word.Id, word.Text, word.LetterId);
    }

    public async Task<WordResponse?> UpdateWordAsync(int id, UpdateWordRequest request, CancellationToken ct)
    {
        var word = await db.Words.FindAsync([id], ct);
        if (word is null || !await db.Letters.AnyAsync(letter => letter.Id == request.LetterId, ct))
        {
            return null;
        }

        word.Text = request.Text;
        word.LetterId = request.LetterId;
        await db.SaveChangesAsync(ct);
        return new WordResponse(word.Id, word.Text, word.LetterId);
    }

    public async Task<bool> DeleteWordAsync(int id, CancellationToken ct)
    {
        var word = await db.Words.FindAsync([id], ct);
        if (word is null)
        {
            return false;
        }

        db.Words.Remove(word);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<SentenceResponse?> CreateSentenceAsync(CreateSentenceRequest request, CancellationToken ct)
    {
        if (!await db.Letters.AnyAsync(letter => letter.Id == request.LetterId, ct))
        {
            return null;
        }

        var sentence = new Sentence { Text = request.Text, LetterId = request.LetterId };
        db.Sentences.Add(sentence);
        await db.SaveChangesAsync(ct);
        return new SentenceResponse(sentence.Id, sentence.Text, sentence.LetterId);
    }

    public async Task<SentenceResponse?> UpdateSentenceAsync(int id, UpdateSentenceRequest request, CancellationToken ct)
    {
        var sentence = await db.Sentences.FindAsync([id], ct);
        if (sentence is null || !await db.Letters.AnyAsync(letter => letter.Id == request.LetterId, ct))
        {
            return null;
        }

        sentence.Text = request.Text;
        sentence.LetterId = request.LetterId;
        await db.SaveChangesAsync(ct);
        return new SentenceResponse(sentence.Id, sentence.Text, sentence.LetterId);
    }

    public async Task<bool> DeleteSentenceAsync(int id, CancellationToken ct)
    {
        var sentence = await db.Sentences.FindAsync([id], ct);
        if (sentence is null)
        {
            return false;
        }

        db.Sentences.Remove(sentence);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
