using Microsoft.EntityFrameworkCore;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Threading.Tasks;
using System.Net.Http.Json;
using System.Net.Http;

#region Functionality
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDbContext<WebAppDiff.Model.DiffDb>(opt => opt.UseInMemoryDatabase("DiffList"));

var app = builder.Build();
app.UseHttpsRedirection();

app.MapGet("/", () => "Diff WebAPI");

app.MapGet("/v1/diff/{id}", async (int id, WebAppDiff.Model.DiffDb db) =>
{
  var diff = await db.Diffs.FindAsync(id);
  if (diff != null)
  {
    WebAppDiff.Model.DiffResult diffRes = new WebAppDiff.Model.DiffResult();
    if (diff.GetDiffs(ref diffRes))
    {
      if(diffRes.diffs == null || diffRes.diffs.Count == 0)
      {
        WebAppDiff.Model.DiffResultDT0 diffResDTO = new WebAppDiff.Model.DiffResultDT0
        {
          diffResultType = diffRes.diffResultType
        };
        return Results.Ok(diffResDTO);
      }
      return Results.Ok(diffRes);     
    }
  }
  return Results.NotFound();
});

app.MapPut("/v1/diff/{id}/right", async (int id, WebAppDiff.Model.DiffData inputDiffData, WebAppDiff.Model.DiffDb db) =>
{
  var diff = await db.Diffs.FindAsync(id);
  if (diff == null)
  {
    diff = new WebAppDiff.Model.Diff(id);
    db.Diffs.Add(diff);
    await db.SaveChangesAsync();
  }
  if (diff.SaveRight(inputDiffData))
  {
    db.Diffs.Update(diff);
    await db.SaveChangesAsync();
    return Results.Created($"/v1/diff/{diff.Id}", null);
  }
  else
  {
    return Results.BadRequest();
  }
});

app.MapPut("/v1/diff/{id}/left", async (int id, WebAppDiff.Model.DiffData inputDiffData, WebAppDiff.Model.DiffDb db) =>
{
  var diff = await db.Diffs.FindAsync(id);
  if (diff == null)
  {
    diff = new WebAppDiff.Model.Diff(id);
    db.Diffs.Add(diff);
    await db.SaveChangesAsync();
  }
  if (diff.SaveLeft(inputDiffData))
  {
    db.Diffs.Update(diff);
    await db.SaveChangesAsync();
    return Results.Created($"/v1/diff/{diff.Id}", db.Diffs);
  }
  else
  {
    return Results.BadRequest();
  }
});

app.Run();
#endregion


#region IntegrationTests
namespace WebAppDiff.IntegrationTests
{
  public partial class Program
  {
    public class DiffApiApplicationFactory<Program> : WebApplicationFactory<Program> where Program : class
    {
      protected override void ConfigureWebHost(IWebHostBuilder builder)
      {
        builder.ConfigureServices(services =>
        {
          var serviceProvider = new ServiceCollection()
                  .AddEntityFrameworkInMemoryDatabase()
                  .BuildServiceProvider();

        // Add a database context (ApplicationDbContext) using an in-memory 
        // database for testing.
        services.AddDbContext<WebAppDiff.Model.DiffDb>(options =>
          {
            options.UseInMemoryDatabase("DiffInMemoryDbForTesting");
            options.UseInternalServiceProvider(serviceProvider);
          });

          var sp = services.BuildServiceProvider();

          using (var scope = sp.CreateScope())
          {
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<WebAppDiff.Model.DiffDb>();
            var logger = scopedServices.GetRequiredService<ILogger<DiffApiApplicationFactory<Program>>>();

            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            try
            {
              WebAppDiff.UnitTests.DiffTests.DiffTestsDb.InitData(ref db);
            }
            catch (Exception ex)
            {
              logger.LogError(ex, "An error occurred seeding the database with test end points. Error: {Message}}", ex.Message);
            }
          }
        });
        base.ConfigureWebHost(builder);
      }
    }

    public class DiffIntegrationTest
    {
      private HttpClient client = new DiffApiApplicationFactory<Program>().CreateClient();
      private string url_rightEndpoint = "/v1/diff/{0}/right";
      private string url_leftEndpoint = "/v1/diff/{0}/left";
      private string url_endpoint = "/v1/diff/{0}";
      private string endpointValid = "AAAAAA==";
      private string endpointNotValid = "AAA==";

      [Theory,
      InlineData(20)]
      public async Task AddRightEndpointTest_Valid(int id)
      {
        var diffData = new WebAppDiff.Model.DiffData(endpointValid);
        var response = await client.PutAsJsonAsync(String.Format(url_rightEndpoint, id), diffData);
        Assert.Equal(response.StatusCode, System.Net.HttpStatusCode.Created);
      }

      [Theory,
      InlineData(21)]
      public async Task AddRightEndpointTest_NotValid(int id)
      {
        var diffData = new WebAppDiff.Model.DiffData(endpointNotValid);
        var response = await client.PutAsJsonAsync(String.Format(url_rightEndpoint, id), diffData);
        Assert.Equal(response.StatusCode, System.Net.HttpStatusCode.BadRequest);
      }

      [Theory,
      InlineData(22)]
      public async Task AddRightEndpointTest_Null(int id)
      {
        var diffData = new WebAppDiff.Model.DiffData(null);
        var response = await client.PutAsJsonAsync(String.Format(url_rightEndpoint, id), diffData);
        Assert.Equal(response.StatusCode, System.Net.HttpStatusCode.BadRequest);
      }

      [Theory,
      InlineData(31)]
      public async Task AddLeftEndpointTest_Valid(int id)
      {
        var diffData = new WebAppDiff.Model.DiffData(endpointValid);
        var response = await client.PutAsJsonAsync(String.Format(url_leftEndpoint, id), diffData);
        Assert.Equal(response.StatusCode, System.Net.HttpStatusCode.Created);
      }


      [Theory,
      InlineData(32)]
      public async Task AddLeftEndpointTest_NotValid(int id)
      {
        var diffData = new WebAppDiff.Model.DiffData(endpointNotValid);
        var response = await client.PutAsJsonAsync(String.Format(url_leftEndpoint, id), diffData);
        Assert.Equal(response.StatusCode, System.Net.HttpStatusCode.BadRequest);
      }

      [Theory,
      InlineData(33)]
      public async Task AddLeftEndpointTest_Null(int id)
      {
        var diffData = new WebAppDiff.Model.DiffData(null);
        var response = await client.PutAsJsonAsync(String.Format(url_leftEndpoint, id), diffData);
        Assert.Equal(response.StatusCode, System.Net.HttpStatusCode.BadRequest);
      }

      [Theory,
      InlineData(40)]
      public async Task Compare_NoEndPoint_NotFound(int id)
      {
        var response = await client.GetAsync(String.Format(url_endpoint, id));
        Assert.Equal(response.StatusCode, System.Net.HttpStatusCode.NotFound);
      }

      [Theory,
      InlineData(32)]
      public async Task Compare_OneEndPoint_NotFound(int id)
      {
        var response = await client.GetAsync(String.Format(url_endpoint, id));
        Assert.Equal(response.StatusCode, System.Net.HttpStatusCode.NotFound);
      }

      [Theory,
      InlineData(32)]
      public async Task Compare_TwoEndPoints_OneValid_NotFound(int id)
      {
        var response = await client.GetAsync(String.Format(url_endpoint, id));
        Assert.Equal(response.StatusCode, System.Net.HttpStatusCode.NotFound);
      }

      [Theory,
      InlineData(1)]
      public async Task Compare_TwoEndPoints_BothValid_Equals(int id)
      { 
        var url = String.Format(url_endpoint, id);
        var response = await client.GetAsync(url);
        var result = await client.GetFromJsonAsync<WebAppDiff.Model.DiffResult>(url);
        Assert.Equal(response.StatusCode, System.Net.HttpStatusCode.OK);
        Assert.True(result.diffResultType == "Equals");
      }

      [Theory,
      InlineData(3)]
      public async Task Compare_TwoEndPoints_BothValid_SizeDoNotMatch(int id)
      {
        var url = String.Format(url_endpoint, id);
        var response = await client.GetAsync(url);
        var result = await client.GetFromJsonAsync<WebAppDiff.Model.DiffResult>(url);
        Assert.Equal(response.StatusCode, System.Net.HttpStatusCode.OK);
        Assert.True(result.diffResultType == "SizeDoNotMatch");
      }

      [Theory,
      InlineData(2)]
      public async Task Compare_TwoEndPoints_BothValid_ContentDoNotMatch(int id)
      {
        var url = String.Format(url_endpoint, id);
        var response = await client.GetAsync(url);
        var result = await client.GetFromJsonAsync<WebAppDiff.Model.DiffResult>(url);
        Assert.Equal(response.StatusCode, System.Net.HttpStatusCode.OK);
        Assert.True(result.diffResultType == "ContentDoNotMatch");
      }
    }
  }
}
#endregion
