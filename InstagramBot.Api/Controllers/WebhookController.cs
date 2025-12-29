using InstagramBot.Application.Services.Interfaces;
using InstagramBot.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace InstagramBot.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WebhookController : ControllerBase
    {
        private readonly IWebhookProcessingService _webhookService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WebhookController> _logger;

        private readonly string _verifyToken;

        public WebhookController(
            IWebhookProcessingService webhookService,
            IConfiguration configuration,
            ILogger<WebhookController> logger)
        {
            _webhookService = webhookService;
            _configuration = configuration;
            _logger = logger;

            _verifyToken = _configuration["Instagram:WebhookVerifyToken"];
        }

        [HttpGet]
        public IActionResult VerifyWebhook([FromQuery] string hub_mode, [FromQuery] string hub_challenge, [FromQuery] string hub_verify_token)
        {
            _logger.LogInformation("Webhook verification request received");

            if (hub_mode == "subscribe" && hub_verify_token == _verifyToken)
            {
                _logger.LogInformation("Webhook verification successful");
                return Ok(hub_challenge);
            }

            _logger.LogWarning("Webhook verification failed. Mode: {Mode}, Token: {Token}", hub_mode, hub_verify_token);
            return Unauthorized();
        }

        [HttpPost]
        public async Task<IActionResult> ReceiveWebhook()
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var payload = await reader.ReadToEndAsync();

                if (string.IsNullOrEmpty(payload))
                {
                    _logger.LogWarning("Empty webhook payload received");
                    return BadRequest("Empty payload");
                }

                // بررسی امضای وب‌هوک
                var signature = Request.Headers["X-Hub-Signature-256"].FirstOrDefault();
                if (!_webhookService.VerifySignature(payload, signature))
                {
                    _logger.LogWarning("Invalid webhook signature");
                    return Unauthorized("Invalid signature");
                }

                // پارس کردن JSON
                var webhook = JsonSerializer.Deserialize<InstagramWebhookDto>(payload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });

                if (webhook == null)
                {
                    _logger.LogWarning("Failed to parse webhook payload");
                    return BadRequest("Invalid payload format");
                }

                // پردازش وب‌هوک
                await _webhookService.ProcessWebhookAsync(webhook, signature);

                _logger.LogInformation("Webhook processed successfully");
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
