// Capturer Dashboard JavaScript
class CapturerDashboard {
    constructor() {
        this.connection = null;
        this.activityChart = null;
        this.organizationId = '00000000-0000-0000-0000-000000000001'; // Default organization
        this.refreshInterval = null;
        this.isConnected = false;
        
        this.init();
    }

    async init() {
        console.log('🚀 Initializing Capturer Dashboard...');
        
        // Initialize SignalR connection
        await this.initSignalR();
        
        // Load initial data
        await this.loadDashboardData();
        
        // Initialize chart
        this.initActivityChart();
        
        // Start periodic refresh
        this.startPeriodicRefresh();
        
        // Setup event listeners
        this.setupEventListeners();
        
        console.log('✅ Dashboard initialized successfully');
    }

    async initSignalR() {
        try {
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/dashboardHub")
                .withAutomaticReconnect()
                .build();

            // Setup event handlers
            this.setupSignalRHandlers();

            // Start connection
            await this.connection.start();
            
            // Join organization group
            await this.connection.invoke("JoinOrganizationGroup", this.organizationId);
            
            this.updateConnectionStatus(true);
            console.log('✅ SignalR connected');
        } catch (error) {
            console.error('❌ SignalR connection failed:', error);
            this.updateConnectionStatus(false);
            
            // Retry connection after 5 seconds
            setTimeout(() => this.initSignalR(), 5000);
        }
    }

    setupSignalRHandlers() {
        // Real-time report received
        this.connection.on("ReportReceived", (data) => {
            console.log('📊 New report received:', data);
            this.showNotification(`Nuevo reporte de ${data.ComputerName}`, 'info');
            this.refreshDashboardData();
        });

        // Connection state changes
        this.connection.onclose(() => {
            this.updateConnectionStatus(false);
            console.log('❌ SignalR disconnected');
        });

        this.connection.onreconnecting(() => {
            this.updateConnectionStatus(false);
            console.log('🔄 SignalR reconnecting...');
        });

        this.connection.onreconnected(() => {
            this.updateConnectionStatus(true);
            this.connection.invoke("JoinOrganizationGroup", this.organizationId);
            console.log('✅ SignalR reconnected');
        });
    }

    updateConnectionStatus(connected) {
        this.isConnected = connected;
        const statusElement = document.getElementById('connectionStatus');
        const iconElement = document.getElementById('connectionIcon');
        
        if (connected) {
            statusElement.textContent = 'Conectado';
            statusElement.className = 'nav-link connected';
            iconElement.className = 'fas fa-circle text-success';
        } else {
            statusElement.textContent = 'Desconectado';
            statusElement.className = 'nav-link disconnected';
            iconElement.className = 'fas fa-circle text-danger pulse';
        }
    }

    async loadDashboardData() {
        try {
            await Promise.all([
                this.loadOverviewData(),
                this.loadComputersData(),
                this.loadAlertsData()
            ]);
        } catch (error) {
            console.error('❌ Error loading dashboard data:', error);
            this.showNotification('Error cargando datos del dashboard', 'error');
        }
    }

    async loadOverviewData() {
        try {
            const response = await fetch('/api/dashboard/overview');
            const data = await response.json();
            
            this.updateOverviewCards(data.summary);
        } catch (error) {
            console.error('❌ Error loading overview:', error);
        }
    }

    updateOverviewCards(summary) {
        document.getElementById('totalComputers').textContent = summary.totalComputers;
        document.getElementById('onlineComputers').textContent = summary.onlineComputers;
        document.getElementById('todayReports').textContent = summary.todayReports;
        document.getElementById('averageActivity').textContent = `${summary.averageActivity}%`;
    }

    async loadComputersData() {
        try {
            // Get computers with real-time status from Capturer APIs
            const response = await fetch('/api/capturer-clients/status');
            const data = await response.json();
            
            this.updateComputersTable(data.computers);
            this.updateConnectionStats(data);
        } catch (error) {
            console.error('❌ Error loading computers:', error);
            
            // Fallback to basic computer list if live status fails
            try {
                const fallbackResponse = await fetch('/api/computers');
                const computers = await fallbackResponse.json();
                this.updateComputersTable(computers);
            } catch (fallbackError) {
                console.error('❌ Fallback computers loading also failed:', fallbackError);
            }
        }
    }

    updateConnectionStats(data) {
        // Update overview cards with real-time data
        document.getElementById('totalComputers').textContent = data.totalComputers;
        document.getElementById('onlineComputers').textContent = data.onlineComputers;
        
        // Add visual indicator for computer connectivity health
        const connectivityRatio = data.totalComputers > 0 ? 
            (data.onlineComputers / data.totalComputers) * 100 : 0;
            
        const connectionElement = document.getElementById('connectionStatus');
        if (connectivityRatio >= 80) {
            connectionElement.classList.add('text-success');
            connectionElement.classList.remove('text-warning', 'text-danger');
        } else if (connectivityRatio >= 50) {
            connectionElement.classList.add('text-warning');
            connectionElement.classList.remove('text-success', 'text-danger');
        } else {
            connectionElement.classList.add('text-danger');
            connectionElement.classList.remove('text-success', 'text-warning');
        }
    }

    updateComputersTable(computers) {
        const tbody = document.getElementById('computersTableBody');
        
        if (computers.length === 0) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="5" class="text-center text-muted">
                        <i class="fas fa-desktop fa-3x mb-2"></i>
                        <p>No hay computadoras registradas</p>
                    </td>
                </tr>
            `;
            return;
        }

        tbody.innerHTML = computers.map(computer => `
            <tr class="fade-in">
                <td>
                    <strong>${computer.name}</strong>
                    <br>
                    <small class="text-muted">${computer.computerId}</small>
                </td>
                <td>
                    <span class="badge ${this.getStatusBadgeClass(computer.isOnline)}">
                        ${computer.isOnline ? '🟢 En línea' : '🔴 Desconectado'}
                    </span>
                </td>
                <td>
                    ${computer.lastSeenAt ? 
                        new Date(computer.lastSeenAt).toLocaleString('es-ES') : 
                        'Nunca'
                    }
                </td>
                <td>
                    <span class="badge bg-secondary">${computer.organization.name}</span>
                </td>
                <td>
                    <div class="btn-group" role="group">
                        <button class="btn btn-outline-primary btn-sm" 
                                onclick="dashboard.viewComputerDetail('${computer.id}')"
                                title="Ver detalles">
                            <i class="fas fa-eye"></i>
                        </button>
                        <button class="btn btn-outline-success btn-sm" 
                                onclick="dashboard.triggerComputerCapture('${computer.id}')"
                                title="Capturar screenshot"
                                ${computer.connection?.isReachable ? '' : 'disabled'}>
                            <i class="fas fa-camera"></i>
                        </button>
                    </div>
                </td>
            </tr>
        `).join('');
    }

    getStatusBadgeClass(isOnline) {
        return isOnline ? 'badge-online' : 'badge-offline';
    }

    async loadAlertsData() {
        try {
            const response = await fetch('/api/dashboard/alerts?limit=5');
            const alerts = await response.json();
            
            this.updateAlertsContainer(alerts);
        } catch (error) {
            console.error('❌ Error loading alerts:', error);
        }
    }

    updateAlertsContainer(alerts) {
        const container = document.getElementById('alertsContainer');
        
        if (alerts.length === 0) {
            container.innerHTML = `
                <div class="text-center text-muted">
                    <i class="fas fa-bell-slash fa-3x"></i>
                    <p>No hay alertas</p>
                </div>
            `;
            return;
        }

        container.innerHTML = alerts.map(alert => `
            <div class="alert-item severity-${alert.severity.toLowerCase()} fade-in">
                <div class="d-flex justify-content-between">
                    <div>
                        <strong>${alert.severityIcon} ${alert.title}</strong>
                        <br>
                        <small>${alert.description}</small>
                        <br>
                        <small class="alert-time">
                            ${new Date(alert.createdAt).toLocaleString('es-ES')}
                        </small>
                    </div>
                    ${!alert.isAcknowledged ? `
                        <button class="btn btn-sm btn-outline-secondary" 
                                onclick="dashboard.acknowledgeAlert('${alert.id}')">
                            <i class="fas fa-check"></i>
                        </button>
                    ` : ''}
                </div>
            </div>
        `).join('');
    }

    initActivityChart() {
        const ctx = document.getElementById('activityChart');
        if (!ctx) return;

        this.activityChart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: [],
                datasets: [{
                    label: 'Actividad Promedio (%)',
                    data: [],
                    borderColor: '#0066CC',
                    backgroundColor: 'rgba(0, 102, 204, 0.1)',
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
                        max: 100,
                        ticks: {
                            callback: function(value) {
                                return value + '%';
                            }
                        }
                    },
                    x: {
                        ticks: {
                            maxTicksLimit: 10
                        }
                    }
                },
                interaction: {
                    intersect: false
                }
            }
        });

        this.loadActivityChartData();
    }

    async loadActivityChartData() {
        try {
            const response = await fetch('/api/dashboard/activity-timeline?hours=24');
            const data = await response.json();
            
            if (data.timeline && data.timeline.length > 0) {
                const labels = data.timeline.map(point => 
                    new Date(point.timestamp).toLocaleTimeString('es-ES', { 
                        hour: '2-digit', 
                        minute: '2-digit' 
                    })
                );
                const values = data.timeline.map(point => point.averageActivity);
                
                this.activityChart.data.labels = labels;
                this.activityChart.data.datasets[0].data = values;
                this.activityChart.update();
            }
        } catch (error) {
            console.error('❌ Error loading chart data:', error);
        }
    }

    setupEventListeners() {
        // Global refresh function
        window.refreshData = () => this.refreshDashboardData();
        
        // Global functions for table actions
        window.dashboard = this;
        
        // Request notification permission
        if ('Notification' in window && Notification.permission === 'default') {
            Notification.requestPermission();
        }
    }

    startPeriodicRefresh() {
        // Refresh every 30 seconds
        this.refreshInterval = setInterval(() => {
            if (this.isConnected) {
                this.loadOverviewData();
                this.loadActivityChartData();
            } else {
                this.loadDashboardData();
            }
        }, 30000);
    }

    async refreshDashboardData() {
        console.log('🔄 Refreshing dashboard data...');
        await this.loadDashboardData();
        await this.loadActivityChartData();
        this.showNotification('Datos actualizados', 'success');
    }

    viewComputerDetail(computerId) {
        // For now, just show an alert. Later this can navigate to a detail page
        this.showNotification(`Detalle de computadora: ${computerId}`, 'info');
        console.log('🖥️ View computer detail:', computerId);
    }

    async triggerComputerCapture(computerId) {
        try {
            const response = await fetch(`/api/capturer-clients/${computerId}/capture`, {
                method: 'POST'
            });
            
            if (response.ok) {
                const result = await response.json();
                this.showNotification(`📸 ${result.message}`, 'success');
            } else {
                const error = await response.json();
                throw new Error(error.error || 'Capture failed');
            }
        } catch (error) {
            console.error('❌ Error triggering capture:', error);
            this.showNotification(`Error al capturar: ${error.message}`, 'error');
        }
    }

    async syncAllComputers() {
        try {
            const response = await fetch('/api/capturer-clients/sync', {
                method: 'POST'
            });
            
            if (response.ok) {
                const result = await response.json();
                this.showNotification('📊 Sincronización iniciada', 'success');
                
                // Refresh data after a short delay to show synced data
                setTimeout(() => this.refreshDashboardData(), 3000);
            } else {
                const error = await response.json();
                throw new Error(error.error || 'Sync failed');
            }
        } catch (error) {
            console.error('❌ Error starting sync:', error);
            this.showNotification(`Error en sincronización: ${error.message}`, 'error');
        }
    }

    async acknowledgeAlert(alertId) {
        try {
            const response = await fetch(`/api/dashboard/alerts/${alertId}/acknowledge`, {
                method: 'PUT'
            });
            
            if (response.ok) {
                this.showNotification('Alerta marcada como leída', 'success');
                await this.loadAlertsData();
            } else {
                throw new Error('Failed to acknowledge alert');
            }
        } catch (error) {
            console.error('❌ Error acknowledging alert:', error);
            this.showNotification('Error al marcar alerta', 'error');
        }
    }

    showNotification(message, type = 'info') {
        // Show browser notification if permitted
        if ('Notification' in window && Notification.permission === 'granted') {
            new Notification('Capturer Dashboard', {
                body: message,
                icon: 'images/icon-192.png'
            });
        }

        // Show toast notification
        const toastContainer = document.getElementById('toastContainer');
        const toastId = 'toast-' + Date.now();
        
        const typeClasses = {
            success: 'bg-success text-white',
            error: 'bg-danger text-white',
            warning: 'bg-warning text-dark',
            info: 'bg-info text-white'
        };

        const toastHTML = `
            <div class="toast fade show" id="${toastId}" role="alert">
                <div class="toast-header ${typeClasses[type] || typeClasses.info}">
                    <i class="fas fa-bell me-2"></i>
                    <strong class="me-auto">Capturer Dashboard</strong>
                    <small>${new Date().toLocaleTimeString('es-ES')}</small>
                    <button type="button" class="btn-close" onclick="document.getElementById('${toastId}').remove()"></button>
                </div>
                <div class="toast-body ${typeClasses[type] || typeClasses.info}">
                    ${message}
                </div>
            </div>
        `;

        toastContainer.insertAdjacentHTML('beforeend', toastHTML);

        // Auto remove after 5 seconds
        setTimeout(() => {
            const toast = document.getElementById(toastId);
            if (toast) {
                toast.remove();
            }
        }, 5000);
    }

    destroy() {
        if (this.refreshInterval) {
            clearInterval(this.refreshInterval);
        }
        
        if (this.connection) {
            this.connection.stop();
        }
    }
}

// Initialize dashboard when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    window.dashboard = new CapturerDashboard();
});

// Cleanup on page unload
window.addEventListener('beforeunload', () => {
    if (window.dashboard) {
        window.dashboard.destroy();
    }
});