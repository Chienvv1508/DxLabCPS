using DxLabCoworkingSpace;
using Newtonsoft.Json;
using System.Text;

namespace JobService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public Worker(ILogger<Worker> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if(DateTime.Now.Day == DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month))
                    {
                        var requestData = new THJobExpenseDTO() { dateSum = DateTime.Now };
                        string jsonContent = JsonConvert.SerializeObject(requestData);
                        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                        string apiUrl = _configuration["APIJob"];
                        var response = await _httpClient.PostAsync(apiUrl, content, stoppingToken);
                        if (response.IsSuccessStatusCode)
                        {
                            _logger.LogInformation("Chạy thành công");
                        }
                        else
                            _logger.LogInformation("Chạy thất bại");
                    }
                   
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error calling API: {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }
    }
}
