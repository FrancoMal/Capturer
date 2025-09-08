# 📊 Plan Completo: Dashboard Web para Capturer

## 🎯 Visión General del Proyecto

### Objetivo
Crear un dashboard web administrativo que permita monitorear múltiples instancias de Capturer desde cualquier dispositivo (PC, móvil, tablet), proporcionando visualización de datos en tiempo real, análisis histórico y administración centralizada.

### Arquitectura Propuesta
Sistema dual con separación de responsabilidades:
- **Capturer**: Aplicación de escritorio enfocada en captura de pantallas
- **Dashboard Web**: Aplicación web para visualización y administración

---

## 🏗️ Arquitectura del Sistema

```
┌─────────────────┐    API REST    ┌─────────────────────┐
│   🖥️ CAPTURER   │◄─────────────►│  🌐 DASHBOARD WEB   │
│   (Cada PC)     │                │   (Servidor único)  │
└─────────────────┘                └─────────────────────┘
│                                                        │
├── Captura pantallas                    ├── Web API REST
├── Genera Activity Reports              ├── Base de datos SQLite/PostgreSQL
├── API HTTP mínima (Puerto 8080)        ├── Gráficos interactivos (Chart.js)
├── Envío automático de datos            ├── SignalR (tiempo real)
└── Autenticación por API Key            └── Progressive Web App (PWA)
```

---

## 🛠️ Stack Tecnológico

### Dashboard Web (Nueva aplicación)
- **Backend**: ASP.NET Core 8 Web API + MVC
- **Frontend**: HTML5 + CSS3 + JavaScript vanilla
- **Gráficos**: Chart.js o ApexCharts
- **Base de datos**: SQLite (inicio) → PostgreSQL (producción)
- **Tiempo real**: SignalR para actualizaciones live
- **Autenticación**: ASP.NET Core Identity
- **Mobile**: Progressive Web App (PWA)
- **Hosting**: Compatible con Azure, AWS, Digital Ocean

### Capturer Modificado (Aplicación actual)
- **API REST**: ASP.NET Core Hosted Service
- **Cliente HTTP**: HttpClient para envío de datos
- **Formato de datos**: JSON estructurado
- **Puerto**: 8080 (configurable)
- **Seguridad**: Autenticación por API Key
- **Envío**: Automático post-generación de reportes

---

## 🗄️ Modelo de Datos

### Schema de Base de Datos

```sql
-- Computadoras registradas en el sistema
CREATE TABLE Computers (
    Id UUID PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    IP VARCHAR(15),
    ApiKey VARCHAR(255) UNIQUE,
    LastSeen DATETIME,
    IsActive BOOLEAN DEFAULT TRUE,
    Version VARCHAR(20),
    OS VARCHAR(50),
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Reportes de actividad de cada computadora
CREATE TABLE ActivityReports (
    Id UUID PRIMARY KEY,
    ComputerId UUID REFERENCES Computers(Id),
    ReportDate DATE NOT NULL,
    StartTime DATETIME NOT NULL,
    EndTime DATETIME NOT NULL,
    SessionName VARCHAR(100),
    TotalQuadrants INT DEFAULT 0,
    TotalComparisons BIGINT DEFAULT 0,
    TotalActivities BIGINT DEFAULT 0,
    AverageActivityRate DECIMAL(5,2),
    JsonData TEXT, -- Reporte completo en JSON
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    
    UNIQUE(ComputerId, ReportDate)
);

-- Configuración de cuadrantes por computadora
CREATE TABLE Quadrants (
    Id UUID PRIMARY KEY,
    ComputerId UUID REFERENCES Computers(Id),
    ConfigurationName VARCHAR(100),
    Name VARCHAR(50) NOT NULL,
    X INT NOT NULL,
    Y INT NOT NULL,
    Width INT NOT NULL,
    Height INT NOT NULL,
    Color VARCHAR(7) DEFAULT '#0066CC',
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Entradas detalladas por cuadrante (para gráficos detallados)
CREATE TABLE QuadrantEntries (
    Id UUID PRIMARY KEY,
    ActivityReportId UUID REFERENCES ActivityReports(Id),
    QuadrantName VARCHAR(50) NOT NULL,
    TotalComparisons BIGINT DEFAULT 0,
    ActivityDetectionCount BIGINT DEFAULT 0,
    ActivityRate DECIMAL(5,2) DEFAULT 0,
    AverageChangePercentage DECIMAL(5,2) DEFAULT 0,
    ActiveDuration TIME,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Métricas en tiempo real (opcional, para dashboard live)
CREATE TABLE RealTimeMetrics (
    Id UUID PRIMARY KEY,
    ComputerId UUID REFERENCES Computers(Id),
    Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
    MetricType VARCHAR(50), -- 'activity_rate', 'screenshot_count', etc.
    QuadrantName VARCHAR(50),
    Value DECIMAL(10,2),
    Metadata TEXT -- JSON adicional si es necesario
);

-- Configuración del sistema y usuarios
CREATE TABLE Users (
    Id UUID PRIMARY KEY,
    Username VARCHAR(50) UNIQUE NOT NULL,
    Email VARCHAR(100) UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    Role VARCHAR(20) DEFAULT 'User', -- 'Admin', 'User', 'Viewer'
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);
```

---

## 📱 Progressive Web App (PWA)

### ¿Por qué PWA en lugar de app nativa?
- **Una sola codebase** para todos los dispositivos
- **Instalable** como aplicación en móvil
- **Funciona offline** con datos en caché
- **Push notifications** para alertas
- **Más económico** de desarrollar y mantener
- **Updates automáticos** sin app stores

### Configuración PWA

#### manifest.json
```json
{
  "name": "Capturer Dashboard",
  "short_name": "Dashboard",
  "description": "Monitor y administra tus sistemas Capturer",
  "start_url": "/",
  "display": "standalone",
  "background_color": "#ffffff",
  "theme_color": "#0066cc",
  "orientation": "portrait-primary",
  "icons": [
    {
      "src": "/icons/icon-72.png",
      "sizes": "72x72",
      "type": "image/png"
    },
    {
      "src": "/icons/icon-192.png",
      "sizes": "192x192",
      "type": "image/png"
    },
    {
      "src": "/icons/icon-512.png",
      "sizes": "512x512",
      "type": "image/png"
    }
  ]
}
```

#### Service Worker básico
```javascript
// sw.js
const CACHE_NAME = 'capturer-dashboard-v1';
const urlsToCache = [
  '/',
  '/css/dashboard.css',
  '/js/dashboard.js',
  '/js/chart.min.js'
];

self.addEventListener('install', event => {
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then(cache => cache.addAll(urlsToCache))
  );
});
```

---

## 🔧 Implementación por Fases

### Fase 1: API en Capturer (Semanas 1-2)

#### Objetivos
- Crear API HTTP mínima en Capturer
- Establecer endpoints básicos para envío de datos
- Implementar autenticación por API Key
- Configurar envío automático de Activity Reports

#### Tareas específicas
1. **Crear CapturerApiService**
   ```csharp
   public class CapturerApiService : BackgroundService
   {
       private readonly HttpClient _httpClient;
       private readonly IConfiguration _config;
       
       protected override async Task ExecuteAsync(CancellationToken stoppingToken)
       {
           var builder = WebApplication.CreateBuilder();
           var app = builder.Build();
           
           // Middlewares básicos
           app.UseAuthentication();
           app.UseCors();
           
           // Endpoints mínimos
           app.MapGet("/api/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }));
           app.MapGet("/api/status", GetSystemStatus);
           app.MapGet("/api/activity/latest", GetLatestActivityReport);
           app.MapPost("/api/activity/sync", SyncToDashboard);
           
           await app.RunAsync("http://localhost:8080", stoppingToken);
       }
   }
   ```

2. **Integrar con ActivityReportService**
   - Modificar generación de reportes para envío automático
   - Configurar retry logic para fallos de red
   - Logging de sincronización

3. **Configuración**
   ```json
   {
     "CapturerApi": {
       "Port": 8080,
       "ApiKey": "generated-unique-key",
       "DashboardUrl": "https://dashboard.local/api/capturer",
       "AutoSync": true,
       "SyncIntervalMinutes": 5
     }
   }
   ```

#### Entregables
- [ ] API HTTP funcionando en puerto 8080
- [ ] Envío automático de Activity Reports
- [ ] Configuración por appsettings.json
- [ ] Logs de sincronización
- [ ] Documentación de endpoints

### Fase 2: Dashboard Web Base (Semanas 3-5)

#### Objetivos
- Crear aplicación web ASP.NET Core
- Implementar base de datos con Entity Framework
- API REST para frontend
- Sistema de autenticación básico

#### Estructura del proyecto
```
CapturerDashboard/
├── Controllers/
│   ├── HomeController.cs
│   ├── ComputersController.cs
│   ├── ReportsController.cs
│   └── ApiController.cs
├── Models/
│   ├── Computer.cs
│   ├── ActivityReport.cs
│   ├── Quadrant.cs
│   └── ViewModels/
├── Data/
│   ├── DashboardContext.cs
│   └── Migrations/
├── Services/
│   ├── ReportService.cs
│   ├── ComputerService.cs
│   └── NotificationService.cs
├── wwwroot/
│   ├── css/
│   ├── js/
│   ├── icons/
│   └── manifest.json
└── Views/
    ├── Home/
    ├── Computers/
    └── Reports/
```

#### API Endpoints principales
```csharp
// Gestión de computadoras
GET    /api/computers                 // Lista de computadoras
POST   /api/computers/register        // Registrar nueva computadora
PUT    /api/computers/{id}            // Actualizar computadora
DELETE /api/computers/{id}            // Eliminar computadora

// Reportes de actividad
GET    /api/reports                   // Lista de reportes (con filtros)
GET    /api/reports/{id}              // Detalle de reporte específico
POST   /api/reports                  // Recibir reporte desde Capturer
GET    /api/reports/stats             // Estadísticas agregadas

// Dashboard en tiempo real
GET    /api/dashboard/overview        // Vista general de todos los PCs
GET    /api/dashboard/computer/{id}   // Detalle específico de un PC
GET    /api/dashboard/alerts          // Alertas y notificaciones
```

#### Entregables
- [ ] Aplicación web funcionando
- [ ] Base de datos configurada
- [ ] API REST completa
- [ ] Sistema de autenticación
- [ ] Registro automático de computadoras

### Fase 3: Frontend + Visualizaciones (Semanas 6-7)

#### Objetivos
- Interfaz web responsive
- Gráficos interactivos con Chart.js
- Dashboard en tiempo real con SignalR
- Optimización para móviles

#### Tipos de gráficos implementados

##### 1. Dashboard Principal - Overview
```javascript
// Gráfico de rosquilla - Estado general de computadoras
const overviewChart = {
    type: 'doughnut',
    data: {
        labels: ['Activas', 'Inactivas', 'Con problemas'],
        datasets: [{
            data: [12, 2, 1],
            backgroundColor: ['#28a745', '#6c757d', '#dc3545'],
            borderWidth: 2
        }]
    },
    options: {
        responsive: true,
        plugins: {
            legend: {
                position: 'bottom'
            }
        }
    }
};

// Timeline de actividad agregada
const activityTimelineChart = {
    type: 'line',
    data: {
        labels: ['09:00', '10:00', '11:00', '12:00', '13:00', '14:00', '15:00', '16:00'],
        datasets: [{
            label: 'Actividad Promedio (%)',
            data: [45, 67, 78, 85, 72, 88, 79, 65],
            borderColor: '#007bff',
            backgroundColor: 'rgba(0, 123, 255, 0.1)',
            tension: 0.4
        }]
    },
    options: {
        responsive: true,
        scales: {
            y: {
                beginAtZero: true,
                max: 100,
                ticks: {
                    callback: function(value) {
                        return value + '%';
                    }
                }
            }
        }
    }
};
```

##### 2. Vista por Computadora
```javascript
// Gráfico de barras - Actividad por cuadrante
const quadrantActivityChart = {
    type: 'bar',
    data: {
        labels: ['Trabajo', 'Dashboard', 'Comunicación', 'Personal'],
        datasets: [{
            label: 'Screenshots tomados',
            data: [1250, 890, 234, 123],
            backgroundColor: ['#28a745', '#007bff', '#ffc107', '#6c757d']
        }, {
            label: 'Actividad detectada',
            data: [856, 623, 145, 45],
            backgroundColor: ['rgba(40, 167, 69, 0.6)', 'rgba(0, 123, 255, 0.6)', 
                             'rgba(255, 193, 7, 0.6)', 'rgba(108, 117, 125, 0.6)']
        }]
    },
    options: {
        responsive: true,
        scales: {
            x: {
                stacked: false
            },
            y: {
                stacked: false,
                beginAtZero: true
            }
        }
    }
};

// Heatmap semanal de actividad
const weeklyHeatmapChart = {
    type: 'scatter',
    data: {
        datasets: [{
            label: 'Actividad por hora/día',
            data: [
                {x: 'Lun', y: 9, v: 85},
                {x: 'Lun', y: 10, v: 92},
                // ... más datos
            ],
            backgroundColor: function(context) {
                const value = context.parsed.v;
                return `rgba(0, 123, 255, ${value/100})`;
            }
        }]
    }
};
```

##### 3. Análisis Histórico
```javascript
// Gráfico de área - Tendencia de actividad mensual
const monthlyTrendChart = {
    type: 'line',
    data: {
        labels: ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun'],
        datasets: [{
            label: 'Actividad Promedio',
            data: [72, 75, 78, 82, 79, 85],
            borderColor: '#007bff',
            backgroundColor: 'rgba(0, 123, 255, 0.1)',
            fill: true
        }, {
            label: 'Screenshots/día',
            data: [450, 520, 580, 620, 580, 680],
            borderColor: '#28a745',
            backgroundColor: 'rgba(40, 167, 69, 0.1)',
            fill: true,
            yAxisID: 'y1'
        }]
    },
    options: {
        responsive: true,
        scales: {
            y: {
                type: 'linear',
                display: true,
                position: 'left'
            },
            y1: {
                type: 'linear',
                display: true,
                position: 'right',
                grid: {
                    drawOnChartArea: false
                }
            }
        }
    }
};
```

#### Layout responsive
```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Capturer Dashboard</title>
    <link rel="manifest" href="/manifest.json">
    <link href="/css/bootstrap.min.css" rel="stylesheet">
    <link href="/css/dashboard.css" rel="stylesheet">
</head>
<body>
    <!-- Navegación -->
    <nav class="navbar navbar-expand-lg navbar-dark bg-primary">
        <div class="container-fluid">
            <a class="navbar-brand" href="#">📊 Capturer Dashboard</a>
            <div class="d-flex">
                <span class="navbar-text me-3">👤 Admin</span>
                <button class="btn btn-outline-light btn-sm">Cerrar Sesión</button>
            </div>
        </div>
    </nav>

    <!-- Contenido principal -->
    <div class="container-fluid mt-4">
        <div class="row">
            <!-- Sidebar (oculto en móvil) -->
            <div class="col-md-3 col-lg-2 d-none d-md-block">
                <div class="list-group">
                    <a href="#" class="list-group-item active">🏠 Dashboard</a>
                    <a href="#" class="list-group-item">🖥️ Computadoras</a>
                    <a href="#" class="list-group-item">📊 Reportes</a>
                    <a href="#" class="list-group-item">⚙️ Configuración</a>
                </div>
            </div>
            
            <!-- Contenido principal -->
            <div class="col-md-9 col-lg-10">
                <!-- Cards de resumen -->
                <div class="row mb-4">
                    <div class="col-6 col-md-3">
                        <div class="card text-center">
                            <div class="card-body">
                                <h2 class="text-success" id="activeComputers">12</h2>
                                <p class="card-text">Computadoras Activas</p>
                            </div>
                        </div>
                    </div>
                    <div class="col-6 col-md-3">
                        <div class="card text-center">
                            <div class="card-body">
                                <h2 class="text-primary" id="todayScreenshots">2,847</h2>
                                <p class="card-text">Screenshots Hoy</p>
                            </div>
                        </div>
                    </div>
                    <div class="col-6 col-md-3">
                        <div class="card text-center">
                            <div class="card-body">
                                <h2 class="text-warning" id="avgActivity">78%</h2>
                                <p class="card-text">Actividad Promedio</p>
                            </div>
                        </div>
                    </div>
                    <div class="col-6 col-md-3">
                        <div class="card text-center">
                            <div class="card-body">
                                <h2 class="text-info" id="totalQuadrants">24</h2>
                                <p class="card-text">Cuadrantes</p>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Gráficos principales -->
                <div class="row">
                    <div class="col-lg-8">
                        <div class="card">
                            <div class="card-header">
                                <h5>📈 Actividad por Hora</h5>
                            </div>
                            <div class="card-body">
                                <canvas id="activityTimelineChart"></canvas>
                            </div>
                        </div>
                    </div>
                    <div class="col-lg-4">
                        <div class="card">
                            <div class="card-header">
                                <h5>🖥️ Estado Computadoras</h5>
                            </div>
                            <div class="card-body">
                                <canvas id="computersOverviewChart"></canvas>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Lista de computadoras -->
                <div class="row mt-4">
                    <div class="col-12">
                        <div class="card">
                            <div class="card-header d-flex justify-content-between align-items-center">
                                <h5>🖥️ Computadoras Monitoreadas</h5>
                                <button class="btn btn-primary btn-sm">+ Nueva</button>
                            </div>
                            <div class="card-body">
                                <div class="table-responsive">
                                    <table class="table table-hover">
                                        <thead>
                                            <tr>
                                                <th>Nombre</th>
                                                <th>Estado</th>
                                                <th>Última Conexión</th>
                                                <th>Actividad</th>
                                                <th>Screenshots</th>
                                                <th>Acciones</th>
                                            </tr>
                                        </thead>
                                        <tbody id="computersTable">
                                            <!-- Se llena dinámicamente -->
                                        </tbody>
                                    </table>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <!-- Scripts -->
    <script src="/js/bootstrap.bundle.min.js"></script>
    <script src="/js/chart.min.js"></script>
    <script src="/js/signalr.min.js"></script>
    <script src="/js/dashboard.js"></script>
</body>
</html>
```

#### Entregables
- [ ] Dashboard responsive funcionando
- [ ] Gráficos interactivos implementados
- [ ] SignalR para tiempo real
- [ ] Optimización móvil

### Fase 4: PWA + Características Móviles (Semana 8)

#### Objetivos
- Configurar Progressive Web App
- Implementar Service Worker para cache offline
- Notificaciones push (opcional)
- Optimizaciones específicas móviles

#### Service Worker avanzado
```javascript
// sw.js
const CACHE_NAME = 'capturer-dashboard-v1.0.0';
const API_CACHE_NAME = 'capturer-api-cache-v1';

// Recursos estáticos a cachear
const STATIC_RESOURCES = [
    '/',
    '/css/bootstrap.min.css',
    '/css/dashboard.css',
    '/js/bootstrap.bundle.min.js',
    '/js/chart.min.js',
    '/js/signalr.min.js',
    '/js/dashboard.js',
    '/icons/icon-192.png',
    '/manifest.json'
];

// Rutas de API a cachear
const API_ROUTES = [
    '/api/dashboard/overview',
    '/api/computers',
    '/api/reports/stats'
];

// Instalación del Service Worker
self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => {
                console.log('Cacheando recursos estáticos');
                return cache.addAll(STATIC_RESOURCES);
            })
    );
    self.skipWaiting();
});

// Activación del Service Worker
self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys().then(cacheNames => {
            return Promise.all(
                cacheNames.map(cacheName => {
                    if (cacheName !== CACHE_NAME && cacheName !== API_CACHE_NAME) {
                        console.log('Eliminando cache obsoleto:', cacheName);
                        return caches.delete(cacheName);
                    }
                })
            );
        })
    );
    self.clients.claim();
});

// Manejo de requests
self.addEventListener('fetch', event => {
    const url = new URL(event.request.url);
    
    // Estrategia Cache First para recursos estáticos
    if (STATIC_RESOURCES.includes(url.pathname)) {
        event.respondWith(
            caches.match(event.request)
                .then(response => {
                    return response || fetch(event.request);
                })
        );
        return;
    }
    
    // Estrategia Network First con fallback para API
    if (url.pathname.startsWith('/api/')) {
        event.respondWith(
            fetch(event.request)
                .then(response => {
                    // Si la respuesta es exitosa, actualizar cache
                    if (response.ok) {
                        const responseClone = response.clone();
                        caches.open(API_CACHE_NAME)
                            .then(cache => {
                                cache.put(event.request, responseClone);
                            });
                    }
                    return response;
                })
                .catch(() => {
                    // Si falla la red, usar cache
                    return caches.match(event.request);
                })
        );
        return;
    }
    
    // Para todo lo demás, estrategia Network First
    event.respondWith(
        fetch(event.request)
            .catch(() => {
                return caches.match(event.request);
            })
    );
});

// Push notifications (opcional)
self.addEventListener('push', event => {
    const options = {
        body: event.data ? event.data.text() : 'Nueva actividad en Capturer Dashboard',
        icon: '/icons/icon-192.png',
        badge: '/icons/badge-72.png',
        actions: [
            {
                action: 'view',
                title: 'Ver Dashboard'
            },
            {
                action: 'close',
                title: 'Cerrar'
            }
        ]
    };
    
    event.waitUntil(
        self.registration.showNotification('Capturer Dashboard', options)
    );
});
```

#### Notificaciones Push (opcional)
```javascript
// dashboard.js - Registro de notificaciones
async function setupPushNotifications() {
    if ('serviceWorker' in navigator && 'PushManager' in window) {
        try {
            const registration = await navigator.serviceWorker.ready;
            const permission = await Notification.requestPermission();
            
            if (permission === 'granted') {
                const subscription = await registration.pushManager.subscribe({
                    userVisibleOnly: true,
                    applicationServerKey: urlBase64ToUint8Array(publicVapidKey)
                });
                
                // Enviar subscription al servidor
                await fetch('/api/notifications/subscribe', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(subscription)
                });
                
                console.log('Notificaciones push configuradas');
            }
        } catch (error) {
            console.error('Error configurando notificaciones:', error);
        }
    }
}
```

#### Entregables
- [ ] PWA completamente funcional
- [ ] Cache offline para navegación básica
- [ ] Instalación como app en móviles
- [ ] Notificaciones push (opcional)

---

## 🔐 Seguridad y Configuración

### Autenticación Multi-nivel

#### 1. Dashboard Web - Usuarios
```csharp
public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum UserRole
{
    Admin,     // Acceso completo
    Manager,   // Ver todos los datos, configurar algunos
    User,      // Ver datos de sus computadoras asignadas
    Viewer     // Solo lectura
}
```

#### 2. Capturer API - API Keys
```json
{
  "ComputerAuth": {
    "ApiKey": "cap_1a2b3c4d5e6f7g8h9i0j",
    "ComputerName": "PC-Oficina-01",
    "RegisteredAt": "2024-01-15T10:30:00Z",
    "LastUsed": "2024-01-20T14:25:00Z"
  }
}
```

### Configuración de Red y Deployment

#### Configuración Local (Development)
```yaml
Dashboard: http://localhost:5000
API Capturer: http://localhost:8080
Database: SQLite local

Network Config:
  - Todo en localhost
  - Sin SSL requerido
  - Acceso directo entre aplicaciones
```

#### Configuración Producción (LAN)
```yaml
Dashboard: https://dashboard.empresa.local
API Capturer: http://192.168.1.X:8080 (cada PC)
Database: PostgreSQL en servidor

Network Config:
  - Dashboard en servidor principal
  - SSL con certificado auto-firmado o Let's Encrypt
  - Capturer APIs accesibles solo internamente
  - Firewall: Puerto 443 abierto, 8080 interno
```

#### Configuración Avanzada (Internet + VPN)
```yaml
Dashboard: https://dashboard.tudominio.com
API Capturer: VPN túneles a cada PC
Database: PostgreSQL con SSL

Network Config:
  - Dashboard en VPS (Azure/AWS/Digital Ocean)
  - VPN (WireGuard/OpenVPN) para acceso a PCs
  - SSL/TLS obligatorio
  - API Keys con expiración
```

### Variables de Entorno

#### Dashboard Web
```bash
# Database
CONNECTION_STRING=Server=localhost;Database=CapturerDashboard;
DB_TYPE=sqlite  # sqlite, postgresql, sqlserver

# Authentication
JWT_SECRET=tu-clave-secreta-muy-larga-y-compleja
JWT_EXPIRATION_HOURS=24
ADMIN_EMAIL=admin@empresa.com
ADMIN_PASSWORD=AdminPassword123!

# Features
ENABLE_PUSH_NOTIFICATIONS=true
ENABLE_REAL_TIME_UPDATES=true
MAX_COMPUTERS=50

# External Services
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USER=notificaciones@empresa.com
SMTP_PASS=contraseña-aplicacion

# HTTPS (Producción)
HTTPS_PORT=443
SSL_CERT_PATH=/etc/ssl/certs/dashboard.crt
SSL_KEY_PATH=/etc/ssl/private/dashboard.key
```

#### Capturer modificado
```json
{
  "CapturerApi": {
    "Port": 8080,
    "EnableApi": true,
    "DashboardUrl": "https://dashboard.empresa.local",
    "ApiKey": "cap_generated_unique_key",
    "SyncEnabled": true,
    "SyncIntervalMinutes": 5,
    "RetryAttempts": 3,
    "TimeoutSeconds": 30
  },
  "Logging": {
    "Api": {
      "LogLevel": "Information",
      "LogRequests": true,
      "LogResponses": false
    }
  }
}
```

---

## 🚀 Plan de Deployment

### Opción 1: Deployment Local (Red Empresarial)

#### Requisitos
- **Servidor Windows/Linux** con .NET 8 Runtime
- **SQL Server Express** o **PostgreSQL**
- **IIS** (Windows) o **Nginx** (Linux)
- **Red local confiable**

#### Pasos de instalación
```bash
# 1. Clonar y compilar
git clone https://github.com/usuario/capturer-dashboard.git
cd capturer-dashboard
dotnet publish -c Release -o ./publish

# 2. Configurar base de datos
dotnet ef database update

# 3. Configurar IIS/Nginx
# Windows: Copiar a C:\inetpub\wwwroot\dashboard
# Linux: Copiar a /var/www/dashboard

# 4. Configurar SSL
# Generar certificado auto-firmado o usar Let's Encrypt

# 5. Configurar servicio Windows/systemd
sc create "CapturerDashboard" binPath="C:\dashboard\CapturerDashboard.exe"
```

### Opción 2: Cloud Deployment (Azure/AWS)

#### Azure App Service
```yaml
Resource Group: rg-capturer-dashboard
App Service Plan: ASP-CapturerDashboard (B1 Basic)
App Service: dashboard-empresa
Database: Azure SQL Database (Basic)
Storage: Blob Storage para backups

Estimated Cost: $15-30/month
```

#### AWS Deployment
```yaml
EC2 Instance: t3.micro (Free Tier elegible)
RDS Database: PostgreSQL micro instance
Load Balancer: Application Load Balancer
SSL: AWS Certificate Manager (gratis)

Estimated Cost: $10-25/month
```

### Opción 3: VPS Self-Hosted

#### Digital Ocean/Linode/Vultr
```yaml
VPS: 2GB RAM, 1 CPU, 50GB SSD ($12/month)
OS: Ubuntu 22.04 LTS
Database: PostgreSQL
Reverse Proxy: Nginx
SSL: Let's Encrypt (gratis)
Domain: tudominio.com ($10/year)

Total Cost: ~$12/month + domain
```

---

## 📊 Métricas y Monitoreo

### KPIs del Sistema
- **Uptime de computadoras**: % tiempo activas
- **Frecuencia de sincronización**: Reportes/día por PC
- **Latencia de API**: Tiempo de respuesta promedio
- **Almacenamiento**: GB de datos históricos
- **Usuarios activos**: Sesiones concurrentes en dashboard

### Alertas Configurables
- PC no reporta en > 30 minutos
- Actividad anómala (muy alta/baja)
- Errores de sincronización repetidos
- Espacio de almacenamiento > 85%
- Dashboard inaccesible

### Logs y Debugging
```csharp
// Structured logging con Serilog
Log.Information("Computer {ComputerName} reported {ScreenshotCount} screenshots with {ActivityRate}% activity", 
    computerName, screenshotCount, activityRate);

Log.Warning("Failed to sync report {ReportId} to dashboard after {RetryCount} attempts", 
    reportId, retryCount);

Log.Error(ex, "Database connection failed for computer {ComputerId}", computerId);
```

---

## 📈 Roadmap Futuro

### Versión 2.0 (3-6 meses)
- **Machine Learning**: Detección de patrones anómalos
- **Integración móvil nativa**: Apps iOS/Android
- **Multi-tenant**: Múltiples organizaciones
- **API pública**: Integraciones con terceros
- **Exports avanzados**: PDF, Excel, integraciones

### Versión 3.0 (6-12 meses)
- **Edge computing**: Procesamiento local avanzado
- **Computer Vision**: Análisis automático de contenido
- **Integración Microsoft Graph**: Calendarios y productividad
- **Blockchain audit**: Trazabilidad inmutable
- **IA generativa**: Reportes automáticos con insights

---

## 💰 Estimación de Costos

### Desarrollo (Una vez)
- **Fase 1-2**: 40-60 horas de desarrollo (~$2,000-4,000)
- **Fase 3-4**: 30-40 horas de desarrollo (~$1,500-3,000)
- **Testing y deployment**: 10-15 horas (~$500-1,000)
- **Total estimado**: $4,000-8,000 (depende del freelancer/equipo)

### Operación (Mensual)
- **Hosting local**: $0 (usar servidor existente)
- **VPS básico**: $12-25/mes
- **Azure/AWS**: $15-50/mes (depende del uso)
- **Domain y SSL**: $1-3/mes
- **Total mensual**: $0-50/mes

### ROI Esperado
- **Ahorro tiempo administración**: 5-10 horas/mes
- **Mejor visibilidad operacional**: Invaluable
- **Troubleshooting más rápido**: 50% menos tiempo
- **Cumplimiento y auditoría**: Documentación automática

---

## ✅ Checklist de Implementación

### Pre-requisitos
- [ ] .NET 8 SDK instalado
- [ ] Visual Studio o VS Code configurado
- [ ] Base de datos PostgreSQL/SQL Server disponible
- [ ] Dominio web configurado (opcional)
- [ ] Certificados SSL (producción)

### Fase 1: Capturer API
- [ ] Crear branch `feature/web-api`
- [ ] Implementar `CapturerApiService`
- [ ] Configurar endpoints básicos
- [ ] Integrar con `ActivityReportService`
- [ ] Testing local de API
- [ ] Documentar endpoints

### Fase 2: Dashboard Web
- [ ] Crear proyecto ASP.NET Core
- [ ] Configurar Entity Framework
- [ ] Implementar modelos de datos
- [ ] Crear controllers de API
- [ ] Setup autenticación
- [ ] Testing de API endpoints

### Fase 3: Frontend
- [ ] Layout responsive básico
- [ ] Integración Chart.js
- [ ] Implementar gráficos principales
- [ ] Setup SignalR
- [ ] Testing en dispositivos móviles
- [ ] Optimización de performance

### Fase 4: PWA
- [ ] Configurar manifest.json
- [ ] Implementar Service Worker
- [ ] Testing offline functionality
- [ ] Setup notificaciones push
- [ ] Validación PWA compliance
- [ ] Testing instalación móvil

### Deployment
- [ ] Preparar entorno producción
- [ ] Configurar base de datos
- [ ] Setup servidor web (IIS/Nginx)
- [ ] Configurar SSL/HTTPS
- [ ] Testing end-to-end
- [ ] Documentación de usuario

---

## 📞 Soporte y Mantenimiento

### Documentación Técnica
- **API Documentation**: Swagger/OpenAPI
- **Database Schema**: Diagramas ER
- **Deployment Guide**: Step-by-step
- **Troubleshooting**: Problemas comunes
- **Performance Tuning**: Optimizaciones

### Plan de Mantenimiento
- **Backups automáticos**: Diarios con retención 30 días
- **Updates de seguridad**: Mensuales
- **Monitoring**: 24/7 con alertas
- **Performance reviews**: Trimestrales
- **Feature updates**: Según roadmap

### Capacitación
- **Administradores**: 2-4 horas de training
- **Usuarios finales**: 1 hora de introducción
- **Documentación**: Manual de usuario completo
- **Video tutorials**: Funciones principales
- **Soporte técnico**: Email/chat durante implementación

---

## 🎯 Conclusión

Este plan proporciona una hoja de ruta completa para implementar un dashboard web profesional que complementa perfectamente a Capturer. La arquitectura separada permite:

- **Escalabilidad**: Monitorear múltiples computadoras desde un punto central
- **Movilidad**: Acceso desde cualquier dispositivo con navegador
- **Profesionalismo**: Interfaz moderna y gráficos interactivos
- **Mantenibilidad**: Código separado y bien estructurado
- **Costo-efectividad**: Tecnologías open-source y hosting flexible

La implementación por fases permite probar y validar cada componente antes de avanzar al siguiente, minimizando riesgos y asegurando un producto final robusto y funcional.

**¿Estás listo para comenzar con la Fase 1?** Podemos empezar implementando la API básica en Capturer y luego proceder con el dashboard web.