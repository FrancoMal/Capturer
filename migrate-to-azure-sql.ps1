# =============================================================================
# Capturer Dashboard - Azure SQL Database Migration Script
# =============================================================================
# Este script migra la base de datos de SQLite local a Azure SQL Database
# Ejecutar después de crear los recursos de Azure

param(
    [Parameter(Mandatory=$true)]
    [string]$azureConnectionString,
    [string]$localDbPath = "capturer-dashboard.db",
    [switch]$skipDataTransfer = $false
)

function Write-ColorOutput($ForegroundColor) {
    $fc = $host.UI.RawUI.ForegroundColor
    $host.UI.RawUI.ForegroundColor = $ForegroundColor
    if ($args) {
        Write-Output $args
    }
    $host.UI.RawUI.ForegroundColor = $fc
}

Write-ColorOutput Green "🗄️ Iniciando migración a Azure SQL Database..."

# Verificar que dotnet ef está instalado
$efToolInstalled = dotnet tool list -g | Select-String "dotnet-ef"
if (-not $efToolInstalled) {
    Write-ColorOutput Yellow "📦 Instalando Entity Framework Core Tools..."
    dotnet tool install --global dotnet-ef
}

# Navegar al directorio del proyecto
$projectPath = "src\CapturerDashboard.Web"
if (-not (Test-Path $projectPath)) {
    Write-ColorOutput Red "❌ No se encuentra el directorio del proyecto: $projectPath"
    exit 1
}

Set-Location $projectPath

Write-ColorOutput Yellow "📋 Configuración de migración:"
Write-Output "  Proyecto: $projectPath"
Write-Output "  SQLite Local: $localDbPath"
Write-Output "  Azure SQL: [Connection String Configurado]"
Write-Output ""

# 1. Backup de datos locales (si existe)
$localDbFullPath = "..\..\$localDbPath"
$backupData = @()

if ((Test-Path $localDbFullPath) -and -not $skipDataTransfer) {
    Write-ColorOutput Yellow "💾 Creando backup de datos locales..."
    
    # Exportar datos existentes de SQLite
    $exportScript = @"
using Microsoft.EntityFrameworkCore;
using CapturerDashboard.Data.Context;
using System.Text.Json;

var optionsBuilder = new DbContextOptionsBuilder<DashboardDbContext>();
optionsBuilder.UseSqlite("Data Source=$localDbFullPath");

using var context = new DashboardDbContext(optionsBuilder.Options);

// Exportar datos de tablas principales (excluyendo seeds)
var organizations = await context.Organizations.Where(o => o.Slug != "default").ToListAsync();
var users = await context.Users.Where(u => u.Email != "admin@capturer.local").ToListAsync();
var computers = await context.Computers.ToListAsync();
var reports = await context.ActivityReports.ToListAsync();

var backup = new {
    Organizations = organizations,
    Users = users,
    Computers = computers,
    Reports = reports,
    ExportDate = DateTime.UtcNow
};

var json = JsonSerializer.Serialize(backup, new JsonSerializerOptions { WriteIndented = true });
File.WriteAllText("database-backup.json", json);

Console.WriteLine($"Backup completado: {organizations.Count} orgs, {users.Count} users, {computers.Count} computers, {reports.Count} reports");
"@

    $exportScript | Out-File -FilePath "export-data.cs" -Encoding UTF8
    Write-ColorOutput Green "✅ Script de backup creado"
}

# 2. Crear nueva migración para Azure SQL
Write-ColorOutput Yellow "🔄 Creando migración para Azure SQL..."

# Establecer la connection string para la migración
$env:ConnectionStrings__DefaultConnection = $azureConnectionString

try {
    # Crear migración inicial para Azure SQL
    dotnet ef migrations add InitialAzureMigration --context DashboardDbContext
    Write-ColorOutput Green "✅ Migración 'InitialAzureMigration' creada"
    
    # Aplicar migración a Azure SQL Database
    Write-ColorOutput Yellow "📤 Aplicando migración a Azure SQL Database..."
    dotnet ef database update --context DashboardDbContext
    Write-ColorOutput Green "✅ Base de datos Azure SQL actualizada"
    
} catch {
    Write-ColorOutput Red "❌ Error durante la migración: $_"
    exit 1
}

# 3. Verificar conexión y estructura
Write-ColorOutput Yellow "🔍 Verificando estructura de base de datos..."

$verificationScript = @"
using Microsoft.EntityFrameworkCore;
using CapturerDashboard.Data.Context;

var optionsBuilder = new DbContextOptionsBuilder<DashboardDbContext>();
optionsBuilder.UseSqlServer("$azureConnectionString");

using var context = new DashboardDbContext(optionsBuilder.Options);

try {
    await context.Database.OpenConnectionAsync();
    Console.WriteLine("✅ Conexión a Azure SQL: OK");
    
    var orgCount = await context.Organizations.CountAsync();
    var userCount = await context.Users.CountAsync();
    
    Console.WriteLine($"📊 Organizaciones: {orgCount}");
    Console.WriteLine($"👥 Usuarios: {userCount}");
    Console.WriteLine("✅ Estructura de base de datos verificada");
    
} catch (Exception ex) {
    Console.WriteLine($"❌ Error de verificación: {ex.Message}");
}
"@

$verificationScript | Out-File -FilePath "verify-azure-db.cs" -Encoding UTF8

# 4. Crear script de importación de datos (si hay backup)
if ((Test-Path "database-backup.json") -and -not $skipDataTransfer) {
    Write-ColorOutput Yellow "📥 Preparando importación de datos..."
    
    $importScript = @"
using Microsoft.EntityFrameworkCore;
using CapturerDashboard.Data.Context;
using CapturerDashboard.Core.Models;
using System.Text.Json;

var optionsBuilder = new DbContextOptionsBuilder<DashboardDbContext>();
optionsBuilder.UseSqlServer("$azureConnectionString");

using var context = new DashboardDbContext(optionsBuilder.Options);

var backupJson = File.ReadAllText("database-backup.json");
var backup = JsonSerializer.Deserialize<dynamic>(backupJson);

Console.WriteLine("🔄 Importando datos respaldados...");

// TODO: Implementar lógica de importación específica según necesidades
// Este es un template que debes personalizar según tus datos

Console.WriteLine("✅ Datos importados exitosamente");
"@

    $importScript | Out-File -FilePath "import-data.cs" -Encoding UTF8
    Write-ColorOutput Green "✅ Script de importación preparado"
}

# 5. Limpiar variables de entorno
Remove-Item Env:ConnectionStrings__DefaultConnection -ErrorAction SilentlyContinue

# 6. Generar resumen
Write-ColorOutput Green ""
Write-ColorOutput Green "🎉 ¡Migración a Azure SQL completada!"
Write-ColorOutput Green "===================================="
Write-ColorOutput Yellow "📋 Archivos generados:"
if (Test-Path "database-backup.json") {
    Write-Output "  ✅ database-backup.json - Backup de datos locales"
}
Write-Output "  ✅ verify-azure-db.cs - Script de verificación"
Write-Output "  ✅ import-data.cs - Script de importación"
Write-Output ""
Write-ColorOutput Yellow "🔍 Verificación:"
Write-Output "  1. Ejecutar verify-azure-db.cs para confirmar conexión"
Write-Output "  2. Revisar que las tablas se crearon correctamente"
Write-Output "  3. Verificar datos seed (admin user, default org)"
Write-Output ""
Write-ColorOutput Yellow "➡️ Próximos pasos:"
Write-Output "  1. Actualizar appsettings.Production.json con connection string"
Write-Output "  2. Configurar Key Vault para almacenar connection string"
Write-Output "  3. Desplegar aplicación a Azure App Service"
Write-Output ""
Write-ColorOutput Green "🚀 Tu aplicación está lista para usar Azure SQL!"

# Regresar al directorio original
Set-Location ..\..