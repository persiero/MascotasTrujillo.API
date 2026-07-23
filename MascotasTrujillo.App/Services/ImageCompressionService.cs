using SkiaSharp;
using System.Diagnostics;

namespace MascotasTrujillo.App.Services
{
    public class ImagenComprimidaResultado
    {
        public string RutaLocal { get; set; } = string.Empty;
        public byte[] Bytes { get; set; } = Array.Empty<byte>();
        public double PesoOriginalMb { get; set; }
        public double PesoFinalMb { get; set; }
    }

    public static class ImageCompressionService
    {
        public static async Task<ImagenComprimidaResultado> ComprimirFileResultAsync(
            FileResult foto,
            string prefijoArchivo = "imagen",
            int maxMb = 5)
        {
            byte[] bytesOriginales;

            await using (Stream sourceStream = await foto.OpenReadAsync())
            using (MemoryStream memoryStream = new MemoryStream())
            {
                await sourceStream.CopyToAsync(memoryStream);
                bytesOriginales = memoryStream.ToArray();
            }

            int maxBytes = maxMb * 1024 * 1024;

            byte[] bytesComprimidos = ComprimirImagenConLimite(
                bytesOriginales,
                maxBytes
            );

            string nombreArchivo = $"{prefijoArchivo}_{Guid.NewGuid():N}.jpg";
            string rutaLocal = Path.Combine(FileSystem.CacheDirectory, nombreArchivo);

            await File.WriteAllBytesAsync(rutaLocal, bytesComprimidos);

            double pesoOriginalMb = bytesOriginales.Length / 1024.0 / 1024.0;
            double pesoFinalMb = bytesComprimidos.Length / 1024.0 / 1024.0;

            Debug.WriteLine($"Imagen original: {pesoOriginalMb:0.00} MB");
            Debug.WriteLine($"Imagen comprimida: {pesoFinalMb:0.00} MB");

            return new ImagenComprimidaResultado
            {
                RutaLocal = rutaLocal,
                Bytes = bytesComprimidos,
                PesoOriginalMb = pesoOriginalMb,
                PesoFinalMb = pesoFinalMb
            };
        }

        private static byte[] ComprimirImagenConLimite(byte[] bytesOriginales, int maxBytes)
        {
            int[] tamaniosMaximos = { 1280, 1080, 900, 720 };
            int[] calidades = { 80, 75, 70, 65, 60 };

            byte[]? mejorResultado = null;

            foreach (int tamanio in tamaniosMaximos)
            {
                foreach (int calidad in calidades)
                {
                    byte[] comprimida = ComprimirImagenJpeg(
                        bytesOriginales,
                        maxWidth: tamanio,
                        maxHeight: tamanio,
                        calidad: calidad
                    );

                    mejorResultado = comprimida;

                    if (comprimida.Length <= maxBytes)
                        return comprimida;
                }
            }

            double pesoMb = (mejorResultado?.Length ?? bytesOriginales.Length) / 1024.0 / 1024.0;
            double maxMb = maxBytes / 1024.0 / 1024.0;

            throw new Exception(
                $"La imagen sigue pesando {pesoMb:0.00} MB después de comprimirla. " +
                $"El máximo permitido es {maxMb:0.00} MB. Intenta con otra foto."
            );
        }

        private static byte[] ComprimirImagenJpeg(
            byte[] bytesOriginales,
            int maxWidth,
            int maxHeight,
            int calidad)
        {
            using var inputStream = new SKMemoryStream(bytesOriginales);
            using var bitmapOriginal = SKBitmap.Decode(inputStream);

            if (bitmapOriginal == null)
            {
                throw new Exception(
                    "No se pudo leer la imagen. Intenta con una foto en formato JPG o PNG."
                );
            }

            int anchoOriginal = bitmapOriginal.Width;
            int altoOriginal = bitmapOriginal.Height;

            double ratioAncho = (double)maxWidth / anchoOriginal;
            double ratioAlto = (double)maxHeight / altoOriginal;
            double ratio = Math.Min(ratioAncho, ratioAlto);

            if (ratio > 1)
                ratio = 1;

            int nuevoAncho = Math.Max(1, (int)(anchoOriginal * ratio));
            int nuevoAlto = Math.Max(1, (int)(altoOriginal * ratio));

            using var bitmapRedimensionado = new SKBitmap(
                nuevoAncho,
                nuevoAlto,
                bitmapOriginal.ColorType,
                bitmapOriginal.AlphaType
            );

            bitmapOriginal.ScalePixels(
                bitmapRedimensionado,
                new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear)
            );

            using var imagen = SKImage.FromBitmap(bitmapRedimensionado);
            using var data = imagen.Encode(SKEncodedImageFormat.Jpeg, calidad);

            if (data == null)
            {
                throw new Exception("No se pudo comprimir la imagen.");
            }

            return data.ToArray();
        }
    }
}