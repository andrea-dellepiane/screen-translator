using Grpc.Core.Interceptors;
using Grpc.Core;
using System.IO;

public static partial class ApiKeyManager
{
    //read apikeyjos string
    public static string GetApiKey()
    {
        return File.ReadAllText("apikey.json");
    }
}
