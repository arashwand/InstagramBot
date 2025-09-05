using Hangfire;
using InstagramBot.Application.Services.Interfaces;
using InstagramBot.Core.Entities;
using InstagramBot.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InstagramBot.Application.Services
{
    public class PublishQueueService : IPublishQueueService
    {
        private readonly IPublishQueueRepository _queueRepository;
        private readonly IRateLimitRepository _rateLimitRepository;
        private readonly IPostPublishingService _publishingService;
        private readonly IAccountRepository _accountRepository;
        private readonly IPostRepository _postRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PublishQueueService> _logger;

        private readonly int _postsPerHour;
        private readonly int _postsPerDay;
        private readonly int _storiesPerDay;
        private readonly int _delayBetweenPosts;
        private readonly int _maxConcurrentJobs;

        public PublishQueueService(
            IPublishQueueRepository queueRepository,
            IRateLimitRepository rateLimitRepository,
            IPostPublishingService publishingService,
            IAccountRepository accountRepository,
            IPostRepository postRepository,
            IConfiguration configuration,
            ILogger<PublishQueueService> logger)
        {
            _queueRepository = queueRepository;
            _rateLimitRepository = rateLimitRepository;
            _publishingService = publishingService;
            _accountRepository = accountRepository;
            _postRepository = postRepository;
            _configuration = configuration;
            _logger = logger;

            _postsPerHour = _configuration.GetValue<int>("Instagram:RateLimits:PostsPerHour");
            _postsPerDay = _configuration.GetValue<int>("Instagram:RateLimits:PostsPerDay");
            _storiesPerDay = _configuration.GetValue<int>("Instagram:RateLimits:StoriesPerDay");
            _delayBetweenPosts = _configuration.GetValue<int>("Instagram:RateLimits:DelayBetweenPosts");
            _maxConcurrentJobs = _configuration.GetValue<int>("Instagram:QueueSettings:MaxConcurrentJobs");
        }

        public async Task<int> EnqueuePostAsync(int accountId, int postId, DateTime scheduledTime, string priority = "Normal")
        {
            try
            {
                // بررسی محدودیت‌های نرخ
                if (!await CheckRateLimitAsync(accountId, "Post"))
                {
                    // اگر محدودیت وجود دارد، زمان را به تاخیر بیندازیم
                    scheduledTime = await CalculateNextAvailableTimeAsync(accountId, "Post", scheduledTime);
                }

                var queueItem = new PublishQueue
                {
                    AccountId = accountId,
                    PostId = postId,
                    QueueType = "Post",
                    Priority = priority,
                    Status = "Pending",
                    ScheduledTime = scheduledTime,
                    AttemptCount = 0,
                    CreatedDate = DateTime.UtcNow
                };

                var savedItem = await _queueRepository.CreateAsync(queueItem);

                // زمان‌بندی Job در Hangfire
                BackgroundJob.Schedule<IPublishQueueService>(
                    service => service.ProcessQueueItemAsync(savedItem.Id),
                    scheduledTime);

                _logger.LogInformation("Post enqueued: QueueId {QueueId}, AccountId {AccountId}, PostId {PostId}",
                    savedItem.Id, accountId, postId);

                return savedItem.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enqueuing post: AccountId {AccountId}, PostId {PostId}", accountId, postId);
                throw;
            }
        }

        public async Task<int> EnqueueStoryAsync(int accountId, int postId, DateTime scheduledTime, string priority = "Normal")
        {
            try
            {
                if (!await CheckRateLimitAsync(accountId, "Story"))
                {
                    scheduledTime = await CalculateNextAvailableTimeAsync(accountId, "Story", scheduledTime);
                }

                var queueItem = new PublishQueue
                {
                    AccountId = accountId,
                    PostId = postId,
                    QueueType = "Story",
                    Priority = priority,
                    Status = "Pending",
                    ScheduledTime = scheduledTime,
                    AttemptCount = 0,
                    CreatedDate = DateTime.UtcNow
                };

                var savedItem = await _queueRepository.CreateAsync(queueItem);

                BackgroundJob.Schedule<IPublishQueueService>(
                    service => service.ProcessQueueItemAsync(savedItem.Id),
                    scheduledTime);

                _logger.LogInformation("Story enqueued: QueueId {QueueId}, AccountId {AccountId}, PostId {PostId}",
                    savedItem.Id, accountId, postId);

                return savedItem.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enqueuing story: AccountId {AccountId}, PostId {PostId}", accountId, postId);
                throw;
            }
        }

        [AutomaticRetry(Attempts = 0)] // خودمان retry را مدیریت می‌کنیم
        public async Task<bool> ProcessQueueItemAsync(int queueId)
        {
            var queueItem = await _queueRepository.GetByIdAsync(queueId);
            if (queueItem == null || queueItem.Status != "Pending")
            {
                _logger.LogWarning("Queue item {QueueId} not found or not pending", queueId);
                return false;
            }

            try
            {
                _logger.LogInformation("Processing queue item {QueueId}", queueId);

                // تغییر وضعیت به در حال پردازش
                queueItem.Status = "Processing";
                queueItem.AttemptCount++;
                await _queueRepository.UpdateAsync(queueItem);

                // بررسی مجدد محدودیت‌های نرخ
                if (!await CheckRateLimitAsync(queueItem.AccountId, queueItem.QueueType))
                {
                    _logger.LogInformation("Rate limit exceeded for queue item {QueueId}, rescheduling", queueId);

                    var nextTime = await CalculateNextAvailableTimeAsync(queueItem.AccountId, queueItem.QueueType, DateTime.UtcNow);
                    queueItem.Status = "Pending";
                    queueItem.ScheduledTime = nextTime;
                    await _queueRepository.UpdateAsync(queueItem);

                    // زمان‌بندی مجدد
                    BackgroundJob.Schedule<IPublishQueueService>(
                        service => service.ProcessQueueItemAsync(queueId),
                        nextTime);

                    return false;
                }

                // دریافت اطلاعات حساب و پست
                var account = await _accountRepository.GetByIdAsync(queueItem.AccountId);
                var post = await _postRepository.GetByIdAsync(queueItem.PostId);

                if (account == null || post == null)
                {
                    queueItem.Status = "Failed";
                    queueItem.ErrorMessage = "حساب یا پست یافت نشد.";
                    await _queueRepository.UpdateAsync(queueItem);
                    return false;
                }

                // انتشار پست
                var publishResult = await _publishingService.PublishPostAsync(post, account);

                if (publishResult.Success)
                {
                    // موفقیت‌آمیز
                    queueItem.Status = "Completed";
                    queueItem.ProcessedTime = DateTime.UtcNow;
                    await _queueRepository.UpdateAsync(queueItem);

                    // ثبت فعالیت برای Rate Limiting
                    await RecordActionAsync(queueItem.AccountId, queueItem.QueueType);

                    _logger.LogInformation("Queue item {QueueId} processed successfully", queueId);
                    return true;
                }
                else
                {
                    // خطا در انتشار
                    queueItem.ErrorMessage = publishResult.ErrorMessage;

                    // بررسی امکان تلاش مجدد
                    var maxRetries = _configuration.GetValue<int>("Instagram:QueueSettings:RetryAttempts");
                    if (queueItem.AttemptCount < maxRetries)
                    {
                        // تلاش مجدد
                        queueItem.Status = "Pending";
                        var retryDelay = _configuration.GetValue<int>("Instagram:QueueSettings:RetryDelay");
                        queueItem.ScheduledTime = DateTime.UtcNow.AddSeconds(retryDelay);
                        await _queueRepository.UpdateAsync(queueItem);

                        BackgroundJob.Schedule<IPublishQueueService>(
                            service => service.ProcessQueueItemAsync(queueId),
                            queueItem.ScheduledTime);

                        _logger.LogInformation("Queue item {QueueId} scheduled for retry (attempt {Attempt})",
                            queueId, queueItem.AttemptCount);
                    }
                    else
                    {
                        // تلاش‌های مجدد تمام شده
                        queueItem.Status = "Failed";
                        await _queueRepository.UpdateAsync(queueItem);

                        _logger.LogError("Queue item {QueueId} failed after {Attempts} attempts: {Error}",
                            queueId, queueItem.AttemptCount, publishResult.ErrorMessage);
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing queue item {QueueId}", queueId);

                queueItem.Status = "Failed";
                queueItem.ErrorMessage = ex.Message;
                await _queueRepository.UpdateAsync(queueItem);

                return false;
            }
        }

        public async Task<bool> CheckRateLimitAsync(int accountId, string actionType)
        {
            try
            {
                var now = DateTime.UtcNow;

                switch (actionType)
                {
                    case "Post":
                        // بررسی محدودیت ساعتی
                        var postsLastHour = await _rateLimitRepository.GetActionCountAsync(
                            accountId, "Post", now.AddHours(-1), now);

                        if (postsLastHour >= _postsPerHour)
                            return false;

                        // بررسی محدودیت روزانه
                        var postsLastDay = await _rateLimitRepository.GetActionCountAsync(
                            accountId, "Post", now.AddDays(-1), now);

                        if (postsLastDay >= _postsPerDay)
                            return false;

                        // بررسی تاخیر بین پست‌ها
                        var lastPost = await _rateLimitRepository.GetLastActionAsync(accountId, "Post");
                        if (lastPost != null && (now - lastPost.ActionTime).TotalSeconds < _delayBetweenPosts)
                            return false;

                        break;

                    case "Story":
                        var storiesLastDay = await _rateLimitRepository.GetActionCountAsync(
                            accountId, "Story", now.AddDays(-1), now);

                        if (storiesLastDay >= _storiesPerDay)
                            return false;
                        break;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking rate limit for account {AccountId}, action {ActionType}",
                    accountId, actionType);
                return false;
            }
        }

        private async Task<DateTime> CalculateNextAvailableTimeAsync(int accountId, string actionType, DateTime requestedTime)
        {
            var now = DateTime.UtcNow;
            var nextAvailableTime = requestedTime;

            switch (actionType)
            {
                case "Post":
                    // بررسی آخرین پست و اضافه کردن تاخیر
                    var lastPost = await _rateLimitRepository.GetLastActionAsync(accountId, "Post");
                    if (lastPost != null)
                    {
                        var minNextTime = lastPost.ActionTime.AddSeconds(_delayBetweenPosts);
                        if (nextAvailableTime < minNextTime)
                            nextAvailableTime = minNextTime;
                    }

                    // بررسی محدودیت ساعتی
                    var postsLastHour = await _rateLimitRepository.GetActionCountAsync(
                        accountId, "Post", now.AddHours(-1), now);

                    if (postsLastHour >= _postsPerHour)
                    {
                        var oldestPostInHour = await _rateLimitRepository.GetOldestActionInPeriodAsync(
                            accountId, "Post", now.AddHours(-1));

                        if (oldestPostInHour != null)
                        {
                            var hourlyResetTime = oldestPostInHour.ActionTime.AddHours(1);
                            if (nextAvailableTime < hourlyResetTime)
                                nextAvailableTime = hourlyResetTime;
                        }
                    }

                    break;

                case "Story":
                    var storiesLastDay = await _rateLimitRepository.GetActionCountAsync(
                        accountId, "Story", now.AddDays(-1), now);

                    if (storiesLastDay >= _storiesPerDay)
                    {
                        var oldestStoryInDay = await _rateLimitRepository.GetOldestActionInPeriodAsync(
                            accountId, "Story", now.AddDays(-1));

                        if (oldestStoryInDay != null)
                        {
                            var dailyResetTime = oldestStoryInDay.ActionTime.AddDays(1);
                            if (nextAvailableTime < dailyResetTime)
                                nextAvailableTime = dailyResetTime;
                        }
                    }
                    break;
            }

            return nextAvailableTime;
        }

        private async Task RecordActionAsync(int accountId, string actionType)
        {
            var rateLimitRecord = new RateLimitTracker
            {
                AccountId = accountId,
                ActionType = actionType,
                ActionTime = DateTime.UtcNow,
                IpAddress = "Server" // یا IP واقعی سرور
            };

            await _rateLimitRepository.CreateAsync(rateLimitRecord);
        }

        public async Task<List<PublishQueue>> GetQueueStatusAsync(int accountId)
        {
            return await _queueRepository.GetByAccountIdAsync(accountId);
        }

        public async Task<bool> CancelQueueItemAsync(int queueId)
        {
            try
            {
                var queueItem = await _queueRepository.GetByIdAsync(queueId);
                if (queueItem == null || queueItem.Status != "Pending")
                    return false;

                queueItem.Status = "Canceled";
                await _queueRepository.UpdateAsync(queueItem);

                // لغو Job در Hangfire (اگر امکان‌پذیر باشد)
                // BackgroundJob.Delete(jobId); // نیاز به ذخیره Job ID دارد

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error canceling queue item {QueueId}", queueId);
                return false;
            }
        }

        public async Task ProcessPendingQueueAsync()
        {
            try
            {
                _logger.LogInformation("Processing pending queue items");

                var pendingItems = await _queueRepository.GetPendingItemsAsync(DateTime.UtcNow);
                var processedCount = 0;

                foreach (var item in pendingItems.Take(_maxConcurrentJobs))
                {
                    BackgroundJob.Enqueue<IPublishQueueService>(
                        service => service.ProcessQueueItemAsync(item.Id));

                    processedCount++;
                }

                _logger.LogInformation("Processed {Count} pending queue items", processedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pending queue");
            }
        }
    }
}

