using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NLP_API.Shared;
namespace NLP_API.Server.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class SubjectEntitiesController : ControllerBase
    {
        [HttpGet("List")]
        public ActionResult List()
        {
            List<SubjectEntity> subjectEntities = ProgramFunctions.GetSubjectEntities();
            return Ok(subjectEntities);
        }
        [HttpGet("articleList")]
        public ActionResult ArticleList()
        {
            List<Tuple<int,string>> articleList = ProgramFunctions.GetArticleList();
            //foreach(var article in articleList)
            //{
            //    Console.WriteLine(article.ToString());
            //}
            return Ok(articleList);
        }
        [HttpGet("sg/{subject}/{articleNum}")]
        public ActionResult BuildDics(string subject, int articleNum)
        {
            List<Tuple<string, string, string, string, string>> testTuples = ProgramFunctions.getTuples(subject, articleNum);
            return Ok(testTuples);
        }
        [HttpGet("{title}/{article}")]
        public ActionResult newArticle(string title, string article)
        {
            Console.WriteLine("test working!");
            Dictionary<string, string> returnValue = new Dictionary<string, string>();
            try
            {
                returnValue["status"] = ProgramFunctions.gatherData(title, article);
            }
            catch (Exception ex)
            {
                returnValue["status"] = "failed";
            }
            return Ok(returnValue);
        }
    }
}
