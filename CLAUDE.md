# Capturer - Screenshot Automation Application

## Descripción del Proyecto

**Capturer** es una aplicación de escritorio para Windows que automatiza la captura de pantallas y el envío de reportes por correo electrónico. La aplicación opera como un servicio en segundo plano con una interfaz de Windows Forms para configuración y gestión.

### Características Principales
- 📸 **Captura Automática**: Screenshots cada X minutos u horas configurables
- 📁 **Almacenamiento Organizado**: Guardado con nombres basados en fecha/hora
- 📧 **Reportes por Email**: Envío semanal automático + selección manual por rangos de fecha
- ⚙️ **Servicio Persistente**: Operación continua en segundo plano
- 🖥️ **Interfaz Simple**: Configuración y operaciones manuales intuitivas

## Arquitectura del Sistema

### Arquitectura de Alto Nivel
```
┌─────────────────────────────────────────────────────────────┐
│                    Aplicación Capturer                     │
├─────────────────────────────────────────────────────────────┤
│  Capa UI (Windows Forms)                                   │
│  ├─ Formulario Principal (Config & Estado)                 │
│  ├─ Formulario de Configuración                            │
│  └─ Formulario de Email (Selección de Fechas)              │
├─────────────────────────────────────────────────────────────┤
│  Capa de Servicios                                         │
│  ├─ ScreenshotService                                      │
│  ├─ EmailService                                           │
│  ├─ SchedulerService                                       │
│  └─ FileService                                            │
├─────────────────────────────────────────────────────────────┤
│  Capa de Datos                                             │
│  ├─ Configuration Manager                                  │
│  ├─ Screenshot Metadata                                    │
│  └─ Email Queue                                            │
├─────────────────────────────────────────────────────────────┤
│  Capa de Almacenamiento                                    │
│  ├─ Sistema de Archivos Local                              │
│  ├─ Archivos de Configuración (JSON)                       │
│  └─ Logs de Aplicación                                     │
└─────────────────────────────────────────────────────────────┘
```

## Componentes Principales

### 1. ScreenshotService
**Responsabilidades**:
- Captura automática usando Windows API
- Nomenclatura de archivos: `yyyy-MM-dd_HH-mm-ss.png`
- Gestión de timers en segundo plano
- Manejo de errores en captura

### 2. EmailService
**Responsabilidades**:
- Configuración y envío SMTP
- Gestión de plantillas de email
- Manejo de adjuntos múltiples
- Lógica de reintentos y recuperación

### 3. SchedulerService
**Responsabilidades**:
- Gestión de timers para capturas y emails
- Orquestación de tareas en segundo plano
- Persistencia de programaciones
- Limpieza de recursos al cerrar

### 4. FileService
**Responsabilidades**:
- Operaciones del sistema de archivos
- Gestión de directorios
- Limpieza y mantenimiento de archivos
- Estadísticas de almacenamiento

## Modelos de Datos

### Configuración Principal
```csharp
public class CapturerConfiguration
{
    public ScreenshotSettings Screenshot { get; set; } = new();
    public EmailSettings Email { get; set; } = new();
    public StorageSettings Storage { get; set; } = new();
    public ScheduleSettings Schedule { get; set; } = new();
}
```

### Configuración de Capturas
```csharp
public class ScreenshotSettings
{
    public TimeSpan CaptureInterval { get; set; } = TimeSpan.FromMinutes(30);
    public bool AutoStartCapture { get; set; } = true;
    public ImageFormat Format { get; set; } = ImageFormat.Png;
    public int Quality { get; set; } = 90;
}
```

### Configuración de Email
```csharp
public class EmailSettings
{
    public string SmtpServer { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public string Username { get; set; } = "";
    public string Password { get; set; } = ""; // Encriptado
    public List<string> Recipients { get; set; } = new();
    public bool EnableWeeklyReports { get; set; } = true;
}
```

## Interfaz de Usuario

### Formulario Principal
```
┌─────────────────────────────────────────────────────────────┐
│  Capturer - Gestor de Screenshots                          │
├─────────────────────────────────────────────────────────────┤
│  Panel de Estado:                                          │
│  ● Estado: [Ejecutándose/Detenido]  Próxima: [HH:MM:SS]    │
│  ● Screenshots Total: [1,234]  Almacenamiento: [2.3 GB]    │
│  ● Último Email: [2024-01-15]   Estado Email: [Exitoso]    │
├─────────────────────────────────────────────────────────────┤
│  Panel de Control:                                         │
│  [Iniciar] [Detener] [Configuración] [Enviar Email]        │
├─────────────────────────────────────────────────────────────┤
│  Screenshots Recientes (Últimos 10):                       │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ 2024-01-20_14-30-15.png    [Vista] [Abrir Carpeta] │   │
│  │ 2024-01-20_14-00-15.png    [Vista] [Abrir Carpeta] │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

## Sistema de Email

### Plantillas de Email
**Reporte Semanal**:
```
Asunto: Reporte Semanal Capturer - {FechaInicio} a {FechaFin}
Cuerpo:
- Período del reporte: {FechaInicio} - {FechaFin}
- Total de screenshots: {Cantidad}
- Computadora: {NombrePC}
- Generado: {FechaGeneracion}
```

### Estrategia de Entrega
1. **Compresión**: Archivos ZIP para múltiples imágenes (>5 screenshots)
2. **Límites de Tamaño**: 25MB máximo por adjunto
3. **Procesamiento por Lotes**: División de rangos grandes en múltiples emails
4. **Lógica de Reintentos**: Backoff exponencial para envíos fallidos
5. **Confirmación de Entrega**: Logging de éxito/fallo

## Tecnologías Utilizadas

### Stack Principal
- **Framework**: .NET 8 (Windows Desktop)
- **Interfaz**: Windows Forms
- **Email**: MailKit/MimeKit
- **Configuración**: Newtonsoft.Json
- **Logging**: NLog
- **Screenshots**: System.Drawing.Common

### Paquetes NuGet Recomendados
```xml
<PackageReference Include="MailKit" Version="4.3.0" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
<PackageReference Include="NLog" Version="5.2.7" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration" Version="8.0.0" />
```

## Roadmap de Implementación

### Fase 1: Infraestructura Base (Semana 1-2)
- ✅ Configuración del proyecto .NET 8
- ✅ Servicio básico de capturas
- ✅ Sistema de configuración

### Fase 2: Integración Email (Semana 2-3)
- 📧 Implementación del EmailService
- ⏰ Sistema de programación
- 🔄 Manejo de errores y reintentos

### Fase 3: Interfaz de Usuario (Semana 3-4)
- 🖥️ Desarrollo del formulario principal
- ⚙️ Interfaces de configuración y email
- 🔔 Integración con system tray

### Fase 4: Refinamiento (Semana 4-5)
- 🧹 Limpieza automática de archivos
- 📊 Monitoreo de almacenamiento
- 🚀 Optimización de rendimiento
- 📦 Testing y empaquetado

## Estructura de Archivos

```
Capturer/
├─ Capturer.exe                 # Aplicación principal
├─ Capturer.exe.config          # Configuración .NET
├─ appsettings.json            # Configuraciones de aplicación
├─ Screenshots/                # Carpeta por defecto de screenshots
├─ Logs/                       # Logs de aplicación
├─ Config/                     # Archivos de configuración
│  ├─ settings.json            # Configuración de usuario (encriptada)
│  └─ schedules.json           # Tareas programadas
└─ Temp/                       # Archivos temporales (prep email)
```

## Consideraciones de Seguridad

- **Encriptación de Contraseñas**: DPAPI para passwords de email
- **Permisos de Archivos**: Acceso restringido a archivos de configuración
- **Seguridad de Red**: SMTP sobre TLS/SSL
- **Protección de Datos**: No hay análisis/transmisión del contenido de screenshots

## Características Avanzadas

### Gestión de Almacenamiento
- Limpieza automática basada en antigüedad (90 días por defecto)
- Límite de tamaño de carpeta (5GB por defecto)
- Monitoreo de espacio en disco
- Compresión de archivos antiguos

### Monitoreo y Logging
- Logs detallados de todas las operaciones
- Métricas de rendimiento
- Alertas de errores críticos
- Rotación automática de logs

### Configuración Flexible
- Intervalos de captura configurables (minutos a horas)
- Múltiples formatos de imagen soportados
- Configuración de calidad de imagen
- Múltiples destinatarios de email
- Programación flexible de reportes semanales

---

## 🚀 Próximos Pasos

1. **Configurar el entorno de desarrollo**: Instalar .NET 8 SDK y configurar Visual Studio
2. **Implementar el servicio de capturas**: Comenzar con la funcionalidad básica de screenshots
3. **Desarrollar el sistema de configuración**: Crear la base para gestión de settings
4. **Integrar el servicio de email**: Implementar envío de correos con adjuntos
5. **Crear la interfaz de usuario**: Desarrollar las forms de Windows
6. **Testing y refinamiento**: Pruebas exhaustivas y optimización

La aplicación está diseñada para ser simple, confiable y fácil de mantener, cumpliendo exactamente con los requisitos especificados para una solución de captura de pantallas automatizada con notificaciones por email.