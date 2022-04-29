using System.Collections;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NLP_API.Server
{
    public class Sentence
    {
        string _rawText;
        string _sentence;
        int _lineNumber;
        int _article;
        List<Tuple<string, string, int>> _subject = new List<Tuple<string, string, int>>();
        List<Tuple<string, string, int>> _object = new List<Tuple<string, string, int>>();
        List<Tuple<string, string, int>> _primaryVerbs = new List<Tuple<string, string, int>>();
        List<Tuple<string, string, int>> _additionalVerbs = new List<Tuple<string, string, int>>();
        List<Tuple<string, string, int>[]> _clausesArray = new List<Tuple<string, string, int>[]>();

        //Dictionary of the classes formatted:
        //          {"clause#" : {
        //                       { "index" : # }
        //                       { "independent" : # } (1 or 0 used as bool)
        Dictionary<string, Dictionary<string, byte>> _clausesDict = new Dictionary<string, Dictionary<string, byte>>();

        //---------------------------------------------------------------------- Class Constructor ----------------------------------------------------------------------
        public Sentence(string rawText, WordBank wordBank, int lineNumber, int article)
        {
            _rawText = rawText.Replace("'", "’").Replace("\"", "”");
            _lineNumber = lineNumber;
            _sentence = CustomRegex.replacePattern.Replace(rawText, "").Replace("  ", " ").Replace(" - ", " ");
            _article = article;
            //text is automatically run through the NLP
            ClauseBuild(wordBank);
        }
        //---------------------------------------------------------------------- Print Results ----------------------------------------------------------------------
        public void PrintResults()
        {
            Console.WriteLine("\nIdentified Subjects:");
            foreach (var item in _subject)
                Console.WriteLine(item);
            Console.WriteLine("\nPrimary Verbs:");
            foreach (var item in _primaryVerbs)
                Console.WriteLine(item);
            Console.WriteLine("\nIdentified Verbs:");
            foreach (var item in _additionalVerbs)
                Console.WriteLine(item);
            Console.WriteLine("\nIdentified Objects:");
            foreach (var item in _object)
                Console.WriteLine(item);
        }
        public void PrintJSON()
        {
            var option = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(_clausesDict, option);
            Console.WriteLine(jsonString);
        }
        #region NLP --------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        #region Remove Punctuation, Determine Word Usage, and Build Clauses -----------------------------------------------------------------------------------------------------------------
        //------------------------------------------------------------ Remove Punctuation, Determine Word Usage, and Build Clauses ------------------------------------------------------------
        public void wordLookup(string entry, WordBank wordBank)
        {
            string newURL = "https://www.merriam-webster.com/dictionary/" + entry;
            var response = CallUrl(newURL).Result;
            ParseHtml(response, entry, wordBank, 0);
        }
        public void ClauseBuild(WordBank wordBank)
        {
            byte clausePosition = 0;
            string[] _rawClauses = _sentence.Split(",");
            foreach (string clauseUntrimmed in _rawClauses)
            {
                string clause = clauseUntrimmed.Trim();
                //Break the line up into individual words
                List<string> entries = new List<string>();
                entries = clause.Split(' ').ToList();
                //whitespace or null value protection. This lambda removes any empty or white space filled entries
                entries = entries.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                string[] rawEntries = new string[entries.Count];
                //strip additional punctuation/characters and change all text to lower case
                for (int i = 0; i < entries.Count; i++)
                {
                    entries[i] = CustomRegex.replacePattern.Replace(entries[i], "").Trim('.').ToLower();
                    rawEntries[i] = CustomRegex.replacePattern.Replace(entries[i], "").Trim('.').ToLower();
                }
                #region Check the word against the JSON. If it isn't there, call web scraping -----------------------------------------------------------------------------------------------
                //Check if the word is already in the JSON. If not, then call the web scraping functions
                foreach (string entry in rawEntries)
                {
                    if (wordBank.CheckWord(entry.Replace("'s", "").Replace("'", "").Replace("“", "").Replace("”", "").Replace("’s", "").Replace("’", "").Replace("‘", "")) == false)
                    {
                        //Check if the word might be possesive and if so, remove the 's and get the type. Or remove any superfluous ' characters
                        if (entry.EndsWith("'s") || entry.EndsWith("’s"))
                        {
                            if (wordBank.CheckWord(entry.Replace("'s", "").Replace("’s", "")))
                            {
                                wordBank.AddWord(entry, new string[] { "possessive noun" });
                            }
                            else
                            {
                                wordLookup(entry.Replace("'s", "").Replace("'", "").Replace("’s", ""), wordBank);
                            }
                        }
                        //Sometimes a superfluous "'" character gets though. This checks for that before looking the word up
                        else if (entry.Contains("'") || entry.Contains("\"") || entry.Contains("“") || entry.Contains("”"))
                        {
                            string removeExtra = entry.Replace("'", "").Replace("“", "").Replace("”", "");
                            wordLookup(removeExtra, wordBank);
                        }
                        //Protect against hyphenated words
                        else if (entry.Contains("-"))
                        {
                            /*Check if the hyphenated word is in the list of hyphenated words that are not recognized by the dictionary. If so, then the individual
                            words have already been defined*/
                            if (wordBank.CheckHyphenatedWord(entry))
                            {
                                string[] splitHyphen = entry.Split("-");
                                entries.Insert(entries.IndexOf(entry), splitHyphen[0]);
                                entries.Insert(entries.IndexOf(entry), "and");
                                entries.Insert(entries.IndexOf(entry), splitHyphen[1]);
                                entries.Remove(entry);
                            }
                            //if the word isn't a known unknown, check if the hyphenated word is in the online dictionary. If so, add it. If not, split the word and define it
                            else
                            {
                                var response = CallUrl(entry).Result;
                                if (checkHtmlParse(response) == false)
                                {
                                    //If the hyphenated word isn't recognized, then split the word up and replace the hyphen with "and"
                                    string[] splitHyphen = entry.Split("-");
                                    entries.Insert(entries.IndexOf(entry), splitHyphen[0]);
                                    entries.Insert(entries.IndexOf(entry), "and");
                                    entries.Insert(entries.IndexOf(entry), splitHyphen[1]);
                                    //check for single word in JSON before looking it up
                                    if (wordBank.CheckWord(splitHyphen[0]) == false)
                                        wordLookup(splitHyphen[0], wordBank);
                                    //check for single word in JSON before looking it up
                                    if (wordBank.CheckWord(splitHyphen[1]) == false)
                                        wordLookup(splitHyphen[1], wordBank);
                                    entries.Remove(entry);
                                }
                                else { wordLookup(entry, wordBank); }
                            }
                        }
                        //Protect against abbreviations
                        else if (entry.Contains("."))
                        {
                            wordBank.AddWord(entry, new string[] { "noun abbreviation" });
                        }
                        //Check for plural words that end with "y"
                        else if (entry.EndsWith("ies"))
                            if (wordBank.CheckWord(entry.Replace("ies", "y")))
                                wordBank.AddWord(entry, new string[] { "plural noun" });
                            else
                            {
                                wordLookup(entry, wordBank);
                                if (wordBank.CheckWord(entry) == false)
                                    wordBank.AddWord(entry, new string[] { "plural noun" });
                            }
                        //Check if the word is a known noun that has just been made plural
                        else if (entry.EndsWith('s'))
                        {
                            string possSingular = entry.Trim('s');
                            if (wordBank.CheckWord(possSingular))
                                wordBank.AddWord(entry, new string[] { "plural noun" });
                            else
                            {
                                wordLookup(entry, wordBank);
                                if (wordBank.CheckWord(entry) == false)
                                    wordBank.AddWord(entry, new string[] { "plural noun" });
                            }
                        }
                        //Check if the word is a known verb that is being shown active
                        else if (entry.EndsWith("ing"))
                            if (wordBank.CheckWord(entry.Replace("ing", "")))
                                wordBank.AddWord(entry, new string[] { "active verb" });
                            else
                            {
                                wordLookup(entry, wordBank);
                                if (wordBank.CheckWord(entry) == false)
                                    wordBank.AddWord(entry, new string[] { "active verb" });
                            }
                        //Check if the word is a known verb that is being used in the past tense
                        else if (entry.EndsWith("ed"))
                            if (wordBank.CheckWord(entry.Replace("ed", "")))
                                wordBank.AddWord(entry, new string[] { "past tense verb" });
                            else
                            {
                                wordLookup(entry, wordBank);
                                if (wordBank.CheckWord(entry) == false)
                                    wordBank.AddWord(entry, new string[] { "past tense verb" });
                            }
                        //If the word doesn't meet any of the above parameters, look it up in the online dictionary
                        else
                            wordLookup(entry, wordBank);
                    }
                }
                #endregion (Check the word against the JSON. If it isn't there, call web scraping)---------------------------------------------------------------------------------------------------------------------------------------------------------------------
                #region Find how the word is being used based on the dictionary in WordBank -------------------------------------------------------------------------------------------------
                //find how each word in the array is used by calling the dictionary in WordBank
                Tuple<string, string, int>[] sentence = new Tuple<string, string, int>[entries.Count];
                //words can be modified by the word that precedes them. ie. An adjective will always be followed by a noun
                string previousWord = "";
                for (int i = 0; i < entries.Count; i++)
                {
                    //Guards against ' characters in the type lookup
                    if (entries[i].Contains("'"))
                    {
                        //if the word ends with 's and is a noun, change the type to "plural noun"
                        if (entries[i].EndsWith("'s") || entries[i].EndsWith("s'"))
                        {
                            string entryStripped = entries[i].Replace("'s", "").Replace("'", "");
                            if (wordBank.CheckWord(entryStripped))
                            {
                                Tuple<string, string, int> wordTuple = Tuple.Create(entries[i], "posessive noun", i);
                                sentence[i] = wordTuple;
                                previousWord = "possesive noun";
                            }
                            else if (wordBank.FindWordType(entryStripped) == "plural noun")
                            {
                                Tuple<string, string, int> wordTuple = Tuple.Create(entries[i], "plural posessive noun", i);
                                sentence[i] = wordTuple;
                                previousWord = "plural possesive noun";
                            }
                        }
                        else
                        {
                            string entryStripped = entries[i].Replace("'", "");
                            string wordType = wordBank.FindWordType(entryStripped);
                            //If the word is a verb (and not an adverb) check the word type before. If the word type before is an adjective, then this word is being used as a noun instead
                            if (wordType.Contains("verb") && !wordType.Contains("adverb"))
                            {
                                if (!previousWord.Contains("adjective"))
                                {
                                    Tuple<string, string, int> wordTuple = Tuple.Create(entryStripped, wordType, i);
                                    sentence[i] = wordTuple;
                                    previousWord = wordType;
                                }
                                else
                                {
                                    Tuple<string, string, int> wordTuple = Tuple.Create(entryStripped, "noun", i);
                                    sentence[i] = wordTuple;
                                    previousWord = "noun";
                                }
                            }
                            else
                            {
                                Tuple<string, string, int> wordTuple = Tuple.Create(entryStripped, wordType, i);
                                sentence[i] = wordTuple;
                                previousWord = wordType;
                            }
                        }
                    }
                    //if the word isn't possesive and doesn't have a superfluous ' character, determine what type of word it is
                    else
                    {
                        string wordType = wordBank.FindWordType(entries[i]);
                        //If the word is a verb (and not an adverb) check the word type before. If the word type before is an adjective, then this word is being used as a noun instead
                        if (wordType.Contains("verb") && !wordType.Contains("adverb"))
                        {
                            if (!previousWord.Contains("adjective"))
                            {
                                Tuple<string, string, int> wordTuple = Tuple.Create(entries[i], wordType, i);
                                sentence[i] = wordTuple;
                                previousWord = wordType;
                            }
                            else
                            {
                                Tuple<string, string, int> wordTuple = Tuple.Create(entries[i], "noun", i);
                                sentence[i] = wordTuple;
                                previousWord = "noun";
                            }
                        }
                        else
                        {
                            Tuple<string, string, int> wordTuple = Tuple.Create(entries[i], wordType, i);
                            sentence[i] = wordTuple;
                            previousWord = wordType;
                        }
                    }
                }
                #endregion (Find how the word is being used based on the dictionary in WordBank)---------------------------------------------------------------------------------------------------------------------------------------------------------------------
                _clausesArray.Add(sentence);
                ClauseFinder(sentence, clausePosition);
                clausePosition += 1;
            }
            sentenceBuild();
        }
        #endregion (Remove Punctuation, Determine Word Usage, and Build Clauses)---------------------------------------------------------------------------------------------------------------------------------------------------------------------
        #region CLAUSE FINDER --------------------------------------------------------------------------------------------------------------------------------------------------------------
        //---------------------------------------------------------------------- Clause Finder ----------------------------------------------------------------------
        public void ClauseFinder(Tuple<string, string, int>[] sentence, byte clausePosition)
        {
            int verbPlace;
            string firstWord = "";
            byte independent = 0;
            string dictKey = "clause" + _clausesDict.Count();
            if (sentence[0].Item2.Contains("verb"))
            {
                Dictionary<string, byte> clauseValues = new Dictionary<string, byte>()
            {
                { "index", clausePosition },
                {"independent", independent}
            };
                _clausesDict.Add(dictKey, clauseValues);
            }
            else
            {
                foreach (Tuple<string, string, int> word in sentence)
                {
                    if (word.Item2.Contains("verb") && firstWord.Contains("adjective") == false && firstWord.Contains("definite article") == false && firstWord != "")
                    {
                        verbPlace = word.Item3;
                        independent = 1;
                    }
                    firstWord = word.Item2;
                }
                Dictionary<string, byte> clauseValues = new Dictionary<string, byte>()
            {
                { "index", clausePosition },
                {"independent", independent}
            };
                _clausesDict.Add(dictKey, clauseValues);
            }

        }
        #endregion (CLAUSE FINDER)---------------------------------------------------------------------------------------------------------------------------------------------------------------------
        #region IDENTIFY VERBS, SUBJECTS, and OBJECTS AND ADD THEM TO THE DATABASE ---------------------------------------------------------------------------------------------------------
        //---------------------------------------------------------------------- Identify Verbs, Subjects, and Objects ----------------------------------------------------------------------
        public void sentenceBuild()
        {
            bool verbFound = false;
            List<string> subjects = new List<string>();
            List<string> objects = new List<string>();
            string primaryVerb = "";
            List<string> secondaryVerbs = new List<string>();
            StringBuilder newSubject = new StringBuilder(100);
            StringBuilder newObject = new StringBuilder(100);
            #region Determine how the word is being used in the sentence -------------------------------------------------------------------------------------------------------------------
            // Version that builds classes of extracted subjects, their primary verbs, and their objects
            foreach (string clause in _clausesDict.Keys)
            {
                //Before finding the primary verb, build out the subject(s) using conjunctions to split them if there are more than one
                foreach (Tuple<string, string, int> word in _clausesArray[_clausesDict[clause]["index"]])
                {
                    //If the verb hasn't been found and the clause is independent, then the identified adjectives/nouns are part of the subject and the verb is the primary verb
                    if (!verbFound && _clausesDict[clause]["independent"] == 1)
                    {
                        if (word.Item2.Contains("noun") || word.Item2.Contains("adjective") || word.Item2.Contains("article"))
                        {
                            if (word.Item1.Contains("'"))
                            {
                                subjects.Add(word.Item1.Replace("'s", "").Replace("'", ""));
                            }
                            newSubject.Append(word.Item1 + " ");
                            _subject.Add(word);
                        }
                        else if (word.Item2.Contains("conjunction"))
                        {
                            subjects.Add(newSubject.ToString().Trim());
                            newSubject.Clear();
                            _object.Add(word);
                        }
                        else if (word.Item2.Contains("verb") && !word.Item2.Contains("adverb"))
                        {
                            verbFound = true;
                            subjects.Add(newSubject.ToString().Trim());
                            newSubject.Clear();
                            primaryVerb = word.Item1;
                            _primaryVerbs.Add(word);
                        }
                    }
                    //If the verb has been found or if the clause is dependent, then the adjectives/nouns are part of the object and verbs are secondary
                    //Temporarily change all of the "newObject.Append" associated with nouns or adjectives to "newSubject.Append" this will add all of the found objects as subjects
                    else
                    {
                        if (word.Item2.Contains("noun") || word.Item2.Contains("adjective"))
                        {
                            newSubject.Append(word.Item1 + " ");
                            _object.Add(word);
                        }
                        else if ((word.Item2.Contains("verb") && !word.Item2.Contains("adverb")) || word.Item2.Contains("conjunction"))
                        {
                            secondaryVerbs.Add(word.Item1);
                            newObject.Append(word.Item1 + " ");
                            _additionalVerbs.Add(word);
                            subjects.Add(newSubject.ToString().Trim());
                            newSubject.Clear();
                        }
                        else
                        {
                            newObject.Append(word.Item1 + " ");
                            _object.Add(word);
                            //if (subjects.Count > 0)
                            //{
                            //    subjects.Add(newSubject.ToString().Trim());
                            //    newSubject.Clear();
                            //}
                        }
                    }
                    objects.Add(newObject.ToString().Trim());
                }
            }
            #endregion (Determine how the word is being used in the sentence)---------------------------------------------------------------------------------------------------------------------------------------------------------------------

            #region Add the extracted entities along with the primary verb and objects to the database
            //Connect to database to store the extracted objects
            string connectionString = @"server=(local)\SQLExpress;database=Basic_NLP;integrated Security=SSPI;";
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                //Add the current sentence to the Sentences table
                string sentenceAdd = "INSERT INTO dbo.Sentences_Master (Article, Line, Sentence) VALUES (" + _article + ", '" + _lineNumber + "', '" + _rawText + "')";
                SqlCommand sentenceCMD = new SqlCommand(sentenceAdd, con);
                sentenceCMD.ExecuteNonQuery();
                //Each subject needs to become its own table and each needs to be created on the linking table
                List<string> tableNames = new();
                foreach (string subject in subjects)
                {
                    if (subject != "")
                    {
                        string tableName = subject.Replace(".", "").Replace("-", "").Replace("'", "").Trim();
                        #region PATTERN MATCHING FOR SUBJECT TABLES -------------------------------------------------------------------------------------------------------------------------
                        //----------------------------------------PATERN MATCHING TO FIND SIMILAR SUBJECTS AND CHECK IF THIS ONE SHOULD BE INCLUDED--------------------------------------
                        string containsLikeSearch = "SELECT subject FROM dbo.Subjects_Master WHERE ";
                        var tableNameSplit = tableName.Split(' ');
                        foreach (string name in tableNameSplit)
                        {
                            containsLikeSearch += "subject LIKE '%" + name + "%' OR ";
                        }
                        containsLikeSearch = containsLikeSearch.Substring(0, containsLikeSearch.Length - 4);
                        containsLikeSearch += " AND article = " + _article;
                        SqlCommand checkCommand = new SqlCommand(containsLikeSearch, con);
                        DataTable customTable = new DataTable("SimilarNames");
                        SqlDataAdapter adapter = new SqlDataAdapter(checkCommand);
                        adapter.Fill(customTable);
                        List<string> possTableNames = new();
                        foreach (DataRow row in customTable.Rows)
                        {
                            possTableNames.Add((string)row[0]);
                        }
                        bool changed = false;
                        foreach (string possibleName in possTableNames)
                        {
                            if (possibleName.Contains(tableName))
                            {
                                changed = true;
                                tableNames.Add(possibleName.Trim());
                            }
                        }
                        if (!changed)
                        {
                            //Search only by the last word in tableName (subject)
                            string[] subjectSplit = tableName.Split(' ');
                            string likeSearch = "SELECT subject FROM dbo.Subjects_Master WHERE subject LIKE '%" + subjectSplit[subjectSplit.Length - 1] + "%' AND article = " + _article;
                            SqlCommand cmd = new SqlCommand(likeSearch, con);
                            adapter = new SqlDataAdapter(cmd);
                            adapter.Fill(customTable);
                            if (customTable.Rows.Count != 0)
                            {
                                //If tables come back in the LIKE search, compare them to the current tableName (subject) and see if they might be the same entity
                                for (int i = 0; i < customTable.Rows.Count; i++)
                                {
                                    //possWord is the entry in the row that this subject might be
                                    string possWord = (string)customTable.Rows[i][0];
                                    string[] possWordArray = possWord.Split(' ');
                                    //If the current tableName (subject) is the same as the last word in the table check (i.e. "Biden" and "President Biden") then make them the same entity
                                    if (tableName.Split(" ")[tableName.Split(" ").Length - 1] == possWordArray[possWordArray.Length - 1])
                                    {
                                        tableNames.Add(possWord.Trim());
                                        continue;
                                    }
                                    //If tableName is more than one word, only check the last word
                                    else if (tableName.Split(' ').Length > 1)
                                    {
                                        if (tableName.Split(' ')[tableName.Split(' ').Length - 1] == possWordArray[possWordArray.Length - 1])
                                            tableNames.Add(possWord.Trim());
                                    }
                                    //If the tableName hasn't changed from the initial subject and no other table has met the matching critera, a new table has to be made
                                    if (tableName == subject.Replace(".", "").Replace("-", "").Replace("'", "") && i == customTable.Rows.Count - 1)
                                    {
                                        //Add the subject to the linking table
                                        string updateSubjects = "INSERT INTO dbo.Subjects_Master (subject, article) VALUES ('" + tableName + "', " + _article + ")";
                                        SqlCommand insertcmd = new SqlCommand(updateSubjects, con);
                                        insertcmd.ExecuteNonQuery();
                                        //If there isn't already a table with this subject name, create it
                                        string sql = "IF NOT EXISTS (SELECT name FROM sysobjects WHERE name = '" + tableName + "' AND xtype='U') CREATE TABLE dbo.[" + tableName + "] (subject nvarchar(2000), primaryVerb nvarchar(200), linkedSubject nvarchar(2000), secondaryVerb nvarchar(2000), article int, line int)";
                                        SqlCommand cmd2 = new SqlCommand(sql, con);
                                        cmd2.ExecuteNonQuery();
                                        tableNames.Add(tableName.Trim());
                                    }
                                }
                            }
                            //If none of the above matching critera were met, a new table has to be made
                            else
                            {
                                if (tableName.Length > 100)
                                {
                                    tableName = tableName.Substring(0, 99);
                                }
                                //Add the subject to the linking table
                                string updateSubjects = "INSERT INTO dbo.Subjects_Master (subject, article) VALUES ('" + tableName + "', " + _article + ")";
                                SqlCommand insertcmd = new SqlCommand(updateSubjects, con);
                                insertcmd.ExecuteNonQuery();
                                //If there isn't already a table with this subject name, create it
                                //string sql = "IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = '[" + tableName + "]' AND xtype='U') CREATE TABLE dbo.[";
                                //sql += tableName + "] (linkedSubjects text, primaryVerbs text, secondaryVerbs text, objects text)";
                                string sql = "IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = '[" + tableName + "]' AND xtype='U') CREATE TABLE dbo.[" + tableName + "] (subject nvarchar(2000), primaryVerb nvarchar(200), linkedSubject nvarchar(2000), secondaryVerb nvarchar(2000), article int, line int)";
                                SqlCommand cmd2 = new SqlCommand(sql, con);
                                cmd2.ExecuteNonQuery();
                                tableNames.Add(tableName.Trim());
                            }
                        }
                    }
                }
                foreach (string tableName in tableNames)
                {
                    //-----------------------------------------------------------END PATTERN MATCHING------------------------------------------------------------------------------
                    #endregion---------------------------------------------------------------------------------------------------------------------------------------------------------------------

                    //Insert the data into the table, this requires a second loop to get the additional subjects to link
                    if (tableNames.Count > 1)
                    {
                        foreach (string otherTable in tableNames)
                        {
                            if (otherTable != tableName)
                            {
                                string sqlData = "INSERT INTO dbo.[" + tableName.Trim() + "] (subject, primaryVerb, linkedSubject, secondaryVerb, article, line) VALUES ('" + tableName + "', '" + primaryVerb + "', '" + otherTable.Replace("'", "’") + "', '" + String.Join(" ", secondaryVerbs) + "', " + _article + ", '" + _lineNumber + "')";
                                SqlCommand cmd3 = new SqlCommand(sqlData, con);
                                cmd3.ExecuteNonQuery();
                            }
                        }
                    }
                    else
                    {
                        string sqlData = "INSERT INTO dbo.[" + tableName.Trim() + "] (subject, primaryVerb, linkedSubject, secondaryVerb, article, line) VALUES ('" + tableName + "', '" + primaryVerb + "', 'null', '" + String.Join(" ", secondaryVerbs) + "', " + _article + ", '" + _lineNumber + "')";
                        SqlCommand cmd3 = new SqlCommand(sqlData, con);
                        cmd3.ExecuteNonQuery();
                    }

                }

                con.Close();
            }
            #endregion (Add the extracted entities along with the primary verb and objects to the database)---------------------------------------------------------------------------------------------------------------------------------------------------------------------
        }
        #endregion (IDENTIFY VERBS, SUBJECTS, and OBJECTS AND ADD THEM TO THE DATABASE)---------------------------------------------------------------------------------------------------------------------------------------------------------------------
        #endregion (NLP)--------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        #region WEB SCRAPING ----------------------------------------------------------------------------------------------------------------------------------------------------------------
        //---------------------------------------------------------------------- Web Scraping API Call Method ----------------------------------------------------------------------
        private static Task<string> CallUrl(string word)
        {
            string url = "https://www.merriam-webster.com/dictionary/" + word;
            HttpClient client = new HttpClient();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13;
            var response = client.GetAsync(url).Result;
            return Task.FromResult(response.Content.ReadAsStringAsync().Result);
        }
        //---------------------------------------------------------------------- Web Scraping Regex (find word type) BOOL OVERLOAD --------------------------------------------------------
        private static bool checkHtmlParse(string html)
        {
            if (CustomRegex.classRGX.IsMatch(html))
            {
                return true;
            }
            else { return false; }
        }
        //---------------------------------------------------------------------- Web Scraping Regex (find word type) ----------------------------------------------------------------------
        private static void ParseHtml(string html, string entry, WordBank wordbank, int count)
        {
            Console.WriteLine("Found new word \"{0}\" please hold on while I look this up...", entry);
            //ArrayList firstPlaces = new ArrayList();
            ArrayList types = new ArrayList();
            //the actual word that ends up being looked up
            string word = entry;
            if (html.Contains("h1 class=\"hword\""))
            {
                word = CustomRegex.headingWord.Match(html).Value;
            }
            //Small dictionary of additional words related and their types
            Dictionary<string, string[]> words = new Dictionary<string, string[]>();
            //If the Regex determines it's the right page, get all of the match indexes and do the second match to find the word type
            if (CustomRegex.classRGX.Match(html).Success)
            {
                int pastTensePlace = html.IndexOf(">past tense");
                int pluralOfPlace = html.IndexOf(">plural of");
                int presentTense = html.IndexOf(">present tense");
                int geographicalPlace = html.IndexOf("geographical name");
                int britishSpelling = html.IndexOf("British spelling");
                if (pastTensePlace != -1 || pluralOfPlace != -1 || geographicalPlace != -1 || presentTense != -1 || britishSpelling != -1)
                    types.Add(checkOtherTypes(html, entry, wordbank, word, count));
                foreach (Match match1 in CustomRegex.classRGX.Matches(html))
                    types.Add(CustomRegex.type.Match(html, match1.Index, 250).Value);
                //Find the list of related words and put their usage into the JSON dictionary
                foreach (Match additionalWord in CustomRegex.otherWords.Matches(html))
                    if (words.ContainsKey(additionalWord.Value) == false)
                        words.Add(additionalWord.Value, new string[] { CustomRegex.otherTypes.Match(html, additionalWord.Index).Value });
            }
            else
            {
                types.Add(checkOtherTypes(html, entry, wordbank, word, count));
            }
            //Add additional word types from <h2>Other Words from
            string[] typeStrings = (string[])types.ToArray(typeof(string));
            //Cyclical function for words not initially found (past tense forms) will run this twice. This if statement is a protection against that.
            //Add the word itself, this protects against numbers being put in. 90 comes back as "ninety" in the online dictionary
            //if (wordbank.CheckWord(entry.ToLower()) == false)
            if (entry.EndsWith("ed") && word.EndsWith("ed") == false && types.IndexOf("verb") != -1)
                wordbank.AddWord(entry.ToLower(), new string[] { "past tense verb" });
            else
                wordbank.AddWord(entry.ToLower(), typeStrings);
            //Adds the word as written in the dictionary. For past tense, plurals, and other types this gives the JSON dictionary the root word too
            //if (wordbank.CheckWord(word.ToLower()) == false)
            wordbank.AddWord(word.ToLower(), typeStrings);
            //Adds any additional words that were found relating to the word looked up
            foreach (string newWord in words.Keys)
            {
                //if (wordbank.CheckWord(newWord.ToLower()) == false)
                wordbank.AddWord(newWord.ToLower(), words[newWord]);
            }
            if (entry.EndsWith('s') && word.EndsWith('s') == false && typeStrings.Contains("noun"))
                //if (wordbank.CheckWord(entry.ToLower()) == false)
                wordbank.AddWord(entry, new string[] { "plural noun" });

        }
        private static string checkOtherTypes(string html, string entry, WordBank wordbank, string word, int count)
        {
            int pastTensePlace = html.IndexOf(">past tense");
            int pluralOfPlace = html.IndexOf(">plural of");
            int presentTense = html.IndexOf(">present tense");
            int geographicalPlace = html.IndexOf("geographical name");
            int britishSpelling = html.IndexOf("British spelling");
            if (pastTensePlace == -1 && pluralOfPlace == -1 && geographicalPlace == -1 && presentTense == -1 && britishSpelling == -1)
            {
                if (entry.EndsWith('s'))
                {
                    if (count > 0)
                    {
                        //If the word isn't in the dictionary, it is most likely being used as a noun. A better protection with user input will be inputted later
                        return "noun";
                    }
                    else
                    {
                        var response = CallUrl(entry.TrimEnd('s')).Result;
                        ParseHtml(response, word, wordbank, count + 1);
                        return null;
                    }
                }
                else
                {
                    //If the word isn't in the dictionary, it is most likely being used as a noun. A better protection with user input will be inputted later
                    return "noun";
                }
            }
            else if (pastTensePlace > 0)
            {
                Match newWord = CustomRegex.type.Match(html, pastTensePlace);
                if (newWord.Success)
                {
                    var response = CallUrl(newWord.Value).Result;
                    ParseHtml(response, entry, wordbank, count);
                }
                return null;
            }
            else if (pluralOfPlace > 0)
            {
                Match newWord = CustomRegex.type.Match(html, pluralOfPlace);
                if (newWord.Success)
                {
                    var response = CallUrl(newWord.Value).Result;
                    ParseHtml(response, entry, wordbank, count);
                }
                return null;
            }
            else if (presentTense > 0)
            {
                Match newWord = CustomRegex.type.Match(html, presentTense);
                if (newWord.Success)
                {
                    var response = CallUrl(newWord.Value).Result;
                    ParseHtml(response, entry, wordbank, count);
                }
                return null;
            }
            else if (britishSpelling > 0)
            {
                Match newWord = CustomRegex.type.Match(html, britishSpelling);
                if (newWord.Success)
                {
                    var response = CallUrl(newWord.Value).Result;
                    ParseHtml(response, entry, wordbank, count);
                }
                return null;
            }
            else if (geographicalPlace > 0)
            {
                //types.Add("noun");
                return "noun";
            }
            else { return null; }
        }
        #endregion (WEB SCRAPING)--------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    }
}
