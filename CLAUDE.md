# 📸 Capturer v3.2.0 - Documentación para Claude

## 🎯 Propósito del Proyecto

**Capturer v3.2.0** es una aplicación de escritorio .NET 8 para Windows diseñada específicamente como **sistema de monitoreo de oficina 24/7**. Su función principal es capturar automáticamente pantallas de trabajo y generar reportes organizados por email.

### Casos de Uso Principal:
- **Monitoreo empresarial continuo** - Supervisión de actividad laboral
- **Documentación automática de procesos** - Registro visual de flujos de trabajo  
- **Cumplimiento y auditoría** - Evidencia para regulaciones corporativas
- **Análisis de productividad** - Métricas de uso de aplicaciones
- **Trabajo remoto** - Supervisión de empleados distribuidos

---

## 🏗️ Arquitectura del Sistema

### Tecnologías Core:
- **.NET 8 Windows Forms** - Interfaz de usuario nativa
- **Dependency Injection** - Microsoft.Extensions.DependencyInjection
- **MailKit/MimeKit** - Sistema de email robusto
- **System.Drawing** - Captura y procesamiento de imágenes
- **Newtonsoft.Json** - Configuración persistente
- **NLog** - Sistema de logging profesional

### Patrón Arquitectural:
```
┌─────────────────────────────────┐
│ Presentation Layer              │ ← Windows Forms UI
│ • Form1 (Principal)             │
│ • EmailForm, RoutineEmailForm   │  
│ • SettingsForm, QuadrantEditor  │
├─────────────────────────────────┤
│ Business Logic Layer            │ ← Servicios principales
│ • ScreenshotService             │
│ • EmailService (dual mode)      │
│ • QuadrantService (v3.2.0)        │
│ • SchedulerService              │
│ • ConfigurationManager          │
├─────────────────────────────────┤
│ Data Layer                      │ ← Modelos y persistencia
│ • CapturerConfiguration         │
│ • QuadrantConfiguration         │
│ • ScreenshotInfo, ProcessingTask│
└─────────────────────────────────┘
```

---

## 🚀 Arquitectura Unificada v3.2.0

### Flujo de Procesamiento Inteligente:
```
📥 Input: Usuario configura "Reporte diario 9:00 AM con cuadrantes"
    ↓
🎯 ReportPeriodService: Calcula período (ayer 8:00-23:00, lunes-viernes)
    ↓  
📂 FileService: Obtiene 87 screenshots que cumplen filtros  
    ↓
🔄 SchedulerService: Llama a SendUnifiedReportAsync()
    ↓
🧩 EmailService: Detecta cuadrantes habilitados
    ↓
✂️ QuadrantService: Procesa solo las 87 imágenes filtradas
    ↓
📧 EmailService: Genera reporte con archivos de cuadrantes
    ↓
✅ Output: Email con regiones específicas del período exacto
```

### Beneficios de la Integración:
- **Eficiencia**: Solo procesa imágenes relevantes al período
- **Precisión**: Filtros temporales + regiones espaciales
- **Flexibilidad**: Todas las combinaciones posibles de configuración
- **Rendimiento**: No procesa archivos que se descartarían después

---

## 🔧 Servicios Principales

### 1. ScreenshotService
**Responsabilidad:** Captura automática/manual de pantallas
```csharp
// Características principales:
- Captura con Windows API (BitBlt) para máximo rendimiento
- Soporte multi-monitor con detección automática
- Configuración flexible: intervalo, calidad, formato
- Timer robusto para operación 24/7
- Inclusión opcional de cursor
```

**Configuraciones de Captura:**
- `AllScreens` - Todas las pantallas como una imagen grande
- `PrimaryScreen` - Solo pantalla principal  
- `SingleScreen` - Pantalla específica por índice

### 2. EmailService (Sistema Dual)
**Responsabilidad:** Envío de reportes manuales y automáticos

#### Email Manual:
```csharp
SendManualReportAsync() // Control total por parte del usuario
- Selección de período personalizado
- Lista de destinatarios por checklist
- Formato ZIP o archivos individuales
- Integración opcional con cuadrantes
```

#### Email Automático (Rutinario) - ✨ MEJORADO v3.2.0:
```csharp  
SendEnhancedReportAsync() // ⭐ NUEVO: Reportes con filtros avanzados
SendRoutineQuadrantReportsAsync() // Con cuadrantes automáticos
- Configuración independiente de destinatarios
- ⭐ NUEVO: Filtros de horario (8:00 AM - 11:00 PM)
- ⭐ NUEVO: Días específicos de la semana
- ⭐ NUEVO: Frecuencias flexibles (diario/semanal/mensual/personalizado)
- Programación inteligente por ReportPeriodService
- Reportes separados por cuadrante disponibles
```

### 🆕 Sistema Unificado de Filtros + Cuadrantes (v3.2.0):

#### Flujo de Procesamiento Integrado:
```yaml
1. Aplicar_Filtros_Horario: "8:00 AM - 11:00 PM, solo lunes-viernes"
2. Obtener_Screenshots_Base: "150 imágenes filtradas del período"
3. Procesar_Cuadrantes: "Dividir en regiones específicas (opcional)"
4. Generar_Reporte: "Email unificado con ambos tipos de archivos"
```

#### Modos de Operación:
- **Solo Filtros**: Capturas completas con filtro horario/días
- **Solo Cuadrantes**: Regiones específicas sin filtro temporal  
- **🔥 UNIFICADO**: Filtros + Cuadrantes = Máxima precisión

#### Configuraciones Avanzadas:
```csharp
// Ejemplo 1: Monitoreo laboral estricto
Filtros: 8:00-18:00, lunes-viernes
Cuadrantes: ["Trabajo", "Dashboard"] 
Resultado: Solo regiones de trabajo en horario laboral

// Ejemplo 2: Vigilancia 24/7 con exclusiones
Filtros: Todo el día, todos los días  
Cuadrantes: ["Publica"] (excluye "Personal")
Resultado: Monitoreo completo sin áreas privadas

// Ejemplo 3: Reportes ejecutivos
Filtros: 9:00-17:00, días laborales
Cuadrantes: ["Métricas", "Rendimiento"]
Resultado: KPIs empresariales en horario ejecutivo
```

### 3. QuadrantService (★ Característica v3.2.0)
**Responsabilidad:** Procesamiento inteligente de regiones de pantalla

```csharp
public async Task<ProcessingTask> ProcessImagesAsync(
    DateTime startDate, 
    DateTime endDate, 
    string configurationName,
    IProgress<ProcessingProgress> progress)
```

**Funcionalidades:**
- **Configuración de cuadrantes** - Definir áreas rectangulares específicas
- **Procesamiento en lotes** - Recortar múltiples imágenes automáticamente  
- **Organización automática** - Carpetas separadas por cuadrante
- **Reportes dirigidos** - Emails específicos por área de pantalla
- **Preview visual** - Vista previa con colores de cuadrantes

### 4. SchedulerService
**Responsabilidad:** Automatización de tareas temporizadas
```csharp
- StartAsync() // Inicia captura automática
- ScheduleScreenshotsAsync(TimeSpan interval)
- Integración con reportes automáticos
- Manejo robusto de errores y recuperación
```

### 5. ConfigurationManager
**Responsabilidad:** Persistencia segura de configuración
```csharp
- Configuración en JSON con validación
- Encriptación DPAPI para contraseñas de email
- Almacenamiento en %APPDATA%\Capturer\
- Validación automática de configuraciones
```

---

## 📁 Estructura de Archivos

### Organización en Disco:
```
C:\Users\[User]\Documents\Capturer\
├── Screenshots\                    ← Capturas principales  
│   ├── 2024-08\                   ← Organización mensual
│   │   ├── 2024-08-26_14-30-15.png
│   │   └── 2024-08-26_15-00-15.png
│   └── 2024-09\
├── Quadrants\                      ← ★ Cuadrantes procesados (v3.2.0)
│   ├── Trabajo\                   ← Un cuadrante = Una carpeta
│   │   ├── 2024-08-26_14-30-15_Trabajo.png
│   │   └── metadata.json
│   ├── Dashboard\
│   └── Personal\  
├── Reports\                        ← ZIPs de reportes generados
│   ├── Weekly\
│   ├── Manual\
│   └── Quadrant\
└── Logs\                          ← Sistema de logging
    ├── capturer-2024-08.log
    └── quadrant-processing-2024-08.log

%APPDATA%\Capturer\                ← Configuración protegida
├── settings.json
├── quadrant-configs.json  
└── security\encrypted_passwords.dat
```

---

## ⚙️ Configuración del Sistema

### Archivo Principal: `CapturerConfiguration`
```json
{
  "Screenshot": {
    "CaptureInterval": "00:30:00",      // Cada 30 minutos
    "CaptureMode": "AllScreens",         
    "Quality": 90,                       // 0-100
    "ImageFormat": "png",
    "IncludeCursor": true
  },
  "Email": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "UseSSL": true,
    "Username": "monitor@empresa.com",
    "Password": "[ENCRYPTED]",           // DPAPI encryption
    "Recipients": ["admin@empresa.com"],
    "SenderName": "Sistema Capturer"
  },
  "QuadrantSystem": {                    // ★ Nuevo en v3.2.0
    "IsEnabled": true,
    "ShowPreviewColors": true,
    "EnableLogging": true,
    "QuadrantsFolder": "..\\Quadrants",
    "Configurations": [
      {
        "Name": "Oficina Trabajo",
        "IsActive": true,
        "Regions": [
          {
            "Name": "Trabajo",
            "X": 0, "Y": 0, 
            "Width": 1920, "Height": 800,
            "Color": "Blue"
          },
          {
            "Name": "Dashboard", 
            "X": 1920, "Y": 0,
            "Width": 900, "Height": 600,
            "Color": "Green"
          }
        ]
      }
    ]
  },
  "Storage": {
    "ScreenshotFolder": "C:\\Users\\[User]\\Documents\\Capturer\\Screenshots",
    "AutoCleanup": true,
    "RetentionDays": 90,
    "MaxFolderSizeMB": 10240
  },
  "Application": {
    "MinimizeToTray": true,
    "ShowNotifications": true,
    "StartWithWindows": false
  }
}
```

---

## 📧 Sistema de Email Dual (v3.2.0)

### Diferencias Clave:

| Característica | Email Manual | Reportes Automáticos |
|----------------|--------------|----------------------|
| **Activación** | Botón "Email Manual" | Botón "Reportes" |
| **Destinatarios** | Checklist por envío | Lista fija guardada |
| **Programación** | Inmediato | Programado (diario/semanal) |
| **Cuadrantes** | Opcional por envío | Configuración persistente |
| **Período** | Selector manual | Calculado automáticamente |

### Flujos de Trabajo:

#### Email Manual:
```
1. Usuario presiona [Email Manual]
2. Abre EmailForm con:
   - DatePicker para período
   - Checklist de destinatarios  
   - Opciones de cuadrantes
   - Botón [Enviar Ahora]
3. Procesamiento inmediato
4. Confirmación visual
```

#### Reportes Automáticos:
```
1. Usuario configura en [Reportes]:
   - Frecuencia (diario/semanal/mensual)
   - Lista fija de destinatarios
   - Configuración de cuadrantes
2. SchedulerService programa tareas
3. Envío automático en horario configurado
4. Logging de resultados
```

---

## 🔲 Sistema de Cuadrantes (★ v3.2.0)

### Concepto:
Los cuadrantes permiten dividir la pantalla en **regiones de interés específicas** y procesarlas por separado. Ideal para:
- **Privacidad** - Excluir áreas personales
- **Reportes dirigidos** - Diferentes destinatarios por área
- **Análisis específico** - Métricas por región de trabajo

### Implementación Técnica:

#### Modelo de Datos:
```csharp
public class QuadrantRegion
{
    public string Name { get; set; }      // "Trabajo", "Dashboard"
    public int X, Y, Width, Height { get; set; }  // Coordenadas pixel
    public Color PreviewColor { get; set; }       // Para preview UI
    public bool IsEnabled { get; set; }
}

public class QuadrantConfiguration  
{
    public string Name { get; set; }              // "Configuración Oficina"
    public List<QuadrantRegion> Regions { get; set; }
    public bool IsActive { get; set; }
}
```

#### Procesamiento:
```csharp
// ImageCropUtils.ProcessImagesAsync() realiza:
1. Carga imagen source completa
2. Por cada cuadrante activo:
   - Recorta Rectangle(x, y, width, height)  
   - Guarda en carpeta específica: Quadrants\[NombreCuadrante]\
   - Mantiene timestamp original
3. Genera metadata.json por cuadrante
4. Reporte de progreso en tiempo real
```

### Casos de Uso de Cuadrantes:

#### Oficina Corporativa:
```
Cuadrante 1: "Aplicación ERP" 
- Región: Monitor principal (0, 0, 1920, 800)
- Destinatarios: supervisor@empresa.com
- Propósito: Monitoreo de productividad

Cuadrante 2: "Dashboard Métricas"
- Región: Monitor secundario (1920, 0, 900, 600)  
- Destinatarios: gerencia@empresa.com
- Propósito: KPIs en tiempo real
```

#### Trabajo Remoto:
```
Cuadrante 1: "Área de Trabajo"
- Región: 70% izquierda pantalla
- Destinatarios: jefe@empresa.com
- Reportes: Cada 2 horas

Cuadrante 2: "Personal" 
- Región: 30% derecha pantalla
- Destinatarios: (ninguno - privado)
- Propósito: Excluir de monitoreo
```

---

## 🔄 Flujos de Trabajo Principales

### 1. Flujo de Monitoreo Básico 24/7:
```
Inicio App → Carga Config → Inicia SchedulerService
     ↓
Timer(30min) → CaptureScreenshot → Guarda PNG → Continúa
     ↓
Reporte Automático(Lunes 9AM) → Recopila semana → Envía ZIP → Logging
```

### 2. Flujo de Email Manual con Cuadrantes:
```
User → [Email Manual] → Selecciona período → ☑️ "Usar cuadrantes"
  ↓
Busca screenshots(período) → Procesa cuadrantes → Crea ZIPs separados
  ↓  
☑️ "Email separado por cuadrante" → Envía 1 email por cuadrante → Confirmación
```

### 3. Flujo de Configuración de Cuadrantes:
```
User → [Cuadrantes] → QuadrantEditorForm
  ↓
Define regiones visuales → Vista previa colores → [Guardar]
  ↓
Config persistida → Auto-actualización → Listo para uso
```

---

## 🔐 Aspectos de Seguridad

### Encriptación:
- **Contraseñas SMTP** - DPAPI (Data Protection API) de Windows
- **Configuración** - JSON con validación de integridad
- **Archivos temporales** - Limpieza automática tras envío email

### Almacenamiento Seguro:
```
%APPDATA%\Capturer\security\
├── encrypted_passwords.dat    ← DPAPI encryption
├── config_hash.txt           ← Validación integridad  
└── last_access.log           ← Auditoría básica
```

### Principios de Seguridad:
- **Mínimo privilegio** - Solo permisos de usuario necesarios
- **Encriptación local** - Nunca texto plano para credenciales  
- **Validación entrada** - Sanitización de paths y emails
- **Cleanup automático** - ZIPs temporales eliminados tras envío

---

## 🔧 Comandos y Tareas Comunes para Claude

### Desarrollo y Modificaciones:
```bash
# Build del proyecto
dotnet build Capturer.csproj

# Análisis de dependencias
dotnet list package

# Tests (si existieran)  
dotnet test
```

### Archivos de Configuración Key:
- **Capturer.csproj** - Dependencias NuGet y configuración build
- **Program.cs** - Entry point y configuración DI
- **Form1.cs** - UI principal y coordinación servicios
- **Services\*.cs** - Lógica business crítica
- **Models\*.cs** - Objetos de dominio y configuración

### Puntos de Extensión Futuros:
1. **Análisis OCR** - Extracción texto de screenshots
2. **Machine Learning** - Detección automática de regiones importantes  
3. **API REST** - Control remoto vía HTTP
4. **Dashboard Web** - Visualización centralizada
5. **Integración Cloud** - Almacenamiento Azure/AWS
6. **Mobile Apps** - Control desde móvil

---

## 🚨 Problemas Comunes y Debugging

### Issues Frecuentes:

#### 1. Error SMTP Connection:
```csharp
// Logs ubicados en: Logs\capturer-2024-MM.log
- Verificar firewall puerto 587
- Validar credenciales SMTP  
- Confirmar configuración SSL/TLS
```

#### 2. Cuadrantes Fuera de Pantalla:
```csharp
// Auto-detected en QuadrantService.ProcessImagesAsync()
- Cambio resolución detecta y ofrece reescalado
- Validación automática de bounds
- Logs: "Quadrant region out of bounds"
```

#### 3. Performance 24/7:
```csharp
// Métricas normales:
- Memoria: 60-150MB (con cuadrantes)
- CPU: <1.2% idle, 5-12% during capture
- Disco: 1-8MB por operación
```

### Debugging Tips:
- **NLog** configurado en `NLog.config` (si existe)
- **Console.WriteLine()** abundante en servicios
- **Event handlers** para monitoreo en tiempo real:
  ```csharp
  _screenshotService.ScreenshotCaptured += OnScreenshotCaptured;
  _emailService.EmailSent += OnEmailSent;
  _quadrantService.ProcessingCompleted += OnProcessingCompleted;
  ```

---

## 📊 Métricas de Rendimiento

### Benchmarks Típicos:
| Operación | Tiempo Promedio | Recursos |
|-----------|----------------|----------|
| **Screenshot capture** | 2-5s | 2-5% CPU |
| **Email con ZIP (100 files)** | 30-180s | Bandwidth dependiente |
| **Procesamiento cuadrantes (50 images)** | 15-60s | 5-12% CPU |
| **Startup aplicación** | 4-10s | 60-90MB RAM |

### Optimizaciones v3.2.0:
- **Async/await** extensivo para no bloquear UI
- **SemaphoreSlim** para control concurrencia
- **Memory streams** para attachments email
- **Batch processing** para cuadrantes
- **Progress reporting** granular

---

## 🎯 Conclusión para Claude

Este proyecto está **bien estructurado** para ser un sistema de monitoreo empresarial robusto. La arquitectura de servicios con DI permite fácil testing y extensión. El sistema de cuadrantes v3.2.0 añade valor significativo para casos de uso enterprise.

**Fortalezas principales:**
- ✅ Arquitectura limpia y mantenible
- ✅ Configuración flexible y segura
- ✅ Sistema dual de emails bien pensado  
- ✅ Cuadrantes añaden diferenciación competitiva
- ✅ Logging y error handling robusto

**Áreas de mejora sugeridas:**
- 📈 Unit tests coverage
- 📈 API REST para integración enterprise
- 📈 Dashboard web complementario
- 📈 Análisis OCR/ML de contenido
- 📈 Cloud storage integration

**En resumen:** Sistema maduro y production-ready para monitoreo de oficina 24/7 con características avanzadas que lo distinguen de competidores básicos.