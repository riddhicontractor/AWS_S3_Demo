using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Model.Internal.MarshallTransformations;
using Amazon.S3.Util;
using Microsoft.AspNetCore.Mvc;
using ThirdParty.BouncyCastle.Asn1;

namespace AWS_S3_Demo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };
        private readonly IAmazonS3 s3Client;
        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(
            IAmazonS3 s3Client,
            ILogger<WeatherForecastController> logger)
        {
            this.s3Client = s3Client;
            _logger = logger;
        }

        public string BucketName = "aws-s3-demo-data";

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [HttpGet("GetFile")] //retrieve file
        public async Task<IActionResult> GetFile(string fileName)
        {
            var response = await s3Client.GetObjectAsync(BucketName, fileName); // retrieve file from bucket

            //using var reader = new StreamReader(response.ResponseStream);
            //var fileContents = await reader.ReadToEndAsync(); //read content of file in code

            return File(response.ResponseStream, response.Headers.ContentType);
        }

        [HttpGet("GetFiles")] //retrieve mutiple files

        public async Task<IActionResult> GetFiles(string prefix)
        {
            var request = new ListObjectsV2Request()
            {
                BucketName = BucketName,
                Prefix = prefix
            };
            var response = await s3Client.ListObjectsV2Async(request); // retrieve multiple files from bucket

            var presignedUrls = response.S3Objects.Select(o =>
            {
                var request = new GetPreSignedUrlRequest()
                {
                    BucketName = BucketName,
                    Key = o.Key,
                    Expires = DateTime.Now.AddSeconds(60)
                };
                return s3Client.GetPreSignedURL(request);
            });
            return Ok(presignedUrls);
        }

        [HttpPost] //send file
        public async Task Post(IFormFile formFile)
        {
            var bucketExists = await AmazonS3Util.DoesS3BucketExistV2Async(s3Client, BucketName);
            if (!bucketExists)
            {
                var bucketRequest = new PutBucketRequest()
                {
                    BucketName = BucketName,
                    UseClientRegion = true
                };
                await s3Client.PutBucketAsync(bucketRequest); // create a new bucket in AWS
            }

            var objectRequest = new PutObjectRequest()
            {
                BucketName = BucketName,
                Key = $"{DateTime.Now:yyyyMMddhhmmss}-{formFile.FileName}",
                InputStream = formFile.OpenReadStream()
            };
            var response = await s3Client.PutObjectAsync(objectRequest); //add object in bucket
        }

        [HttpDelete]

        public async Task Delete(string fileName) //delete file
        {
            await s3Client.DeleteObjectAsync(BucketName, fileName);
        }
    }
}