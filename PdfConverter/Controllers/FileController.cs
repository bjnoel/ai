using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using ImageMagick;

namespace PdfConverter.Controllers
{
    public class FileController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ConvertAndDownload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Invalid file.");

            string outputFileName = $"output_{Path.GetFileNameWithoutExtension(file.FileName)}.pdf";
            string outputPath = Path.Combine(Path.GetTempPath(), outputFileName);

            using (var inputStream = file.OpenReadStream())
            using (var outputStream = System.IO.File.Create(outputPath))
            {
                await ConvertPdf(inputStream, outputStream);
            }

            return PhysicalFile(outputPath, "application/pdf", outputFileName);
        }

        private async Task ConvertPdf(Stream inputFile, Stream outputFile)
        {
            MagickReadSettings settings = new MagickReadSettings
            {
                Density = new Density(130),
                Format = MagickFormat.Pdf
            };

            using (var images = new MagickImageCollection(inputFile, settings))
            {
                foreach (MagickImage image in images)
                {
                    image.Rotate(GetRandomRotation());

                    // Apply linear stretch
                    image.LinearStretch(new Percentage(1.5), new Percentage(2));

                    image.Modulate(new Percentage(98), new Percentage(100), new Percentage(100)); // Adjust brightness
                    image.ColorSpace = ColorSpace.CMYK;

                    // Create a clone of the original image to apply noise
                    using (var noisyImage = image.Clone())
                    {
                        noisyImage.AddNoise(NoiseType.Poisson);

                        // Adjust the opacity of the noisy image (1/20 opacity)
                        noisyImage.Evaluate(Channels.Alpha, EvaluateOperator.Multiply, 0.05);

                        // Blend the original image with the noisy image
                        image.Composite(noisyImage, CompositeOperator.Over);
                    }
                }

                images.Write(outputFile, MagickFormat.Pdf);
            }
        }



        private double GetRandomRotation()
        {
            var random = new System.Random();
            double rotation = random.Next(0, 2) == 0 ? -1 : 1;
            rotation *= random.NextDouble() * 0.45 + 0.05;
            return rotation;
        }
    }
}