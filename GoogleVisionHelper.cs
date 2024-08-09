using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public static class GoogleVisionHelper
{
    private static readonly string apiKey = "your-google-cloud-api-key"; // Inserisci qui la tua chiave API

    public static async Task<string> DetectTextFromImage(string imagePath)
    {
        string url = $"https://vision.googleapis.com/v1/images:annotate?key={ApiKeyManager.GetApiKey()}";

        // Leggi l'immagine come base64
        var imageContent = Convert.ToBase64String(System.IO.File.ReadAllBytes(imagePath));

        // Prepara la richiesta JSON
        var requestJson = new JObject
        {
            ["requests"] = new JArray
            {
                new JObject
                {
                    ["image"] = new JObject
                    {
                        ["content"] = imageContent
                    },
                    ["features"] = new JArray
                    {
                        new JObject
                        {
                            ["type"] = "TEXT_DETECTION"
                        }
                    }
                }
            }
        };

        using (var client = new HttpClient())
        {
            var content = new StringContent(requestJson.ToString(), System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content);
            var jsonResponse = await response.Content.ReadAsStringAsync();

            // Analizza la risposta JSON
            var responseObject = JObject.Parse(jsonResponse);
            var detectedText = responseObject["responses"]?[0]?["fullTextAnnotation"]?["text"]?.ToString();

            return detectedText ?? string.Empty;
        }
    }
}
