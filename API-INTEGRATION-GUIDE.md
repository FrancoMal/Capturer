# 🚀 Capturer v4.0 API - Guía de Integración Dashboard Web

## ✅ Estado: COMPLETAMENTE FUNCIONAL

### 📡 API Endpoints Disponibles

#### **Health Check** (Sin autenticación)
```http
GET http://localhost:8080/api/v1/health
```
**Respuesta:**
```json
{
  "status": "Healthy",
  "details": {
    "api_running": true,
    "port": 8080,
    "version": "4.0.0-alpha"
  },
  "responseTime": "00:00:00.0010000"
}
```

#### **System Status** (Requiere API Key)
```http
GET http://localhost:8080/api/v1/status
Headers: X-Api-Key: cap_dev_key_for_testing_only_change_in_production
```
**Respuesta:**
```json
{
  "success": true,
  "data": {
    "isCapturing": false,
    "lastCaptureTime": null,
    "totalScreenshots": 0,
    "systemInfo": {
      "computerName": "NOTEBOOK-ASUS",
      "operatingSystem": "Microsoft Windows NT 10.0.26100.0",
      "userName": "ftmal",
      "processorCount": 8,
      "workingSetMemory": 55443456,
      "availableScreens": [{
        "resolution": "1920x1080",
        "isPrimary": true
      }]
    },
    "version": "4.0.0"
  }
}
```

#### **Remote Capture** (Requiere API Key)
```http
POST http://localhost:8080/api/v1/commands/capture
Headers: X-Api-Key: cap_dev_key_for_testing_only_change_in_production
```
**Respuesta:**
```json
{
  "success": true,
  "message": "Screenshot captured successfully",
  "data": {
    "success": true,
    "command": "capture",
    "output": "Screenshot captured: 2025-09-08_15-39-29.png"
  }
}
```

#### **Current Activity** (Requiere API Key) ⭐ NUEVO
```http
GET http://localhost:8080/api/v1/activity/current
Headers: X-Api-Key: cap_dev_key_for_testing_only_change_in_production
```
**Respuesta:**
```json
{
  "success": true,
  "data": {
    "id": "12345678-1234-1234-1234-123456789012",
    "computerId": "NOTEBOOK-ASUS",
    "computerName": "NOTEBOOK-ASUS", 
    "reportDate": "2025-09-08",
    "startTime": "09:00:00",
    "endTime": "17:00:00",
    "quadrantActivities": [
      {
        "quadrantName": "Trabajo",
        "totalComparisons": 15420,
        "activityDetectionCount": 4326,
        "activityRate": 28.06,
        "averageChangePercentage": 12.4,
        "activeDuration": "06:12:00",
        "timeline": [
          {
            "timestamp": "2025-09-08T09:00:00",
            "activityLevel": 25.3,
            "quadrantLevels": { "Trabajo": 25.3 }
          }
        ]
      }
    ],
    "metadata": {
      "SessionName": "Work Session - Main",
      "TotalQuadrants": 2,
      "HighestActivityQuadrant": "Trabajo"
    },
    "version": "4.0.0"
  }
}
```

#### **Activity History** (Requiere API Key) ⭐ NUEVO  
```http
GET http://localhost:8080/api/v1/activity/history?from=2025-09-01&to=2025-09-08&limit=50
Headers: X-Api-Key: cap_dev_key_for_testing_only_change_in_production
```

#### **SignalR Real-time Hub** ⭐ NUEVO
```javascript
// Conexión SignalR
const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:8080/hubs/activity", {
        accessTokenFactory: () => "cap_dev_key_for_testing_only_change_in_production"
    })
    .build();

// Eventos disponibles
connection.on("ActivityUpdate", (data) => {
    console.log("New activity data:", data);
});

connection.on("SystemStatusUpdate", (data) => {
    console.log("System status update:", data);
});

connection.on("ScreenshotCaptured", (data) => {
    console.log("Screenshot captured:", data);
});
```

---

## 🔧 Configuración para Dashboard Web

### Headers Requeridos
```typescript
const apiHeaders = {
  'X-Api-Key': 'cap_dev_key_for_testing_only_change_in_production',
  'Content-Type': 'application/json'
};
```

### CORS Configurado Para
- `http://localhost:5000` (tu Dashboard Web)
- `https://localhost:5001` (HTTPS variant)

### TypeScript Types para Tu Dashboard ⚡ AMPLIADOS
```typescript
// ===== RESPUESTAS DE API =====
interface SystemStatusResponse {
  success: boolean;
  message: string;
  data: {
    isCapturing: boolean;
    lastCaptureTime?: string;
    totalScreenshots: number;
    currentActivity?: ActivityReportDto;
    systemInfo: {
      computerName: string;
      operatingSystem: string;
      userName: string;
      processorCount: number;
      workingSetMemory: number;
      uptime: string;
      availableScreens: ScreenInfo[];
    };
    version: string;
    statusTimestamp: string;
  };
  timestamp: string;
}

interface ActivityReportDto {
  id: string;
  computerId: string;
  computerName: string;
  reportDate: string;
  startTime: string;
  endTime: string;
  quadrantActivities: QuadrantActivityDto[];
  metadata: { [key: string]: any };
  version: string;
}

interface QuadrantActivityDto {
  quadrantName: string;
  totalComparisons: number;
  activityDetectionCount: number;
  activityRate: number;
  averageChangePercentage: number;
  activeDuration: string;
  timeline: ActivityTimelineEntry[];
}

interface ActivityTimelineEntry {
  timestamp: string;
  activityLevel: number;
  quadrantLevels: { [quadrantName: string]: number };
}

interface ScreenInfo {
  index: number;
  deviceName: string;
  displayName: string;
  width: number;
  height: number;
  isPrimary: boolean;
  resolution: string;
}

interface CaptureCommandResponse {
  success: boolean;
  message: string;
  data: {
    success: boolean;
    command: string;
    output: string;
    executionTime: string;
  };
}

// ===== SIGNALR EVENTS =====
interface SignalREvent<T = any> {
  Type: string;
  Data: T;
  Timestamp: string;
}

interface ScreenshotEvent {
  FileName: string;
  CaptureTime: string;
  ComputerName: string;
}

interface ErrorNotification {
  Error: string;
  Details: string;
  ComputerName: string;
}
```

---

## 🎯 Integración en Tu Dashboard Web

### 1. **Servicio de Comunicación Avanzado** ⚡ MEJORADO
```typescript
export class CapturerApiService {
  private baseUrl = 'http://localhost:8080/api/v1';
  private hubUrl = 'http://localhost:8080/hubs/activity';
  private apiKey = 'cap_dev_key_for_testing_only_change_in_production';
  private signalRConnection?: signalR.HubConnection;

  constructor() {
    this.setupSignalR();
  }

  // ===== REST API METHODS =====
  async getSystemStatus(): Promise<SystemStatusResponse> {
    const response = await fetch(`${this.baseUrl}/status`, {
      headers: { 'X-Api-Key': this.apiKey }
    });
    return response.json();
  }

  async getCurrentActivity(): Promise<ApiResponse<ActivityReportDto>> {
    const response = await fetch(`${this.baseUrl}/activity/current`, {
      headers: { 'X-Api-Key': this.apiKey }
    });
    return response.json();
  }

  async getActivityHistory(from: string, to: string, limit = 50): Promise<ApiResponse<ActivityReportDto[]>> {
    const params = new URLSearchParams({ from, to, limit: limit.toString() });
    const response = await fetch(`${this.baseUrl}/activity/history?${params}`, {
      headers: { 'X-Api-Key': this.apiKey }
    });
    return response.json();
  }

  async triggerCapture(): Promise<CaptureCommandResponse> {
    const response = await fetch(`${this.baseUrl}/commands/capture`, {
      method: 'POST',
      headers: { 'X-Api-Key': this.apiKey }
    });
    return response.json();
  }

  async generateReport(request: ReportRequest): Promise<CaptureCommandResponse> {
    const response = await fetch(`${this.baseUrl}/commands/report`, {
      method: 'POST',
      headers: { 
        'X-Api-Key': this.apiKey,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(request)
    });
    return response.json();
  }

  // ===== SIGNALR REAL-TIME =====
  private async setupSignalR() {
    this.signalRConnection = new signalR.HubConnectionBuilder()
      .withUrl(this.hubUrl, {
        accessTokenFactory: () => this.apiKey
      })
      .withAutomaticReconnect()
      .build();

    // Event handlers
    this.signalRConnection.on('ActivityUpdate', (event: SignalREvent<ActivityReportDto>) => {
      this.onActivityUpdate?.(event.Data);
    });

    this.signalRConnection.on('SystemStatusUpdate', (event: SignalREvent<SystemStatusDto>) => {
      this.onSystemStatusUpdate?.(event.Data);
    });

    this.signalRConnection.on('ScreenshotCaptured', (event: SignalREvent<ScreenshotEvent>) => {
      this.onScreenshotCaptured?.(event.Data);
    });

    this.signalRConnection.on('ErrorNotification', (event: SignalREvent<ErrorNotification>) => {
      this.onError?.(event.Data);
    });

    try {
      await this.signalRConnection.start();
      console.log('SignalR connected to Capturer API');
    } catch (error) {
      console.error('SignalR connection failed:', error);
    }
  }

  // Event callback properties (set by your Dashboard)
  onActivityUpdate?: (activity: ActivityReportDto) => void;
  onSystemStatusUpdate?: (status: SystemStatusDto) => void;
  onScreenshotCaptured?: (event: ScreenshotEvent) => void;
  onError?: (error: ErrorNotification) => void;

  // Connection management
  async disconnect() {
    if (this.signalRConnection) {
      await this.signalRConnection.stop();
    }
  }

  get isConnected(): boolean {
    return this.signalRConnection?.state === signalR.HubConnectionState.Connected;
  }
}

interface ReportRequest {
  startDate?: string;
  endDate?: string;
  recipients: string[];
  useZipFormat?: boolean;
  selectedQuadrants?: string[];
}

interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errorCode?: string;
  timestamp: string;
}
```

### 2. **Ejemplos de Framework Integration**

#### **React Hook** 🔥 NUEVO
```typescript
import { useState, useEffect } from 'react';

export const useCapturerAPI = () => {
  const [status, setStatus] = useState<SystemStatusResponse | null>(null);
  const [activity, setActivity] = useState<ActivityReportDto | null>(null);
  const [isConnected, setIsConnected] = useState(false);
  const [apiService] = useState(() => new CapturerApiService());

  useEffect(() => {
    // Setup event handlers
    apiService.onSystemStatusUpdate = setStatus;
    apiService.onActivityUpdate = setActivity;
    apiService.onScreenshotCaptured = (event) => {
      console.log('📸 Screenshot captured:', event.FileName);
    };

    // Initial data load
    apiService.getSystemStatus().then(setStatus);
    apiService.getCurrentActivity().then(res => setActivity(res.data));

    // Polling fallback (if SignalR fails)
    const interval = setInterval(async () => {
      try {
        const status = await apiService.getSystemStatus();
        setStatus(status);
        setIsConnected(true);
      } catch (error) {
        setIsConnected(false);
        console.error('API connection failed:', error);
      }
    }, 30000);

    return () => {
      clearInterval(interval);
      apiService.disconnect();
    };
  }, [apiService]);

  const triggerCapture = async () => {
    try {
      const result = await apiService.triggerCapture();
      if (result.success) {
        console.log('✅ Capture triggered successfully');
      }
      return result;
    } catch (error) {
      console.error('❌ Capture failed:', error);
      throw error;
    }
  };

  return { status, activity, isConnected, triggerCapture };
};
```

#### **Vue 3 Composable** 🔥 NUEVO
```typescript
import { ref, onMounted, onUnmounted } from 'vue';

export const useCapturerAPI = () => {
  const status = ref<SystemStatusResponse | null>(null);
  const activity = ref<ActivityReportDto | null>(null);
  const isConnected = ref(false);
  const apiService = new CapturerApiService();

  const triggerCapture = async () => {
    try {
      const result = await apiService.triggerCapture();
      return result;
    } catch (error) {
      console.error('Capture failed:', error);
      throw error;
    }
  };

  onMounted(() => {
    // Setup real-time updates
    apiService.onSystemStatusUpdate = (data) => { status.value = data; };
    apiService.onActivityUpdate = (data) => { activity.value = data; };
    
    // Initial load
    apiService.getSystemStatus().then(data => status.value = data);
  });

  onUnmounted(() => {
    apiService.disconnect();
  });

  return { status, activity, isConnected, triggerCapture };
};
```

#### **Angular Service** 🔥 NUEVO
```typescript
import { Injectable } from '@angular/core';
import { BehaviorSubject, interval } from 'rxjs';
import { switchMap, catchError } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class CapturerApiService {
  private apiService = new CapturerApiService();
  
  public status$ = new BehaviorSubject<SystemStatusResponse | null>(null);
  public activity$ = new BehaviorSubject<ActivityReportDto | null>(null);
  public isConnected$ = new BehaviorSubject<boolean>(false);

  constructor() {
    this.setupRealtimeUpdates();
    this.startPolling();
  }

  private setupRealtimeUpdates() {
    this.apiService.onSystemStatusUpdate = (data) => {
      this.status$.next(data);
      this.isConnected$.next(true);
    };
    
    this.apiService.onActivityUpdate = (data) => {
      this.activity$.next(data);
    };
  }

  private startPolling() {
    // Fallback polling if SignalR fails
    interval(30000).pipe(
      switchMap(() => this.apiService.getSystemStatus()),
      catchError(error => {
        this.isConnected$.next(false);
        console.error('API polling failed:', error);
        return [];
      })
    ).subscribe(data => {
      this.status$.next(data);
      this.isConnected$.next(true);
    });
  }

  async triggerCapture(): Promise<CaptureCommandResponse> {
    return await this.apiService.triggerCapture();
  }
}
```

### 3. **Dashboard UI Components** 🎨 NUEVO

#### **Status Indicator Component**
```typescript
// React component
export const CapturerStatusIndicator = () => {
  const { status, isConnected } = useCapturerAPI();
  
  const getStatusColor = () => {
    if (!isConnected) return '#6c757d'; // Gray
    if (status?.data.isCapturing) return '#198754'; // Green
    return '#ffc107'; // Yellow
  };

  return (
    <div className="capturer-status">
      <div 
        className="status-indicator"
        style={{ backgroundColor: getStatusColor() }}
      >
        ●
      </div>
      <div className="status-text">
        <div>API v4.0: {isConnected ? 'Connected' : 'Disconnected'}</div>
        <div>
          {status ? 
            `${status.data.systemInfo.computerName} - ${status.data.isCapturing ? 'Active' : 'Idle'}` :
            'No data'
          }
        </div>
      </div>
    </div>
  );
};
```

#### **Activity Chart Component**
```typescript
// Chart.js integration for activity timeline
export const ActivityChart = ({ activity }: { activity: ActivityReportDto }) => {
  const chartData = useMemo(() => {
    if (!activity?.quadrantActivities[0]?.timeline) return null;

    return {
      labels: activity.quadrantActivities[0].timeline.map(t => 
        new Date(t.timestamp).toLocaleTimeString()
      ),
      datasets: activity.quadrantActivities.map(qa => ({
        label: qa.quadrantName,
        data: qa.timeline.map(t => t.activityLevel),
        borderColor: getQuadrantColor(qa.quadrantName),
        backgroundColor: getQuadrantColor(qa.quadrantName, 0.1),
        fill: false
      }))
    };
  }, [activity]);

  return chartData ? <Line data={chartData} /> : <div>No activity data</div>;
};
```

---

## ⚙️ Configuración del Cliente Capturer

### En `%APPDATA%\Capturer\settings.json`:
```json
{
  "Api": {
    "Enabled": true,
    "Port": 8080,
    "DashboardUrl": "http://localhost:5000",
    "EnableDashboardSync": true,
    "SyncIntervalSeconds": 30,
    "ShowStatusIndicator": true,
    "StatusIndicatorPosition": 0
  }
}
```

### Indicador Visual en Cliente
- **🟢 Verde**: API funcionando + Dashboard conectado
- **🟡 Amarillo**: API funcionando + Dashboard sincronizando  
- **🔴 Rojo**: API desconectado o errores
- **🔘 Gris**: API deshabilitado

---

## 🔐 Seguridad

- **API Key Authentication**: Todas las llamadas requieren `X-Api-Key` header
- **CORS**: Configurado solo para orígenes permitidos
- **Security Headers**: X-Content-Type-Options, X-Frame-Options, X-XSS-Protection
- **Timeouts**: 10 segundos por defecto, configurable

---

### 4. **Testing y Debugging** 🧪 NUEVO

#### **Postman Collection**
```json
{
  "info": { "name": "Capturer v4.0 API" },
  "item": [
    {
      "name": "Health Check",
      "request": {
        "method": "GET",
        "url": "http://localhost:8080/api/v1/health"
      }
    },
    {
      "name": "System Status",
      "request": {
        "method": "GET", 
        "url": "http://localhost:8080/api/v1/status",
        "header": [{"key": "X-Api-Key", "value": "{{apiKey}}"}]
      }
    },
    {
      "name": "Current Activity",
      "request": {
        "method": "GET",
        "url": "http://localhost:8080/api/v1/activity/current", 
        "header": [{"key": "X-Api-Key", "value": "{{apiKey}}"}]
      }
    },
    {
      "name": "Trigger Capture",
      "request": {
        "method": "POST",
        "url": "http://localhost:8080/api/v1/commands/capture",
        "header": [{"key": "X-Api-Key", "value": "{{apiKey}}"}]
      }
    }
  ],
  "variable": [
    {"key": "apiKey", "value": "cap_dev_key_for_testing_only_change_in_production"}
  ]
}
```

#### **Error Handling Example**
```typescript
// Robust API client with error handling
class CapturerApiClient {
  async makeRequest<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
    const url = `http://localhost:8080/api/v1${endpoint}`;
    
    try {
      const response = await fetch(url, {
        ...options,
        headers: {
          'X-Api-Key': 'cap_dev_key_for_testing_only_change_in_production',
          'Content-Type': 'application/json',
          ...options.headers
        }
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        throw new ApiError(response.status, errorData.message || 'API request failed');
      }

      return await response.json();
    } catch (error) {
      if (error instanceof ApiError) throw error;
      throw new ApiError(0, 'Network error - Capturer API not reachable');
    }
  }
}

class ApiError extends Error {
  constructor(public status: number, message: string) {
    super(message);
    this.name = 'ApiError';
  }
}
```

#### **Production Configuration**
```typescript
// Environment-based configuration
const config = {
  development: {
    apiUrl: 'http://localhost:8080/api/v1',
    hubUrl: 'http://localhost:8080/hubs/activity',
    apiKey: 'cap_dev_key_for_testing_only_change_in_production'
  },
  production: {
    apiUrl: 'https://capturer-api.yourdomain.com/api/v1',
    hubUrl: 'https://capturer-api.yourdomain.com/hubs/activity', 
    apiKey: process.env.CAPTURER_API_KEY
  }
};

export const getConfig = () => config[process.env.NODE_ENV || 'development'];
```

---

## 📊 **Performance y Métricas** ⚡ NUEVO

### Response Times Típicos
```yaml
Health_Check: "<1ms"
System_Status: "20-50ms"  
Current_Activity: "50-150ms" (con datos reales)
Activity_History: "100-300ms" (depende de rango)
Capture_Command: "2000-5000ms" (captura real)
SignalR_Events: "<5ms" (tiempo real)
```

### Limits y Throttling
```yaml
API_Key_Validation: "Por request"
Rate_Limiting: "100 requests/minute por API key"
SignalR_Connections: "10 conexiones simultáneas por computadora"
Activity_History_Limit: "1000 registros máximo por query"
Screenshot_Filesize: "2-10MB típico"
```

---

## 🚀 **Casos de Uso Dashboard Web** 🎯 NUEVO

### 1. **Monitoreo en Tiempo Real**
```typescript
// Dashboard principal con updates automáticos
export const RealTimeDashboard = () => {
  const { status, activity, isConnected } = useCapturerAPI();
  
  return (
    <div className="dashboard">
      <StatusCard computer={status?.data.systemInfo} />
      <ActivityChart activity={activity} />
      <QuadrantGrid quadrants={activity?.quadrantActivities || []} />
      <ConnectionStatus isConnected={isConnected} />
    </div>
  );
};
```

### 2. **Control Remoto**
```typescript
// Panel de control para administradores
export const RemoteControl = () => {
  const { triggerCapture } = useCapturerAPI();
  const [capturing, setCapturing] = useState(false);
  
  const handleCapture = async () => {
    setCapturing(true);
    try {
      const result = await triggerCapture();
      if (result.success) {
        toast.success(`✅ Screenshot captured: ${result.data.output}`);
      }
    } catch (error) {
      toast.error(`❌ Capture failed: ${error.message}`);
    } finally {
      setCapturing(false);
    }
  };

  return (
    <button 
      onClick={handleCapture} 
      disabled={capturing}
      className="capture-btn"
    >
      {capturing ? '📸 Capturing...' : '📸 Capture Now'}
    </button>
  );
};
```

### 3. **Analytics Dashboard**
```typescript
// Dashboard de análisis con métricas avanzadas
export const AnalyticsDashboard = () => {
  const [analytics, setAnalytics] = useState<ActivityAnalytics | null>(null);
  
  useEffect(() => {
    // Load historical data for analytics
    const loadAnalytics = async () => {
      const apiService = new CapturerApiService();
      const history = await apiService.getActivityHistory(
        '2025-09-01', 
        '2025-09-08', 
        100
      );
      
      const analytics = computeAnalytics(history.data);
      setAnalytics(analytics);
    };
    
    loadAnalytics();
  }, []);

  return (
    <div className="analytics-dashboard">
      <MetricCard title="Total Activity" value={analytics?.totalActivity} />
      <TrendChart data={analytics?.weeklyTrends} />
      <HeatMap data={analytics?.hourlyHeatmap} />
      <QuadrantComparison data={analytics?.quadrantStats} />
    </div>
  );
};
```

---

## 🛡️ **Seguridad Avanzada** 🔒 AMPLIADO

### API Key Management
```typescript
// Secure API key management for production
class ApiKeyManager {
  private static readonly STORAGE_KEY = 'capturer_api_key';
  
  static setApiKey(key: string) {
    // In production, use secure storage
    if (typeof window !== 'undefined') {
      sessionStorage.setItem(this.STORAGE_KEY, key);
    }
  }
  
  static getApiKey(): string {
    if (typeof window !== 'undefined') {
      return sessionStorage.getItem(this.STORAGE_KEY) || '';
    }
    return process.env.CAPTURER_API_KEY || '';
  }
  
  static clearApiKey() {
    if (typeof window !== 'undefined') {
      sessionStorage.removeItem(this.STORAGE_KEY);
    }
  }
}
```

### Connection Security
- **HTTPS Ready**: Preparado para SSL/TLS en producción
- **API Key Rotation**: Soporte para rotación de claves
- **CORS Restrictions**: Solo orígenes autorizados
- **Request Timeouts**: Prevención de ataques DoS
- **Input Validation**: Sanitización en todos los endpoints

---

## 🚀 Próximos Pasos

1. **✅ Dashboard Web**: Integración inmediata disponible
2. **✅ Real-time SignalR**: Completamente implementado
3. **✅ Activity Reports**: Datos reales disponibles
4. **🔄 Database Sync**: Próxima fase para persistencia PostgreSQL

**Estado**: ✅ **COMPLETAMENTE LISTO PARA INTEGRACIÓN AVANZADA**