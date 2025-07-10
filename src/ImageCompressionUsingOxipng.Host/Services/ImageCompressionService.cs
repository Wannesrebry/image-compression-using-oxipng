using System.Diagnostics;
using System.Text;

namespace ImageCompressionUsingOxipng.Host.Services;

public interface IImageCompressionService
{
    Task<byte[]> CompressPngAsync(byte[] imageData, int optimizationLevel = 2);
    Task<string> CompressPngFileAsync(string inputPath, string outputPath, int optimizationLevel = 2);
}

public class OxipngCompressionService : IImageCompressionService
{
    private readonly ILogger<OxipngCompressionService> _logger;
    private readonly string _oxipngPath;

    public OxipngCompressionService(ILogger<OxipngCompressionService> logger)
    {
        _logger = logger;

        // TODO: Set the path to the oxipng executable based on your deployments OS and architecture.
        _oxipngPath = "oxipng/windows-x64/oxipng.exe"; // Adjust the path as necessary
    }

    public async Task<byte[]> CompressPngAsync(byte[] imageData, int optimizationLevel = 2)
    {
        var tempInputFile = Path.GetTempFileName() + ".png";
        var tempOutputFile = Path.GetTempFileName() + ".png";

        try
        {
            // Write input data to temp file
            await File.WriteAllBytesAsync(tempInputFile, imageData);

            // Compress the file
            await CompressPngFileAsync(tempInputFile, tempOutputFile, optimizationLevel);

            // Read compressed result
            return await File.ReadAllBytesAsync(tempOutputFile);
        }
        finally
        {
            // Cleanup temp files
            CleanupTempFile(tempInputFile);
            CleanupTempFile(tempOutputFile);
        }
    }

    public async Task<string> CompressPngFileAsync(string inputPath, string outputPath, int optimizationLevel = 2)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = _oxipngPath,
            Arguments = $"-o {optimizationLevel} \"{inputPath}\" --out \"{outputPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process();
        process.StartInfo = processStartInfo;

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (sender, e) => {
            if (!string.IsNullOrEmpty(e.Data))
                outputBuilder.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (sender, e) => {
            if (!string.IsNullOrEmpty(e.Data))
                errorBuilder.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        var output = outputBuilder.ToString();
        var error = errorBuilder.ToString();

        if (process.ExitCode != 0)
        {
            _logger.LogError("Oxipng compression failed. Exit code: {ExitCode}, Error: {Error}",
                process.ExitCode, error);
            throw new InvalidOperationException($"PNG compression failed: {error}");
        }

        _logger.LogInformation("PNG compression completed successfully. Output: {Output}", output);
        return output;
    }

    private void CleanupTempFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup temp file: {FilePath}", filePath);
        }
    }
}
