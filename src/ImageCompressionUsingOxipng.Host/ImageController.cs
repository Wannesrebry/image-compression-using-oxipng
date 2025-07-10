using ImageCompressionUsingOxipng.Host.Services;
using Microsoft.AspNetCore.Mvc;

namespace ImageCompressionUsingOxipng.Host;

[ApiController]
[Route("api/[controller]")]
public class ImageController : ControllerBase
{
    private readonly IImageCompressionService _compressionService;
    private readonly ILogger<ImageController> _logger;

    public ImageController(IImageCompressionService compressionService, ILogger<ImageController> logger)
    {
        _compressionService = compressionService;
        _logger = logger;
    }

    [HttpPost("upload-and-compress")]
    public async Task<IActionResult> UploadAndCompressImage(IFormFile file, [FromQuery] int optimizationLevel = 2)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        if (!file.ContentType.Equals("image/png", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only PNG files are supported");

        if (optimizationLevel < 0 || optimizationLevel > 6)
            return BadRequest("Optimization level must be between 0 and 6");

        try
        {
            // Read uploaded file
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var originalData = memoryStream.ToArray();

            // Compress the image
            var compressedData = await _compressionService.CompressPngAsync(originalData, optimizationLevel);

            var compressionRatio = (double)compressedData.Length / originalData.Length;
            var savedBytes = originalData.Length - compressedData.Length;

            _logger.LogInformation("Image compressed. Original: {OriginalSize} bytes, Compressed: {CompressedSize} bytes, Saved: {SavedBytes} bytes ({SavedPercentage:P2})",
                originalData.Length, compressedData.Length, savedBytes, 1 - compressionRatio);

            return File(compressedData, "image/png", $"compressed_{file.FileName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compressing image");
            return StatusCode(500, "Error processing image");
        }
    }
}
