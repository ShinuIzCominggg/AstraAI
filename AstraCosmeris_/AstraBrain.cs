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
        private static readonly HttpClient _httpClient = new HttpClient();

        public static async Task<string> ThinkAndReply()
        {
            var data = DataManager.Data;
            string apiKey = data.ApiKey;
            string provider = data.ApiProvider;

            // Lấy trực tiếp Model mà người dùng nhập
            string targetModel = string.IsNullOrWhiteSpace(data.ApiModel) ? "llama-3.1-8b-instant" : data.ApiModel;

            string factsJson = JsonSerializer.Serialize(data.Facts);
            string systemContext = $"{data.SystemPrompt}\n\n[USER FACTS/MEMORY]: {factsJson}";

            if (string.IsNullOrEmpty(apiKey) && provider != "Ollama") return "Cậu chưa nhập API Key trong phần Cài đặt kìa!";

            try
            {
                // 1. OPENAI / GROQ / OPENROUTER
                if (provider is "OpenAI" or "Groq" or "OpenRouter")
                {
                    string apiUrl = provider switch
                    {
                        "OpenAI" => "https://api.openai.com/v1/chat/completions",
                        "Groq" => "https://api.groq.com/openai/v1/chat/completions",
                        "OpenRouter" => "https://openrouter.ai/api/v1/chat/completions",
                        _ => ""
                    };

                    _httpClient.DefaultRequestHeaders.Clear();
                    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                    var messagesList = new List<object> { new { role = "system", content = systemContext } };
                    foreach (var msg in data.History) messagesList.Add(new { role = msg["role"], content = msg["content"] });

                    var requestData = new { model = targetModel, messages = messagesList };
                    var content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, content);
                    response.EnsureSuccessStatusCode();

                    JsonNode? jsonNode = JsonNode.Parse(await response.Content.ReadAsStringAsync());
                    return jsonNode?["choices"]?[0]?["message"]?["content"]?.ToString() ?? "Lỗi đọc JSON!";
                }

                // 2. GEMINI
                else if (provider == "Gemini")
                {
                    string apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{targetModel}:generateContent?key={apiKey}";
                    var contentsList = new List<object>
                    {
                        new { role = "user", parts = new[] { new { text = $"[SYSTEM PROMPT & CONTEXT]: {systemContext}\n\n[USER]: Xin chào" } } },
                        new { role = "model", parts = new[] { new { text = "Vâng, tớ đã hiểu hoàn toàn thông tin hiện tại của cậu!" } } }
                    };

                    foreach (var msg in data.History)
                    {
                        string role = msg["role"] == "user" ? "user" : "model";
                        contentsList.Add(new { role = role, parts = new[] { new { text = msg["content"] } } });
                    }

                    var requestData = new { contents = contentsList };
                    var content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, content);
                    response.EnsureSuccessStatusCode();

                    JsonNode? jsonNode = JsonNode.Parse(await response.Content.ReadAsStringAsync());
                    return jsonNode?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString() ?? "Lỗi JSON Gemini!";
                }

                // 3. CLAUDE
                else if (provider == "Claude")
                {
                    string apiUrl = "https://api.anthropic.com/v1/messages";
                    _httpClient.DefaultRequestHeaders.Clear();
                    _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
                    _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

                    var messagesList = new List<object>();
                    foreach (var msg in data.History) messagesList.Add(new { role = msg["role"], content = msg["content"] });

                    var requestData = new { model = targetModel, max_tokens = 1024, system = systemContext, messages = messagesList };
                    var content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, content);
                    response.EnsureSuccessStatusCode();

                    JsonNode? jsonNode = JsonNode.Parse(await response.Content.ReadAsStringAsync());
                    return jsonNode?["content"]?[0]?["text"]?.ToString() ?? "Lỗi JSON Claude!";
                }

                // 4. OLLAMA
                else if (provider == "Ollama")
                {
                    string apiUrl = "http://localhost:11434/api/chat";
                    var messagesList = new List<object> { new { role = "system", content = systemContext } };
                    foreach (var msg in data.History) messagesList.Add(new { role = msg["role"], content = msg["content"] });

                    var requestData = new { model = targetModel, messages = messagesList, stream = false };
                    var content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, content);
                    response.EnsureSuccessStatusCode();

                    JsonNode? jsonNode = JsonNode.Parse(await response.Content.ReadAsStringAsync());
                    return jsonNode?["message"]?["content"]?.ToString() ?? "Lỗi JSON Ollama!";
                }
            }
            catch (Exception ex)
            {
                return $"[Lỗi {provider} ({targetModel})] {ex.Message}";
            }

            return "Provider này tớ chưa hỗ trợ!";
        }
    }
}