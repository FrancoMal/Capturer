# ⚡ Quick Start - Azure Deployment

## 🎯 Comandos Esenciales

### 1. Login a Azure
```bash
az login
az account list --output table
```

### 2. Crear Recursos de Azure (5-10 min)
```powershell
.\azure-setup.ps1 -subscriptionId "TU-SUBSCRIPTION-ID"
```

### 3. Migrar Base de Datos (2-3 min)
```powershell
# Usar connection string del paso anterior
.\migrate-to-azure-sql.ps1 -azureConnectionString "Server=tcp:..."
```

### 4. Deploy Aplicación (5-15 min)
```powershell
.\deploy-to-azure.ps1 -subscriptionId "TU-SUBSCRIPTION-ID"
```

### 5. Verificar
```bash
# Reemplaza con tu URL real
curl https://tu-app.azurewebsites.net/health
```

## 🔧 Configuración Local

### appsettings.Development.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=capturer-dashboard.db"
  },
  "Features": {
    "UseAzureSignalR": false,
    "UseApplicationInsights": false,
    "UseKeyVault": false
  }
}
```

### Ejecutar localmente
```bash
dotnet run --project src/CapturerDashboard.Web
# Visita: https://localhost:5001
```

## 📊 URLs Post-Deployment
- **App**: `https://tu-app.azurewebsites.net`
- **Health**: `https://tu-app.azurewebsites.net/health`
- **API Docs**: `https://tu-app.azurewebsites.net/api/docs`
- **SignalR**: `https://tu-app.azurewebsites.net/dashboardHub`

## 🆘 Troubleshooting Rápido
```bash
# Ver logs
az webapp log tail --name tu-app --resource-group rg-capturer-dashboard

# Reiniciar app
az webapp restart --name tu-app --resource-group rg-capturer-dashboard

# Ver secrets
az keyvault secret list --vault-name tu-keyvault
```

**¡Eso es todo!** 🚀