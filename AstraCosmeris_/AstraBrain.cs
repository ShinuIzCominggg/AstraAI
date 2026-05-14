using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace AstraCosmeris_
{
    public static class AstraBrain
    {
        public static async Task<string> ThinkAndReply()
        {
            string apiKey = MemoryManager.Data.ApiKey;
            string provider = MemoryManager.Data.ApiProvider;

            // XỬ LÝ MODEL (Lấy từ Settings, nếu trống thì dùng mặc định)
            string targetModel = MemoryManager.Data.ApiModel;
            if (string.IsNullOrWhiteSpace(targetModel))
            {
                targetModel = provider switch
                {
                    "OpenAI" => "gpt-3.5-turbo",
                    "Groq" => "llama-3.1-8b-instant",
                    "OpenRouter" => "meta-llama/llama-3-8b-instruct:free",
                    "Gemini" => "gemini-3.1-flash-preview", // Dùng bản ngon nhất 2026 làm mặc định
                    "Claude" => "claude-3.5-sonnet-20241022",
                    "Ollama" => "llama3",
                    _ => "llama-3.1-8b-instant"
                };
            }

            string factsJson = JsonSerializer.Serialize(MemoryManager.Data.Facts);
            string systemContext = $"{MemoryManager.Data.SystemPrompt}\nUser facts you must remember: {factsJson}";

            if (string.IsNullOrEmpty(apiKey) && provider != "Ollama")
            {
                return "Cậu chưa nhập API Key trong phần Cài đặt kìa!";
            }

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // 1. OPENAI / GROQ / OPENROUTER
                    if (provider == "OpenAI" || provider == "Groq" || provider == "OpenRouter")
                    {
                        string apiUrl = provider switch
                        {
                            "OpenAI" => "https://api.openai.com/v1/chat/completions",
                            "Groq" => "https://api.groq.com/openai/v1/chat/completions",
                            "OpenRouter" => "https://openrouter.ai/api/v1/chat/completions",
                            _ => ""
                        };

                        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                        var messagesList = new List<object> { new { role = "system", content = systemContext } };
                        foreach (var msg in MemoryManager.Data.History) messagesList.Add(new { role = msg["role"], content = msg["content"] });

                        // Nhét targetModel vào đây
                        var requestData = new { model = targetModel, messages = messagesList };
                        var content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");

                        HttpResponseMessage response = await client.PostAsync(apiUrl, content);
                        response.EnsureSuccessStatusCode();

                        JsonNode? jsonNode = JsonNode.Parse(await response.Content.ReadAsStringAsync());
                        return jsonNode?["choices"]?[0]?["message"]?["content"]?.ToString() ?? "Lỗi đọc JSON!";
                    }

                    // 2. GEMINI (GOOGLE)
                    else if (provider == "Gemini")
                    {
                        // Nhét targetModel vào URL của Gemini
                        string apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{targetModel}:generateContent?key={apiKey}";

                        var contentsList = new List<object>();
                        contentsList.Add(new { role = "user", parts = new[] { new { text = $"[SYSTEM PROMPT]: {systemContext}\n\n[USER]: Xin chào" } } });
                        contentsList.Add(new { role = "model", parts = new[] { new { text = "Vâng, tớ đã hiểu thiết lập và thông tin của cậu!" } } });

                        foreach (var msg in MemoryManager.Data.History)
                        {
                            string role = msg["role"] == "user" ? "user" : "model";
                            contentsList.Add(new { role = role, parts = new[] { new { text = msg["content"] } } });
                        }

                        var requestData = new { contents = contentsList };
                        var content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");

                        HttpResponseMessage response = await client.PostAsync(apiUrl, content);
                        response.EnsureSuccessStatusCode();

                        JsonNode? jsonNode = JsonNode.Parse(await response.Content.ReadAsStringAsync());
                        return jsonNode?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString() ?? "Lỗi JSON Gemini!";
                    }

                    // 3. CLAUDE
                    else if (provider == "Claude")
                    {
                        string apiUrl = "https://api.anthropic.com/v1/messages";
                        client.DefaultRequestHeaders.Add("x-api-key", apiKey);
                        client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

                        var messagesList = new List<object>();
                        foreach (var msg in MemoryManager.Data.History) messagesList.Add(new { role = msg["role"], content = msg["content"] });

                        var requestData = new { model = targetModel, max_tokens = 1024, system = systemContext, messages = messagesList };
                        var content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");

                        HttpResponseMessage response = await client.PostAsync(apiUrl, content);
                        response.EnsureSuccessStatusCode();

                        JsonNode? jsonNode = JsonNode.Parse(await response.Content.ReadAsStringAsync());
                        return jsonNode?["content"]?[0]?["text"]?.ToString() ?? "Lỗi JSON Claude!";
                    }

                    // 4. OLLAMA
                    else if (provider == "Ollama")
                    {
                        string apiUrl = "http://localhost:11434/api/chat";

                        var messagesList = new List<object> { new { role = "system", content = systemContext } };
                        foreach (var msg in MemoryManager.Data.History) messagesList.Add(new { role = msg["role"], content = msg["content"] });

                        var requestData = new { model = targetModel, messages = messagesList, stream = false };
                        var content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");

                        HttpResponseMessage response = await client.PostAsync(apiUrl, content);
                        response.EnsureSuccessStatusCode();

                        JsonNode? jsonNode = JsonNode.Parse(await response.Content.ReadAsStringAsync());
                        return jsonNode?["message"]?["content"]?.ToString() ?? "Lỗi JSON Ollama!";
                    }
                }
            }
            catch (Exception ex)
            {
                return $"[Lỗi {provider} ({targetModel})] {ex.Message}";
            }

            return "Provider này tớ chưa được dạy cách dùng =))";
        }
    }
}