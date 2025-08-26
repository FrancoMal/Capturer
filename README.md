# 📸 Capturer - Guía de Uso Completa

**Versión**: 1.0  
**Fecha**: 25 de Agosto 2025  
**Autor**: Franco Malfetano

---

## 📋 Tabla de Contenidos

1. [¿Qué es Capturer?](#qué-es-capturer)
2. [Instalación y Primer Uso](#instalación-y-primer-uso)
3. [Interfaz Principal](#interfaz-principal)
4. [Configuración Avanzada](#configuración-avanzada)
5. [Captura de Screenshots](#captura-de-screenshots)
6. [Sistema de Email Mejorado](#sistema-de-email-mejorado)
7. [Gestión de Archivos](#gestión-de-archivos)
8. [System Tray y Atajos](#system-tray-y-atajos)
9. [Ejemplos Prácticos](#ejemplos-prácticos)
10. [Solución de Problemas](#solución-de-problemas)
11. [Preguntas Frecuentes](#preguntas-frecuentes)
12. [Documentación Técnica](#documentación-técnica)

---

## ¿Qué es Capturer?

Capturer es una aplicación de escritorio avanzada para Windows que **automatiza la captura de pantallas** y el **envío de reportes por email** con funcionalidades profesionales. Es perfecta para:

- 🏢 **Monitoreo de estaciones de trabajo**
- 📊 **Registro de actividad de pantalla**
- 📧 **Reportes automáticos personalizables**
- 🗄️ **Gestión organizada de capturas**
- ⏰ **Programación flexible de horarios**
- 🔐 **Seguridad y privacidad**

### Características Principales v2.0

| Característica | Descripción |
|----------------|-------------|
| ⏰ **Captura Automática** | Screenshots configurable (minutos, horas, días) |
| 📧 **Email Inteligente** | Reportes diarios, semanales, mensuales o personalizados |
| 🕐 **Horarios Fijos** | Configuración de hora específica para envíos |
| 📋 **Formatos Flexibles** | ZIP comprimido o imágenes individuales |
| 👁️ **Seguridad UI** | Toggle de contraseña con botón de ojo |
| 📊 **Progreso Visual** | Barra de progreso durante envío de emails |
| 📱 **System Tray** | Captura rápida desde área de notificación |
| 🧹 **Limpieza Automática** | Eliminación inteligente de archivos antiguos |
| 🔐 **Encriptación** | Contraseñas protegidas con DPAPI |

---

## Instalación y Primer Uso

### Requisitos del Sistema

- ✅ **Windows 7/10/11** (64-bit)
- ✅ **.NET 8 Runtime** (se instala automáticamente)
- ✅ **2 GB de RAM** mínimo (recomendado)
- ✅ **2 GB de espacio libre** (configurable)
- ✅ **Conexión a Internet** (para emails)

### Instalación

1. **Descargar la aplicación**
   ```
   Ubicación: C:\Users\Usuario\Desktop\Capturer\Capturer\bin\Debug\net8.0-windows\
   ```

2. **Ejecutar Capturer.exe**
   - Doble clic en `Capturer.exe`
   - La aplicación se iniciará automáticamente

3. **Primera ejecución**
   ```
   ✅ Se creará: C:\Users\[Usuario]\Documents\Capturer\Screenshots\
   ✅ Se generará: %APPDATA%\Capturer\capturer-settings.json
   ✅ Se iniciará el programador de tareas avanzado
   ```

### Verificación de Instalación

Al abrir la aplicación por primera vez, verás:

```
┌─────────────────────────────────────────────────────────────┐
│ Capturer - Gestor de Screenshots v2.0                      │
├─────────────────────────────────────────────────────────────┤
│ Panel de Estado:                                            │
│ ● Estado: Detenido          Próxima: --:--:--              │
│ ● Screenshots Total: 0      Almacenamiento: 0 MB           │
│ ● Último Email: Nunca       Estado Email: Sin configurar   │
├─────────────────────────────────────────────────────────────┤
│ [Iniciar] [Detener] [Configuración] [Enviar Email]         │
├─────────────────────────────────────────────────────────────┤
│ Screenshots Recientes:                                      │
│ (No hay capturas disponibles)                              │
├─────────────────────────────────────────────────────────────┤
│                                      [Minimizar] [Salir]   │
└─────────────────────────────────────────────────────────────┘
```

---

## Interfaz Principal

### Panel de Estado Actualizado

La interfaz principal muestra información en tiempo real mejorada:

#### Sección "Estado del Sistema"
```
● Estado: Ejecutándose/Detenido
● Próxima: 14:35:20 (próxima captura automática)
● Screenshots Total: 1,234 (contador total)
● Almacenamiento: 2.3 GB (espacio usado)
● Último Email: 2024-01-15 09:00 (último envío)
● Estado Email: Exitoso/Error/Enviando (estado actual)
```

#### Panel de Controles Principal
- 🟢 **[Iniciar]**: Comienza la captura automática
- 🔴 **[Detener]**: Detiene la captura automática  
- ⚙️ **[Configuración]**: Abre el formulario de configuración avanzada
- 📧 **[Enviar Email]**: Envío manual con opciones avanzadas

#### Lista de Capturas Recientes Mejorada
Muestra las últimas 10 capturas con columnas:
- **Archivo**: Nombre del screenshot (formato: YYYY-MM-DD_HH-mm-ss.png)
- **Fecha y Hora**: Timestamp completo de captura
- **Tamaño**: Tamaño del archivo en MB/KB
- **Acciones**: Botón "Ver" para abrir el archivo

#### Botones Inferiores Optimizados
- **[Minimizar]**: Envía la aplicación al system tray
- **[Salir]**: Cierre seguro con confirmación

### System Tray Mejorado
Al minimizar, el icono en system tray permite:
- **Mostrar**: Restaurar ventana principal
- **📸 Captura de Pantalla**: Tomar screenshot instantáneo
- **Salir**: Cerrar aplicación

---

## Configuración Avanzada

### 1. Abrir Configuración

Haz clic en **[Configuración]** para abrir el formulario con 3 pestañas mejoradas:

### 2. Pestaña "Screenshots" - Captura Automática

```
┌─────────────────────────────────────────┐
│ Screenshots - Configuración Avanzada   │
├─────────────────────────────────────────┤
│ Intervalo de captura (minutos): [30]    │
│ Carpeta de screenshots: [Examinar...]   │
│ C:\Users\Usuario\Documents\Capturer\... │
│ ☑ Iniciar captura automáticamente       │
│                                         │
│ Configuración de Calidad:               │
│ Formato: PNG (Alta calidad)            │
│ Compresión: 90%                        │
└─────────────────────────────────────────┘
```

**Configuraciones recomendadas:**
- **Intervalo**: 15-60 minutos según necesidades
- **Carpeta**: Mantener ubicación por defecto
- **Auto-inicio**: ☑ Habilitado para automatización total

### 3. Pestaña "Email" - Sistema Inteligente

```
┌─────────────────────────────────────────┐
│ Email - Configuración Completa         │
├─────────────────────────────────────────┤
│ Servidor SMTP: smtp.gmail.com           │
│ Puerto: 587                             │
│ Usuario: capturer@empresa.com           │
│ Contraseña: ************** [👁]         │
│ [Probar Email] ← Verificación Real      │
│                                         │
│ Destinatarios (separados por ;):       │
│ supervisor@empresa.com;                 │
│ admin@empresa.com;soporte@empresa.com   │
│                                         │
│ ☑ Habilitar reportes automáticos       │
│                                         │
│ Frecuencia: [Semanal ▼]                │
│ Hora de envío: [09:00 ▼]               │
└─────────────────────────────────────────┘
```

#### 🔐 Nuevas Funcionalidades de Seguridad:
- **👁️ Toggle de Contraseña**: Botón del ojo para mostrar/ocultar contraseña
- **✅ Prueba Real**: El botón "Probar Email" realiza conexión SMTP real
- **🔒 Encriptación**: Contraseñas protegidas con DPAPI de Windows

#### 📅 Programación Flexible:
- **Diario**: Reportes cada 24 horas
- **Semanal**: Reportes cada 7 días (lunes por defecto)
- **Mensual**: Reportes cada 30 días
- **Personalizado**: Especificar días exactos (1-365)

#### ⏰ Horarios Fijos:
- **Selección de hora**: ComboBox con horas de 00:00 a 23:00
- **Formato 24 horas**: Claridad total en programación
- **Hora por defecto**: 09:00 AM

**Ejemplo de configuración Gmail:**
```
Servidor SMTP: smtp.gmail.com
Puerto: 587
Usuario: capturer@miempresa.com
Contraseña: [contraseña de aplicación de 16 dígitos]
```

**⚠️ Importante**: Para Gmail/Outlook corporativo:
1. Habilitar verificación en 2 pasos
2. Generar "contraseña de aplicación"
3. Usar esa contraseña específica (no la normal)

### 4. Pestaña "Almacenamiento" - Gestión Inteligente

```
┌─────────────────────────────────────────┐
│ Almacenamiento - Gestión Automática    │
├─────────────────────────────────────────┤
│ Retener archivos por (días): [90]       │
│ Tamaño máximo carpeta (GB): [10]        │
│ ☑ Limpieza automática habilitada       │
│                                         │
│ Estado actual:                          │
│ Archivos: 1,234 screenshots            │
│ Espacio usado: 3.2 GB de 10 GB         │
│ Archivo más antiguo: 2024-06-15        │
└─────────────────────────────────────────┘
```

**Estrategias de limpieza:**
- **Por antigüedad**: Elimina archivos > días configurados
- **Por tamaño**: Elimina más antiguos si se excede límite
- **Automática**: Ejecución diaria en segundo plano

### 5. Guardar y Verificar

1. **Prueba de email**: Haz clic en **[Probar Email]** - verás resultado específico:
   - ✅ "Conexión SMTP exitosa!"
   - ❌ "Error de autenticación. Verifique usuario y contraseña"
   - ❌ "No se pudo conectar al servidor SMTP"

2. **Guardar**: Si la prueba es exitosa, haz clic en **[Guardar]**

3. **Confirmación**: "Configuración actualizada exitosamente" + aplicación inmediata

---

## Captura de Screenshots

### Métodos de Captura

#### 1. Captura Automática (Recomendada)

```
Paso 1: Configurar intervalo
  Configuración → Screenshots → Intervalo: 30 minutos

Paso 2: Iniciar servicio
  [Iniciar] → Estado: "Ejecutándose"

Resultado:
  ● Próxima: 14:35:20
  ● Captura cada 30 minutos automáticamente
  ● Archivos guardados con timestamp
```

#### 2. Captura Manual Instantánea

```
Método A: Desde interfaz principal
  Aplicación abierta → [Capturar Ahora]

Método B: Desde system tray (NUEVO)
  Click derecho en icono → "📸 Captura de Pantalla"
```

#### 3. Captura Programada

```
Configuración avanzada:
  - Horarios específicos
  - Días de la semana
  - Rangos de horas laborales
```

### Proceso de Captura Detallado

Una vez iniciado, Capturer ejecuta:

1. **Captura**: Pantalla completa (todos los monitores)
2. **Nomenclatura**: `2024-08-26_14-30-15.png`
3. **Almacenamiento**: Carpeta configurada
4. **Logging**: Registro de éxito/error
5. **Actualización UI**: Lista de recientes + contadores

### Ejemplo de Captura Exitosa
```
Archivo: 2024-08-26_14-30-15.png
Ubicación: C:\Users\Usuario\Documents\Capturer\Screenshots\
Tamaño: 1.4 MB (1920x1080, PNG)
Estado: ✅ Captura exitosa
Próxima: 2024-08-26_15-00-15
```

---

## Sistema de Email Mejorado

### Reportes Automáticos Inteligentes

#### Configuración Flexible de Frecuencia

```
┌─────────────────────────────────────────┐
│ Opciones de Programación:               │
├─────────────────────────────────────────┤
│ ● Diario (24 horas)                    │
│ ● Semanal (7 días) - Lunes 09:00       │
│ ● Mensual (30 días) - Día 1, 09:00     │
│ ● Personalizado (1-365 días)           │
└─────────────────────────────────────────┘
```

#### Contenido del Email Automático v2.0

```
Asunto: Reporte [Frecuencia] Capturer - [Período]

Contenido mejorado:
┌─────────────────────────────────────────┐
│ 📸 REPORTE AUTOMÁTICO - CAPTURER v2.0   │
├─────────────────────────────────────────┤
│ Tipo: Reporte Semanal                  │
│ Período: 2024-08-19 hasta 2024-08-26   │
│ Total capturas: 336 screenshots        │
│ Computadora: OFICINA-PC-01             │
│ Usuario: [Usuario del sistema]          │
│ Generado: 2024-08-26 09:00:15          │
│                                        │
│ 📊 Estadísticas:                       │
│ - Promedio/día: 48 capturas           │
│ - Tamaño total: 1.2 GB                │
│ - Formato: ZIP comprimido              │
│                                        │
│ 📎 Adjunto: capturas_20240819-20240826.zip │
└─────────────────────────────────────────┘
```

### Envío Manual Avanzado

#### 1. Abrir Formulario de Email Mejorado

```
[Enviar Email] → Formulario avanzado con nuevas opciones
```

#### 2. Selección de Período Extendida

```
┌─────────────────────────────────────────┐
│ Rango de Fechas - Opciones Avanzadas   │
├─────────────────────────────────────────┤
│ Desde: [26/08/2024] Hasta: [26/08/2024] │
│ [Hoy] [Últimos 7] [Últimos 30] [Este mes] │
│                                         │
│ Vista previa: 12 screenshots           │
│ Tamaño estimado: 8.4 MB                │
└─────────────────────────────────────────┘
```

**Nuevos atajos:**
- **[Hoy]**: Solo screenshots del día actual
- **[Últimos 7 días]**: Última semana completa
- **[Últimos 30 días]**: Último mes completo
- **[Este mes]**: Desde el 1ro del mes actual

#### 3. Formato de Adjuntos (NUEVO)

```
┌─────────────────────────────────────────┐
│ Formato de Adjuntos                     │
├─────────────────────────────────────────┤
│ ● Archivo ZIP (Recomendado) ✓           │
│ ○ Imágenes individuales                 │
│                                         │
│ Nota: Imágenes individuales pueden      │
│ tener limitación de tamaño por email    │
└─────────────────────────────────────────┘
```

**Opciones de formato:**
- **ZIP**: Compresión eficiente, un solo archivo
- **Individual**: Archivos PNG separados (límite 25MB total)

#### 4. Progreso Visual (NUEVO)

Durante el envío, se muestra:

```
┌─────────────────────────────────────────┐
│ Enviando Email...                       │
├─────────────────────────────────────────┤
│ ████████████████████░░░░ 80%            │
│ Enviando email...                       │
└─────────────────────────────────────────┘
```

**Etapas del progreso:**
1. **20%**: "Preparando envío..."
2. **40%**: "Obteniendo screenshots..."
3. **60%**: "Procesando archivos..."
4. **80%**: "Enviando email..."
5. **100%**: "Email enviado exitosamente!"

#### 5. Confirmación Mejorada

```
Mensaje antes del envío:
"¿Enviar email con screenshots desde 2024-08-26 hasta 2024-08-26 
en formato ZIP a 2 destinatarios?

Formato: Archivo ZIP comprimido"

[Sí] [No]
```

### Ejemplo Completo v2.0

```
📧 EJEMPLO: Reporte de actividad semanal

Configuración:
  Frecuencia: Semanal
  Día: Lunes
  Hora: 09:00
  Formato: ZIP
  Destinatarios: 3

Resultado automático:
  ✅ 336 screenshots de la semana
  ✅ 1 archivo ZIP de 1.2 GB
  ✅ Email enviado el lunes 09:00
  ✅ Confirmación en interfaz
```

---

## Gestión de Archivos

### Estructura de Carpetas v2.0

```
C:\Users\[Usuario]\Documents\Capturer\
├── Screenshots\                    ← Capturas organizadas
│   ├── 2024-08-26_09-00-15.png    ← Formato estándar
│   ├── 2024-08-26_09-30-15.png
│   └── ... (archivos ordenados por fecha)
├── Temp\                          ← Archivos ZIP temporales
│   └── capturas_temp.zip
└── Logs\                          ← Logs detallados
    ├── capturer-2024-08.log
    └── email-2024-08.log

%APPDATA%\Capturer\
├── capturer-settings.json         ← Configuración encriptada
└── schedules.json                 ← Programaciones activas
```

### Nomenclatura Inteligente

Todos los screenshots siguen el formato ISO:
```
YYYY-MM-DD_HH-mm-ss.png

Ejemplos reales:
2024-08-26_14-30-15.png = 26 agosto 2024, 2:30:15 PM
2024-12-31_23-59-59.png = 31 diciembre 2024, 11:59:59 PM
```

### Sistema de Limpieza Inteligente

#### Limpieza por Antigüedad
```
Configuración: Retener 90 días
Proceso:
  1. Escaneo diario automático
  2. Identifica archivos > 90 días
  3. Elimina automáticamente
  4. Log de archivos eliminados
```

#### Limpieza por Tamaño
```
Configuración: Máximo 10 GB
Proceso:
  1. Monitoreo continuo de tamaño
  2. Si > 10 GB → elimina más antiguos
  3. Reduce hasta 80% del límite (8 GB)
  4. Prioriza archivos más antiguos
```

#### Monitoreo en Tiempo Real

La interfaz muestra:
```
Panel de estado:
● Screenshots Total: 1,234
● Almacenamiento: 3.2 GB de 10 GB (32%)
● Espacio disponible: 6.8 GB
● Próxima limpieza: Automática (noche)
```

---

## System Tray y Atajos

### Funcionalidades del System Tray v2.0

Al minimizar la aplicación, el icono en la bandeja del sistema permite:

#### Menú Contextual Completo
```
┌────────────────────────────────┐
│ Mostrar                        │
│ 📸 Captura de Pantalla         │ ← NUEVO
│ ────────────────────────────── │
│ Salir                          │
└────────────────────────────────┘
```

#### Funcionalidades:
- **Mostrar**: Restaura la ventana principal
- **📸 Captura de Pantalla**: Toma screenshot instantáneo
  - Sin abrir aplicación
  - Notificación de confirmación
  - Archivo guardado automáticamente
- **Salir**: Cierre completo de la aplicación

#### Notificaciones Inteligentes

```
Tipos de notificaciones:
✅ "Screenshot capturado desde la bandeja del sistema"
📧 "Email enviado exitosamente a 3 destinatarios"
⚠️ "Error al capturar: [descripción del error]"
🧹 "Limpieza automática: 15 archivos eliminados"
```

### Atajos de Teclado

| Acción | Atajo | Descripción |
|--------|-------|-------------|
| Iniciar/Detener | F1 | Toggle captura automática |
| Screenshot manual | F2 | Captura inmediata |
| Abrir configuración | F3 | Formulario de configuración |
| Enviar email | F4 | Formulario de envío |
| Minimizar | Esc | Envía a system tray |

---

## Ejemplos Prácticos

### Ejemplo 1: Monitoreo Corporativo

**Escenario**: Oficina con 50 estaciones de trabajo

```yaml
Configuración por PC:
  Intervalo: 30 minutos
  Horario: 8:00 AM - 6:00 PM (días laborales)
  Reportes: Semanales, lunes 9:00 AM
  Destinatarios:
    - supervisor@empresa.com
    - rrhh@empresa.com
    - seguridad@empresa.com
  Almacenamiento:
    Retención: 60 días
    Límite: 5 GB por PC

Resultado esperado:
  - 20 capturas por día por PC (10h / 30min)
  - 100 capturas por semana por PC
  - 5,000 capturas totales por semana
  - ~10 GB de almacenamiento por PC por mes
```

### Ejemplo 2: Servidor Crítico 24/7

**Escenario**: Monitoreo de servidor de producción

```yaml
Configuración:
  Intervalo: 10 minutos
  Horario: 24/7 continuo
  Reportes: Diarios, 6:00 AM
  Destinatarios:
    - admin@empresa.com
    - soporte@empresa.com
    - oncall@empresa.com
  Formato: ZIP
  Almacenamiento:
    Retención: 30 días
    Límite: 15 GB

Resultado esperado:
  - 144 capturas por día (24h / 10min)
  - 4,320 capturas por mes
  - ~5-7 GB por mes
  - Email diario con 144 screenshots comprimidos
```

### Ejemplo 3: Trabajo Remoto Individual

**Escenario**: Empleado trabajando desde casa

```yaml
Configuración:
  Intervalo: 15 minutos
  Horario: 9:00 AM - 5:00 PM
  Reportes: Semanales, viernes 5:00 PM
  Destinatarios:
    - jefe@empresa.com
    - empleado@empresa.com (copia)
  Formato: Imágenes individuales
  Almacenamiento:
    Retención: 30 días
    Límite: 2 GB

Proceso semanal:
  1. Lunes: Iniciar captura automática
  2. Martes-Jueves: Captura continua
  3. Viernes 5:00 PM: Email automático
  4. Fin de semana: Sin capturas
```

### Ejemplo 4: Proyecto Temporal

**Escenario**: Monitoreo de proyecto específico por 2 semanas

```yaml
Configuración inicial:
  Fecha inicio: 2024-08-26
  Fecha fin: 2024-09-09
  Intervalo: 20 minutos
  Reportes: Al finalizar (manual)

Proceso:
  Semana 1:
    - Iniciar captura el lunes
    - Monitoreo continuo
    - Sin reportes automáticos
    
  Semana 2:
    - Continuar captura
    - Viernes: Envío manual de todo el período
    - Detener captura
    - Archivo final del proyecto

Email final:
  - Período: 2024-08-26 a 2024-09-09
  - Screenshots: ~400 archivos
  - Formato: ZIP de proyecto completo
  - Destinatarios: Equipo del proyecto
```

---

## Solución de Problemas

### Problemas de Captura

#### 1. No Se Capturan Screenshots

**Síntomas:**
```
● Estado: Ejecutándose
● Screenshots Total: 0 (no aumenta después de tiempo configurado)
● Próxima: Se actualiza pero no hay archivos nuevos
```

**Diagnóstico paso a paso:**

1. **Verificar permisos de carpeta**
   ```
   Acción: Click derecho en carpeta Screenshots → Propiedades → Seguridad
   Verificar: Usuario actual tiene "Control total"
   Solución: Agregar permisos o ejecutar como Administrador
   ```

2. **Verificar configuración**
   ```
   Abrir: [Configuración] → Screenshots
   Verificar:
     - Intervalo > 0 minutos ✓
     - Carpeta existe y es accesible ✓
     - Auto-inicio habilitado ✓
   ```

3. **Reiniciar servicio completo**
   ```
   Paso 1: [Detener] → Esperar 10 segundos
   Paso 2: [Iniciar] → Verificar cambio de estado
   Paso 3: Esperar un ciclo completo (ej: 30 min)
   ```

4. **Verificar logs**
   ```
   Ubicación: C:\Users\[Usuario]\Documents\Capturer\Logs\
   Archivo: capturer-2024-08.log
   Buscar: Errores o excepciones recientes
   ```

#### 2. Capturas Corruptas o Vacías

**Síntomas:**
```
Archivos PNG de 0 KB o no se pueden abrir
```

**Soluciones:**
1. **Verificar múltiples monitores**
   - Desconectar monitores adicionales temporalmente
   - Probar captura con un solo monitor
   
2. **Verificar resolución**
   - Reducir resolución temporalmente
   - Probar con configuración de pantalla estándar

### Problemas de Email

#### 1. Emails No Se Envían - Error de Autenticación

**Síntomas:**
```
Estado Email: Error de autenticación
Botón "Probar Email": ❌ Error de autenticación
```

**Soluciones por proveedor:**

**Gmail/Google Workspace:**
```
Paso 1: Verificar 2FA habilitada
  Google Account → Seguridad → Verificación en 2 pasos

Paso 2: Generar contraseña de aplicación
  Seguridad → Contraseñas de aplicaciones
  Seleccionar: Correo → Windows Computer → Generar

Paso 3: Usar contraseña de 16 dígitos (NO la contraseña normal)
  Ejemplo: "abcd efgh ijkl mnop"

Configuración correcta:
  Servidor: smtp.gmail.com
  Puerto: 587
  Usuario: tu-email@gmail.com
  Contraseña: [contraseña de aplicación de 16 dígitos]
```

**Outlook/Office 365:**
```
Configuración:
  Servidor: smtp-mail.outlook.com
  Puerto: 587
  Usuario: tu-email@outlook.com o @empresa.com
  Contraseña: [contraseña normal o de aplicación]

Para cuentas corporativas:
  Consultar con IT sobre autenticación moderna
```

#### 2. Emails Se Envían Pero No Llegan

**Síntomas:**
```
Estado Email: Exitoso
Pero destinatarios no reciben emails
```

**Verificaciones:**

1. **Spam/Junk folders**
   ```
   Verificar en destinatarios:
   - Carpeta Spam/Correo no deseado
   - Carpeta Promociones (Gmail)
   - Filtros automáticos del servidor
   ```

2. **Tamaño de archivos**
   ```
   Límite típico: 25 MB por email
   Solución: 
   - Usar formato ZIP (más pequeño)
   - Reducir rango de fechas
   - Verificar configuración del servidor de email corporativo
   ```

#### 3. Error de Conexión SMTP

**Síntomas:**
```
❌ "No se pudo conectar al servidor SMTP"
```

**Soluciones:**

1. **Verificar firewall/antivirus**
   ```
   Acción: Agregar Capturer.exe a excepciones
   Puertos: Permitir salida por puerto 587 (SMTP)
   ```

2. **Verificar conexión de red**
   ```
   Cmd: telnet smtp.gmail.com 587
   Resultado esperado: Conexión exitosa
   ```

3. **Probar servidor alternativo**
   ```
   Gmail: smtp.gmail.com:587
   Outlook: smtp-mail.outlook.com:587
   Yahoo: smtp.mail.yahoo.com:587
   ```

### Problemas de Almacenamiento

#### 1. Disco Lleno

**Síntomas:**
```
Error: "No hay espacio suficiente en disco"
Almacenamiento: 9.8 GB de 10 GB
```

**Soluciones inmediatas:**

1. **Limpieza manual urgente**
   ```
   [Ver Todo] → Seleccionar archivos más antiguos
   Eliminar: Screenshots > 30 días
   Liberar: ~50% del espacio
   ```

2. **Ajustar configuración**
   ```
   [Configuración] → Almacenamiento
   Reducir retención: 90 días → 30 días
   O aumentar límite: 10 GB → 20 GB
   ```

3. **Cambiar ubicación**
   ```
   [Configuración] → Screenshots → [Examinar]
   Seleccionar: Disco con más espacio (ej: D:\Capturer\)
   ```

#### 2. Archivos No Se Eliminan Automáticamente

**Síntomas:**
```
Archivos > 90 días siguen presentes
Limpieza automática no funciona
```

**Verificaciones:**

1. **Configuración de limpieza**
   ```
   [Configuración] → Almacenamiento
   Verificar: ☑ Limpieza automática habilitada
   ```

2. **Permisos de eliminación**
   ```
   Verificar permisos de escritura en carpeta Screenshots
   Ejecutar como Administrador si es necesario
   ```

### Problemas de Interfaz

#### 1. Botones No Responden

**Síntomas:**
```
Clicks en [Iniciar], [Configuración] no hacen nada
Aplicación parece "congelada"
```

**Soluciones:**
1. **Verificar proceso activo**
   ```
   Ctrl+Shift+Esc → Procesos
   Buscar: Capturer.exe
   Si no responde: Finalizar proceso y reiniciar
   ```

2. **Verificar .NET Runtime**
   ```
   Descargar: .NET 8 Runtime desde Microsoft
   Instalar versión más reciente
   Reiniciar aplicación
   ```

#### 2. System Tray No Funciona

**Síntomas:**
```
[Minimizar] no envía al system tray
Icono no aparece en área de notificación
```

**Soluciones:**
1. **Habilitar notificaciones**
   ```
   Windows Settings → Sistema → Notificaciones
   Permitir notificaciones para Capturer
   ```

2. **Mostrar iconos ocultos**
   ```
   Click en "^" en system tray
   Arrastrar icono Capturer al área visible
   ```

---

## Preguntas Frecuentes

### Funcionalidades Nuevas v2.0

**P: ¿Qué significa el botón del ojo en la contraseña?**
R: Es un toggle de visibilidad. Click para mostrar/ocultar la contraseña mientras escribes. Mejora la seguridad y facilita la configuración.

**P: ¿Cómo funciona la programación personalizada?**
R: Puedes configurar reportes cada X días (1-365). Por ejemplo: cada 3 días, cada 15 días, etc. Además de la hora exacta de envío.

**P: ¿Qué diferencia hay entre ZIP e imágenes individuales?**
R: 
- **ZIP**: Un solo archivo comprimido, más eficiente, recomendado
- **Individual**: Cada imagen como archivo separado, útil para revisión rápida, limitado a 25MB total

**P: ¿La barra de progreso funciona en todos los envíos?**
R: Sí, tanto en envíos manuales como automáticos. Muestra 5 etapas: preparación, obtención, procesamiento, envío y confirmación.

### Seguridad y Privacidad

**P: ¿Mis contraseñas están seguras?**
R: Sí. Usamos DPAPI (Data Protection API) de Windows, que encripta las contraseñas con la clave del usuario y máquina. Solo tú puedes desencriptarlas.

**P: ¿Capturer registra mis teclas o clicks?**
R: No. Solo captura imágenes de pantalla. No hay keylogger, no registra URLs visitadas, ni actividad del mouse/teclado.

**P: ¿Puedo usar Capturer en red corporativa?**
R: Sí, pero consulta con IT sobre:
- Políticas de monitoreo
- Configuración de firewall para SMTP
- Aprobación de software de captura

**P: ¿Los screenshots incluyen información sensible?**
R: Capturer captura todo lo visible en pantalla. Es responsabilidad del usuario configurar adecuadamente y cumplir políticas de privacidad.

### Configuración Avanzada

**P: ¿Puedo tener diferentes horarios entre semana y fines de semana?**
R: Actualmente no directamente. Puedes:
- Configurar para días laborales y pausar manualmente los fines de semana
- Usar la programación personalizada para alternar períodos

**P: ¿Capturer funciona con múltiples monitores?**
R: Sí, captura todos los monitores configurados en Windows como una sola imagen extendida.

**P: ¿Puedo cambiar el formato de imagen?**
R: Actualmente solo PNG (alta calidad, sin pérdidas). Formatos adicionales (JPG, WebP) están planeados para futuras versiones.

**P: ¿Hay límite en destinatarios de email?**
R: No hay límite técnico, pero recomendamos máximo 10 destinatarios para mejor rendimiento y evitar problemas con servidores SMTP.

### Rendimiento y Recursos

**P: ¿Capturer afecta el rendimiento del sistema?**
R: Impacto mínimo:
- RAM: ~50-100 MB
- CPU: <1% durante captura
- Disco: Solo durante guardado de archivos
- Red: Solo durante envío de emails

**P: ¿Puedo usar Capturer en múltiples computadoras?**
R: Sí, cada PC necesita:
- Instalación independiente
- Configuración propia
- Carpeta de screenshots local

**P: ¿Capturer funciona sin Internet?**
R: Las capturas sí. Los emails requieren conexión. Si no hay Internet:
- Capturas continúan normalmente
- Emails se fallan (reintento automático cuando se restaure conexión)

### Integración y Compatibilidad

**P: ¿Capturer se inicia automáticamente con Windows?**
R: No por defecto. Para habilitarlo:
1. Crear acceso directo en Inicio → Programas → Inicio
2. O usar Task Scheduler de Windows
3. Futura versión incluirá esta opción

**P: ¿Puedo integrar Capturer con otros sistemas?**
R: Los archivos están en formato estándar (PNG) con nombres predictibles. Fácil integración con:
- Scripts de PowerShell
- Sistemas de backup
- Herramientas de análisis de imágenes

**P: ¿Hay API o línea de comandos?**
R: Actualmente no. Planeado para versiones futuras:
- Parámetros de línea de comandos
- API REST básica
- PowerShell module

### Casos de Uso Específicos

**P: ¿Capturer es adecuado para cumplimiento legal?**
R: Depende de regulaciones específicas. Capturer provee:
- Timestamps precisos
- Integridad de archivos
- Logs detallados
- Configuración auditables

Consulta con asesoría legal para requisitos específicos.

**P: ¿Puedo usar Capturer para monitorear empleados remotos?**
R: Sí, pero considera:
- Leyes locales de privacidad laboral
- Consentimiento explícito del empleado
- Políticas claras de uso
- Configuración transparente

**P: ¿Capturer funciona con aplicaciones en pantalla completa?**
R: Sí, captura todo incluyendo:
- Juegos en pantalla completa
- Videos
- Presentaciones
- Aplicaciones de diseño

---

## Documentación Técnica

### Stack Tecnológico

#### Framework y Runtime
- **.NET 8**: Framework principal
- **Windows Forms**: Interfaz de usuario nativa
- **C# 12**: Lenguaje de programación
- **Windows Desktop Runtime**: Requerimiento de ejecución

#### Librerías y Dependencias

| Librería | Versión | Propósito |
|----------|---------|-----------|
| **MailKit** | 4.8.0 | Cliente SMTP robusto |
| **MimeKit** | 4.8.0 | Composición de mensajes MIME |
| **Newtonsoft.Json** | 13.0.3 | Serialización de configuración |
| **NLog** | 5.2.7 | Sistema de logging avanzado |
| **System.Drawing.Common** | 8.0.0 | Captura y manipulación de imágenes |
| **Microsoft.Extensions.DI** | 8.0.0 | Inyección de dependencias |

#### APIs del Sistema Windows

| API | Librería | Función |
|-----|----------|---------|
| **GetDC** | user32.dll | Obtener contexto de dispositivo |
| **BitBlt** | gdi32.dll | Copia de bits de pantalla |
| **DPAPI** | crypt32.dll | Encriptación de datos sensibles |
| **Task Scheduler** | taskschd.dll | Programación de tareas |

### Arquitectura del Sistema

#### Patrón de Diseño
- **MVVM Light**: Separación de lógica y UI
- **Dependency Injection**: Inversión de control
- **Observer Pattern**: Eventos y notificaciones
- **Repository Pattern**: Acceso a datos

#### Estructura de Servicios

```
┌─────────────────────────────────────────┐
│ Presentation Layer (Windows Forms)     │
├─────────────────────────────────────────┤
│ Business Logic Layer                   │
│ ├─ ScreenshotService                   │
│ ├─ EmailService                        │
│ ├─ SchedulerService                    │
│ ├─ FileService                         │
│ └─ ConfigurationManager                │
├─────────────────────────────────────────┤
│ Data Access Layer                      │
│ ├─ JSON Configuration                  │
│ ├─ File System                         │
│ └─ Windows Registry                    │
├─────────────────────────────────────────┤
│ Infrastructure Layer                   │
│ ├─ Windows APIs                        │
│ ├─ SMTP Protocols                      │
│ └─ File System Watchers                │
└─────────────────────────────────────────┘
```

### Detalles de Implementación

#### Sistema de Captura

**Tecnología**: GDI32 API
```csharp
// Pseudocódigo simplificado
public class ScreenCaptureEngine 
{
    // Usar Windows GDI para captura nativa
    IntPtr screenDC = GetDC(IntPtr.Zero);
    
    // Crear bitmap compatible
    using (Bitmap bitmap = new Bitmap(width, height))
    {
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            // Copia bits de pantalla a bitmap
            IntPtr hdc = graphics.GetHdc();
            BitBlt(hdc, 0, 0, width, height, screenDC, 0, 0, SRCCOPY);
            graphics.ReleaseHdc(hdc);
        }
        
        // Guardar como PNG con compresión optimizada
        bitmap.Save(path, ImageFormat.Png);
    }
    
    ReleaseDC(IntPtr.Zero, screenDC);
}
```

**Características técnicas:**
- **Multi-monitor**: Soporte automático para configuraciones extendidas
- **DPI Awareness**: Adaptación automática a diferentes resoluciones
- **Performance**: ~100-200ms por captura en hardware estándar
- **Memoria**: Uso eficiente con liberación inmediata

#### Sistema de Email

**Protocolo**: SMTP con TLS/StartTLS
```csharp
// Pseudocódigo del flujo de email
public class EmailEngine
{
    async Task<bool> SendEmailAsync()
    {
        using SmtpClient client = new SmtpClient();
        
        // Conexión segura
        await client.ConnectAsync(server, port, SecureSocketOptions.StartTlsWhenAvailable);
        
        // Autenticación
        await client.AuthenticateAsync(username, password);
        
        // Crear mensaje MIME
        MimeMessage message = CreateMimeMessage();
        
        // Adjuntos eficientes (ZIP o individuales)
        if (useZip) 
        {
            AddZipAttachment(message, files);
        } 
        else 
        {
            AddIndividualAttachments(message, files);
        }
        
        // Envío con retry logic
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
```

**Optimizaciones:**
- **Memory Streams**: Evita bloqueos de archivos
- **Compression**: ZIP con nivel óptimo (balance tamaño/velocidad)
- **Retry Logic**: Reintento automático con backoff exponencial
- **Connection Pooling**: Reutilización de conexiones SMTP

#### Sistema de Almacenamiento

**Configuración**: JSON con encriptación selectiva
```json
{
  "screenshot": {
    "interval": "00:30:00",
    "folder": "C:\\Users\\...\\Screenshots",
    "autoStart": true,
    "quality": 90
  },
  "email": {
    "smtpServer": "smtp.gmail.com",
    "smtpPort": 587,
    "username": "user@example.com",
    "passwordEncrypted": "[DPAPI_ENCRYPTED_BLOB]",
    "recipients": ["admin@example.com"]
  },
  "schedule": {
    "frequency": "Weekly",
    "customDays": 7,
    "reportTime": "09:00:00",
    "enableAutomaticReports": true
  }
}
```

**Seguridad DPAPI:**
```csharp
// Encriptación de contraseñas
public static class SecureStorage
{
    public static string EncryptPassword(string password)
    {
        byte[] data = Encoding.UTF8.GetBytes(password);
        byte[] encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encrypted);
    }
    
    public static string DecryptPassword(string encryptedPassword)
    {
        byte[] encrypted = Convert.FromBase64String(encryptedPassword);
        byte[] data = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(data);
    }
}
```

### Métricas de Rendimiento

#### Recursos del Sistema

| Métrica | Valor Típico | Valor Máximo |
|---------|--------------|--------------|
| **Memoria RAM** | 50-80 MB | 150 MB |
| **CPU Usage** | <0.5% idle | 3-5% during capture |
| **Disco I/O** | 1-3 MB/capture | 5 MB/capture (4K) |
| **Red** | 0 KB idle | Variable (email size) |

#### Tiempos de Operación

| Operación | Tiempo Promedio | Notas |
|-----------|-----------------|-------|
| **Screenshot** | 150-300ms | Dependiente de resolución |
| **Guardado PNG** | 50-150ms | SSD vs HDD |
| **Creación ZIP** | 2-10s | Por 100-500 archivos |
| **Envío Email** | 5-30s | Dependiente de conexión |
| **Limpieza Automática** | 1-5s | Por 1000 archivos |

#### Escalabilidad

| Escenario | Capturas/día | Almacenamiento/mes | Rendimiento |
|-----------|--------------|-------------------|-------------|
| **Ligero** | 48 (30min) | 500 MB | Excelente |
| **Normal** | 96 (15min) | 1.2 GB | Muy bueno |
| **Intensivo** | 288 (5min) | 3.5 GB | Bueno |
| **Extremo** | 1440 (1min) | 18 GB | Aceptable |

### Consideraciones de Seguridad

#### Protección de Datos
- **Contraseñas**: Encriptación DPAPI (nivel usuario + máquina)
- **Configuración**: Archivos protegidos en %APPDATA%
- **Screenshots**: Solo acceso del usuario propietario
- **Logs**: Información no sensible únicamente

#### Privacidad
- **No keylogging**: Solo capturas de pantalla
- **No network snooping**: Sin análisis de tráfico red
- **Local storage**: Datos permanecen en PC del usuario
- **Opt-in email**: Usuario controla destinatarios

#### Cumplimiento
- **GDPR**: Compatible con configuración adecuada
- **HIPAA**: Verificar con asesoría legal para datos médicos
- **SOX**: Logs auditables para cumplimiento financiero
- **Local laws**: Verificar regulaciones locales de monitoreo

---

**© 2025 Capturer v2.0 - Sistema Avanzado de Captura de Pantallas**

*Esta guía completa cubre todas las funcionalidades de Capturer v2.0. La aplicación se actualiza continuamente con nuevas características y mejoras de seguridad.*
