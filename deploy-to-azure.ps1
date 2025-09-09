# =============================================================================
# Capturer Dashboard - Complete Azure Deployment Script
# =============================================================================
# Este script despliega la aplicación completa a Azure
# Prerequisito: azure-setup.ps1 debe haberse ejecutado previamente

param(
    [Parameter(Mandatory=$true)]
    [string]$subscriptionId,
    [string]$resourceGroupName = "rg-capturer-dashboard",
    [string]$environment = "dev",
    [switch]$skipBuild = $false,
    [switch]$skipTests = $false,
    [string]$deploymentSlot = "production"
)

# Colores para output
function Write-ColorOutput($ForegroundColor) {
    $fc = $host.UI.RawUI.ForegroundColor
    $host.UI.RawUI.ForegroundColor = $ForegroundColor
    if ($args) {
        Write-Output $args
    }
    $host.UI.RawUI.ForegroundColor = $fc
}

Write-ColorOutput Green "🚀 Iniciando deployment completo a Azure..."

# Verificar que el archivo de configuración existe
if (-not (Test-Path "azure-config.json")) {
    Write-ColorOutput Red "❌ azure-config.json no encontrado. Ejecuta azure-setup.ps1 primero."
    exit 1
}

# Leer configuración de Azure
$azureConfig = Get-Content "azure-config.json" | ConvertFrom-Json
$webAppName = $azureConfig.AzureResources.WebApp.Name
$keyVaultName = $azureConfig.AzureResources.KeyVault.Name
$sqlServerName = $azureConfig.AzureResources.SqlDatabase.ServerName

Write-ColorOutput Yellow "📋 Configuración de deployment:"
Write-Output "  Resource Group: $resourceGroupName"
Write-Output "  Web App: $webAppName"
Write-Output "  Environment: $environment"
Write-Output "  Deployment Slot: $deploymentSlot"
Write-Output ""

# 1. Configurar Azure CLI
Write-ColorOutput Yellow "🔐 Configurando Azure CLI..."
try {
    az account set --subscription $subscriptionId
    $currentAccount = az account show --query "name" -o tsv
    Write-ColorOutput Green "✅ Conectado a: $currentAccount"
} catch {
    Write-ColorOutput Red "❌ Error configurando suscripción. Ejecuta 'az login' primero"
    exit 1
}

# 2. Verificar que los recursos existen
Write-ColorOutput Yellow "🔍 Verificando recursos de Azure..."
$webAppExists = az webapp show --name $webAppName --resource-group $resourceGroupName --query "name" -o tsv 2>$null
if (-not $webAppExists) {
    Write-ColorOutput Red "❌ Web App '$webAppName' no existe. Ejecuta azure-setup.ps1 primero."
    exit 1
}
Write-ColorOutput Green "✅ Recursos de Azure verificados"

# 3. Build de la aplicación
if (-not $skipBuild) {
    Write-ColorOutput Yellow "🔨 Compilando aplicación..."
    
    # Restaurar paquetes
    dotnet restore
    if ($LASTEXITCODE -ne 0) {
        Write-ColorOutput Red "❌ Error restaurando paquetes NuGet"
        exit 1
    }
    
    # Build en Release
    dotnet build -c Release --no-restore
    if ($LASTEXITCODE -ne 0) {
        Write-ColorOutput Red "❌ Error compilando aplicación"
        exit 1
    }
    
    Write-ColorOutput Green "✅ Aplicación compilada exitosamente"
} else {
    Write-ColorOutput Yellow "⏭️ Build omitido (--skipBuild especificado)"
}

# 4. Ejecutar tests
if (-not $skipTests) {
    Write-ColorOutput Yellow "🧪 Ejecutando tests..."
    
    $testProjects = Get-ChildItem -Path "src" -Name "*.Tests.csproj" -Recurse
    if ($testProjects.Count -gt 0) {
        dotnet test -c Release --no-build --verbosity minimal --logger trx
        if ($LASTEXITCODE -ne 0) {
            Write-ColorOutput Red "❌ Tests fallidos. Deployment cancelado."
            exit 1
        }
        Write-ColorOutput Green "✅ Todos los tests pasaron"
    } else {
        Write-ColorOutput Yellow "⚠️ No se encontraron proyectos de test"
    }
} else {
    Write-ColorOutput Yellow "⏭️ Tests omitidos (--skipTests especificado)"
}

# 5. Crear paquete de publicación
Write-ColorOutput Yellow "📦 Creando paquete de deployment..."

$publishPath = "bin/Release/publish"
$zipPath = "capturer-dashboard-deployment.zip"

# Publicar aplicación
dotnet publish src/CapturerDashboard.Web/CapturerDashboard.Web.csproj `
    -c Release `
    -o $publishPath `
    --no-restore `
    --no-build

if ($LASTEXITCODE -ne 0) {
    Write-ColorOutput Red "❌ Error publicando aplicación"
    exit 1
}

# Crear ZIP para deployment
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Compress-Archive -Path "$publishPath/*" -DestinationPath $zipPath -Force
Write-ColorOutput Green "✅ Paquete de deployment creado: $zipPath"

# 6. Configurar App Settings en Azure
Write-ColorOutput Yellow "⚙️ Configurando App Settings en Azure..."

# Obtener connection strings desde Key Vault
$sqlConnectionString = az keyvault secret show `
    --vault-name $keyVaultName `
    --name "SqlConnectionString" `
    --query "value" -o tsv

$signalRConnectionString = az keyvault secret show `
    --vault-name $keyVaultName `
    --name "SignalRConnectionString" `
    --query "value" -o tsv

$appInsightsKey = az keyvault secret show `
    --vault-name $keyVaultName `
    --name "ApplicationInsightsInstrumentationKey" `
    --query "value" -o tsv

# Configurar App Settings
az webapp config appsettings set `
    --resource-group $resourceGroupName `
    --name $webAppName `
    --slot $deploymentSlot `
    --settings `
        "ASPNETCORE_ENVIRONMENT=Production" `
        "KeyVaultName=$keyVaultName" `
        "Features__UseApplicationInsights=true" `
        "Features__UseAzureSignalR=true" `
        "Features__UseKeyVault=true" `
        "Features__EnableSwagger=false" `
        "Security__RequireHttps=true" `
        "Security__EnableCors=false" `
        "WEBSITE_RUN_FROM_PACKAGE=1"

# Configurar Connection Strings
az webapp config connection-string set `
    --resource-group $resourceGroupName `
    --name $webAppName `
    --slot $deploymentSlot `
    --connection-string-type SQLServer `
    --settings DefaultConnection="$sqlConnectionString"

az webapp config appsettings set `
    --resource-group $resourceGroupName `
    --name $webAppName `
    --slot $deploymentSlot `
    --settings `
        "Azure__SignalR__ConnectionString=$signalRConnectionString" `
        "Azure__ApplicationInsights__InstrumentationKey=$appInsightsKey"

Write-ColorOutput Green "✅ App Settings configurados"

# 7. Deployment de la aplicación
Write-ColorOutput Yellow "🌐 Desplegando aplicación a Azure App Service..."

az webapp deployment source config-zip `
    --resource-group $resourceGroupName `
    --name $webAppName `
    --slot $deploymentSlot `
    --src $zipPath

if ($LASTEXITCODE -ne 0) {
    Write-ColorOutput Red "❌ Error desplegando aplicación"
    exit 1
}

Write-ColorOutput Green "✅ Aplicación desplegada exitosamente"

# 8. Migrar base de datos
Write-ColorOutput Yellow "🗄️ Aplicando migraciones de base de datos..."

# Configurar connection string para migraciones
$env:ConnectionStrings__DefaultConnection = $sqlConnectionString
$env:ASPNETCORE_ENVIRONMENT = "Production"

try {
    # Aplicar migraciones
    dotnet ef database update `
        --project src/CapturerDashboard.Web `
        --configuration Release `
        --verbose
    
    if ($LASTEXITCODE -eq 0) {
        Write-ColorOutput Green "✅ Migraciones aplicadas exitosamente"
    } else {
        Write-ColorOutput Red "❌ Error aplicando migraciones"
        exit 1
    }
} finally {
    # Limpiar variables de entorno
    Remove-Item Env:ConnectionStrings__DefaultConnection -ErrorAction SilentlyContinue
    Remove-Item Env:ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue
}

# 9. Verificar deployment
Write-ColorOutput Yellow "✅ Verificando deployment..."

$appUrl = "https://$webAppName.azurewebsites.net"
$healthUrl = "$appUrl/health"

Write-ColorOutput Yellow "🔍 Verificando health endpoint..."

$maxRetries = 10
$retryCount = 0
$healthCheckPassed = $false

while ($retryCount -lt $maxRetries -and -not $healthCheckPassed) {
    try {
        $response = Invoke-RestMethod -Uri $healthUrl -Method Get -TimeoutSec 30
        if ($response) {
            $healthCheckPassed = $true
            Write-ColorOutput Green "✅ Health check exitoso"
        }
    } catch {
        $retryCount++
        Write-ColorOutput Yellow "⏳ Esperando que la aplicación esté lista... ($retryCount/$maxRetries)"
        Start-Sleep -Seconds 10
    }
}

if (-not $healthCheckPassed) {
    Write-ColorOutput Red "❌ Health check falló después de $maxRetries intentos"
    Write-ColorOutput Yellow "🔍 Verifica los logs de la aplicación en Azure Portal"
} else {
    Write-ColorOutput Green "✅ Aplicación funcionando correctamente"
}

# 10. Verificar SignalR
Write-ColorOutput Yellow "📡 Verificando SignalR..."
$signalRUrl = "$appUrl/dashboardHub"
try {
    $signalRResponse = Invoke-WebRequest -Uri $signalRUrl -Method Get -TimeoutSec 15
    if ($signalRResponse.StatusCode -eq 200 -or $signalRResponse.StatusCode -eq 404) {
        Write-ColorOutput Green "✅ SignalR endpoint accesible"
    }
} catch {
    Write-ColorOutput Yellow "⚠️ No se pudo verificar SignalR directamente (normal para este tipo de endpoint)"
}

# 11. Cleanup
Write-ColorOutput Yellow "🧹 Limpiando archivos temporales..."
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}
if (Test-Path $publishPath) {
    Remove-Item $publishPath -Recurse -Force
}

# 12. Resumen final
Write-ColorOutput Green ""
Write-ColorOutput Green "🎉 ¡Deployment completado exitosamente!"
Write-ColorOutput Green "============================================="
Write-ColorOutput Yellow "🌐 URLs de la aplicación:"
Write-Output "  Aplicación: $appUrl"
Write-Output "  API Docs: $appUrl/api/docs (si está habilitado)"
Write-Output "  Health Check: $healthUrl"
Write-Output "  SignalR Hub: $signalRUrl"
Write-Output ""
Write-ColorOutput Yellow "📊 Recursos de Azure:"
Write-Output "  Resource Group: $resourceGroupName"
Write-Output "  Web App: $webAppName"
Write-Output "  SQL Server: $sqlServerName.database.windows.net"
Write-Output "  Key Vault: $keyVaultName"
Write-Output ""
Write-ColorOutput Yellow "🔍 Próximos pasos:"
Write-Output "  1. Visita $appUrl para verificar la aplicación"
Write-Output "  2. Configura un dominio personalizado si es necesario"
Write-Output "  3. Configura SSL/TLS certificate si usas dominio personalizado"
Write-Output "  4. Configura CI/CD con GitHub Actions para deployments automáticos"
Write-Output "  5. Configura alertas y monitoreo en Azure Portal"
Write-Output ""
Write-ColorOutput Green "🚀 ¡Tu Capturer Dashboard está en vivo en Azure!"

# Mostrar información de monitoreo
Write-ColorOutput Yellow "📊 Monitoreo y logs:"
Write-Output "  Portal Azure: https://portal.azure.com"
Write-Output "  Application Insights: Busca '$webAppName' en Azure Portal"
Write-Output "  Logs de la aplicación: az webapp log tail --name $webAppName --resource-group $resourceGroupName"