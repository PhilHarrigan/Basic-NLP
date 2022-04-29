using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;


namespace NLP_API.Shared
{
    public class SubjectEntity
    {
        public string Subject { get; set; }
        public Dictionary<string, Dictionary<string, List<string>>> data = new Dictionary<string, Dictionary<string, List<string>>>();
        //public List<string> Relations = new();
        public Dictionary<string, List<string>> Relations = new();
        //public List<Tuple<string, string, string, string, string>> allData { get; set; }
        //public Tuple<string, string, string, string, string> subjectData { get; set; }
        public SubjectEntity() { }

        //Overload constructor to only build the Subject field
        public SubjectEntity(string name)
        {
            Subject = name.Trim();
        }
        public SubjectEntity(List<Tuple<string, string, string, string, string>> allData, string name)
        {
            Subject = name.Trim();
            //subjectData = allData[0];
            foreach (var obj in allData)
            {
                if (!data.ContainsKey(obj.Item1.Trim()))
                {
                    Dictionary<string, List<string>> baseData = new Dictionary<string, List<string>>();
                    baseData.Add("relatedSubjects", new List<string> { obj.Item2.Trim() });
                    baseData.Add("secondaryVerbs", new List<string>());
                    baseData["secondaryVerbs"] = obj.Item3.Split(' ').Select(p => p.Trim()).ToList();
                    baseData.Add("article", new List<string> { obj.Item4 });
                    baseData.Add("line", new List<string>() { obj.Item5 });
                    data.Add(obj.Item1.Trim(), baseData);
                    //data[obj.Item1] = baseData;
                }
                else
                {
                    data[obj.Item1]["relatedSubjects"].Add(obj.Item2);
                }
            }
        }
        public void buildDict(List<Tuple<string, string, string, string, string>> TupleData)
        {
            foreach (var obj in TupleData)
            {
                if (obj.Item2.Trim() != "null" && obj.Item2.Trim() != "")
                {
                    if (!Relations.ContainsKey("article" + obj.Item4.Trim()))
                    {
                        Relations.Add("article" + obj.Item4.Trim(), new List<string>());
                        Relations["article" + obj.Item4.Trim()].Add(obj.Item2.Trim());
                    }
                    else
                    {
                        Relations["article" + obj.Item4.Trim()].Add(obj.Item2.Trim());
                    }
                }
                if (!data.ContainsKey(obj.Item1.Trim()))
                {
                    Dictionary<string, List<string>> baseData = new Dictionary<string, List<string>>();
                    baseData.Add("relatedSubjects", new List<string> { obj.Item2.Trim() });
                    baseData.Add("secondaryVerbs", new List<string>());
                    baseData["secondaryVerbs"] = obj.Item3.Split(' ').Select(p => p.Trim()).ToList();
                    baseData.Add("article", new List<string> { "1" });
                    baseData.Add("line", new List<string>() { obj.Item5 });
                    data.Add(obj.Item1, baseData);
                    //data[obj.Item1] = baseData;
                }
                else
                {
                    data[obj.Item1.Trim()]["relatedSubjects"].Add(obj.Item2.Trim());
                }
            }
        }
        public string stringify()
        {
            return JsonSerializer.Serialize(data);
        }
        //public List<string> getKeys()
        //{
        //    List<string> keys = new List<string>();
        //    foreach(string key in data.Keys)
        //    {
        //        keys.Add(key);
        //    }
        //    return keys;
        //}
        //public string getJSON()
        //{
        //    var option = new JsonSerializerOptions { WriteIndented = true };
        //    return JsonSerializer.Serialize(data, option);
        //}
    }
}
