using System;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.Json;

class Program
{
    static async Task Main(string[] args)
    {
        string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            Console.WriteLine("請先設定 OPENAI_API_KEY 環境變數。");
            return;
        }

        string sourceDir = "/Users/fanghuaian/Documents/Projects/dotnet/ragexample/Reflections";
        string targetDir = "/Users/fanghuaian/Documents/Projects/dotnet/ragexample/translated/Reflections";
        Directory.CreateDirectory(targetDir);

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        foreach (var file in Directory.GetFiles(sourceDir, "*.md"))
        {
            string content = await File.ReadAllTextAsync(file);
            var paragraphs = content.Split(new string[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            var optimizedParagraphs = new List<string>();
            foreach (var para in paragraphs)
            {
                if (para.Length <= 500)
                {
                    optimizedParagraphs.Add(para.Trim());
                }
                else
                {
                    // 以句號優先細分
                    var sentences = System.Text.RegularExpressions.Regex.Split(para, @"(?<=[。.!?\.])");
                    var buffer = new List<string>();
                    int currentLen = 0;
                    foreach (var sentence in sentences)
                    {
                        var trimmed = sentence.Trim();
                        if (string.IsNullOrEmpty(trimmed)) continue;
                        if (currentLen + trimmed.Length > 500 && buffer.Count > 0)
                        {
                            optimizedParagraphs.Add(string.Join("", buffer).Trim());
                            buffer.Clear();
                            currentLen = 0;
                        }
                        buffer.Add(trimmed);
                        currentLen += trimmed.Length;
                    }
                    if (buffer.Count > 0)
                        optimizedParagraphs.Add(string.Join("", buffer).Trim());
                }
            }
            string targetPath = Path.Combine(targetDir, Path.GetFileName(file));
            using (var writer = new StreamWriter(targetPath, false, System.Text.Encoding.UTF8))
            {
                int idx = 1;
                foreach (var para in optimizedParagraphs)
                {
                    await writer.WriteLineAsync($"--- 段落 {idx} ({para.Length} 字元) ---");
                    await writer.WriteLineAsync(para);
                    await writer.WriteLineAsync();
                    // 呼叫 OpenAI API 進行翻譯
                    string translated = await TranslateWithRetry(para, httpClient);
                    await writer.WriteLineAsync($"[翻譯]");
                    await writer.WriteLineAsync(translated);
                    await writer.WriteLineAsync();
                    Console.WriteLine($"已寫入段落 {idx} ({para.Length} 字元) in {Path.GetFileName(file)}");
                    idx++;
                }
            }
            Console.WriteLine($"已分段寫入：{file} -> {targetPath}");
        }
    }

    // 新增遞迴細分與重試的函式
    static async Task<string> TranslateWithRetry(string text, HttpClient httpClient, int depth = 0)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        try
        {
            var requestBody = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                    new { role = "user", content = $"請將以下內容翻譯成繁體中文：\n\n{text.Trim()}" }
                }
            };
            var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
            var response = await httpClient.PostAsync(
                "https://api.openai.com/v1/chat/completions",
                new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json")
            );
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(responseString);
            var translated = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content").GetString()?.Trim() ?? "";
            Console.WriteLine($"已翻譯一段 ({text.Length} 字元)");
            return translated;
        }
        catch (System.Net.Http.HttpRequestException ex) when (ex.Message.Contains("400") && depth < 3)
        {
            // 遇到 400 Bad Request 時細分
            Console.WriteLine($"分段過長，嘗試細分 (depth={depth})");
            // 依據細分層級選擇分割方式
            string[] subparts = depth switch
            {
                0 => text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries), // 單行
                1 => System.Text.RegularExpressions.Regex.Split(text, @"(?=^#|^>|^- |^\* |^\+ |^\d+\. |^---|^\*\*\*)", System.Text.RegularExpressions.RegexOptions.Multiline), // 標題/清單/水平線
                _ => text.ToCharArray().Select(c => c.ToString()).ToArray() // 最細為每字
            };
            var results = new List<string>();
            foreach (var part in subparts)
            {
                results.Add(await TranslateWithRetry(part, httpClient, depth + 1));
            }
            return string.Join("", results);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"翻譯失敗: {ex.Message}");
            return "[翻譯失敗]";
        }
    }
}
