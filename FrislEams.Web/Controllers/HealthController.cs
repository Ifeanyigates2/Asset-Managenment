using FrislEams.Web.Configuration;
using FrislEams.Web.Data;
using FrislEams.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FrislEams.Web.Controllers;

[ApiController]
[Route("health")]
public class HealthController(IMongoClient mongoClient, IOptions<MongoDbOptions> options) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var mongoOptions = options.Value;
        if (string.IsNullOrWhiteSpace(mongoOptions.ConnectionString))
        {
            return StatusCode(503, new
            {
                status = "unhealthy",
                database = mongoOptions.DatabaseName,
                error = "MongoDb ConnectionString is not configured."
            });
        }

        try
        {
            var database = mongoClient.GetDatabase(mongoOptions.DatabaseName);
            await database.RunCommandAsync<BsonDocument>(
                new BsonDocument("ping", 1),
                cancellationToken: cancellationToken);

            var users = database.GetCollection<UserAccount>(mongoOptions.UsersCollectionName);
            var userCount = await users.CountDocumentsAsync(
                FilterDefinition<UserAccount>.Empty,
                cancellationToken: cancellationToken);
            var adminPresent = await users
                .Find(UserAccountRepository.CaseInsensitiveUsernameFilter("Admin"))
                .AnyAsync(cancellationToken);

            return Ok(new
            {
                status = "healthy",
                database = mongoOptions.DatabaseName,
                usersCollection = mongoOptions.UsersCollectionName,
                userCount,
                adminAccountPresent = adminPresent
            });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new
            {
                status = "unhealthy",
                database = mongoOptions.DatabaseName,
                error = ex.Message
            });
        }
    }
}
