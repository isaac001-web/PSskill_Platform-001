using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace Product_Management.Helpers
{
    public static class SessionHelper
    {
        // Save object to session
        public static void SetObject(HttpContext context, string key, object value)
        {
            context.Session.SetString(key, JsonSerializer.Serialize(value));
        }

        // Get object from session
        public static T GetObject<T>(HttpContext context, string key)
        {
            var value = context.Session.GetString(key);

            return value == null ? default : JsonSerializer.Deserialize<T>(value);
        }
    }
}