using System.Text.Json.Serialization;

namespace DasTest.DTO
{
    public sealed class ParseResponse
    {
        [JsonPropertyName("is_error")]
        public int IsError { get; init; } 

        [JsonPropertyName("error_code")]
        public string? ErrorCode { get; init; }

        [JsonPropertyName("error_message")]
        public string? ErrorMessage { get; init; }

        [JsonPropertyName("elements_count")]
        public int ElementsCount { get; init; }

        [JsonPropertyName("emails_count")]
        public int EmailsCount { get; init; }

        [JsonPropertyName("url")]
        public string? Url { get; init; }

        [JsonPropertyName("decrypted_plain_text")]
        public string? DecryptedPlainText { get; init; }

        [JsonPropertyName("elements_attr_list")]
        public List<string> ElementsAttrList { get; init; } = [];

        [JsonPropertyName("emails_list")]
        public List<string> EmailsList { get; init; } = [];

        public static ParseResponse Success(
            int elementsCount,
            int emailsCount,
            string url,
            string decryptedPlainText,
            List<string> elementsAttrList,
            List<string> emailsList) => new()
            {
                IsError = 0,
                ElementsCount = elementsCount,
                EmailsCount = emailsCount,
                Url = url,
                DecryptedPlainText = decryptedPlainText,
                ElementsAttrList = elementsAttrList,
                EmailsList = emailsList
            };

        public static ParseResponse Error(string code, string? message = null) => new()
        {
            IsError = 1,
            ErrorCode = code,
            ErrorMessage = message
        };
    }
}
