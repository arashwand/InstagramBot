class InstagramDashboard {
    constructor() {
        this.charts = {};
        this.currentAccount = null;
        this.dateRange = {
            from: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000), // 30 days ago
            to: new Date()
        };

        this.init(); 0
    }

    async init() {
        await this.loadAccounts();
        this.setupEventListeners();
        this.setupDateInputs();

        // Load default account data if available
        if (this.currentAccount) {
            await this.loadDashboardData();
        }
    }

    async loadAccounts() {
        try {
            const response = await fetch('/api/instagram/accounts', {
                headers: {
                    'Authorization': `Bearer ${this.getAuthToken()}`
                }
            });

            if (response.ok) {
                const accounts = await response.json();
                const select = document.getElementById('accountSelect');

                accounts.forEach(account => {
                    const option = document.createElement('option');
                    option.value = account.id;
                    option.textContent = `@${account.instagramUsername}`;
                    select.appendChild(option);
                });

                if (accounts.length > 0) {
                    this.currentAccount = accounts[0].id;
                    select.value = this.currentAccount;
                }
            }
        } catch (error) {
            console.error('Error loading accounts:', error);
        }
    }

    setupEventListeners() {
        // Account selection
        document.getElementById('accountSelect').addEventListener('change', (e) => {
            this.currentAccount = e.target.value;
            if (this.currentAccount) {
                this.loadDashboardData();
            }
        });

        // Date range
        document.getElementById('fromDate').addEventListener('change', (e) => {
            this.dateRange.from = new Date(e.target.value);
            if (this.currentAccount) {
                this.loadDashboardData();
            }
        });

        document.getElementById('toDate').addEventListener('change', (e) => {
            this.dateRange.to = new Date(e.target.value);
            if (this.currentAccount) {
                this.loadDashboardData();
            }
        });

        // Refresh button
        document.getElementById('refreshData').addEventListener('click', () => {
            if (this.currentAccount) {
                this.loadDashboardData();
            }
        });

        // Navigation
        document.querySelectorAll('[data-section]').forEach(link => {
            link.addEventListener('click', (e) => {
                e.preventDefault();
                this.showSection(e.target.dataset.section);

                // Update active nav
                document.querySelectorAll('.nav-link').forEach(l => l.classList.remove('active'));
                e.target.classList.add('active');
            });
        });
    }

    setupDateInputs() {
        const fromInput = document.getElementById('fromDate');
        const toInput = document.getElementById('toDate');

        fromInput.value = this.dateRange.from.toISOString().split('T')[0];
        toInput.value = this.dateRange.to.toISOString().split('T')[0];
    }

    showSection(sectionName) {
        // Hide all sections
        document.querySelectorAll('.dashboard-section').forEach(section => {
            section.style.display = 'none';
        });

        // Show selected section
        const section = document.getElementById(`${sectionName}-section`);
        if (section) {
            section.style.display = 'block';

            // Load section-specific data
            this.loadSectionData(sectionName);
        }
    }

    async loadDashboardData() {
        if (!this.currentAccount) return;

        try {
            // Load overview data
            await this.loadOverviewData();

            // Load current section data
            const activeSection = document.querySelector('.nav-link.active')?.dataset.section || 'overview';
            await this.loadSectionData(activeSection);

        } catch (error) {
            console.error('Error loading dashboard data:', error);
        }
    }

    async loadOverviewData() {
        const params = new URLSearchParams({
            fromDate: this.dateRange.from.toISOString().split('T')[0],
            toDate: this.dateRange.to.toISOString().split('T')[0]
        });

        // Load account report
        const reportResponse = await fetch(`/api/reports/account/${this.currentAccount}?${params}`, {
            headers: { 'Authorization': `Bearer ${this.getAuthToken()}` }
        });

        if (reportResponse.ok) {
            const report = await reportResponse.json();
            this.updateOverviewMetrics(report);
            this.createEngagementTrendChart(report.engagementByHour);
            this.createFollowersGrowthChart(report.followersGrowth);
        }
    }

    async loadSectionData(sectionName) {
        if (!this.currentAccount) return;

        const params = new URLSearchParams({
            fromDate: this.dateRange.from.toISOString().split('T')[0],
            toDate: this.dateRange.to.toISOString().split('T')[0]
        });

        switch (sectionName) {
            case 'engagement':
                await this.loadEngagementData(params);
                break;
            case 'audience':
                await this.loadAudienceData(params);
                break;
            case 'content':
                await this.loadContentData(params);
                break;
            case 'hashtags':
                await this.loadHashtagData(params);
                break;
        }
    }

    async loadEngagementData(params) {
        // Load engagement trends
        const trendsResponse = await fetch(`/api/reports/account/${this.currentAccount}/engagement-trends?${params}`, {
            headers: { 'Authorization': `Bearer ${this.getAuthToken()}` }
        });

        if (trendsResponse.ok) {
            const trends = await trendsResponse.json();
            this.createHourlyEngagementChart(trends.trends);
        }

        // Load best posting times
        const timesResponse = await fetch(`/api/reports/account/${this.currentAccount}/best-posting-times`, {
            headers: { 'Authorization': `Bearer ${this.getAuthToken()}` }
        });

        if (timesResponse.ok) {
            const times = await timesResponse.json();
            this.createDailyEngagementChart(times.dailyPerformance);
        }

        // Load top posts
        const postsResponse = await fetch(`/api/reports/account/${this.currentAccount}/top-posts?${params}&count=10`, {
            headers: { 'Authorization': `Bearer ${this.getAuthToken()}` }
        });

        if (postsResponse.ok) {
            const posts = await postsResponse.json();
            this.displayTopPosts(posts);
        }
    }

    async loadAudienceData(params) {
        const response = await fetch(`/api/reports/account/${this.currentAccount}/audience-insights?${params}`, {
            headers: { 'Authorization': `Bearer ${this.getAuthToken()}` }
        });

        if (response.ok) {
            const insights = await response.json();
            this.createGenderChart(insights.gender);
            this.createAgeChart(insights.age);
            this.createCountryChart(insights.country);
        }
    }

    async loadHashtagData(params) {
        const response = await fetch(`/api/reports/account/${this.currentAccount}/hashtag-performance?${params}`, {
            headers: { 'Authorization': `Bearer ${this.getAuthToken()}` }
        });

        if (response.ok) {
            const data = await response.json();
            this.createHashtagPerformanceChart(data.hashtagStats);
            this.displayRecommendedHashtags(data.recommendations);
        }
    }

    updateOverviewMetrics(report) {
        document.getElementById('totalImpressions').textContent = this.formatNumber(report.totalImpressions);
        document.getElementById('totalReach').textContent = this.formatNumber(report.totalReach);
        document.getElementById('totalLikes').textContent = this.formatNumber(report.totalLikes);
        document.getElementById('avgEngagement').textContent = `${report.averageEngagementRate.toFixed(2)}%`;
    }

    createEngagementTrendChart(data) {
        const ctx = document.getElementById('engagementTrendChart').getContext('2d');

        if (this.charts.engagementTrend) {
            this.charts.engagementTrend.destroy();
        }

        const labels = Object.keys(data);
        const values = Object.values(data);

        this.charts.engagementTrend = new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [{
                    label: 'نرخ تعامل (%)',
                    data: values,
                    borderColor: 'rgb(75, 192, 192)',
                    backgroundColor: 'rgba(75, 192, 192, 0.2)',
                    tension: 0.4,
                    fill: true
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            callback: function (value) {
                                return value + '%';
                            }
                        }
                    }
                }
            }
        });
    }

    createFollowersGrowthChart(growthData) {
        const ctx = document.getElementById('followersGrowthChart').getContext('2d');

        if (this.charts.followersGrowth) {
            this.charts.followersGrowth.destroy();
        }

        this.charts.followersGrowth = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: ['رشد فالوور', 'فالوور قبلی'],
                datasets: [{
                    data: [growthData, 100 - growthData],
                    backgroundColor: [
                        'rgb(54, 162, 235)',
                        'rgb(201, 203, 207)'
                    ]
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom'
                    }
                }
            }
        });
    }

    createGenderChart(genderData) {
        const ctx = document.getElementById('genderChart').getContext('2d');

        if (this.charts.gender) {
            this.charts.gender.destroy();
        }

        this.charts.gender = new Chart(ctx, {
            type: 'pie',
            data: {
                labels: Object.keys(genderData),
                datasets: [{
                    data: Object.values(genderData),
                    backgroundColor: [
                        'rgb(255, 99, 132)',
                        'rgb(54, 162, 235)',
                        'rgb(255, 205, 86)'
                    ]
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom'
                    }
                }
            }
        });
    }

    createHashtagPerformanceChart(hashtagData) {
        const ctx = document.getElementById('hashtagPerformanceChart').getContext('2d');

        if (this.charts.hashtag) {
            this.charts.hashtag.destroy();
        }

        const labels = Object.keys(hashtagData).slice(0, 10);
        const values = labels.map(label => hashtagData[label].averageEngagement);

        this.charts.hashtag = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: 'میانگین تعامل (%)',
                    data: values,
                    backgroundColor: 'rgba(153, 102, 255, 0.8)',
                    borderColor: 'rgba(153, 102, 255, 1)',
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                indexAxis: 'y',
                plugins: {
                    legend: {
                        display: false
                    }
                },
                scales: {
                    x: {
                        beginAtZero: true,
                        ticks: {
                            callback: function (value) {
                                return value + '%';
                            }
                        }
                    }
                }
            }
        });
    }

    displayTopPosts(posts) {
        const container = document.getElementById('topPostsList');
        container.innerHTML = '';

        posts.forEach((post, index) => {
            const postElement = document.createElement('div');
            postElement.className = 'row mb-3 p-3 border rounded';
            postElement.innerHTML = `
                <div class="col-md-1">
                    <span class="badge bg-primary">${index + 1}</span>
                </div>
                <div class="col-md-3">
                    <img src="${post.thumbnailUrl || '/images/placeholder.jpg'}" class="img-fluid rounded" alt="Post">
                </div>
                <div class="col-md-6">
                    <p class="mb-1">${post.caption ? post.caption.substring(0, 100) + '...' : 'بدون کپشن'}</p>
                    <small class="text-muted">${new Date(post.date).toLocaleDateString('fa-IR')}</small>
                </div>
                <div class="col-md-2 text-end">
                    <div><i class="fas fa-heart text-danger"></i> ${this.formatNumber(post.likesCount)}</div>
                    <div><i class="fas fa-comment text-primary"></i> ${this.formatNumber(post.commentsCount)}</div>
                    <div><strong>${post.engagementRate.toFixed(2)}%</strong></div>
                </div>
            `;
            container.appendChild(postElement);
        });
    }

    displayRecommendedHashtags(hashtags) {
        const container = document.getElementById('recommendedHashtags');
        container.innerHTML = '<h6>هشتگ‌های پیشنهادی:</h6>';

        hashtags.forEach(hashtag => {
            const badge = document.createElement('span');
            badge.className = 'badge bg-secondary me-2 mb-2';
            badge.textContent = hashtag;
            container.appendChild(badge);
        });
    }

    formatNumber(num) {
        if (num >= 1000000) {
            return (num / 1000000).toFixed(1) + 'M';
        } else if (num >= 1000) {
            return (num / 1000).toFixed(1) + 'K';
        }
        return num.toString();
    }

    getAuthToken() {
        return localStorage.getItem('authToken') || '';
    }
}

// Initialize dashboard when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    new InstagramDashboard();
});
