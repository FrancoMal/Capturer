# =============================================================================
# Capturer Dashboard - Azure Resources Setup Script
# =============================================================================
# Este script configura todos los recursos necesarios en Azure
# Ejecutar: .\azure-setup.ps1 -subscriptionId "your-subscription-id"

param(
    [Parameter(Mandatory=$true)]
    [string]$subscriptionId,
    [string]$resourceGroupName = "rg-capturer-dashboard",
    [string]$location = "West Europe",
    [string]$environment = "dev"
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

Write-ColorOutput Green "Iniciando configuracion de Azure para Capturer Dashboard..."

# Variables de configuración
$appServicePlanName = "asp-capturer-dashboard-$environment"
$webAppName = "capturer-dashboard-$environment-$(Get-Random -Maximum 9999)"
$sqlServerName = "sql-capturer-$environment-$(Get-Random -Maximum 9999)"
$sqlDatabaseName = "capturer-dashboard-db"
$signalRName = "signalr-capturer-$environment-$(Get-Random -Maximum 9999)"
$keyVaultName = "kv-capturer-$environment-$(Get-Random -Maximum 9999)"
$appInsightsName = "ai-capturer-$environment"

# SQL Admin credentials
$sqlAdminUser = "captureadmin"
$sqlAdminPassword = "Capturer@Azure$(Get-Random -Maximum 99)!"

Write-ColorOutput Yellow "Configuracion:"
Write-Output "  Resource Group: $resourceGroupName"
Write-Output "  Location: $location"
Write-Output "  Environment: $environment"
Write-Output "  Web App: $webAppName"
Write-Output "  SQL Server: $sqlServerName"
Write-Output ""

# 1. Login y configurar suscripción
Write-ColorOutput Yellow "Configurando Azure CLI..."
try {
    az account set --subscription $subscriptionId
    $currentAccount = az account show --query "name" -o tsv
    Write-ColorOutput Green "Conectado a: $currentAccount"
} catch {
    Write-ColorOutput Red "Error configurando suscripcion. Ejecuta 'az login' primero"
    exit 1
}

# 2. Crear Resource Group
Write-ColorOutput Yellow "Creando Resource Group..."
az group create --name $resourceGroupName --location $location
Write-ColorOutput Green "Resource Group '$resourceGroupName' creado"

# 3. Crear SQL Server y Database
Write-ColorOutput Yellow "Creando Azure SQL Database..."
az sql server create `
    --name $sqlServerName `
    --resource-group $resourceGroupName `
    --location $location `
    --admin-user $sqlAdminUser `
    --admin-password $sqlAdminPassword

# Configurar firewall para Azure services
az sql server firewall-rule create `
    --resource-group $resourceGroupName `
    --server $sqlServerName `
    --name "AllowAzureServices" `
    --start-ip-address 0.0.0.0 `
    --end-ip-address 0.0.0.0

# Crear la base de datos
az sql db create `
    --resource-group $resourceGroupName `
    --server $sqlServerName `
    --name $sqlDatabaseName `
    --service-objective Basic `
    --backup-storage-redundancy Local

Write-ColorOutput Green "Azure SQL Database configurada"

# 4. Crear App Service Plan
Write-ColorOutput Yellow "Creando App Service Plan..."
az appservice plan create `
    --name $appServicePlanName `
    --resource-group $resourceGroupName `
    --location $location `
    --sku B1

Write-ColorOutput Green "App Service Plan creado"

# 5. Crear Web App
Write-ColorOutput Yellow "Creando Web App..."
az webapp create `
    --resource-group $resourceGroupName `
    --plan $appServicePlanName `
    --name $webAppName `
    --runtime "DOTNETCORE|8.0"

# Habilitar logs
az webapp log config `
    --resource-group $resourceGroupName `
    --name $webAppName `
    --application-logging azureblobstorage `
    --level verbose

Write-ColorOutput Green "Web App '$webAppName' creada"

# 6. Crear SignalR Service
Write-ColorOutput Yellow "Creando SignalR Service..."
az signalr create `
    --name $signalRName `
    --resource-group $resourceGroupName `
    --location $location `
    --sku Standard_S1 `
    --service-mode Default

Write-ColorOutput Green "SignalR Service configurado"

# 7. Crear Key Vault
Write-ColorOutput Yellow "Creando Key Vault..."
az keyvault create `
    --name $keyVaultName `
    --resource-group $resourceGroupName `
    --location $location `
    --sku standard

# Obtener el principal ID de la web app para acceso a Key Vault
$webAppPrincipalId = az webapp identity assign `
    --name $webAppName `
    --resource-group $resourceGroupName `
    --query principalId -o tsv

# Dar permisos a la web app para acceder al Key Vault
az keyvault set-policy `
    --name $keyVaultName `
    --object-id $webAppPrincipalId `
    --secret-permissions get list

Write-ColorOutput Green "Key Vault configurado"

# 8. Crear Application Insights
Write-ColorOutput Yellow "Creando Application Insights..."
az monitor app-insights component create `
    --app $appInsightsName `
    --location $location `
    --resource-group $resourceGroupName `
    --kind web `
    --application-type web

Write-ColorOutput Green "Application Insights configurado"

# 9. Obtener connection strings
Write-ColorOutput Yellow "Obteniendo connection strings..."

$sqlConnectionString = "Server=tcp:$sqlServerName.database.windows.net,1433;Initial Catalog=$sqlDatabaseName;Persist Security Info=False;User ID=$sqlAdminUser;Password=$sqlAdminPassword;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

$signalRConnectionString = az signalr key list `
    --name $signalRName `
    --resource-group $resourceGroupName `
    --query primaryConnectionString -o tsv

$appInsightsInstrumentationKey = az monitor app-insights component show `
    --app $appInsightsName `
    --resource-group $resourceGroupName `
    --query instrumentationKey -o tsv

# 10. Almacenar secrets en Key Vault
Write-ColorOutput Yellow "Almacenando secrets en Key Vault..."

az keyvault secret set `
    --vault-name $keyVaultName `
    --name "SqlConnectionString" `
    --value $sqlConnectionString

az keyvault secret set `
    --vault-name $keyVaultName `
    --name "SignalRConnectionString" `
    --value $signalRConnectionString

az keyvault secret set `
    --vault-name $keyVaultName `
    --name "ApplicationInsightsInstrumentationKey" `
    --value $appInsightsInstrumentationKey

# 11. Configurar App Settings
Write-ColorOutput Yellow "Configurando App Settings..."

az webapp config appsettings set `
    --resource-group $resourceGroupName `
    --name $webAppName `
    --settings `
        "WEBSITE_RUN_FROM_PACKAGE=1" `
        "ASPNETCORE_ENVIRONMENT=Production" `
        "KeyVaultName=$keyVaultName" `
        "AZURE_CLIENT_ID=" `
        "AllowedHosts=*"

# 12. Generar archivo de configuración
Write-ColorOutput Yellow "Generando archivos de configuracion..."

$configContent = @"
{
  "AzureResources": {
    "ResourceGroup": "$resourceGroupName",
    "Location": "$location",
    "Environment": "$environment",
    "WebApp": {
      "Name": "$webAppName",
      "Url": "https://$webAppName.azurewebsites.net"
    },
    "SqlDatabase": {
      "ServerName": "$sqlServerName",
      "DatabaseName": "$sqlDatabaseName",
      "AdminUser": "$sqlAdminUser"
    },
    "SignalR": {
      "Name": "$signalRName"
    },
    "KeyVault": {
      "Name": "$keyVaultName",
      "Url": "https://$keyVaultName.vault.azure.net/"
    },
    "ApplicationInsights": {
      "Name": "$appInsightsName",
      "InstrumentationKey": "$appInsightsInstrumentationKey"
    }
  }
}
"@

$configContent | Out-File -FilePath "azure-config.json" -Encoding UTF8

# 13. Resumen final
Write-ColorOutput Green ""
Write-ColorOutput Green "Configuracion de Azure completada exitosamente!"
Write-ColorOutput Green "=============================================="
Write-ColorOutput Yellow "Recursos creados:"
Write-Output "  🌐 Web App: https://$webAppName.azurewebsites.net"
Write-Output "  🗄️ SQL Server: $sqlServerName.database.windows.net"
Write-Output "  📡 SignalR: $signalRName"
Write-Output "  🔐 Key Vault: https://$keyVaultName.vault.azure.net"
Write-Output "  📊 Application Insights: $appInsightsName"
Write-Output ""
Write-ColorOutput Yellow "Credenciales SQL (guardalas de forma segura!):"
Write-Output "  Usuario: $sqlAdminUser"
Write-Output "  Contraseña: $sqlAdminPassword"
Write-Output ""
Write-ColorOutput Yellow "Archivos generados:"
Write-Output "  ✅ azure-config.json - Configuración de recursos"
Write-Output ""
Write-ColorOutput Yellow "Proximos pasos:"
Write-Output "  1. Ejecutar: dotnet ef database update (para migrar a Azure SQL)"
Write-Output "  2. Configurar GitHub Actions para CI/CD"
Write-Output "  3. Desplegar aplicación: az webapp deployment source config-zip"
Write-Output ""
Write-ColorOutput Green "Tu aplicacion esta lista para Azure!"