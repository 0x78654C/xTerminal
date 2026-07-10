using Commands.TerminalCommands.Network;
using Core;
using Core.Encryption;
using Core.Security;
using Core.Spreadsheets;
using System.IO.Compression;
using System.Text;
using System.Text.Json.Nodes;
using Xunit;

namespace Tests.Commands.Security;

public class SecurityRegressionTests
{
    [Fact]
    public void VaultEncryption_UsesRandomSaltAndAuthenticatedEncryption()
    {
        const string password = "correct horse battery staple";
        const string plaintext = "vault contents";

        string first = AES.Encrypt(plaintext, password);
        string second = AES.Encrypt(plaintext, password);

        Assert.NotEqual(first, second);
        Assert.Equal(plaintext, AES.Decrypt(first, password));

        var payload = JsonNode.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(first)))!.AsObject();
        byte[] ciphertext = Convert.FromBase64String(payload["value"]!.GetValue<string>());
        ciphertext[0] ^= 0x01;
        payload["value"] = Convert.ToBase64String(ciphertext);
        string tampered = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload.ToJsonString()));

        Assert.StartsWith("Error decrypting:", AES.Decrypt(tampered, password));
    }

    [Theory]
    [InlineData("waifu -u file.txt -p hunter2 -b bucket-token", "hunter2", "bucket-token")]
    [InlineData("curl --token=abc123 https://example.test", "abc123", null)]
    [InlineData("curl -H \"Authorization: Bearer topsecret\" https://example.test", "topsecret", null)]
    [InlineData("wget https://user:pass@example.test/file", "pass", null)]
    public void HistorySanitizer_RemovesCredentialValues(string command, string firstSecret, string? secondSecret)
    {
        string sanitized = CommandHistorySanitizer.Sanitize(command);

        Assert.DoesNotContain(firstSecret, sanitized);
        if (secondSecret != null)
            Assert.DoesNotContain(secondSecret, sanitized);
        Assert.Contains("[REDACTED]", sanitized);
    }

    [Fact]
    public void Wget_DerivesFilenameFromNormalizedUriAndKeepsItInsideDestination()
    {
        string destination = Path.Combine(Path.GetTempPath(), "wget-destination");
        var uri = new Uri("https://example.test/files/..%5C..%5Cescaped.txt");

        string result = WGet.GetSafeDownloadPath(uri, destination);

        Assert.Equal(Path.Combine(Path.GetFullPath(destination), "escaped.txt"), result);
    }

    [Fact]
    public void Worksheet_RejectsUnboundedIndexesBeforeAllocating()
    {
        var worksheet = new SpreadsheetWorksheet("Sheet1");

        Action rowAttack = () => worksheet.SetCell(int.MaxValue, 0, "x");
        Action columnAttack = () => worksheet.SetCell(0, int.MaxValue, "x");

        Assert.Throws<ArgumentOutOfRangeException>(rowAttack);
        Assert.Throws<ArgumentOutOfRangeException>(columnAttack);
    }

    [Fact]
    public void XlsxLoader_RejectsHugeRowReference()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".xlsx");
        try
        {
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                WriteEntry(archive, "xl/workbook.xml",
                    "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
                WriteEntry(archive, "xl/_rels/workbook.xml.rels",
                    "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Target=\"worksheets/sheet1.xml\"/></Relationships>");
                WriteEntry(archive, "xl/worksheets/sheet1.xml",
                    "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData><row r=\"2147483647\"><c r=\"A2147483647\" t=\"inlineStr\"><is><t>x</t></is></c></row></sheetData></worksheet>");
            }

            Action load = () => SpreadsheetFile.Load(path);
            Assert.Throws<InvalidDataException>(load);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void RecursiveDelete_DoesNotTraverseDirectoryLinks()
    {
        string root = Path.Combine(Path.GetTempPath(), "xterminal-delete-" + Guid.NewGuid());
        string target = Path.Combine(Path.GetTempPath(), "xterminal-target-" + Guid.NewGuid());
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(target);
        string protectedFile = Path.Combine(target, "keep.txt");
        File.WriteAllText(protectedFile, "keep");

        try
        {
            try
            {
                Directory.CreateSymbolicLink(Path.Combine(root, "link"), target);
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }
            catch (IOException)
            {
                // Creating directory links requires Developer Mode or SeCreateSymbolicLinkPrivilege.
                return;
            }

            FileDirManager.RecursiveDeleteDir(new DirectoryInfo(root));
            Assert.True(File.Exists(protectedFile));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
            if (Directory.Exists(target))
                Directory.Delete(target, true);
        }
    }

    private static void WriteEntry(ZipArchive archive, string name, string contents)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.NoCompression);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(contents);
    }
}
