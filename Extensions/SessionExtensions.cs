
using System.Text.Json;

namespace Jam.Extensions;

public static class SessionExtensions
{
    // Stores an object in the session as a JSON string
    public static void SetObject<T>(this ISession session, string key, T value)
    {
        if (session == null)
            throw new ArgumentNullException(nameof(session));

        var json = JsonSerializer.Serialize(value);
        session.SetString(key, json);
    }

    // Retrieves an object from the session and deserializes it
    // Returns default(T) if the key is not found or if deserialization fails
    public static T? GetObject<T>(this ISession session, string key)
    {
        if (session == null)
            throw new ArgumentNullException(nameof(session));

        var json = session.GetString(key);
        if (string.IsNullOrEmpty(json))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (JsonException)
        {
            // Optional, but should log and handle corrupt JSON
            return default;
        }
    }

    // Removes an object from session by key
    public static void RemoveObject(this ISession session, string key)
    {
        if (session == null)
            throw new ArgumentNullException(nameof(session));

        session.Remove(key);
    }
}