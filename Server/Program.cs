using Microsoft.AspNetCore.ResponseCompression;
using NLP_API.Server;
using NLP_API.Shared;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();


app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

public static class ProgramFunctions
{
    //Original function, takes the table name and returns the full class with the constructor that should build the dictionary (NOT WORKING)
    //public static SubjectEntity getTableData(string tableName)
    //{
    //    string connectionString = @"server=(local)\SQLExpress;database=Basic_NLP;integrated Security=SSPI;";
    //    List<Tuple<string, string, string, string, string>> allData = new List<Tuple<string, string, string, string, string>>();
    //    //Dictionary<string, Dictionary<string, List<string>>> data = new Dictionary<string, Dictionary<string, List<string>>>();
    //    using (SqlConnection con = new SqlConnection(connectionString))
    //    {
    //        SqlCommand cmd = new SqlCommand($"SELECT * FROM [{tableName}]", con);
    //        con.Open();
    //        SqlDataReader reader = cmd.ExecuteReader();
    //        while (reader.Read())
    //        {
    //            Tuple<string, string, string, string, string> x = Tuple.Create((string)reader[1], (string)reader[2], (string)reader[3], reader[4].ToString(), reader[5].ToString());
    //            allData.Add(x);
    //        }
    //        reader.Close();
    //        con.Close();
    //    }
    //    SubjectEntity newSubject = new SubjectEntity(allData, tableName);
    //    return newSubject;
    //}
    //Original function, gets all the subjects from the Subjects table and runs them through the original function getTableData. Returns a list of classes with pre-made dictionaries (NOT WORKING)
    //public static List<SubjectEntity> GetSubjectEntities()
    //{
    //    string connectionString = @"server=(local)\SQLExpress;database=Basic_NLP;integrated Security=SSPI;";
    //    List<string> subjectList = new List<string>();
    //    List<SubjectEntity> subjects = new List<SubjectEntity>();
    //    using (SqlConnection con = new SqlConnection(connectionString))
    //    {
    //        SqlCommand cmd = new SqlCommand("SELECT subject FROM Subjects_Master", con);
    //        con.Open();
    //        SqlDataReader r = cmd.ExecuteReader();
    //        while (r.Read())
    //        {
    //            subjectList.Add((string)r[0]);
    //        }
    //        r.Close();
    //        con.Close();
    //    }
    //    foreach (string subject in subjectList)
    //    {
    //        subjects.Add(getTableData(subject));
    //    }
    //    return subjects;
    //}
    //First overload, takes a random string and builds a list of SubjectEntities without pre-made dictionaries
    public static List<SubjectEntity> GetSubjectEntities()
    {
        string connectionString = @"server=(local)\SQLExpress;database=Basic_NLP;integrated Security=SSPI;";
        List<string> subjectList = new List<string>();
        List<SubjectEntity> subjects = new List<SubjectEntity>();
        using (SqlConnection con = new SqlConnection(connectionString))
        {
            SqlCommand cmd = new SqlCommand("SELECT subject FROM Subjects_Master", con);
            con.Open();
            SqlDataReader r = cmd.ExecuteReader();
            while (r.Read())
            {
                subjectList.Add((string)r[0]);
            }
            r.Close();
            con.Close();
        }
        foreach (string subject in subjectList)
        {
            subjects.Add(getTableData(subject));
        }
        return subjects;
    }
    //First overload, builds a SubjectEntity class with only the "Subject" field populated
    public static SubjectEntity getTableData(string tableName)
    {
        SubjectEntity newSubject = new SubjectEntity(tableName);
        return newSubject;
    }
    public static List<Tuple<string, string, string, string, string>> getTuples(string tableName, int articleNum)
    {
        string connectionString = @"server=(local)\SQLExpress;database=Basic_NLP;integrated Security=SSPI;";
        List<Tuple<string, string, string, string, string>> allData = new List<Tuple<string, string, string, string, string>>();
        //Dictionary<string, Dictionary<string, List<string>>> data = new Dictionary<string, Dictionary<string, List<string>>>();
        using (SqlConnection con = new SqlConnection(connectionString))
        {
            SqlCommand cmd = new SqlCommand($"SELECT * FROM [{tableName.Trim()}] WHERE article = " + articleNum, con);
            con.Open();
            SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Tuple<string, string, string, string, string> x = Tuple.Create((string)reader[1], (string)reader[2], (string)reader[3], reader[4].ToString(), reader[5].ToString());
                allData.Add(x);
            }
            reader.Close();
            con.Close();
        }
        return allData;
    }
    public static string gatherData(string articleTitle, string article)
    {
        WordBank wordbank = new();
        wordbank.Load();
        //Determine which article this is in the database
        Task<int> numCheck = Task.Run<int>(() =>
        {
            int result = 0;
            SqlConnection con = new SqlConnection(@"server=(local)\SQLExpress;database=Basic_NLP;integrated Security=SSPI;");
            con.Open();
            SqlCommand articleNumCheck = new SqlCommand("SELECT TOP 1 * FROM Sentences_Master ORDER BY Article DESC", con);
            SqlDataReader reader = articleNumCheck.ExecuteReader();
            while (reader.Read())
            {
                result = Convert.ToInt32(reader[0]) + 1;
            }
            //if (result == null)
            //    result = 0;
            con.Close();
            con.Open();
            SqlCommand updateArticleTable = new SqlCommand("INSERT INTO Articles_Master (ArticleNumber, ArticleTitle) VALUES (" + result + ", '" + articleTitle + "')", con);
            updateArticleTable.ExecuteNonQuery();
            con.Close();
            return result;
        });
        int articleNum = numCheck.Result;
        //using (SqlConnection con = new SqlConnection(@"server=(local)\SQLExpress;database=Basic_NLP;integrated Security=SSPI;"))
        //{
        //    con.Open();
        //    SqlCommand articleBuild = new SqlCommand("INSERT INTO Articles_Master (ArticleNumber, ArticleTitle) VALUES (" + articleNum + ", '" + articleTitle + "')", con);
        //    articleBuild.ExecuteNonQuery();
        //}
        //WEB SCRAPE THE ARTICLE AND USE THIS AFTER : .Replace("\n", " ").Replace("\r", "").Replace("  ", " ").Replace("...", "").Replace("--", "");
        article = article.Replace("  ", " ").Replace("...", "").Replace("--", "");
        List<int> articleIndexes = new List<int>();
        //string unfixed = article;
        //article = CustomRegex.findSentence.Replace(unfixed, ". ");
        foreach (Match match in CustomRegex.sentenceStart.Matches(article))
        {
            articleIndexes.Add(match.Index);
        }
        for (int i = 0; i <= articleIndexes.Count; i++)
        {
            //The first and last sentences have to performe differently with their substring functions
            if (i == 0)
            {
                string line = article.Substring(0, articleIndexes[i]).Replace("n't", " not").Replace("'re", " are").Replace("'d", " had").Replace("'ll", " will").Replace("'m", " am").Replace("'ve", " have");
                Sentence tempName = new Sentence(line.Trim(), wordbank, i + 1, articleNum);
            }
            else if (i == articleIndexes.Count)
            {
                string line = article.Substring(articleIndexes[i - 1], article.Length - articleIndexes[i - 1]).Replace("n't", " not").Replace("'re", " are").Replace("'d", " had").Replace("'ll", " will").Replace("'m", " am").Replace("'ve", " have");
                Sentence tempName = new Sentence(line.Trim(), wordbank, i + 1, articleNum);
            }
            else
            {
                string line = article.Substring(articleIndexes[i - 1], articleIndexes[i] - articleIndexes[i - 1]).Replace("n't", " not").Replace("'re", " are").Replace("'d", " had").Replace("'ll", " will").Replace("'m", " am").Replace("'ve", " have");
                Sentence tempName = new Sentence(line.Trim(), wordbank, i + 1, articleNum);
            }
        }
        wordbank.Save();
        return "success";
    }
    public static List<Tuple<int,string>> GetArticleList()
    {
        List<Tuple<int,string>> articleList = new();

        using (SqlConnection con = new SqlConnection(@"server=(local)\SQLExpress;database=Basic_NLP;integrated Security=SSPI;"))
        {
            SqlCommand cmd = new SqlCommand("SELECT ArticleNumber, ArticleTitle FROM Articles_Master", con);
            con.Open();
            SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Tuple<int, string> x = Tuple.Create((int)reader[0], reader[1].ToString().Trim());
                articleList.Add(x);
            }
            reader.Close();
            con.Close();
        }
        return articleList;
    }
}
