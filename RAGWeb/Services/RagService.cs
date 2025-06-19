using System.Text.Json;
using System.Text.RegularExpressions;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;

namespace RAGWeb.Services
{
    public class RagService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;
        private readonly string _reflectionsDir;
        private readonly string _openAiApiKey;
        private readonly string _openAiEndpoint;
        private readonly string _openAiModel;

        public RagService(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _httpClient = httpClientFactory.CreateClient();
            _reflectionsDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "Reflections");
            _openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? _config["OpenAI:ApiKey"] ?? "";
            _openAiEndpoint = _config["OpenAI:Endpoint"] ?? "https://api.openai.com/v1/chat/completions";
            _openAiModel = _config["OpenAI:Model"] ?? "o4-mini";
        }

        public async Task<string> AnswerQuestionAsync(string question)
        {
            // 1. 讀取所有分段翻譯檔案內容
            var allSegments = new List<string>();
            if (Directory.Exists(_reflectionsDir))
            {
                foreach (var file in Directory.GetFiles(_reflectionsDir, "*.md"))
                {
                    var content = await File.ReadAllTextAsync(file);
                    // 以分段符號分割（修正跳脫字元）
                    var segments = Regex.Split(content, @"(?=^#|^\d+\.|^\*|^\-|^\s*$)", RegexOptions.Multiline);
                    allSegments.AddRange(segments.Where(s => !string.IsNullOrWhiteSpace(s)));
                }
            }

            // 2. 根據問題檢索最相關的段落（簡單關鍵字比對，可後續優化）
            var topSegments = allSegments
                .OrderByDescending(s => GetKeywordScore(s, question))
                .Take(5)
                .ToList();

            var context = string.Join("\n---\n", topSegments);

            // 3. 呼叫 OpenAI 生成回答
            var prompt = $"根據以下內容回答問題：\n{context}\n\n問題：{question}\n請用繁體中文簡要回答。";
            var answer = await CallOpenAiAsync(prompt);
            return answer;
        }

        private int GetKeywordScore(string text, string question)
        {
            // 若有 Net Assessment 專業詞彙，可在此加強比對
            var domainKeywords = new[] { "Net Assessment", "淨評估", "戰略評估", "軍事平衡", "威脅分析", "能力差距", "戰略競爭" };
            int score = 0;
            foreach (var k in domainKeywords)
                score += Regex.Matches(text, Regex.Escape(k), RegexOptions.IgnoreCase).Count * 3; // 專業詞加權
            var keywords = question.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            score += keywords.Sum(k => Regex.Matches(text, Regex.Escape(k), RegexOptions.IgnoreCase).Count);
            return score;
        }

        private async Task<string> CallOpenAiAsync(string prompt)
        {
            var requestBody = new
            {
                model = _openAiModel,
                messages = new[] {
                    new { role = "system", content = "你是一個知識型助理。" },
                    new { role = "user", content = prompt }
                },
                temperature = 0.2
            };
            var req = new HttpRequestMessage(HttpMethod.Post, _openAiEndpoint);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openAiApiKey);
            req.Content = new StringContent(JsonSerializer.Serialize(requestBody));
            req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var resp = await _httpClient.SendAsync(req);
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
        }
    }
}
