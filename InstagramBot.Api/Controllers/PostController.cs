using InstagramBot.Application.DTOs;
using InstagramBot.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InstagramBot.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PostController : ControllerBase
    {
        private readonly IPostSchedulingService _schedulingService;
        private readonly IPostPublishingService _publishingService;
        private readonly IMediaFileService _mediaService;

        public PostController(
            IPostSchedulingService schedulingService,
            IPostPublishingService publishingService,
            IMediaFileService mediaService)
        {
            _schedulingService = schedulingService;
            _publishingService = publishingService;
            _mediaService = mediaService;
        }

        [HttpPost("schedule")]
        public async Task<IActionResult> SchedulePost([FromBody] SchedulePostDto scheduleDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            try
            {
                var result = await _schedulingService.SchedulePostAsync(scheduleDto, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("publish-now")]
        public async Task<IActionResult> PublishNow([FromBody] PostPublishDto publishDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            try
            {
                var result = await _publishingService.PublishPostNowAsync(publishDto, userId);

                if (result.Success)
                {
                    return Ok(new { Message = "پست با موفقیت منتشر شد.", Result = result });
                }
                else
                {
                    return BadRequest(new { Error = result.ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("publish-carousel")]
        public async Task<IActionResult> PublishCarousel([FromBody] CarouselPostDto carouselDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            try
            {
                var result = await _publishingService.PublishCarouselPostAsync(carouselDto, userId);

                if (result.Success)
                {
                    return Ok(new { Message = "کاروسل با موفقیت منتشر شد.", Result = result });
                }
                else
                {
                    return BadRequest(new { Error = result.ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("scheduled")]
        public async Task<IActionResult> GetScheduledPosts([FromQuery] int? accountId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            try
            {
                var posts = await _schedulingService.GetScheduledPostsAsync(userId, accountId);
                return Ok(posts);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPut("scheduled/{postId}")]
        public async Task<IActionResult> UpdateScheduledPost(int postId, [FromBody] UpdateScheduledPostDto updateDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            try
            {
                var result = await _schedulingService.UpdateScheduledPostAsync(postId, updateDto, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpDelete("scheduled/{postId}")]
        public async Task<IActionResult> CancelScheduledPost(int postId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            try
            {
                var result = await _schedulingService.CancelScheduledPostAsync(postId, userId);

                if (result)
                {
                    return Ok(new { Message = "پست زمان‌بندی‌شده لغو شد." });
                }
                else
                {
                    return NotFound(new { Error = "پست یافت نشد یا قابل لغو نیست." });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("upload-media")]
        public async Task<IActionResult> UploadMedia([FromForm] MediaUploadDto uploadDto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            try
            {
                var result = await _mediaService.UploadMediaAsync(uploadDto, userId);

                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(new { Errors = result.Errors });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("media")]
        public async Task<IActionResult> GetUserMedia([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            try
            {
                var media = await _mediaService.GetUserMediaAsync(userId, page, pageSize);
                return Ok(media);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
