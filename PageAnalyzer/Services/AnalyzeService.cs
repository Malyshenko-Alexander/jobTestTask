using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using Dapper;
using Npgsql;
using PageAnalyzer.Models;

namespace PageAnalyzer.Services;

public partial class AnalyzeService : IAnalyzeService
{
    private readonly string _connectionString;

    public AnalyzeService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured");
    }

    [GeneratedRegex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();

    public async Task<AnalyzeResponse> AnalyzeAsync(AnalyzeRequest request)
    {
        try
        {
            string url;
            try
            {
                url = Encoding.UTF8.GetString(Convert.FromBase64String(request.UrlB64!));
            }
            catch (Exception)
            {
                return Error("INVALID_BASE64_URL", "Failed to decode url_b64 from Base64");
            }

            string pageHtml;
            try
            {
                pageHtml = Encoding.UTF8.GetString(Convert.FromBase64String(request.PageB64!));
            }
            catch (Exception)
            {
                return Error("INVALID_BASE64_PAGE", "Failed to decode page_b64 from Base64");
            }

            var parser = new HtmlParser();
            var document = parser.ParseDocument(pageHtml);
            var elements = document.QuerySelectorAll(request.Selector!);

            var attrList = new List<string>();
            var rows = new List<(string AttrValue, string Html)>();
            foreach (var element in elements)
            {
                var attrValue = element.GetAttribute(request.Attribute!) ?? string.Empty;
                attrList.Add(attrValue);
                rows.Add((attrValue, element.OuterHtml));
            }

            await SaveElementsAsync(rows);

            var emailMatches = EmailRegex().Matches(pageHtml);
            var emails = emailMatches.Select(m => m.Value).Distinct().ToList();

            string decryptedText;
            try
            {
                decryptedText = DecryptAesEcb(request.EncryptedTextBytesB64!, request.KeyBytesB64!);
            }
            catch (Exception ex)
            {
                return Error("DECRYPT_ERROR", ex.Message);
            }

            return new AnalyzeResponse
            {
                IsError = 0,
                Url = url,
                ElementsCount = attrList.Count,
                ElementsAttrList = attrList,
                EmailsCount = emails.Count,
                EmailsList = emails,
                DecryptedPlainText = decryptedText
            };
        }
        catch (Exception ex)
        {
            return Error("INTERNAL_ERROR", ex.Message);
        }
    }

    private async Task SaveElementsAsync(List<(string AttrValue, string Html)> rows)
    {
        if (rows.Count == 0)
        {
            return;
        }

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = "INSERT INTO elements (attr_value, html) VALUES (@AttrValue, @Html)";
        foreach (var row in rows)
        {
            await connection.ExecuteAsync(sql, new { row.AttrValue, row.Html });
        }
    }

    // AES-256 ECB PaddingMode.None
    private static string DecryptAesEcb(string encryptedB64, string keyB64)
    {
        var cipherBytes = Convert.FromBase64String(encryptedB64);
        var keyBytes = Convert.FromBase64String(keyB64);

        using var aes = Aes.Create();
        aes.Key = keyBytes;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;

        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        return Encoding.UTF8.GetString(plainBytes).TrimEnd('\0');
    }

    private static AnalyzeResponse Error(string code, string message)
    {
        return new AnalyzeResponse
        {
            IsError = 1,
            ErrorCode = code,
            ErrorMessage = message
        };
    }
}
