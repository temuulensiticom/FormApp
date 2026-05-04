using System.Text;

namespace fromApp.Services
{
    public class FileUploadService
    {
        private readonly string _uploadFolder;
        private readonly long _maxFileSize = 5242880;
        private readonly string[] _allowedExtensions = new[] { ".sql", ".txt" };

        public FileUploadService()
        {
            _uploadFolder = Path.Combine(AppContext.BaseDirectory, "uploads");
            if (!Directory.Exists(_uploadFolder))
            {
                Directory.CreateDirectory(_uploadFolder);
            }
        }

        public async Task<FileUploadResult> UploadAndReadFileAsync(string filePath)
        {
            var result = new FileUploadResult();

            try
            {
                var validation = ValidateFile(filePath);
                if (!validation.IsValid)
                {
                    result.HasError = true;
                    result.Message = validation.Message;
                    return result;
                }

                var fileName = Path.GetFileName(filePath);
                var destinationPath = Path.Combine(_uploadFolder, fileName);
                var fileIndex = 1;

                while (File.Exists(destinationPath))
                {
                    var nameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                    var extension = Path.GetExtension(filePath);
                    fileName = $"{nameWithoutExt}_{fileIndex}{extension}";
                    destinationPath = Path.Combine(_uploadFolder, fileName);
                    fileIndex++;
                    result.IsDuplicate = true;
                    result.DuplicateMessage = $"File name already exists. Saved as: {fileName}";
                }

                using (var stream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    await sourceStream.CopyToAsync(stream);
                }

                using (var reader = new StreamReader(destinationPath, Encoding.UTF8))
                {
                    result.Content = await reader.ReadToEndAsync();
                }

                result.FileName = fileName;
                result.FilePath = destinationPath;
                result.HasError = false;
                result.Message = "File uploaded and read successfully!";
            }
            catch (Exception ex)
            {
                result.HasError = true;
                result.Message = $"Error processing file: {ex.Message}";
            }

            return result;
        }

        public async Task<FileUploadResult> SaveGeneratedFileAsync(string content, string fileNameHint)
        {
            var result = new FileUploadResult();

            try
            {
                var cleanHint = string.Join("_", fileNameHint.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
                var fileName = string.IsNullOrWhiteSpace(cleanHint) ? "generated_schemas.sql" : $"{cleanHint}.sql";
                var destinationPath = Path.Combine(_uploadFolder, fileName);
                var fileIndex = 1;

                while (File.Exists(destinationPath))
                {
                    var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                    fileName = $"{nameWithoutExt}_{fileIndex}.sql";
                    destinationPath = Path.Combine(_uploadFolder, fileName);
                    fileIndex++;
                    result.IsDuplicate = true;
                    result.DuplicateMessage = $"File name already exists. Saved as: {fileName}";
                }

                await File.WriteAllTextAsync(destinationPath, content, Encoding.UTF8);

                result.FileName = fileName;
                result.FilePath = destinationPath;
                result.Content = content;
                result.HasError = false;
                result.Message = "Generated SQL file saved successfully.";
            }
            catch (Exception ex)
            {
                result.HasError = true;
                result.Message = $"Error saving generated file: {ex.Message}";
            }

            return result;
        }

        private ValidationResult ValidateFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return new ValidationResult { IsValid = false, Message = "No file selected or file does not exist." };
            }

            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length == 0)
            {
                return new ValidationResult { IsValid = false, Message = "File is empty." };
            }

            if (fileInfo.Length > _maxFileSize)
            {
                return new ValidationResult { IsValid = false, Message = $"File size exceeds limit of {_maxFileSize / (1024 * 1024)}MB." };
            }

            var extension = fileInfo.Extension.ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
            {
                return new ValidationResult { IsValid = false, Message = $"File type not allowed. Allowed types: {string.Join(", ", _allowedExtensions)}" };
            }

            return new ValidationResult { IsValid = true, Message = "File is valid." };
        }

        public void DeleteFile(string fileName)
        {
            try
            {
                var filePath = Path.Combine(_uploadFolder, fileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch
            {
            }
        }
    }

    public class FileUploadResult
    {
        public bool HasError { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public bool IsDuplicate { get; set; }
        public string DuplicateMessage { get; set; } = string.Empty;
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
