# 📸 Capturer v3.2.0 - Sistema Avanzado de Captura de Pantallas

![Capturer Logo](Capturer_Logo.png)

**Capturer v3.2.0** es una aplicación de escritorio profesional que automatiza la captura de pantallas y gestión inteligente de reportes por correo electrónico. Incluye un sistema avanzado de cuadrantes, gestión separada de emails rutinarios/manuales, reportes de Activity Dashboard automatizados, y ayuda contextual integrada.

🎯 **Perfecto para**: Monitoreo corporativo, trabajo remoto, documentación de procesos, cumplimiento regulatorio, análisis de productividad, y gestión de proyectos con reportes automatizados.

---

## 📋 Índice - Funcionalidades v3.2.0

1. [¿Qué hay de nuevo en v3.2.0?](#qué-hay-de-nuevo-en-v250)
2. [Sistema de Email Avanzado](#sistema-de-email-avanzado)
3. [Sistema de Cuadrantes Inteligente](#sistema-de-cuadrantes-inteligente)
4. [Interfaz de Usuario Renovada](#interfaz-de-usuario-renovada)
5. [Configuración Completa](#configuración-completa)
6. [Capturas de Pantalla](#capturas-de-pantalla)
7. [Gestión de Almacenamiento](#gestión-de-almacenamiento)
8. [System Tray y Atajos](#system-tray-y-atajos)
9. [Casos de Uso Avanzados](#casos-de-uso-avanzados)
10. [Solución de Problemas](#solución-de-problemas)
11. [Documentación Técnica](#documentación-técnica)
12. [FAQ v3.2.0](#preguntas-frecuentes-v250)

---

## ¿Qué hay de nuevo en v3.2.0?

### 🚀 Funcionalidades Principales Nuevas

#### 📧 **Sistema de Email Dual**
- **Email Manual** y **Reportes Automáticos** completamente separados
- Interfaz dedicada para cada tipo de reporte
- Configuración independiente con opciones avanzadas

#### 📊 **Sistema de Cuadrantes Avanzado**
- Procesamiento inteligente de áreas específicas de pantalla
- Perfiles de procesamiento configurables
- Integración completa con ambos sistemas de email
- Reportes separados por cuadrante

#### 🔧 **Gestión de Destinatarios Avanzada**
- Lista de verificación (checklist) para selección de destinatarios
- Gestión independiente por tipo de reporte
- Configuración de grupos de destinatarios

#### 📁 **Opciones de Formato Mejoradas**
- Envío en formato ZIP o archivos individuales
- Opciones separadas para email manual vs rutinario
- Compresión optimizada y gestión de tamaños

#### ❓ **Sistema de Ayuda Contextual**
- Tooltips informativos en toda la aplicación
- Botones de ayuda "?" en formularios principales
- Guías paso a paso integradas

#### ⚙️ **Interfaz Renovada**
- Separación clara entre funcionalidades manuales y automáticas
- Formularios dedicados para cada tipo de operación
- Mejor organización visual y flujo de trabajo

---

## Sistema de Email Avanzado

### 📧 Email Manual - Envío Inmediato

**Botón**: `[Email Manual]` - Para envío inmediato con control total

#### Características del Email Manual:

```
┌─────────────────────────────────────────────┐
│ 📧 FORMULARIO DE EMAIL MANUAL               │
├─────────────────────────────────────────────┤
│ 📅 Selección de Período:                    │
│   Desde: [26/08/2024] Hasta: [26/08/2024]   │
│   [Hoy] [Últimos 7] [Últimos 30] [Este mes] │
│                                             │
│ 👥 Destinatarios (Checklist):               │
│   ☑ admin@empresa.com                      │
│   ☑ supervisor@empresa.com                 │
│   ☐ backup@empresa.com                     │
│                                             │
│ 📎 Formato de Adjuntos:                     │
│   ● Archivo ZIP (Recomendado) ✓             │
│   ○ Imágenes individuales                   │
│                                             │
│ 🔲 Sistema de Cuadrantes:                   │
│   ☑ Procesar cuadrantes antes del envío     │
│   Perfil: [Trabajo Diario ▼]               │
│   ☑ Email separado por cuadrante           │
│                                             │
│ [? Ayuda] [Enviar Ahora] [Cancelar]         │
└─────────────────────────────────────────────┘
```

#### Flujo de Email Manual:
1. **Seleccionar período**: Desde/hasta con atajos rápidos
2. **Elegir destinatarios**: Checklist con selección múltiple
3. **Configurar formato**: ZIP vs individual
4. **Opcional - Cuadrantes**: Procesamiento previo con perfil específico
5. **Envío inmediato**: Confirmación y progreso visual

### 📅 Reportes Automáticos - Sistema Rutinario

**Botón**: `[Reportes]` - Para configurar reportes automáticos

#### Formulario de Reportes Automáticos:

```
┌─────────────────────────────────────────────┐
│ 📅 CONFIGURACIÓN DE REPORTES AUTOMÁTICOS    │
├─────────────────────────────────────────────┤
│ ⏰ Programación:                             │
│   Frecuencia: [Semanal ▼]                   │
│   Día: [Lunes ▼] Hora: [09:00 ▼]            │
│   ☑ Habilitar reportes automáticos          │
│                                             │
│ 👥 Destinatarios para Reportes:             │
│   ☑ gerencia@empresa.com                   │
│   ☑ rrhh@empresa.com                       │
│   ☐ auditoria@empresa.com                  │
│   [Agregar] [Quitar]                       │
│                                             │
│ 📎 Formato de Reportes:                     │
│   ● Archivo ZIP comprimido ✓                │
│   ○ Archivos individuales                   │
│                                             │
│ 🔲 Sistema de Cuadrantes para Reportes:     │
│   ☑ Usar cuadrantes en reportes automáticos │
│   Cuadrantes seleccionados:                │
│     ☑ Área de trabajo                      │
│     ☑ Dashboard                            │
│     ☐ Personal                             │
│   ☑ Procesar cuadrantes antes del envío     │
│   Perfil: [Producción ▼]                   │
│   ☑ Email separado por cuadrante           │
│                                             │
│ [? Ayuda] [Probar Envío] [Guardar] [Cancelar] │
└─────────────────────────────────────────────┘
```

#### Características de Reportes Automáticos:
- **Programación flexible**: Diario, semanal, mensual, personalizado
- **Destinatarios independientes**: Lista separada del email manual
- **Integración de cuadrantes**: Procesamiento automático con perfiles
- **Emails separados por cuadrante**: Un email por cada cuadrante seleccionado

### 📨 Ejemplo de Email Generado v3.2.0

#### Email de Reporte Semanal Estándar:
```
De: capturer@empresa.com
Para: gerencia@empresa.com, rrhh@empresa.com
Asunto: Reporte Semanal Capturer - 19/08/2024 a 26/08/2024

📸 REPORTE AUTOMÁTICO - CAPTURER v3.2.0
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Tipo: Reporte Semanal
Período: 2024-08-19 hasta 2024-08-26
Total capturas: 336 screenshots
Computadora: OFICINA-PC-01
Usuario: Juan.Perez
Generado: 2024-08-26 09:00:15

📊 Estadísticas Detalladas:
- Promedio por día: 48 capturas
- Tamaño total: 1.2 GB
- Formato: ZIP comprimido
- Compresión: 68% (ahorro 2.1 GB)

📎 Adjunto: capturas_20240819-20240826.zip (1.2 GB)

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Este email fue generado automáticamente por Capturer v3.2.0
```

#### Email de Cuadrante Individual:
```
De: capturer@empresa.com  
Para: supervisor@empresa.com
Asunto: Reporte Cuadrante "Área de Trabajo" - 26/08/2024

🔲 REPORTE POR CUADRANTE - CAPTURER v3.2.0
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Cuadrante: "Área de Trabajo"
Período: 2024-08-26 (Manual)
Perfil procesamiento: Trabajo Diario
Imágenes procesadas: 24 screenshots
Tamaño: 385 MB

📊 Detalles del Procesamiento:
- Región: X:0, Y:0, Ancho:1920, Alto:800
- Filtros aplicados: Enfoque de trabajo, OCR mejorado
- Calidad: Alta resolución mantenida
- Formato salida: PNG optimizado

📎 Adjunto: cuadrante_area-trabajo_20240826.zip (385 MB)

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Procesado con perfil: Trabajo Diario
```

---

## Sistema de Cuadrantes Inteligente

### 🔲 ¿Qué son los Cuadrantes en v3.2.0?

Los cuadrantes son **áreas definidas de la pantalla** que Capturer puede procesar de forma independiente. En v3.2.0, se integran completamente con el sistema de email para reportes especializados y con el nuevo sistema de Activity Dashboard.

#### Ventajas de los Cuadrantes:
- 🎯 **Enfoque específico**: Solo áreas relevantes para cada destinatario
- 🔒 **Privacidad mejorada**: Excluir áreas personales automáticamente  
- 📊 **Reportes especializados**: Diferentes cuadrantes para diferentes roles
- ⚡ **Procesamiento optimizado**: Solo procesar lo necesario
- 📈 **Análisis dirigido**: Métricas específicas por área de trabajo

### ⚙️ Configuración de Cuadrantes

#### Definición de Cuadrantes:
```
┌─────────────────────────────────────────────┐
│ 🔲 CONFIGURACIÓN DE CUADRANTES              │
├─────────────────────────────────────────────┤
│ Cuadrante: "Área de Trabajo"                │
│   Región: X:[0] Y:[0] Ancho:[1920] Alto:[800]│
│   ☑ Activo                                  │
│   Descripción: [Panel principal de trabajo] │
│                                             │
│ Cuadrante: "Dashboard"                      │
│   Región: X:[1920] Y:[0] Ancho:[900] Alto:[600]│
│   ☑ Activo                                  │
│   Descripción: [Métricas y KPIs]           │
│                                             │
│ [Agregar] [Editar] [Eliminar] [Vista previa]│
└─────────────────────────────────────────────┘
```

#### Perfiles de Procesamiento:
```
┌─────────────────────────────────────────────┐
│ 🎯 PERFILES DE PROCESAMIENTO                │
├─────────────────────────────────────────────┤
│ Perfil: "Trabajo Diario"                    │
│   - Enfoque de trabajo: ☑                  │
│   - OCR mejorado: ☑                        │
│   - Compresión: Media                       │
│   - Calidad: Alta                          │
│                                             │
│ Perfil: "Producción"                        │
│   - Análisis automático: ☑                 │
│   - Detección de errores: ☑                │
│   - Métricas de rendimiento: ☑             │
│   - Alertas automáticas: ☑                 │
│                                             │
│ [Nuevo Perfil] [Editar] [Duplicar]         │
└─────────────────────────────────────────────┘
```

### 📧 Integración con Sistema de Email

#### En Email Manual:
```
🔲 Sistema de Cuadrantes:
  ☑ Procesar cuadrantes antes del envío
  Perfil: [Trabajo Diario ▼]
  ☑ Email separado por cuadrante

Resultado:
  ✅ 3 emails separados (uno por cuadrante seleccionado)
  ✅ Cada email contiene solo imágenes de ese cuadrante
  ✅ Procesamiento específico según perfil elegido
```

#### En Reportes Automáticos:
```
🔲 Sistema de Cuadrantes para Reportes:
  ☑ Usar cuadrantes en reportes automáticos
  Cuadrantes seleccionados:
    ☑ Área de trabajo
    ☑ Dashboard  
    ☐ Personal
  ☑ Procesar cuadrantes antes del envío
  Perfil: [Producción ▼]
  ☑ Email separado por cuadrante

Resultado automático cada lunes 09:00:
  ✅ Email 1: Reporte "Área de trabajo" → supervisor@empresa.com
  ✅ Email 2: Reporte "Dashboard" → gerencia@empresa.com
  ✅ Procesamiento automático con perfil "Producción"
```

### 🎯 Casos de Uso de Cuadrantes

#### 🏢 **Oficina Corporativa**:
```yaml
Cuadrante 1: "Aplicación Principal"
  - Región: Monitor principal, zona de trabajo
  - Destinatarios: Supervisor directo
  - Perfil: Productividad estándar

Cuadrante 2: "Dashboard KPI"  
  - Región: Monitor secundario, métricas
  - Destinatarios: Gerencia, Analytics
  - Perfil: Análisis de rendimiento

Cuadrante 3: "Comunicaciones"
  - Región: Panel de chat/email
  - Destinatarios: RRHH (opcional)
  - Perfil: Análisis de comunicación
```

#### 🏠 **Trabajo Remoto**:
```yaml
Cuadrante 1: "Área de Trabajo"
  - Región: 70% izquierda de pantalla
  - Destinatarios: Jefe, RRHH
  - Perfil: Trabajo remoto

Cuadrante 2: "Referencia"
  - Región: 30% derecha de pantalla  
  - Destinatarios: Solo empleado (privado)
  - Perfil: Documentación personal
```

#### 🔧 **Monitoreo de Servidor**:
```yaml
Cuadrante 1: "CPU y Memoria"
  - Región: Panel de métricas sistema
  - Destinatarios: Administradores
  - Perfil: Alerta crítica

Cuadrante 2: "Logs de Error"
  - Región: Terminal y logs
  - Destinatarios: Equipo desarrollo
  - Perfil: Análisis de errores

Cuadrante 3: "Tráfico de Red"
  - Región: Gráficos de red
  - Destinatarios: Seguridad IT
  - Perfil: Monitoreo de red
```

---

## Interfaz de Usuario Renovada

### 🖥️ Ventana Principal v3.2.0

```
┌─────────────────────────────────────────────────────────────┐
│ 📸 Capturer v3.2.0 - Sistema Avanzado de Capturas            │
├─────────────────────────────────────────────────────────────┤
│ 📊 Panel de Estado Mejorado:                               │
│   Estado: [🟢 Funcionando]  Próxima: [14:35:20]           │
│   Total: [1,234 capturas]  Storage: [3.2GB/10GB]         │
│   Último email: [Ayer 09:00]  Próximo reporte: [Lun 09:00] │
│                                                             │
│ 🎮 Panel de Control Renovado:                              │
│   [▶ Iniciar] [⏹ Detener] [⚙ Configurar] [📄 Ver Todo]     │
│                                                             │
│ 📧 Sistema de Email Dual:                                  │
│   [📧 Email Manual] [📅 Reportes] [🔲 Cuadrantes]          │
│                                                             │
│ 📸 Capturas Recientes (Últimas 10):                        │
│ ┌─────────────────────────────────────────────────────┐   │
│ │ 📸 2024-08-26_14-30-15.png  2.1MB  [👁 Ver] [📁 Abrir] │   │  
│ │ 📸 2024-08-26_14-00-15.png  1.9MB  [👁 Ver] [📁 Abrir] │   │
│ │ 📸 2024-08-26_13-30-15.png  2.3MB  [👁 Ver] [📁 Abrir] │   │
│ └─────────────────────────────────────────────────────┘   │
│                                                             │
│ 💡 Estado del Sistema:                                     │
│   🔲 Cuadrantes: [2 activos]  📧 Email: [✅ Configurado]   │
│   🧹 Limpieza: [Automática]  🔒 Seguridad: [Encriptado]   │
└─────────────────────────────────────────────────────────────┘
```

#### Nuevos Botones y Funciones:

- **[📧 Email Manual]**: Abre formulario de email inmediato con todas las opciones
- **[📅 Reportes]**: Abre configuración de reportes automáticos
- **[🔲 Cuadrantes]**: Acceso directo a configuración de cuadrantes
- **[📄 Ver Todo]**: Lista completa de capturas con búsqueda y filtros

### ❓ Sistema de Ayuda Contextual

#### Botones de Ayuda "?" en todos los formularios:

```
┌─────────────────────────────────────────────┐
│ 📧 Destinatarios: [?]                       │
│   Lista de emails separados por ;           │
│   Ejemplo: admin@empresa.com; rrhh@corp.com │
│                                             │
│ 📎 Formato ZIP: [?]                         │
│   Comprime todas las imágenes en un archivo │
│   Recomendado para más de 5 screenshots     │
│                                             │
│ 🔲 Procesar cuadrantes: [?]                 │
│   Aplica filtros específicos a áreas         │
│   definidas antes del envío del email       │
└─────────────────────────────────────────────┘
```

#### Tooltips Informativos:

**Al pasar mouse sobre controles importantes**:
- 📅 **Selector de fechas**: "Selecciona el período de capturas a incluir"
- ✅ **Checkbox destinatarios**: "Marca los destinatarios para este envío"  
- 🔄 **Formato ZIP**: "Recomendado: reduce tamaño y facilita descarga"
- ⚙️ **Perfil cuadrante**: "Configuración de procesamiento para este cuadrante"

---

## Configuración Completa

### ⚙️ Panel de Configuración Expandido

#### Pestaña 1: 📸 **Capturas de Pantalla**
```
┌─────────────────────────────────────────────┐
│ ⏱️ Configuración de Captura:                │
│   Intervalo: [30] minutos  [?]              │
│   ☑ Inicio automático al abrir programa     │
│   ☑ Captura en horario laboral únicamente   │
│   Horario: [09:00] a [17:00]               │
│                                             │
│ 📱 Opciones de Pantalla:                    │
│   Modo: [Todas las pantallas ▼]            │
│   Calidad: [90%] ████████████████░░░        │
│   Formato: [PNG ▼] (JPG próximamente)      │
│   ☑ Incluir cursor del mouse               │
│                                             │
│ 📁 Ubicación y Organización:                │
│   Carpeta: [C:\Users\...\Capturer\Screenshots]│
│   [Examinar]                               │
│   ☑ Crear subcarpetas por fecha            │
│   Nomenclatura: [yyyy-MM-dd_HH-mm-ss]      │
└─────────────────────────────────────────────┘
```

#### Pestaña 2: 📧 **Configuración de Email**
```
┌─────────────────────────────────────────────┐
│ 🌐 Servidor SMTP:                           │
│   Servidor: [smtp.gmail.com] [?]           │
│   Puerto: [587]  Seguridad: [TLS ▼]        │
│                                             │
│ 🔐 Autenticación:                           │
│   Usuario: [tu_email@gmail.com]            │
│   Contraseña: [****************] [👁]      │
│   [🧪 Probar Email] ← Prueba real SMTP      │
│                                             │
│ 👤 Información del Remitente:               │
│   Nombre: [Capturer - Sistema OFICINA-01]  │
│   Email respuesta: [mismo que usuario]     │
│                                             │
│ ⚙️ Configuración Avanzada:                  │
│   Timeout: [30] segundos                   │
│   Reintentos: [3] intentos                 │
│   ☑ Usar SSL/TLS                           │
│   ☑ Verificar certificados                 │
└─────────────────────────────────────────────┘
```

#### Pestaña 3: 💾 **Almacenamiento y Limpieza**
```
┌─────────────────────────────────────────────┐
│ 🧹 Limpieza Automática:                     │
│   ☑ Habilitar limpieza automática          │
│   Retener archivos por: [90] días          │
│   Límite de espacio: [10] GB               │
│   Hora de limpieza: [02:00] (madrugada)    │
│                                             │
│ 📊 Estado Actual:                           │
│   Archivos actuales: 1,234 screenshots    │
│   Espacio usado: 3.2 GB de 10 GB (32%)    │
│   Archivo más antiguo: hace 45 días        │
│   Próxima limpieza: En 7 horas             │
│                                             │
│ 🔧 Mantenimiento Manual:                    │
│   [Limpiar Ahora] [Ver Detalles] [Cambiar Ubicación] │
└─────────────────────────────────────────────┘
```

#### Pestaña 4: 🔲 **Sistema de Cuadrantes**
```
┌─────────────────────────────────────────────┐
│ ☑ Habilitar sistema de cuadrantes          │
│ Máximo cuadrantes: [36] (grilla 6x6)       │
│                                             │
│ 📝 Cuadrantes Configurados:                 │
│ ┌─────────────────────────────────────────┐ │
│ │ ✅ Área de Trabajo    1920×800  Activo   │ │
│ │ ✅ Dashboard         900×600   Activo   │ │  
│ │ ❌ Personal          800×600   Inactivo │ │
│ └─────────────────────────────────────────┘ │
│                                             │
│ [🆕 Nuevo] [✏️ Editar] [🗑️ Eliminar] [📋 Duplicar]│
│                                             │
│ 🎯 Perfiles de Procesamiento:               │
│   Activo: [Trabajo Diario ▼]               │
│   [Gestionar Perfiles]                     │
│                                             │
│ 🎨 Opciones Visuales:                       │
│   ☑ Mostrar colores de preview             │
│   ☑ Habilitar logging detallado            │
└─────────────────────────────────────────────┘
```

### 🔐 Configuración de Seguridad

#### Encriptación de Contraseñas:
- **DPAPI de Windows**: Cifrado a nivel usuario + máquina
- **Almacenamiento seguro**: Archivos protegidos en %APPDATA%
- **No persistencia**: Contraseñas nunca en texto plano
- **Verificación**: Botón "👁" para mostrar/ocultar durante configuración

#### Validación en Tiempo Real:
```
🧪 Prueba de Email:
✅ Conectando a smtp.gmail.com:587... OK
✅ Negociando TLS... OK  
✅ Autenticando usuario... OK
✅ Enviando email de prueba... OK
✅ Email de prueba enviado exitosamente a tu_email@gmail.com
```

---

## Capturas de Pantalla

### 📸 Modos de Captura v3.2.0

#### 1. 🔄 **Captura Automática Inteligente**
```yaml
Configuración estándar:
  Intervalo: 30 minutos
  Horario: 09:00 - 17:00
  Solo días laborales: Lunes a Viernes
  
Configuración intensiva:
  Intervalo: 5 minutos  
  Horario: 24/7
  Incluye fines de semana

Configuración proyectos:
  Intervalo variable: 15-60 min según actividad
  Detección de inactividad: Pausa automática
  Reanudación inteligente: Detecta regreso al trabajo
```

#### 2. 🖱️ **Captura Manual Mejorada**
- **Desde ventana principal**: `[Tomar Foto Ahora]`
- **Desde system tray**: Click derecho → "📸 Captura"
- **Atajo de teclado**: `F2` (configurable)
- **Con cuadrantes**: Captura procesada inmediatamente

#### 3. 📅 **Captura Programada Avanzada**
```
Ejemplos de programación:
⏰ Cada 2 horas de 08:00 a 18:00
📅 Solo lunes, miércoles, viernes  
🎯 5 minutos después de login del usuario
🔔 30 minutos antes de reuniones programadas (Outlook integration)
```

### 🖼️ Calidad y Formato

#### Configuraciones de Calidad:
| Calidad | Tamaño promedio | Uso recomendado |
|---------|----------------|------------------|
| **Baja (70%)** | 0.8-1.2 MB | Monitoreo masivo, archivado |
| **Media (85%)** | 1.5-2.2 MB | Uso estándar, balance calidad/tamaño |
| **Alta (95%)** | 2.8-4.1 MB | Documentación, análisis detallado |
| **Máxima (100%)** | 4.5-8.2 MB | Proyectos críticos, evidencia legal |

#### Nomenclatura Inteligente:
```
Formato estándar:
2024-08-26_14-30-15.png

Formato con cuadrantes:  
2024-08-26_14-30-15_Trabajo.png
2024-08-26_14-30-15_Dashboard.png

Formato de proyecto:
PROYECTO_2024-08-26_14-30-15_Q1.png
```

### 📊 Análisis y Métricas

#### Estadísticas Automáticas:
```
📈 Panel de Estadísticas:
- Capturas por día: Promedio 48 (últimos 7 días)
- Pico de actividad: 14:00-16:00 (mayor frecuencia)  
- Días más activos: Martes y miércoles
- Tamaño promedio por captura: 2.1 MB
- Tendencia semanal: +12% vs semana anterior
- Eficiencia de compresión: 68% (formato ZIP)
```

---

## Gestión de Almacenamiento

### 📁 Estructura de Directorios v3.2.0

```
C:\Users\[Usuario]\Documents\Capturer\
├── Screenshots\                           ← Capturas principales
│   ├── 2024-08\                          ← Organización mensual  
│   │   ├── 2024-08-26_14-30-15.png
│   │   └── 2024-08-26_14-00-15.png
│   └── 2024-09\
├── Quadrants\                             ← Cuadrantes procesados
│   ├── Q1-AreaTrabajo\
│   │   ├── 2024-08-26_14-30-15_Q1.png
│   │   └── processed_metadata.json
│   ├── Q2-Dashboard\
│   └── Q3-Personal\
├── Reports\                               ← Reportes generados
│   ├── Weekly\
│   │   └── 2024-08-19_to_2024-08-26.zip
│   ├── Manual\
│   └── Quadrant\
├── Temp\                                  ← Archivos temporales
│   ├── zip_staging\
│   └── email_queue\
└── Logs\                                  ← Sistema de logging
    ├── capturer-2024-08.log
    ├── email-2024-08.log
    └── quadrant-processing-2024-08.log

%APPDATA%\Capturer\                        ← Configuración segura
├── settings.json                          ← Config principal
├── quadrant-configs.json                  ← Configuraciones cuadrantes
├── email-templates.json                   ← Plantillas de email
└── security\
    └── encrypted_passwords.dat            ← Contraseñas encriptadas
```

### 🧹 Sistema de Limpieza Inteligente v3.2.0

#### Políticas de Retención:
```yaml
Retención por Tipo:
  Screenshots_Standard: 90 días
  Screenshots_Quadrant: 120 días (mayor valor)
  Reports_Weekly: 365 días (compliance)
  Reports_Manual: 180 días
  Logs_Application: 30 días
  Temp_Files: 7 días

Limpieza por Prioridad:
  1. Archivos temporales (inmediato)
  2. Screenshots más antiguos (por política)
  3. Logs de aplicación (rotación)
  4. Reportes manuales (menor prioridad)
  5. Reportes automáticos (máxima retención)
```

#### Algoritmo de Limpieza Inteligente:
```
📊 Proceso de Limpieza Automática:
1. Análisis de espacio: Verificar límites configurados
2. Categorización: Clasificar archivos por importancia
3. Cálculo de prioridad: Edad + tamaño + tipo + uso
4. Limpieza escalonada:
   - Fase 1: Temp y caché (objetivo: liberar 10%)
   - Fase 2: Screenshots antiguos (objetivo: liberar 20%)  
   - Fase 3: Logs rotados (objetivo: liberar 5%)
5. Verificación: Comprobar objetivos alcanzados
6. Logging: Registrar archivos eliminados y espacio liberado
```

#### Panel de Control de Almacenamiento:
```
┌─────────────────────────────────────────────┐
│ 💾 GESTIÓN DE ALMACENAMIENTO v3.2.0           │
├─────────────────────────────────────────────┤
│ 📊 Estado Actual:                           │
│   Used: ████████████░░░░ 8.2GB/10GB (82%)  │
│   Screenshots: 6.1GB (1,234 archivos)      │
│   Cuadrantes: 1.8GB (456 archivos)         │
│   Reportes: 0.3GB (24 archivos)            │
│                                             │
│ 🧹 Próxima Limpieza Automática:             │
│   Programada: Hoy 02:00 AM                 │
│   Archivos a eliminar: ~180 screenshots    │
│   Espacio a liberar: ~1.2 GB               │
│                                             │
│ 🎮 Acciones Manuales:                       │
│   [Limpiar Ahora] [Vista Detallada]        │
│   [Cambiar Límites] [Exportar Todo]        │
└─────────────────────────────────────────────┘
```

### 💽 Opciones de Backup y Archivado

#### Backup Automático:
```yaml
Configuración de Backup:
  Frecuencia: Semanal
  Destino: D:\Backup\Capturer\
  Compresión: ZIP con cifrado AES-256
  Retención backup: 12 semanas
  Verificación integridad: SHA-256 checksums
  
Contenido del Backup:
  - Screenshots (últimos 30 días)
  - Configuración completa
  - Logs importantes
  - Reportes generados
  - Metadatos de cuadrantes
```

---

## System Tray y Atajos

### 🖥️ Funcionalidad System Tray Avanzada

#### Menú Contextual Completo v3.2.0:
```
┌────────────────────────────────────┐
│ 📸 Capturer v3.2.0                   │
├────────────────────────────────────┤
│ 👁️  Mostrar ventana principal       │
│ ─────────────────────────────────  │
│ 📸 Captura inmediata               │
│ 📧 Email manual rápido             │
│ 🔲 Captura con cuadrantes          │
│ ─────────────────────────────────  │
│ ▶️  Iniciar capturas automáticas    │
│ ⏹️ Detener capturas               │
│ ─────────────────────────────────  │
│ ⚙️  Configuración rápida            │
│ 📊 Ver estadísticas               │
│ 🔍 Abrir carpeta capturas         │
│ ─────────────────────────────────  │
│ ❌ Salir completamente             │
└────────────────────────────────────┘
```

#### Funciones Mejoradas del System Tray:

**📸 Captura inmediata**:
- Captura instantánea sin abrir ventana principal
- Notificación con nombre del archivo generado
- Opción de procesamiento de cuadrantes inmediato

**📧 Email manual rápido**:
- Formulario simplificado para envío inmediato
- Período preseleccionado: "Último día"
- Destinatarios desde configuración rápida

**🔲 Captura con cuadrantes**:
- Captura inmediata con procesamiento de cuadrantes activos
- Usando último perfil utilizado
- Resultados guardados en carpeta de cuadrantes

### ⌨️ Atajos de Teclado Avanzados

#### Atajos Globales (funcionan desde cualquier aplicación):
| Atajo | Acción | Descripción |
|-------|--------|-------------|
| **Ctrl+Shift+C** | Captura inmediata | Screenshot instantáneo |
| **Ctrl+Shift+Q** | Captura cuadrantes | Captura con procesamiento |
| **Ctrl+Shift+E** | Email rápido | Formulario email simplificado |
| **Ctrl+Shift+S** | Mostrar/Ocultar | Toggle ventana principal |

#### Atajos en Ventana Principal:
| Atajo | Acción | Contexto |
|-------|--------|----------|
| **F1** | Iniciar/Detener | Toggle captura automática |
| **F2** | Captura manual | Screenshot inmediato |
| **F3** | Configuración | Abrir panel configuración |
| **F4** | Email manual | Formulario email manual |
| **F5** | Reportes | Configuración reportes automáticos |
| **F6** | Cuadrantes | Configuración cuadrantes |
| **Ctrl+R** | Recargar | Actualizar interface y configuración |
| **Esc** | Minimizar | Enviar a system tray |

#### Atajos en Formularios:
| Atajo | Acción | Formularios |
|-------|--------|-------------|
| **Ctrl+Enter** | Enviar/Guardar | Email, Configuración |
| **Ctrl+T** | Probar | Prueba de email SMTP |
| **F1** | Ayuda contextual | Todos los formularios |
| **Ctrl+Z** | Deshacer | Campos de texto |
| **Ctrl+A** | Seleccionar todo | Listas, destinatarios |

### 📢 Sistema de Notificaciones Inteligente

#### Tipos de Notificaciones:
```
✅ Operaciones Exitosas:
"✅ Screenshot capturado: 2024-08-26_14-30-15.png"
"📧 Email enviado exitosamente a 3 destinatarios"  
"🔲 Cuadrantes procesados: 2 regiones completadas"
"🧹 Limpieza automática: 25 archivos eliminados, 1.2GB liberados"

⚠️ Advertencias:
"⚠️ Espacio bajo: 85% del límite alcanzado"
"⚠️ Email pendiente: Reintentando en 5 minutos"
"⚠️ Cuadrante inactivo: 'Personal' deshabilitado"

❌ Errores:
"❌ Error captura: Sin permisos en carpeta destino"
"❌ Error email: Falló autenticación SMTP"  
"❌ Error cuadrante: Región fuera de pantalla"

📊 Información:
"📊 Reporte semanal generado: 336 capturas procesadas"
"🔄 Configuración actualizada exitosamente"
"⏰ Próximo reporte automático: Lunes 09:00"
```

#### Configuración de Notificaciones:
```
┌─────────────────────────────────────────────┐
│ 🔔 CONFIGURACIÓN DE NOTIFICACIONES          │
├─────────────────────────────────────────────┤
│ ☑ Mostrar notificaciones del sistema        │
│ ☑ Sonidos de notificación                   │
│ ☑ Mantener historial de notificaciones      │
│                                             │
│ 🎯 Tipos de Notificación:                   │
│   ☑ Capturas exitosas                      │
│   ☑ Emails enviados                        │
│   ☑ Errores críticos                       │
│   ☐ Información general (puede ser verboso) │
│                                             │
│ ⏱️ Duración en Pantalla:                    │
│   Éxito: [5] segundos                      │
│   Advertencia: [10] segundos               │
│   Error: [15] segundos                     │
└─────────────────────────────────────────────┘
```

---

## Casos de Uso Avanzados

### 🏢 **Caso 1: Corporación Multinacional**

#### Configuración Empresarial:
```yaml
Escenario: 500 estaciones de trabajo en 5 países
Configuración por región:
  
América (EST):
  Horario: 08:00-17:00 EST
  Intervalo: 20 minutos
  Cuadrantes:
    - Aplicación ERP (obligatorio)
    - Dashboard ventas (gerencia)
    - Comunicaciones (RRHH opcional)
  
Europa (CET):  
  Horario: 09:00-18:00 CET
  Intervalo: 30 minutos
  Cuadrantes:
    - Sistema CRM (ventas)
    - Reporting financiero (contabilidad)
    
Asia-Pacific (JST):
  Horario: 09:00-18:00 JST  
  Intervalo: 15 minutos
  Cuadrantes:
    - Manufacturing dashboard
    - Quality control metrics

Reportes automáticos por región:
  América: Viernes 17:00 → US-management@corp.com
  Europa: Viernes 18:00 → EU-management@corp.com
  Asia: Viernes 18:00 → APAC-management@corp.com

Reportes globales:
  Domingo 00:00 UTC → global-exec@corp.com
  Contenido: Resumen ejecutivo con métricas agregadas
```

#### Gestión de Compliance:
```
🔒 Configuración de Compliance:
- Retención: 7 años (regulaciones financieras)
- Encriptación: AES-256 para archivos + DPAPI para credenciales
- Auditoría: Logs completos con timestamps precisos
- Backup: Triple redundancia geográfica
- Acceso: Solo personal autorizado por región

📊 Métricas de Compliance:
- 99.7% uptime en capturas
- 0% pérdida de datos en 12 meses
- 100% de emails entregados exitosamente
- Tiempo promedio de retención: 7.2 años
```

### 🏠 **Caso 2: Equipo Remoto Distribuido**

#### Configuración por Empleado:
```yaml
Perfil: Desarrollador Senior
  Configuración:
    Horario: Flexible 06:00-22:00
    Intervalo: Variable por proyecto
      - Desarrollo: 30 minutos
      - Meetings: 10 minutos  
      - Research: 60 minutos
    
  Cuadrantes inteligentes:
    Q1: "Código Principal" (IDE + terminal)
      - Destinatarios: Tech Lead, PM
      - Perfil: Desarrollo estándar
      - Email separado: Solo para reviews de código
      
    Q2: "Documentación" (Navegador + docs)
      - Destinatarios: Solo empleado (privado)
      - Perfil: Personal
      
    Q3: "Comunicación" (Slack + email)
      - Destinatarios: RRHH (opcional)
      - Perfil: Análisis de comunicación

  Reportes adaptativos:
    Lunes-Miércoles: Reporte diario a Tech Lead
    Jueves-Viernes: Reporte combinado semanal a Management
    Fin sprint: Reporte especial con métricas de productividad

Perfil: Project Manager  
  Configuración:
    Horario: 09:00-18:00 (horario fijo)
    Intervalo: 20 minutos consistente
    
  Cuadrantes especializados:
    Q1: "Dashboard de Proyecto" (JIRA + métricas)
      - Destinatarios: Stakeholders, Executive team
      - Perfil: Reporting ejecutivo
      - Email separado: Daily a stakeholders
      
    Q2: "Comunicación con Equipo" (reuniones + chat)
      - Destinatarios: Solo para auditoría interna
      - Perfil: Gestión de equipo
```

### 🏥 **Caso 3: Sistema de Salud (Compliance HIPAA)**

#### Configuración Médica Crítica:
```yaml
Configuración hospitalaria:
  Personal médico: 150 estaciones
  Áreas críticas: UCI, Emergencias, Farmacia
  Compliance: HIPAA, Joint Commission

Configuración por área:
  
UCI (Unidad Cuidados Intensivos):
    Intervalo: 5 minutos (crítico)
    Cuadrantes protegidos:
      Q1: "Signos Vitales" - Solo dashboards médicos
      Q2: "Medicación" - Sistema farmacéutico
      Q3: "Comunicación" - Excluye info pacientes
    
    Reportes especiales:
      - Supervisor médico: Cada turno (8h)
      - Auditoría HIPAA: Semanal, datos anonimizados
      - Seguridad TI: Diario, solo métricas sistema

  Farmacia:
    Intervalo: 10 minutos
    Cuadrantes específicos:
      Q1: "Sistema dispensación" - Tracking medicamentos
      Q2: "Inventario" - Stock y reposición
    
    Compliance automation:
      - FDA reporting: Mensual automático
      - Auditoría interna: Semanal
      - Control de calidad: Diario

Seguridad especial:
  - Encriptación médica: AES-256 + certificados digitales
  - Anonimización: OCR que elimina nombres pacientes
  - Acceso restringido: Solo personal autorizado
  - Logs inmutables: Blockchain para auditoría
  - Backup geográfico: 3 ubicaciones separadas
```

### 🏭 **Caso 4: Planta Industrial 24/7**

#### Monitoreo de Producción:
```yaml
Planta automotriz: 3 turnos, 24x7x365
Personal: 300 operadores + 50 supervisores

Configuración por turno:
  
Turno 1 (06:00-14:00):
  Producción estándar: 15 minutos intervalo
  Cuadrantes de línea:
    Q1: "Línea Ensamble" - Métricas producción
    Q2: "Control Calidad" - Defectos y rechazos  
    Q3: "Mantenimiento" - Estado maquinaria
    
Turno 2 (14:00-22:00):
  Producción intensiva: 10 minutos
  Cuadrantes especializados:
    Q1: "Producción High-Volume"
    Q2: "Logistics & Shipping"
    
Turno 3 (22:00-06:00):
  Mantenimiento: 30 minutos
  Cuadrantes de servicio:
    Q1: "Mantenimiento Preventivo"
    Q2: "Monitoreo Seguridad"

Reportes automáticos:
  - Supervisor turno: Al finalizar cada turno
  - Gerencia planta: Diario 07:00
  - Corporativo: Semanal lunes 08:00
  - Mantenimiento: Continuo (alertas automáticas)

Métricas de rendimiento:
  - OEE (Overall Equipment Effectiveness)
  - Takt time vs cycle time
  - Defect rate por línea
  - Downtime analysis
  - Safety incidents tracking
```

#### Integración con Sistemas ERP:
```
🔗 Integración Automática:
- SAP connector: Métricas production → SAP PP
- MES integration: Real-time data flow
- Quality management: Auto-detect defects from screenshots
- Predictive maintenance: AI analysis de patterns en screenshots

📊 Dashboard Ejecutivo:
- KPIs en tiempo real desde screenshots
- Alertas automáticas por thresholds
- Reportes ejecutivos con capturas contextuales
- ROI tracking por área/turno/operador
```

### 🎓 **Caso 5: Institución Educativa Online**

#### Campus Virtual Monitoring:
```yaml
Universidad online: 5,000 estudiantes + 200 profesores
Plataformas: Moodle, Zoom, Teams, laboratorios virtuales

Configuración por rol:

Estudiantes (opcional, privacy-focused):
  Horario: Clases programadas únicamente
  Intervalo: 45 minutos (duración clase)
  Cuadrantes educativos:
    Q1: "Plataforma de clase" - Solo contenido educativo
    Q2: "Materiales estudio" - Recursos académicos
  
  Privacy protection:
    - Solo durante evaluaciones (con consentimiento)
    - Datos anonimizados para analytics
    - Control total del estudiante

Profesores:
  Horario: Horas académicas (08:00-20:00)
  Intervalo: 30 minutos
  Cuadrantes pedagógicos:
    Q1: "Plataforma enseñanza" - LMS, grading
    Q2: "Preparación clases" - Materiales, planning
    Q3: "Comunicación estudiantes" - Email, forums
    
  Reportes académicos:
    - Coordinación académica: Semanal
    - Evaluación docente: Mensual (métricas engagement)
    - Research tracking: Por proyecto

Administradores IT:
  Monitoreo 24/7 de infraestructura
  Cuadrantes técnicos:
    Q1: "System monitoring" - Servidores, DB
    Q2: "Network performance" - Bandwidth, latencia  
    Q3: "Security dashboard" - Threats, access logs
    
  Alertas automáticas:
    - Performance degradation
    - Security incidents
    - Capacity planning alerts
```

---

## Solución de Problemas

### 🚨 Diagnóstico Automático v3.2.0

#### Sistema de Auto-Diagnóstico:
```
🔍 Herramienta de Diagnóstico Integrada:
[⚙ Configurar] → [🔧 Diagnóstico] → [🚀 Ejecutar Verificación]

Verificaciones automáticas:
✅ Permisos de archivos y carpetas
✅ Conectividad SMTP y puertos
✅ Integridad de configuración
✅ Recursos del sistema (RAM, CPU, espacio)
✅ Compatibilidad de cuadrantes con resolución actual
✅ Estado de encriptación de contraseñas
✅ Validez de programaciones de reportes

Resultado ejemplo:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🎯 DIAGNÓSTICO SISTEMA CAPTURER v3.2.0
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✅ Sistema operativo: Windows 11 compatible
✅ .NET Runtime: 8.0.3 instalado
✅ Permisos carpeta: Lectura/escritura OK
✅ Espacio disponible: 15.2 GB libre
⚠️ SMTP: Timeout elevado (verificar firewall)
❌ Cuadrante Q3: Región fuera de pantalla actual
✅ Configuración: Válida y encriptada

🔧 Acciones recomendadas:
1. Configurar excepción firewall para puerto 587
2. Reconfigurar cuadrante Q3 para resolución actual
```

### 📧 **Problemas de Email Avanzados**

#### Error: Emails Rutinarios No Se Envían
```
🔍 Síntomas:
- Estado: "Reportes automáticos configurados"  
- Próximo reporte: "Lunes 09:00"
- Pero emails no llegan el lunes

🛠️ Soluciones paso a paso:
1. Verificar configuración independiente:
   [Reportes] → [Probar Envío] → Debe ser ✅
   
2. Verificar destinatarios específicos:
   Lista de destinatarios rutinarios ≠ lista manual
   Verificar que hay al menos 1 destinatario marcado
   
3. Verificar programación:
   Frecuencia: ¿Correcta?
   Hora: ¿En zona horaria local?
   ☑ "Habilitar reportes automáticos" debe estar marcado
   
4. Verificar logs:
   C:\Users\[Usuario]\Documents\Capturer\Logs\
   Buscar: "SchedulerService" errors
```

#### Error: Emails Separados por Cuadrante No Funcionan
```  
🔍 Síntomas:
- Configuración: ☑ "Email separado por cuadrante"
- Resultado: Solo 1 email recibido en lugar de N emails

🛠️ Diagnóstico:
1. Verificar cuadrantes activos:
   [🔲 Cuadrantes] → Verificar ☑ "Activo" en cada cuadrante
   
2. Verificar selección en formulario email:
   Debe tener cuadrantes seleccionados en checklist
   
3. Verificar procesamiento:
   Debe estar marcado ☑ "Procesar cuadrantes antes del envío"
   
4. Verificar perfil:
   Perfil seleccionado debe existir y ser válido

🔧 Solución:
Ir a [🔲 Cuadrantes] → [✏️ Editar] cada cuadrante → Verificar:
- Región dentro de pantalla actual
- ☑ "Activo" marcado  
- Nombre único (sin duplicados)
```

### 🔲 **Problemas de Cuadrantes**

#### Error: Cuadrante Fuera de Pantalla
```
🔍 Síntomas:
❌ "Error cuadrante: Región fuera de pantalla actual"

🛠️ Causas comunes:
- Cambió resolución de pantalla
- Desconectó monitor secundario  
- Cambió configuración de escalado Windows

🔧 Solución automática:
1. [🔲 Cuadrantes] → [🔧 Auto-ajustar]
2. Sistema detecta resolución actual
3. Reescala cuadrantes proporcionalmente
4. Verifica que todas las regiones sean válidas

🔧 Solución manual:
1. [🔲 Cuadrantes] → [✏️ Editar] cuadrante problemático
2. Ajustar coordenadas:
   Resolución actual: 1920x1080
   Máximo X: 1920, Máximo Y: 1080
3. [💾 Guardar] → [🧪 Probar] para verificar
```

#### Error: Procesamiento de Cuadrantes Lento
```
🔍 Síntomas:  
- Envío de emails tarda >5 minutos
- CPU al 100% durante procesamiento
- Aplicación "no responde"

🛠️ Optimizaciones:
1. Reducir calidad procesamiento:
   Perfil → [Calidad: Alta] cambiar a [Media]
   
2. Procesar menos cuadrantes simultáneamente:
   Configuración → [Max cuadrantes en paralelo: 2]
   
3. Aumentar timeout procesamiento:
   Configuración avanzada → [Timeout: 300 segundos]
   
4. Verificar recursos disponibles:
   RAM libre: >2GB recomendado
   CPU libre: >30% recomendado
```

### 💾 **Problemas de Almacenamiento Avanzados**

#### Error: Limpieza Automática No Funciona
```
🔍 Síntomas:
- Archivos > 90 días permanecen
- Espacio usado sigue creciendo
- "Próxima limpieza" no se ejecuta

🛠️ Verificación paso a paso:
1. Configuración limpieza:
   [⚙ Configurar] → [💾 Almacenamiento]
   ☑ "Limpieza automática" debe estar marcado
   
2. Permisos de eliminación:
   Carpeta Screenshots → Click derecho → Propiedades → Seguridad
   Usuario actual debe tener "Eliminar" permissions
   
3. Verificar Task Scheduler:
   Windows → Task Scheduler → Capturer Cleanup Task
   Estado: "Ready" o "Running"
   Próxima ejecución: Debe tener fecha futura
   
4. Logs de limpieza:
   Logs\ → capturer-2024-MM.log
   Buscar: "CleanupService" entries
   Verificar errores específicos
```

#### Error: Cuadrantes Ocupan Demasiado Espacio  
```
🔍 Problema:
- Carpeta Quadrants\ usa >50% del espacio total
- Archivos procesados 3-5x más grandes que originales

🛠️ Optimización:
1. Configurar limpieza independiente:
   Cuadrantes: Retener solo 60 días (vs 90 screenshots)
   
2. Ajustar calidad procesamiento:
   [🔲 Cuadrantes] → [Perfil] → [Calidad: Media]
   Reduce tamaño ~40% sin pérdida significativa
   
3. Comprimir cuadrantes antiguos:
   [💾 Almacenamiento] → [Comprimir archivos > 30 días]
   ZIP automático reduce ~70% espacio
   
4. Configurar eliminación por uso:
   Solo mantener cuadrantes enviados por email
   Eliminar cuadrantes no utilizados en reportes
```

### 🔒 **Problemas de Seguridad y Configuración**

#### Error: Contraseña No Se Guarda
```
🔍 Síntomas:
- Configuración email: Contraseña se borra al reiniciar
- Error: "Password decryption failed"

🛠️ Diagnóstico DPAPI:
1. Verificar usuario Windows:
   Usuario actual = Usuario que configuró contraseña
   DPAPI no funciona entre usuarios diferentes
   
2. Verificar integridad sistema:
   cmd → sfc /scannow
   DPAPI puede fallar con corrupción sistema
   
3. Regenerar claves DPAPI:
   cmd → cipher /w:C:\
   Limpia claves corruptas, fuerza regeneración
   
4. Reconfigurar desde cero:
   [📧 Email] → [Limpiar configuración] → Reconfigurar
```

#### Error: "Access Denied" en Archivos de Configuración
```
🔍 Síntomas:  
- Error al guardar configuración
- Archivos en %APPDATA%\Capturer\ no se crean

🛠️ Solución permisos:
1. Ejecutar como Administrador temporalmente:
   Click derecho Capturer.exe → "Ejecutar como administrador"
   Configurar una vez → Cerrar
   
2. Verificar permisos %APPDATA%:
   %APPDATA%\Capturer\ → Propiedades → Seguridad
   Usuario actual: Control total ✓
   
3. Recrear carpeta configuración:
   Cerrar Capturer → Eliminar %APPDATA%\Capturer\
   Abrir Capturer → Recreará carpeta con permisos correctos
```

### 🌐 **Problemas de Red y Conectividad**

#### Error: Timeout SMTP Intermitente
```
🔍 Síntomas:
- Emails a veces se envían, a veces fallan
- Error: "Connection timeout after 30 seconds"

🛠️ Optimización conectividad:
1. Aumentar timeout SMTP:
   [📧 Email] → [Avanzado] → [Timeout: 60 segundos]
   
2. Configurar DNS alternativo:
   Cambiar DNS a 8.8.8.8 / 1.1.1.1
   Mejora resolución nombres SMTP servers
   
3. Verificar MTU network:
   cmd → ping smtp.gmail.com -f -l 1472
   Si falla, reducir MTU adaptador red
   
4. Configurar QoS para SMTP:
   Router → QoS → Priorizar puerto 587
   Asegura ancho banda para emails
```

---

## Preguntas Frecuentes v3.2.0

### 📧 **Sistema de Email Dual**

**P: ¿Cuál es la diferencia entre "Email Manual" y "Reportes"?**
R: 
- **Email Manual**: Envío inmediato bajo demanda con control total sobre período, destinatarios y formato
- **Reportes**: Sistema automático programado con configuración independiente para reportes rutinarios

**P: ¿Puedo tener destinatarios diferentes para email manual vs reportes automáticos?**
R: ¡Sí! Cada sistema tiene su propia lista de destinatarios:
- Email Manual: Selección por checklist para cada envío
- Reportes: Lista fija configurada una vez, envío automático

**P: ¿Los emails separados por cuadrante van a todos los destinatarios?**
R: Sí, cuando está marcado "Email separado por cuadrante", cada destinatario seleccionado recibe un email por cada cuadrante. Por ejemplo:
- 3 destinatarios + 2 cuadrantes = 6 emails totales

### 🔲 **Sistema de Cuadrantes**

**P: ¿Qué ocurre si cambio la resolución después de configurar cuadrantes?**
R: Capturer detecta automáticamente cambios de resolución y ofrece:
1. **Auto-ajuste proporcional**: Reescala cuadrantes manteniendo proporciones
2. **Reconfiguración asistida**: Guía para redefinir cuadrantes
3. **Desactivación temporal**: Hasta reconfigurar manualmente

**P: ¿Puedo usar cuadrantes solo para algunos emails y no para otros?**
R: ¡Por supuesto! Los cuadrantes son opcionales en cada envío:
- Email Manual: Checkbox "Procesar cuadrantes antes del envío"
- Reportes: Configuración independiente "Usar cuadrantes en reportes automáticos"

**P: ¿Los perfiles de procesamiento afectan el tamaño de los archivos?**
R: Sí, los perfiles controlan varios aspectos que afectan el tamaño:
- **Calidad de procesamiento**: Alta/Media/Baja
- **Nivel de compresión**: Sin comprimir/Compresión estándar/Máxima  
- **Filtros aplicados**: OCR, análisis automático, etc.

### ⚙️ **Configuración y Rendimiento**

**P: ¿Capturer v3.2.0 usa más recursos que v1.0?**
R: El uso base es similar, pero v3.2.0 ofrece más control:
- **Modo básico**: Similar a v1.0 (~50-80MB RAM)
- **Con cuadrantes activos**: +20-30MB por cuadrante procesado
- **Durante procesamiento**: Pico temporal según configuración de calidad

**P: ¿Puedo desactivar funcionalidades que no uso para ahorrar recursos?**
R: ¡Sí! Configuración modular:
```
[⚙ Configurar] → [Rendimiento] → Desactivar:
☐ Sistema de cuadrantes
☐ Procesamiento avanzado  
☐ Logs detallados
☐ Notificaciones sistema
```

**P: ¿El sistema de ayuda contextual siempre está activo?**
R: Los tooltips son ligeros y opcionales:
- **Tooltips**: Siempre disponibles, impacto mínimo
- **Botones ayuda "?"**: Solo cargan contenido cuando se presionan
- **Desactivar**: Configuración → Interfaz → ☐ "Mostrar ayuda contextual"

### 🔒 **Seguridad y Privacidad**

**P: ¿Los cuadrantes mejoran la privacidad?**
R: ¡Significativamente! Los cuadrantes permiten:
- **Exclusión automática**: Áreas privadas nunca capturadas
- **Reportes dirigidos**: Solo información relevante por destinatario
- **Control granular**: Diferentes niveles de acceso por región

**P: ¿Las contraseñas de email están más seguras en v3.2.0?**
R: Sí, v3.2.0 incluye mejoras de seguridad:
- **DPAPI reforzado**: Encriptación más robusta
- **Verificación integridad**: Detecta corrupción de claves
- **Regeneración automática**: Recovery automático de claves corruptas

### 🚀 **Funcionalidades Avanzadas**

**P: ¿Puedo automatizar reportes diferentes para diferentes días?**
R: No directamente, pero hay workarounds:
- **Múltiples configuraciones**: Exportar/importar configuraciones
- **Programación Windows**: Task Scheduler con diferentes configs
- **v2.1 (próximamente)**: Programación multi-horario nativa

**P: ¿Hay límite en el número de cuadrantes?**
R: Límites recomendados:
- **Técnico**: Máximo 36 cuadrantes (grilla 6x6)
- **Práctico**: 2-4 cuadrantes para rendimiento óptimo
- **Por email**: Sin límite, pero más cuadrantes = más emails separados

**P: ¿Capturer puede integrarse con otros sistemas?**
R: v3.2.0 incluye mejores opciones de integración:
- **APIs REST**: Endpoints básicos para estado y configuración (beta)
- **PowerShell module**: Scripts de automatización
- **Archivos estándar**: JSON configs fáciles de leer por otros sistemas
- **Webhooks**: Notificaciones HTTP a sistemas externos (próximamente)

### 📊 **Métricas y Análisis**

**P: ¿Capturer v3.2.0 incluye análisis de datos?**
R: Funcionalidades básicas incluidas:
- **Estadísticas automáticas**: Tendencias de captura, usage patterns
- **Métricas de email**: Tasas de entrega, tamaños de archivo
- **Análisis de cuadrantes**: Actividad por región, efficiency metrics
- **Reportes de rendimiento**: Recursos usados, optimización suggestions

**P: ¿Puedo exportar datos para análisis externo?**  
R: Múltiples formatos de exportación:
```
[📊 Estadísticas] → [📤 Exportar]:
- CSV: Datos tabulares para Excel/análisis
- JSON: Datos estructurados para APIs
- XML: Compatibilidad enterprise systems  
- PDF: Reportes ejecutivos con gráficos
```

---

## Documentación Técnica

### 🏗️ **Arquitectura v3.2.0 - Nuevos Componentes**

#### Servicios Principales Expandidos:
```
┌─────────────────────────────────────────────┐
│ Presentation Layer (Windows Forms v3.2.0)    │
│ ├─ MainForm (renovado con dual email)      │
│ ├─ EmailForm (manual, con cuadrantes)      │
│ ├─ RoutineEmailForm (nuevo, automático)    │
│ ├─ QuadrantEditorForm (nuevo)              │
│ └─ HelpSystem (tooltips contextual)        │
├─────────────────────────────────────────────┤
│ Business Logic Layer (expandido)           │
│ ├─ ScreenshotService                       │
│ ├─ EmailService (dual mode support)        │
│ ├─ SchedulerService (mejorado)             │
│ ├─ QuadrantService (nuevo)                 │
│ ├─ QuadrantSchedulerService (nuevo)        │
│ ├─ FileService                             │
│ └─ ConfigurationManager (extendido)        │
├─────────────────────────────────────────────┤
│ Data Layer (nuevos modelos)                │
│ ├─ QuadrantConfiguration (nuevo)           │
│ ├─ ProcessingTask (nuevo)                  │
│ ├─ RoutineEmailQuadrantSettings (nuevo)    │
│ └─ ScheduledProcessing (nuevo)             │
├─────────────────────────────────────────────┤
│ Infrastructure (optimizado)                │
│ ├─ Enhanced DPAPI encryption               │
│ ├─ Improved SMTP with retry logic          │
│ ├─ Advanced file system operations         │
│ └─ Windows API for quadrant processing     │
└─────────────────────────────────────────────┘
```

#### Nuevas Clases y Interfaces:

**IQuadrantService**:
```csharp
public interface IQuadrantService
{
    Task<List<QuadrantConfiguration>> GetAllConfigurationsAsync();
    Task<QuadrantConfiguration> GetActiveConfigurationAsync(string name);
    Task ProcessQuadrantsAsync(List<string> quadrantNames, string profileName);
    Task<List<string>> GetAvailableProfilesAsync();
    Task<bool> ValidateQuadrantRegionsAsync(List<QuadrantConfiguration> quadrants);
}
```

**QuadrantConfiguration**:
```csharp
public class QuadrantConfiguration
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public List<QuadrantRegion> Regions { get; set; } = new();
    public string ProcessingProfile { get; set; } = "Default";
    public DateTime LastModified { get; set; } = DateTime.Now;
    public Dictionary<string, object> Metadata { get; set; } = new();
}
```

**ProcessingTask**:  
```csharp
public class ProcessingTask
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = string.Empty; // "Email", "Quadrant", "Cleanup"
    public Dictionary<string, object> Parameters { get; set; } = new();
    public DateTime ScheduledTime { get; set; }
    public TaskStatus Status { get; set; } = TaskStatus.Pending;
    public string? ErrorMessage { get; set; }
}
```

### 📊 **Métricas de Rendimiento v3.2.0**

#### Recursos del Sistema Actualizados:
| Componente | Base v1.0 | v3.2.0 Sin Cuadrantes | v3.2.0 Con Cuadrantes |
|------------|-----------|-------------------|-------------------|
| **Memoria RAM** | 50-80 MB | 60-90 MB | 90-150 MB |
| **CPU (idle)** | <0.5% | <0.7% | <1.2% |
| **CPU (capture)** | 2-4% | 2-5% | 5-12% |
| **Disco I/O** | 1-3 MB | 1-4 MB | 3-8 MB |
| **Arranque** | 3-5 seg | 4-6 seg | 6-10 seg |

#### Tiempos de Operación Nuevos:
| Operación v3.2.0 | Tiempo Promedio | Notas |
|----------------|-----------------|-------|
| **Email Manual** | 10-45s | Dependiente de cuadrantes |
| **Procesamiento Cuadrante** | 2-8s | Por cuadrante, según perfil |
| **Auto-diagnóstico** | 5-15s | Verificación completa sistema |
| **Configuración cuadrantes** | <1s | UI responsiva |
| **Generación reporte rutinario** | 30-180s | Con/sin cuadrantes |

#### Escalabilidad por Cuadrantes:
| Cuadrantes Activos | Memoria Extra | CPU Extra | Tiempo Email |
|-------------------|---------------|-----------|--------------|
| **0 (desactivado)** | +0 MB | +0% | Tiempo base |
| **1-2** | +20-40 MB | +2-4% | +50% tiempo |
| **3-4** | +50-80 MB | +5-8% | +100% tiempo |
| **5+** | +100+ MB | +10%+ | +200%+ tiempo |

### 🔒 **Seguridad Mejorada v3.2.0**

#### Encriptación Multi-Capa:
```csharp
public class EnhancedSecurityManager
{
    // Nivel 1: DPAPI para contraseñas
    public string EncryptPassword(string password)
    {
        var entropy = GenerateEntropy();
        var data = Encoding.UTF8.GetBytes(password);
        var encrypted = ProtectedData.Protect(data, entropy, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encrypted);
    }
    
    // Nivel 2: Configuración sensible
    public void EncryptConfiguration(CapturerConfiguration config)
    {
        config.Email.Password = EncryptPassword(config.Email.Password);
        config.QuadrantSystem.Configurations
            .ForEach(q => q.Metadata = EncryptMetadata(q.Metadata));
    }
    
    // Nivel 3: Verificación integridad
    public bool VerifyConfigurationIntegrity(string configPath)
    {
        var hash = ComputeFileHash(configPath);
        var storedHash = GetStoredHash(configPath);
        return hash.SequenceEqual(storedHash);
    }
}
```

#### Auditoría y Compliance:
```csharp
public class ComplianceLogger
{
    public void LogSecurityEvent(SecurityEvent eventType, string details)
    {
        var logEntry = new
        {
            Timestamp = DateTime.UtcNow,
            EventType = eventType.ToString(),
            Details = details,
            UserContext = Environment.UserName,
            MachineContext = Environment.MachineName,
            ProcessId = Environment.ProcessId,
            Hash = ComputeEntryHash(details)
        };
        
        // Immutable log with digital signature
        WriteToSecureLog(logEntry);
    }
}
```

### 🌐 **API y Extensibilidad (Beta)**

#### REST API Endpoints:
```http
GET /api/status
Returns: System status, active captures, next scheduled reports

GET /api/screenshots?from=2024-08-01&to=2024-08-31
Returns: List of screenshots in date range

GET /api/quadrants
Returns: Active quadrant configurations

POST /api/email/manual
Body: { "recipients": [], "dateFrom": "", "dateTo": "", "useZip": true }
Returns: Email sending result

GET /api/diagnostics
Returns: System diagnostics and health check
```

#### PowerShell Module:
```powershell
# Instalación
Install-Module -Name CapturerPS

# Comandos disponibles
Get-CapturerStatus
Send-CapturerEmail -Recipients @("admin@corp.com") -DateFrom "2024-08-01"
Get-CapturerScreenshots -Path "C:\Screenshots" -Days 7
Set-CapturerConfiguration -IntervalMinutes 30 -AutoStart $true
```

#### Webhooks Configuration:
```json
{
  "webhooks": {
    "onScreenshot": "https://api.company.com/capturer/screenshot",
    "onEmailSent": "https://api.company.com/capturer/email",
    "onError": "https://api.company.com/capturer/error",
    "authentication": {
      "type": "bearer",
      "token": "[encrypted_token]"
    }
  }
}
```

---

**© 2025 Capturer v3.2.0 - Sistema Avanzado de Captura de Pantallas con Cuadrantes e Email Inteligente**

*Esta documentación completa cubre todas las funcionalidades nuevas y mejoradas de Capturer v3.2.0. El sistema evoluciona continuamente con nuevas características, mejoras de seguridad, y optimizaciones de rendimiento basadas en feedback de usuarios y requisitos empresariales.*

---

## 🚀 Roadmap v2.1+

### Próximas Características Planeadas:

**🎯 Q1 2025 - v2.1**:
- Programación multi-horario (diferentes horarios por día)
- Integración nativa con Outlook Calendar
- Machine Learning para detección automática de regiones importantes
- API REST completa con autenticación OAuth

**🎯 Q2 2025 - v2.2**:
- Soporte para formatos JPG y WebP
- Grabación de video de pantalla (timeline screenshots)
- Dashboard web para gestión remota
- Integración con Microsoft Teams y Slack

**🎯 Q3 2025 - v2.3**:
- OCR automático con búsqueda de texto en capturas
- Detección de anomalías con AI (cambios importantes)
- Modo cluster (coordinación entre múltiples PCs)
- Cumplimiento GDPR automático con anonimización

*Para sugerencias de funcionalidades o reporte de bugs, contactar al equipo de desarrollo.*