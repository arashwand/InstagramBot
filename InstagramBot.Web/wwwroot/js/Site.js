// Chart.js configurations and functions
window.initializeEngagementChart = (canvasId) => {
    const ctx = document.getElementById(canvasId).getContext('2d');

    // نمونه داده برای نمودار تعاملات
    const data = {
        labels: ['شنبه', 'یکشنبه', 'دوشنبه', 'سه‌شنبه', 'چهارشنبه', 'پنج‌شنبه', 'جمعه'],
        datasets: [
            {
                label: 'لایک‌ها',
                data: [120, 190, 300, 500, 200, 300, 450],
                borderColor: 'rgb(255, 99, 132)',
                backgroundColor: 'rgba(255, 99, 132, 0.1)',
                tension: 0.4
            },
            {
                label: 'کامنت‌ها',
                data: [20, 35, 45, 78, 32, 48, 67],
                borderColor: 'rgb(54, 162, 235)',
                backgroundColor: 'rgba(54, 162, 235, 0.1)',
                tension: 0.4
            },
            {
                label: 'اشتراک‌گذاری',
                data: [5, 12, 18, 25, 15, 22, 28],
                borderColor: 'rgb(75, 192, 192)',
                backgroundColor: 'rgba(75, 192, 192, 0.1)',
                tension: 0.4
            }
        ]
    };

    const config = {
        type: 'line',
        data: data,
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                title: {
                    display: false
                },
                legend: {
                    position: 'bottom',
                    labels: {
                        usePointStyle: true,
                        padding: 20
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    grid: {
                        color: 'rgba(0, 0, 0, 0.1)'
                    }
                },
                x: {
                    grid: {
                        display: false
                    }
                }
            },
            elements: {
                point: {
                    radius: 4,
                    hoverRadius: 6
                }
            }
        }
    };

    window.engagementChart = new Chart(ctx, config);
};

window.initializeFollowersChart = (canvasId) => {
    const ctx = document.getElementById(canvasId).getContext('2d');

    // نمونه داده برای نمودار فالوورها
    const data = {
        labels: ['فروردین', 'اردیبهشت', 'خرداد', 'تیر', 'مرداد', 'شهریور'],
        datasets: [{
            label: 'فالوورها',
            data: [1200, 1350, 1500, 1680, 1820, 1950],
            borderColor: 'rgb(147, 51, 234)',
            backgroundColor: 'rgba(147, 51, 234, 0.1)',
            fill: true,
            tension: 0.4
        }]
    };

    const config = {
        type: 'line',
        data: data,
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                title: {
                    display: false
                },
                legend: {
                    display: false
                }
            },
            scales: {
                y: {
                    beginAtZero: false,
                    grid: {
                        color: 'rgba(0, 0, 0, 0.1)'
                    }
                },
                x: {
                    grid: {
                        display: false
                    }
                }
            },
            elements: {
                point: {
                    radius: 4,
                    hoverRadius: 6
                }
            }
        }
    };

    window.followersChart = new Chart(ctx, config);
};

window.refreshCharts = () => {
    if (window.engagementChart) {
        window.engagementChart.update();
    }
    if (window.followersChart) {
        window.followersChart.update();
    }
};

window.exportChart = (canvasId, filename) => {
    const canvas = document.getElementById(canvasId);
    const url = canvas.toDataURL('image/png');
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
};

// SignalR Connection Helper
window.blazorSignalR = {
    connections: {},

    createConnection: (hubUrl) => {
        const connection = new signalR.HubConnectionBuilder()
            .withUrl(hubUrl)
            .build();

        return connection;
    },

    startConnection: async (connection) => {
        try {
            await connection.start();
            console.log('SignalR Connected');
            return true;
        } catch (err) {
            console.error('SignalR Connection Error: ', err);
            return false;
        }
    },

    stopConnection: async (connection) => {
        try {
            await connection.stop();
            console.log('SignalR Disconnected');
        } catch (err) {
            console.error('SignalR Disconnection Error: ', err);
        }
    }
};

// Utility Functions
window.blazorUtils = {
    // نمایش Toast Notification
    showToast: (message, type = 'info', duration = 5000) => {
        const toastContainer = document.querySelector('.toast-container');
        if (!toastContainer) return;

        const toastId = 'toast-' + Date.now();
        const iconClass = {
            'success': 'fa-check-circle text-success',
            'error': 'fa-times-circle text-danger',
            'warning': 'fa-exclamation-triangle text-warning',
            'info': 'fa-info-circle text-info'
        }[type] || 'fa-info-circle text-info';

        const toastHtml = `
            <div id="${toastId}" class="toast show" role="alert">
                <div class="toast-header">
                    <i class="fas ${iconClass} me-2"></i>
                    <strong class="me-auto">${type.charAt(0).toUpperCase() + type.slice(1)}</strong>
                    <small>الان</small>
                    <button type="button" class="btn-close" onclick="blazorUtils.hideToast('${toastId}')"></button>
                </div>
                <div class="toast-body">
                    ${message}
                </div>
            </div>
        `;

        toastContainer.insertAdjacentHTML('beforeend', toastHtml);

        // حذف خودکار پس از مدت زمان مشخص
        setTimeout(() => {
            blazorUtils.hideToast(toastId);
        }, duration);
    },

    hideToast: (toastId) => {
        const toast = document.getElementById(toastId);
        if (toast) {
            toast.classList.remove('show');
            setTimeout(() => {
                toast.remove();
            }, 300);
        }
    },

    // کپی متن به کلیپ‌بورد
    copyToClipboard: async (text) => {
        try {
            await navigator.clipboard.writeText(text);
            blazorUtils.showToast('متن کپی شد', 'success');
            return true;
        } catch (err) {
            console.error('Failed to copy: ', err);
            blazorUtils.showToast('خطا در کپی کردن', 'error');
            return false;
        }
    },

    // تأیید حذف
    confirmDelete: (message = 'آیا از حذف این آیتم اطمینان دارید؟') => {
        return confirm(message);
    },

    // نمایش/مخفی کردن Loading Spinner
    showLoading: (elementId) => {
        const element = document.getElementById(elementId);
        if (element) {
            element.innerHTML = `
                <div class="loading-spinner">
                    <div class="spinner"></div>
                </div>
            `;
        }
    },

    hideLoading: (elementId) => {
        const element = document.getElementById(elementId);
        if (element) {
            element.innerHTML = '';
        }
    },

    // اسکرول به بالای صفحه
    scrollToTop: () => {
        window.scrollTo({
            top: 0,
            behavior: 'smooth'
        });
    },

    // فرمت کردن اعداد
    formatNumber: (number) => {
        return new Intl.NumberFormat('fa-IR').format(number);
    },

    // فرمت کردن تاریخ
    formatDate: (dateString) => {
        const date = new Date(dateString);
        return new Intl.DateTimeFormat('fa-IR').format(date);
    }
};

// Initialize Bootstrap tooltips and popovers
document.addEventListener('DOMContentLoaded', function () {
    // Initialize tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Initialize popovers
    var popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
    var popoverList = popoverTriggerList.map(function (popoverTriggerEl) {
        return new bootstrap.Popover(popoverTriggerEl);
    });
});

// Handle responsive sidebar
window.addEventListener('resize', function () {
    const sidebar = document.querySelector('.sidebar');
    const content = document.querySelector('.content');
    const topRow = document.querySelector('.top-row');

    if (window.innerWidth <= 768) {
        sidebar.classList.add('mobile');
        content.style.marginLeft = '0';
        topRow.style.marginLeft = '0';
    } else {
        sidebar.classList.remove('mobile');
        content.style.marginLeft = '260px';
        topRow.style.marginLeft = '260px';
    }
});
