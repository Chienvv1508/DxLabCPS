using DxLabCoworkingSpace;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobService
{
    public class DepreciationTHBackGroundTask : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public DepreciationTHBackGroundTask(ILogger<Worker> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
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
                    int.TryParse(_configuration["DepreciationTH:Start"], out int start);
                    _logger.LogInformation($"Khấu hao: {start}");
                    int.TryParse(_configuration["DepreciationTH:End"], out int end);
                    _logger.LogInformation($"{end}");
                    int.TryParse(DateTime.Now.ToString("HHmm"), out int realTime);
                    _logger.LogInformation($"Khấu hao: {realTime}");
                   
                    if (DateTime.Now.Day == DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month) && realTime >= start && realTime <= end)
                    {
                        string apiUrl = _configuration["DepreciationTH:APIDepreciationTH"];
                        var response = await _httpClient.PostAsync(apiUrl, null, stoppingToken);
                        if (response.IsSuccessStatusCode)
                        {
                            _logger.LogInformation("Chạy thành công khấu hao");
                        }
                        else
                            _logger.LogInformation("Chạy thất bại");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error calling API: {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromMinutes(20), stoppingToken);
            }
        }
    }
}
