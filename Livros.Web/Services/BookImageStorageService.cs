using Microsoft.AspNetCore.Http;

namespace Livros.Web.Services;

public sealed class BookImageStorageService {
    private readonly IWebHostEnvironment _environment;

    public BookImageStorageService(IWebHostEnvironment environment) {
        _environment = environment;
    }

    public string Save(IFormFile imageFile) {
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
        var webRootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var imageFolder = Path.Combine(webRootPath, "assets", "img");

        Directory.CreateDirectory(imageFolder);

        var filePath = Path.Combine(imageFolder, fileName);
        using var stream = new FileStream(filePath, FileMode.Create);
        imageFile.CopyTo(stream);

        return $"/assets/img/{fileName}";
    }
}
