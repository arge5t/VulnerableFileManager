using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using System.Diagnostics;
using VulnerableFileManager.Models;

namespace VulnerableFileManager.Controllers
{
    public class HomeController : Controller
    {
        private readonly string _uploadPath;

        public HomeController(IWebHostEnvironment env)
        {
            _uploadPath = Path.Combine(env.WebRootPath, "uploadss");

            Directory.CreateDirectory(_uploadPath);
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Download(string file)
        {
            try
            {
                var path = Path.Combine(_uploadPath, file);

                if (!System.IO.File.Exists(path))
                {
                    TempData["Error"] = "Файл не найден";
                    return RedirectToAction("Index");
                }

                var fileName = Path.GetFileName(path);
                var mimeType = GetMimeType(fileName);

                return PhysicalFile(path, mimeType, fileName);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public IActionResult CreateDirectory(string currentPath, string folderName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(folderName))
                {
                    TempData["Error"] = "Имя папки не может быть пустым";
                    return RedirectToAction("Index");
                }

                var sanitizedPath = Path.Combine(
                    _uploadPath,
                    currentPath ?? string.Empty,
                    folderName
                );

                var fullPath = Path.GetFullPath(sanitizedPath);
                if (!fullPath.StartsWith(_uploadPath))
                {
                    TempData["Error"] = "Недопустимый путь";
                    return RedirectToAction("Index");
                }

                Directory.CreateDirectory(fullPath);
                TempData["Message"] = $"Папка создана: {fullPath}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка создания папки: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Upload(string path)
        {
            if (Request.Form.Files.Count == 0)
            {
                TempData["Error"] = "Ошибка: файл не выбран";
                return RedirectToAction("Index");
            }

            var file = Request.Form.Files[0];

            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Ошибка: файл имеет нулевой размер";
                return RedirectToAction("Index");
            }

            try
            {
                var fullPath = Path.Combine(_uploadPath, path, file.FileName);

                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                TempData["Message"] = $"Файл успешно загружен: {fullPath}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка загрузки: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        public IActionResult List(string dir)
        {
            var fullPath = Path.Combine(_uploadPath, dir);
            ViewBag.Files = Directory.GetFiles(fullPath);
            ViewBag.Dirs = Directory.GetDirectories(fullPath);
            ViewBag.CurrentDir = dir;
            return View();
        }

        private string GetMimeType(string fileName)
        {
            var provider = new FileExtensionContentTypeProvider();
            return provider.TryGetContentType(fileName, out var mimeType)
                ? mimeType
                : "application/octet-stream";
        }
    }
}
