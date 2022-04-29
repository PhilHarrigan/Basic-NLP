using System.Text.Json;

namespace NLP_API.Server
{
    public class WordBank
    {
        Dictionary<string, Dictionary<string, int>> _wordBank = new Dictionary<string, Dictionary<string, int>>();

        public void AddWord(string word, string[] usages)
        {
            if (_wordBank.ContainsKey(word) == false)
            {
                _wordBank.Add(word, new Dictionary<string, int> { { usages[0], 1 } });
                for (int i = 1; i < usages.Length; i++)
                {
                    if (usages[i] != "")
                    {
                        if (_wordBank[word].ContainsKey(usages[i]) == false)
                            _wordBank[word].Add(usages[i], 0);
                    }
                }
            }
            else
            {
                foreach (string usage in usages)
                {
                    if (usage != null)
                    {
                        if (_wordBank[word].ContainsKey(usage) == false)
                        {
                            _wordBank[word].Add((string)usage, 0);
                        }
                    }
                }
            }
        }
        public bool CheckWord(string word)
        {
            return _wordBank.ContainsKey(word);
        }
        public string FindWordType(string word)
        {
            string type = "";
            int usageQuantity = 0;
            foreach (KeyValuePair<string, int> kvp in _wordBank[word])
            {
                if (kvp.Value > usageQuantity)
                {
                    type = kvp.Key;
                    usageQuantity = kvp.Value;
                }
            }
            return type;
        }
        public bool CheckHyphenatedWord(string word)
        {
            if (_wordBank["hyphenatedNonWords"].ContainsKey(word))
                return true;
            else
                _wordBank["hyphenatedNonWords"].Add(word, 0);
            return false;
        }
        public void Save()
        {
            //remove option when live
            var option = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText("word_bank.json", JsonSerializer.Serialize(_wordBank, option));
        }
        public void Load()
        {
            string jsonString = File.ReadAllText("word_bank.json");
            _wordBank = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, int>>>(jsonString);
        }
    }
}
