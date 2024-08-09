using Google.Cloud.Translate.V3;
using Grpc.Net.Client;
using static ApiKeyManager;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public static class GoogleTranslateHelper
{
   

    public static async Task<string> TranslateText(string text, string targetLanguage)
    {
        string url = $"https://translation.googleapis.com/language/translate/v2?key={ApiKeyManager.GetApiKey()}";

        var requestJson = new JObject
        {
            ["q"] = text,
            ["target"] = targetLanguage
        };

        using (var client = new HttpClient())
        {
            var content = new StringContent(requestJson.ToString(), System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content);
            var jsonResponse = await response.Content.ReadAsStringAsync();

            var responseObject = JObject.Parse(jsonResponse);
            var translatedText = responseObject["data"]["translations"][0]["translatedText"]?.ToString();

            return translatedText ?? string.Empty;
        }
    }
}