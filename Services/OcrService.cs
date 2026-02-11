using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Jot.Services
{
    /// <summary>
    /// Servicio para reconocimiento óptico de caracteres (OCR)
    /// </summary>
    public class OcrService
    {
        private OcrEngine _ocrEngine;

        public OcrService()
        {
            // Inicializar OCR con el idioma del sistema
            try
            {
                _ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
                if (_ocrEngine == null)
                {
                    // Si falla, usar inglés por defecto
                    var englishLanguage = new Windows.Globalization.Language("en");
                    _ocrEngine = OcrEngine.TryCreateFromLanguage(englishLanguage);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing OCR: {ex.Message}");
            }
        }

        /// <summary>
        /// Extrae texto de una imagen usando OCR
        /// </summary>
        public async Task<string> ExtractTextFromImageAsync(string imagePath)
        {
            try
            {
                if (_ocrEngine == null)
                {
                    return "[OCR not available]";
                }

                if (!File.Exists(imagePath))
                {
                    return "[Image file not found]";
                }

                // Cargar la imagen
                var file = await StorageFile.GetFileFromPathAsync(imagePath);
                using (var stream = await file.OpenAsync(FileAccessMode.Read))
                {
                    // Decodificar la imagen
                    var decoder = await BitmapDecoder.CreateAsync(stream);
                    var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

                    // Convertir si es necesario
                    if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 ||
                        softwareBitmap.BitmapAlphaMode != BitmapAlphaMode.Premultiplied)
                    {
                        softwareBitmap = SoftwareBitmap.Convert(
                            softwareBitmap,
                            BitmapPixelFormat.Bgra8,
                            BitmapAlphaMode.Premultiplied
                        );
                    }

                    // Ejecutar OCR
                    var ocrResult = await _ocrEngine.RecognizeAsync(softwareBitmap);

                    // Extraer texto con formato
                    var extractedText = "";
                    foreach (var line in ocrResult.Lines)
                    {
                        extractedText += line.Text + "\n";
                    }

                    return extractedText.Trim();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error extracting text from image: {ex.Message}");
                return $"[OCR Error: {ex.Message}]";
            }
        }

        /// <summary>
        /// Extrae texto de una imagen desde un Stream
        /// </summary>
        public async Task<string> ExtractTextFromStreamAsync(IRandomAccessStream stream)
        {
            try
            {
                if (_ocrEngine == null)
                {
                    return "[OCR not available]";
                }

                // Decodificar la imagen
                var decoder = await BitmapDecoder.CreateAsync(stream);
                var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

                // Convertir si es necesario
                if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 ||
                    softwareBitmap.BitmapAlphaMode != BitmapAlphaMode.Premultiplied)
                {
                    softwareBitmap = SoftwareBitmap.Convert(
                        softwareBitmap,
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Premultiplied
                    );
                }

                // Ejecutar OCR
                var ocrResult = await _ocrEngine.RecognizeAsync(softwareBitmap);

                // Extraer texto con formato
                var extractedText = "";
                foreach (var line in ocrResult.Lines)
                {
                    extractedText += line.Text + "\n";
                }

                return extractedText.Trim();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error extracting text from stream: {ex.Message}");
                return $"[OCR Error: {ex.Message}]";
            }
        }

        /// <summary>
        /// Extrae texto de múltiples imágenes
        /// </summary>
        public async Task<string> ExtractTextFromMultipleImagesAsync(string[] imagePaths)
        {
            try
            {
                var allText = "";
                for (int i = 0; i < imagePaths.Length; i++)
                {
                    allText += $"--- Image {i + 1} ---\n";
                    var text = await ExtractTextFromImageAsync(imagePaths[i]);
                    allText += text + "\n\n";
                }
                return allText.Trim();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error extracting text from multiple images: {ex.Message}");
                return $"[Error: {ex.Message}]";
            }
        }

        /// <summary>
        /// Verifica si OCR está disponible para un idioma específico
        /// </summary>
        public bool IsLanguageSupported(string languageTag)
        {
            try
            {
                var language = new Windows.Globalization.Language(languageTag);
                return OcrEngine.IsLanguageSupported(language);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Obtiene los idiomas soportados por OCR
        /// </summary>
        public string[] GetSupportedLanguages()
        {
            try
            {
                return OcrEngine.AvailableRecognizerLanguages
                    .Select(l => l.DisplayName)
                    .ToArray();
            }
            catch
            {
                return new string[] { "No languages available" };
            }
        }

        /// <summary>
        /// Cambia el idioma de OCR
        /// </summary>
        public bool SetLanguage(string languageTag)
        {
            try
            {
                var language = new Windows.Globalization.Language(languageTag);
                if (OcrEngine.IsLanguageSupported(language))
                {
                    _ocrEngine = OcrEngine.TryCreateFromLanguage(language);
                    return _ocrEngine != null;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting OCR language: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Extrae texto del portapapeles si contiene una imagen
        /// </summary>
        public async Task<string> ExtractTextFromClipboardAsync()
        {
            try
            {
                var dataPackageView = Windows.ApplicationModel.DataTransfer.Clipboard.GetContent();
                
                if (dataPackageView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Bitmap))
                {
                    var bitmap = await dataPackageView.GetBitmapAsync();
                    using (var stream = await bitmap.OpenReadAsync())
                    {
                        return await ExtractTextFromStreamAsync(stream);
                    }
                }

                return "[No image in clipboard]";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error extracting text from clipboard: {ex.Message}");
                return $"[Error: {ex.Message}]";
            }
        }
    }
}
