using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApplication3
{
    public static class TodoEndpoints
    {
        public static void Map(WebApplication app)
        {
            var group = app.MapGroup("/todos").RequireAuthorization();
            group.MapGet("/oldepath",  () => Results.Redirect("/todos/umair"));
            group.MapGet("/umair", async (TodoDb db) =>
                await db.Todos.ToListAsync());
            group.MapGet("/GetRouteParams/{userid}/book/{bookid}"
                ,(int userid,int bookid) => $"User Id: {userid} & Book Id: {bookid}");
            //group.MapPost("/upload", async (IFormFile file) => {
            //    var tempFile = Path.GetTempFileName();
            //    app.Logger.LogInformation(tempFile);
            //    using var stream = File.OpenWrite(tempFile);
            //    await file.CopyToAsync(stream);
            //});
            group.MapPost("/upload", async Task<Results<Ok<string>,
   BadRequest<string>>> (IFormFile file, HttpContext context, IAntiforgery antiforgery) =>
            {
                var fileSaveName = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName);
                await UploadFileWithName(file, fileSaveName);
                return TypedResults.Ok("File uploaded successfully!");
            });
            group.MapPost("/upload_many", async (IFormFileCollection myFiles) =>
            {
                foreach (var file in myFiles)
                {
                    var tempFile = Path.GetTempFileName();
                    app.Logger.LogInformation(tempFile);
                    using var stream = File.OpenWrite(tempFile);
                    await file.CopyToAsync(stream);
                }
            });
            string GetOrCreateFilePath(string fileName, string filesDirectory = "uploadFiles")
            {
                var directoryPath = Path.Combine(app.Environment.ContentRootPath, filesDirectory);
                Directory.CreateDirectory(directoryPath);
                return Path.Combine(directoryPath, fileName);
            }
            async Task UploadFileWithName(IFormFile file, string fileSaveName)
            {
                var filePath = GetOrCreateFilePath(fileSaveName);
                await using var fileStream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(fileStream);
            }


            // GET /todoitems/querssy-string-ids?ids=1&ids=3
            group.MapGet("/query-ids", async ([FromHeader(Name = "X-Todo-Id")]  int[] ids, TodoDb db) =>
            {
                return await db.Todos
                    .Where(t => ids.Contains(t.Id))
                    .ToListAsync();
            });

            // GET /todoitems/query-string-ids?ids=1&ids=3
            group.MapGet("/query-string-ids", async ([FromHeader(Name = "X-Todo-Id")] string idsHeader, TodoDb db) =>
            {
                if (string.IsNullOrEmpty(idsHeader))
                {
                    return Results.BadRequest("No IDs provided.");
                }

                // Split the string by commas and parse to int array
                var ids = idsHeader.Split(',')
                                   .Select(id => int.TryParse(id, out var parsedId) ? parsedId : (int?)null)
                                   .Where(id => id.HasValue)
                                   .Select(id => id.Value)
                                   .ToArray();

                if (ids.Length == 0)
                {
                    return Results.BadRequest("Invalid IDs provided.");
                }
                var todos = await db.Todos
         .Where(t => ids.Contains(t.Id))
         .ToListAsync();

                return Results.Ok(todos);
            });
        }


    }
}
