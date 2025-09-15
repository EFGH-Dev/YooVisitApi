using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum QuizType
{
    // Les valeurs correspondent aux chaînes envoyées par Flutter
    QCM,
    VraiFaux,
    TexteLibre
}