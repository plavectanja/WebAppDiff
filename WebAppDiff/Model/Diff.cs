using System.Text;
using Microsoft.EntityFrameworkCore;
using Xunit;

#region UnitTests
namespace WebAppDiff.UnitTests
{
  public class DiffTests
  {
    public class DiffTestsDb
    {
      readonly DbContextOptions<Model.DiffDb> dbContextOptions;
      public Model.DiffDb db;

      public DiffTestsDb()
      {
        // Build DbContextOptions
        dbContextOptions = new DbContextOptionsBuilder<Model.DiffDb>()
            .UseInMemoryDatabase(databaseName: "DiffDbForTesting")
            .Options;

        db = new Model.DiffDb(dbContextOptions);
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();

        InitData(ref db);       
      }

      public static void InitData(ref WebAppDiff.Model.DiffDb db)
      {
        SaveDiff(ref db, 1, "AAAAAA==", "AAAAAA==");
        SaveDiff(ref db, 2, "AAAAAA==", "AQABAQ==");
        SaveDiff(ref db, 3, "AAAAAA==", "AAA=");
        SaveDiff(ref db, 4, "AAAAAA==", null);
        SaveDiff(ref db, 5, "AAAAAA==", "AAA==");
        SaveDiff(ref db, 6, "AAAAAA==", "AAAAAA==");
      }

      public static bool SaveDiff(ref WebAppDiff.Model.DiffDb db, int id, string left, string right)
      {
        bool isSaved = false;
        try
        {
          var diff = new Model.Diff(id);
          if (left != null)
          {
            var dataL = new Model.DiffData(left);
            diff.SaveLeft(dataL);
          }
          if (right != null)
          {
            var dataR = new Model.DiffData(right);
            diff.SaveRight(dataR);
          }
          db.Diffs.Add(diff);
          db.SaveChanges();
          isSaved = true;
        }
        catch { }

        return isSaved;
      }
    }

    DiffTestsDb TestDb;

    public DiffTests()
    {
      TestDb = new DiffTestsDb();
    }

    [Theory,
      InlineData(10)]
    public void Diff_Not_Found(int id)
    {
      Assert.Equal(TestDb.db.Diffs.Count(d => d.Id == id), 0);
    }

    [Theory,
      InlineData(20, "AAAAAA==", "AAAAAA==")]
    public async Task Diff_NewCreated(int id, string left, string right)
    {
      bool isCreated = false;
      if (TestDb.db.Diffs.Count(d => d.Id == id) == 0)
      {
        var diff = new Model.Diff(id);
        if (diff.SaveLeft(new Model.DiffData(left)) && diff.SaveRight(new Model.DiffData(right)))
        {
          await TestDb.db.Diffs.AddAsync(diff);
          await TestDb.db.SaveChangesAsync();
          isCreated = true;
        }
      }
      Assert.True(isCreated && TestDb.db.Diffs.Count(d => d.Id == id) > 0);
    }

    [Theory,
      InlineData(30, "AAAAAA==")]
    public async Task Diff_LeftCreated_Ok(int id, string data)
    {
      bool isCreated = false;
      if (TestDb.db.Diffs.Count(d => d.Id == id) == 0)
      {
        var diff = new Model.Diff(id);
        if (diff.SaveLeft(new Model.DiffData(data)))
        {
          await TestDb.db.AddAsync(diff);
          await TestDb.db.SaveChangesAsync();
          isCreated = true;
        }
      }
      Assert.True(isCreated && TestDb.db.Diffs.Count(d => d.Id == id) > 0);
    }

    [Theory,
      InlineData(31, "AAAAAA==")]
    public async Task Diff_RightCreated_Ok(int id, string data)
    {
      bool isCreated = false;
      if (TestDb.db.Diffs.Count(d => d.Id == id) == 0)
      {
        var diff = new Model.Diff(id);
        if (diff.SaveRight(new Model.DiffData(data)))
        {
          await TestDb.db.AddAsync(diff);
          await TestDb.db.SaveChangesAsync();
          isCreated = true;
        }
      }
      Assert.True(isCreated && TestDb.db.Diffs.Count(d => d.Id == id) > 0);    
    }

    [Theory,
      InlineData(32, null)]
    public async Task Diff_LeftNotCreated_NullData(int id, string data)
    {
      bool isFound = true, isCreated = false;
      if (TestDb.db.Diffs.Count(d => d.Id == id) == 0)
      {
        isFound = false;
        var diff = new Model.Diff(id);
        if (diff.SaveLeft(new Model.DiffData(data)))
        {
          await TestDb.db.AddAsync(diff);
          await TestDb.db.SaveChangesAsync();
          isCreated = true;
        }
      }
      Assert.True(!isFound && !isCreated && TestDb.db.Diffs.Count(d => d.Id == id) == 0);
    }

    [Theory,
      InlineData(32, "AAA==")]
    public async Task Diff_LeftNotCreated_InvalidData(int id, string data)
    {
      bool isFound = true, isCreated = false;
      if (TestDb.db.Diffs.Count(d => d.Id == id) == 0)
      {
        isFound = false;
        var diff = new Model.Diff(id);
        if (diff.SaveLeft(new Model.DiffData(data)))
        {
          await TestDb.db.AddAsync(diff);
          await TestDb.db.SaveChangesAsync();
          isCreated = true;
        }
      }
      Assert.True(!isFound && !isCreated && TestDb.db.Diffs.Count(d => d.Id == id) == 0);
    }

    [Theory,
      InlineData(6, "AAAAAA==", "AQABAQ==")]
    public async Task Diff_ExistingEdited(int id, string left, string right)
    {
      bool isEdited = false;
      var diff = TestDb.db.Diffs.First(d => d.Id == id);
      if (diff != null)
      {
        if (diff.SaveLeft(new Model.DiffData(left)) && diff.SaveRight(new Model.DiffData(right)))
        {
          TestDb.db.Diffs.Update(diff);
          await TestDb.db.SaveChangesAsync();
          isEdited = true;
        }
      }
      Assert.True(isEdited);
    }    

    [Theory,
      InlineData(1)]
    public void Diff_Endpoints_Equals(int id)
    {
      string type = "";
      var diff = TestDb.db.Diffs.First(d => d.Id == id);
      if (diff != null)
      {
        Model.DiffResult diffRes = new Model.DiffResult();
        diff.Compare(ref diffRes);
        type = diffRes.diffResultType;
      }
      Assert.Equal(type, "Equals");
    }

    [Theory,
      InlineData(2)]
    public void Diff_Endpoints_ContentDoNotMatch(int id)
    {
      string type = "";
      var res = TestDb.db.Diffs.First(d => d.Id == id);
      if (res != null)
      {
        Model.DiffResult diffRes = new Model.DiffResult();
        res.Compare(ref diffRes);
        type = diffRes.diffResultType;
      }
      Assert.Equal(type, "ContentDoNotMatch");
    }

    [Theory,
      InlineData(3)]
    public void Diff_Endpoints_SizeDoNotMatch(int id)
    {
      string type = "";
      var res = TestDb.db.Diffs.First(d => d.Id == id);
      if (res != null)
      {
        Model.DiffResult diffRes = new Model.DiffResult();
        res.Compare(ref diffRes);
        type = diffRes.diffResultType;
      }
      Assert.Equal(type, "SizeDoNotMatch");
    }
  }
}
#endregion

#region InternalLogic
namespace WebAppDiff.Model
{  
  /// <summary>
  /// InMemory databese to save endpoints.
  /// </summary>
  public class DiffDb : DbContext
  {
    public DiffDb(DbContextOptions<DiffDb> options)
        : base(options) { }

    public DbSet<Diff> Diffs => Set<Diff>();
  }

  /// <summary>
  /// Class that represents left and right endpoint and has method to return differences between them.
  /// </summary>
  public class Diff
  {
    public Diff() { }
    public Diff(int newId)
    {
      Id = newId;
    }

    public int Id { get; set; }
    /// <summary>
    /// Left endpoint.
    /// </summary>
    public DiffData Left { get; set; }
    /// <summary>
    /// Right endpoint.
    /// </summary>
    public DiffData Right { get; set; }

    /// <summary>
    /// Save left endpoint. Decode its value first (UTF8, base64). If value is not valid base64 encoded, data is set to new object.
    /// </summary>
    /// <param name="newData">Endpoint new data value.</param>
    /// <returns>Return true if data not null.</returns>
    public bool SaveLeft(DiffData newData)
    {
      Left = new DiffData();
      if (newData.DecodeData())
      {
        Left = newData;
      }
      return (Left.Data != null);
    }
    /// <summary>
    /// Save right endpoint. Decode its value first (UTF8, base64). If value is not valid base64 encoded, data is set to new object.
    /// </summary>
    /// <param name="newData">Endpoint new data value.</param>
    /// <returns>Return true if data not null.</returns>
    public bool SaveRight(DiffData newData)
    {
      Right = new DiffData();
      if (newData.DecodeData())
      {
        Right = newData;
      }
      return (Right.Data != null);
    }
    /// <summary>
    /// Get differences between left and right endpoint.
    /// </summary>
    /// <param name="diffRes"></param>
    /// <returns>Return true if both endpoints are created (data not null).</returns>
    public bool GetDiffs(ref DiffResult diffRes)
    {
      if (!Left.IsCreated || !Right.IsCreated)
      {
        return false;
      }
      Compare(ref diffRes);
      return true;
    }
    /// <summary>
    /// Compare left and right endpoint data.
    /// </summary>
    /// <param name="diffRes">Result differences and difference type.</param>
    public void Compare(ref DiffResult diffRes)
    {
      diffRes = new DiffResult();
      if (Left.Data.Length > 0 && Left.Data.Length == Right.Data.Length)
      {
        int i0 = -1, len = 0;
        diffRes.diffs = new List<DiffDetails>();
        for (int i = 0; i < Left.Data.Length; ++i)
        {
          if (Left.Data[i] != Right.Data[i])
          {
            if (i0 < 0)
            {
              i0 = i;
            }
            ++len;
          }
          else
          {
            if (i0 > -1)
            {
              diffRes.diffs.Add(new DiffDetails
              {
                offset = i0,
                length = len
              });
              i0 = -1;
              len = 0;
            }
          }
        }
        if (i0 > -1)
        {
          diffRes.diffs.Add(new DiffDetails
          {
            offset = i0,
            length = len
          });
        }
        if (diffRes.diffs.Count > 0)
        {
          diffRes.diffResultType = "ContentDoNotMatch";
        }
        else
        {
          diffRes.diffResultType = "Equals";
        }
      }
      else
      {
        diffRes.diffResultType = "SizeDoNotMatch";
      }
    }
  }

  /// <summary>
  /// Structure for endpoint data. Input data value is base64 encoded.
  /// </summary>
  public struct DiffData
  {
    /// <summary>
    /// Creates new DiffData object. Parameter isCreated is set to false (data needs to be decoded first).
    /// </summary>
    /// <param name="val">Value of Data.</param>
    public DiffData(string val)
    {
      Data = val;
      IsCreated = false; //not encoded yet
    }

    /// <summary>
    /// Contains data of endpoint.
    /// </summary>
    public string Data { get; set; }
    /// <summary>
    /// True if Data not null and Data value decoded (UTF8, base64).
    /// </summary>
    public bool IsCreated { get; set; }

    /// <summary>
    /// Decode data (UTF8, base64).
    /// </summary>
    /// <returns>Returns true if data not null.</returns>
    public bool DecodeData()
    {
      IsCreated = false;
      string val = Data;
      if (val != null)
      {
        try
        {
          Data = Encoding.UTF8.GetString(Convert.FromBase64String(val));
          IsCreated = true;
        }
        catch { }
      }
      return IsCreated;
    }
  }

  /// <summary>
  /// Structure for diff results.
  /// </summary>
  public struct DiffResult
  {
    /// <summary>
    /// Diff result type:
    /// - "Equals": left and right endpoint are equal.
    /// - "SizeDoNotMatch": left and rigth endpoint have data with different length.
    /// - "ContentDoNotMatch": left and right endpoint have data with same length but contents do not match.
    /// </summary>
    public string diffResultType { get; set; }
    public List<DiffDetails> diffs { get; set; }
  }

  /// <summary>
  /// Structure for diff results. Just result type shown.
  /// </summary>
  public struct DiffResultDT0
  {
    /// <summary>
    /// Diff result type:
    /// - "Equals": left and right endpoint are equal.
    /// - "SizeDoNotMatch": left and rigth endpoint have data with different length.
    /// - "ContentDoNotMatch": left and right endpoint have data with same length but contents do not match.
    /// </summary>
    public string diffResultType { get; set; }
  }

  /// <summary>
  /// Structure that represents diff details (offset and length).
  /// </summary>
  public struct DiffDetails
  {
    /// <summary>
    /// Position of first char of different substring.
    /// </summary>
    public long offset { get; set; }
    /// <summary>
    /// Length of different substring.
    /// </summary>
    public long length { get; set; }
  }
}
#endregion
