using WebAppDiff.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace WebAppDiff.Tests
{
  public class DiffControllerTests
  {
    [Fact]
    public void GetPoints_ReturnsNumberOfPoints()
    {
      WebApplication app = WebApplication.Create();

      app.Run("hostname::3000");
    }
  }
}