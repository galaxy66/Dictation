using Dictation.Api.Models;
using Dictation.Api.Services;
using Microsoft.AspNetCore.Authorization;

namespace Dictation.Api.Endpoints;

public static class ContentEndpoints
{
    public static void MapContentEndpoints(this WebApplication app)
    {
        var user = app.MapGroup("/api").WithTags("Dictation content");
        user.MapGet("/letters", async (IContentService content, CancellationToken ct) =>
                TypedResults.Ok(await content.GetLettersAsync(ct)))
            .WithName("GetLetters")
            .WithSummary("Get letters in their configured order")
            .Produces<IReadOnlyList<LetterResponse>>();
        user.MapGet("/letters/{id:int}", async Task<IResult> (
            int id, IContentService content, CancellationToken ct) =>
        {
            var letter = await content.GetLetterAsync(id, ct);
            return letter is null ? TypedResults.NotFound() : TypedResults.Ok(letter);
        })
            .WithName("GetLetterById")
            .WithSummary("Get one letter by ID")
            .Produces<LetterResponse>()
            .Produces(StatusCodes.Status404NotFound);
        user.MapGet("/words", async (int? letterId, IContentService content, CancellationToken ct) =>
                TypedResults.Ok(await content.GetWordsAsync(letterId, ct)))
            .WithName("GetWords")
            .WithSummary("Get words, optionally filtered by letter")
            .Produces<IReadOnlyList<WordResponse>>();
        user.MapGet("/words/{id:int}", async Task<IResult> (
            int id, IContentService content, CancellationToken ct) =>
        {
            var word = await content.GetWordAsync(id, ct);
            return word is null ? TypedResults.NotFound() : TypedResults.Ok(word);
        })
            .WithName("GetWordById")
            .WithSummary("Get one word by ID")
            .Produces<WordResponse>()
            .Produces(StatusCodes.Status404NotFound);
        user.MapGet("/sentences", async (int? letterId, IContentService content, CancellationToken ct) =>
                TypedResults.Ok(await content.GetSentencesAsync(letterId, ct)))
            .WithName("GetSentences")
            .WithSummary("Get sentences, optionally filtered by letter")
            .Produces<IReadOnlyList<SentenceResponse>>();
        user.MapGet("/sentences/{id:int}", async Task<IResult> (
            int id, IContentService content, CancellationToken ct) =>
        {
            var sentence = await content.GetSentenceAsync(id, ct);
            return sentence is null ? TypedResults.NotFound() : TypedResults.Ok(sentence);
        })
            .WithName("GetSentenceById")
            .WithSummary("Get one sentence by ID")
            .Produces<SentenceResponse>()
            .Produces(StatusCodes.Status404NotFound);

        var admin = app.MapGroup("/api/admin")
            .WithTags("Admin content management")
            .RequireAuthorization(new AuthorizeAttribute { Roles = Roles.Admin });

        admin.MapPost("/letters", async Task<IResult> (
            CreateLetterRequest request, IContentService content, CancellationToken ct) =>
        {
            var created = await content.CreateLetterAsync(request, ct);
            return created is null
                ? TypedResults.Conflict()
                : TypedResults.Created($"/api/letters/{created.Id}", created);
        })
            .WithName("CreateLetter")
            .WithSummary("Add a letter")
            .Produces<LetterResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status409Conflict);
        admin.MapPut("/letters/{id:int}", async Task<IResult> (
            int id, UpdateLetterRequest request, IContentService content, CancellationToken ct) =>
        {
            var updated = await content.UpdateLetterAsync(id, request, ct);
            return updated switch
            {
                { Kind: UpdateLetterResultKind.NotFound } => TypedResults.NotFound(),
                { Kind: UpdateLetterResultKind.CharacterConflict } => TypedResults.Conflict(),
                { Letter: { } letter } => TypedResults.Ok(letter),
                _ => throw new InvalidOperationException("The letter update result is incomplete."),
            };
        })
            .WithName("UpdateLetter")
            .WithSummary("Update a letter and its display order")
            .Produces<LetterResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);
        admin.MapDelete("/letters/{id:int}", async Task<IResult> (
            int id, IContentService content, CancellationToken ct) =>
        {
            var result = await content.DeleteLetterAsync(id, ct);
            return result switch
            {
                DeleteResult.Deleted => TypedResults.NoContent(),
                DeleteResult.HasChildren => TypedResults.Conflict(),
                _ => TypedResults.NotFound(),
            };
        })
            .WithName("DeleteLetter")
            .WithSummary("Delete a letter that has no linked content")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        admin.MapPost("/words", async Task<IResult> (
            CreateWordRequest request, IContentService content, CancellationToken ct) =>
        {
            var created = await content.CreateWordAsync(request, ct);
            return created is null
                ? TypedResults.NotFound()
                : TypedResults.Created($"/api/words/{created.Id}", created);
        })
            .WithName("CreateWord")
            .WithSummary("Add a word to a letter")
            .Produces<WordResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status404NotFound);
        admin.MapPut("/words/{id:int}", async Task<IResult> (
            int id, UpdateWordRequest request, IContentService content, CancellationToken ct) =>
        {
            var updated = await content.UpdateWordAsync(id, request, ct);
            return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated);
        })
            .WithName("UpdateWord")
            .WithSummary("Update a word")
            .Produces<WordResponse>()
            .Produces(StatusCodes.Status404NotFound);
        admin.MapDelete("/words/{id:int}", async Task<IResult> (
            int id, IContentService content, CancellationToken ct) =>
                await content.DeleteWordAsync(id, ct) ? TypedResults.NoContent() : TypedResults.NotFound())
            .WithName("DeleteWord")
            .WithSummary("Delete a word")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        admin.MapPost("/sentences", async Task<IResult> (
            CreateSentenceRequest request, IContentService content, CancellationToken ct) =>
        {
            var created = await content.CreateSentenceAsync(request, ct);
            return created is null
                ? TypedResults.NotFound()
                : TypedResults.Created($"/api/sentences/{created.Id}", created);
        })
            .WithName("CreateSentence")
            .WithSummary("Add a sentence to a letter")
            .Produces<SentenceResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status404NotFound);
        admin.MapPut("/sentences/{id:int}", async Task<IResult> (
            int id, UpdateSentenceRequest request, IContentService content, CancellationToken ct) =>
        {
            var updated = await content.UpdateSentenceAsync(id, request, ct);
            return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated);
        })
            .WithName("UpdateSentence")
            .WithSummary("Update a sentence")
            .Produces<SentenceResponse>()
            .Produces(StatusCodes.Status404NotFound);
        admin.MapDelete("/sentences/{id:int}", async Task<IResult> (
            int id, IContentService content, CancellationToken ct) =>
                await content.DeleteSentenceAsync(id, ct) ? TypedResults.NoContent() : TypedResults.NotFound())
            .WithName("DeleteSentence")
            .WithSummary("Delete a sentence")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }
}
