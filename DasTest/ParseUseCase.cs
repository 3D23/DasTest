using AngleSharp.Html.Parser;
using Dapper;
using DasTest.DTO;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace DasTest
{
    public interface IParseUseCase
    {
        Task<ParseResponse> ProcessAsync(InputData input, CancellationToken ct = default);
    }

    public partial class ParseUseCase(IDbConnection db) : IParseUseCase
    {
        private readonly IDbConnection _db = db;

        public async Task<ParseResponse> ProcessAsync(InputData input, CancellationToken ct = default)
        {
            var url = Encoding.UTF8.GetString(Convert.FromBase64String(input.UrlBase64!));
            var html = Encoding.UTF8.GetString(Convert.FromBase64String(input.PageBase64!));
            var keyBytes = Convert.FromBase64String(input.KeyBytesBase64!);
            var cipherBytes = Convert.FromBase64String(input.EncryptedTextBytesBase64!);

            string decrypted;
            try
            {
                decrypted = DecryptAes256Ecb(cipherBytes, keyBytes);
            }
            catch (CryptographicException)
            {
                return ParseResponse.Error(
                    ErrorCodes.DecryptionError,
                    "Не удалось расшифровать текст: неверный ключ или повреждённые данные");
            }

            List<string> attributeValues;
            int elementsCount;
            try
            {
                var parser = new HtmlParser();
                var document = await parser.ParseDocumentAsync(html, ct);
                var elements = document.QuerySelectorAll(input.Selector!).ToList();
                elementsCount = elements.Count;

                attributeValues = new List<string>(elementsCount);
                foreach (var el in elements)
                {
                    var value = el.GetAttribute(input.Attribure!);
                    attributeValues.Add(value ?? string.Empty);
                }

                const string insertSql = """
                    INSERT INTO elements (attribute_value, html_code)
                    VALUES (@AttributeValue, @HtmlCode);
                    """;

                if (elements.Count > 0)
                {
                    await _db.ExecuteAsync(new CommandDefinition(
                        insertSql,
                        elements.Select(el => new
                        {
                            AttributeValue = el.GetAttribute(input.Attribure!) ?? string.Empty,
                            HtmlCode = el.OuterHtml
                        }),
                        cancellationToken: ct));
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                return ParseResponse.Error(ErrorCodes.HtmlParseError);
            }

            List<string> emails;
            try
            {
                emails = [.. RegexPatterns.Email.Matches(html)
                    .Select(m => m.Value)
                    .Distinct(StringComparer.OrdinalIgnoreCase)];
            }
            catch (Exception ex)
            {
                return ParseResponse.Error(ErrorCodes.UnknownError, ex.Message);
            }

            return ParseResponse.Success(
                elementsCount: elementsCount,
                emailsCount: emails.Count,
                url: url,
                decryptedPlainText: decrypted,
                elementsAttrList: attributeValues,
                emailsList: emails);
        }

        private static string DecryptAes256Ecb(byte[] cipherText, byte[] key)
        {
            using var aes = Aes.Create();
            aes.Key = key;

            byte[] plainBytes = aes.DecryptEcb(cipherText, PaddingMode.None);
            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}
