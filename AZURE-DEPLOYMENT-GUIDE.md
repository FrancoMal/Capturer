# 🚀 Guía Completa de Deployment a Azure - Capturer Dashboard

Esta guía te lleva paso a paso desde el desarrollo local hasta tener tu Capturer Dashboard funcionando completamente en Azure.

## 📋 Prerequisitos

### Software Requerido
- ✅ **Azure CLI** instalado y actualizado
- ✅ **.NET 8 SDK** instalado
- ✅ **Entity Framework Core Tools**: `dotnet tool install -g dotnet-ef`
- ✅ **PowerShell 5.1** o superior
- ✅ **Suscripción de Azure** activa

### Verificación Rápida
```bash
# Verificar instalaciones
az --version
dotnet --version
dotnet ef --version
```

---

## 🎯 Proceso Completo (5 Pasos)

### **Paso 1: Login a Azure**
```bash
# Hacer login
az login

# Listar suscripciones disponibles
az account list --output table

# Anotar el subscription-id que quieres usar
```

### **Paso 2: Configurar Recursos de Azure**
```powershell
# Ejecutar script de provisioning (tarda ~5-10 minutos)
.\azure-setup.ps1 -subscriptionId "TU-SUBSCRIPTION-ID"

# Ejemplo:
.\azure-setup.ps1 -subscriptionId "12345678-1234-1234-1234-123456789012"
```

**¿Qué hace este script?**
- 🏗️ Crea Resource Group
- 🗄️ Configura Azure SQL Database
- 🌐 Crea App Service + Plan
- 📡 Configura SignalR Service
- 🔐 Configura Key Vault
- 📊 Configura Application Insights
- 💾 Almacena todas las connection strings
- 📄 Genera `azure-config.json`

### **Paso 3: Migrar Base de Datos**
```powershell
# Usar la connection string del paso anterior
.\migrate-to-azure-sql.ps1 -azureConnectionString "Server=tcp:tu-server.database.windows.net..."

# La connection string la encuentras en azure-config.json
```

**¿Qué hace este script?**
- 📦 Crea migraciones para Azure SQL
- 🔄 Migra estructura de SQLite a Azure SQL
- 📊 Verifica conexión y estructura
- 💾 Backup opcional de datos locales

### **Paso 4: Deploy de la Aplicación**
```powershell
# Deploy completo (tarda ~5-15 minutos)
.\deploy-to-azure.ps1 -subscriptionId "TU-SUBSCRIPTION-ID"

# Con opciones adicionales:
.\deploy-to-azure.ps1 -subscriptionId "TU-SUBSCRIPTION-ID" -environment "prod" -skipTests
```

**¿Qué hace este script?**
- 🔨 Compila la aplicación en Release
- 🧪 Ejecuta tests automáticamente
- 📦 Crea paquete de deployment
- ⚙️ Configura App Settings en Azure
- 🌐 Despliega a App Service
- 🗄️ Aplica migraciones de BD
- ✅ Verifica que todo funcione

### **Paso 5: Verificación Final**
```bash
# Verificar que la aplicación funciona
# La URL la obtienes del output del script anterior

curl https://tu-app-name.azurewebsites.net/health
```

---

## 🔧 Configuración Post-Deployment

### **Configurar Dominio Personalizado (Opcional)**
```bash
# Si tienes un dominio propio
az webapp config hostname add \
  --resource-group rg-capturer-dashboard \
  --webapp-name tu-app-name \
  --hostname tu-dominio.com
```

### **Configurar SSL Certificate (Opcional)**
```bash
# SSL gratuito para dominio personalizado
az webapp config ssl bind \
  --resource-group rg-capturer-dashboard \
  --name tu-app-name \
  --certificate-thumbprint THUMBPRINT \
  --ssl-type SNI
```

### **Configurar Alertas**
En Azure Portal:
1. Ve a tu App Service
2. Monitoring > Alerts
3. Configura alertas para:
   - HTTP 5xx errors
   - Response time > 5 seconds
   - CPU usage > 80%

---

## 📊 URLs Importantes

Después del deployment, tendrás acceso a:

| Servicio | URL | Propósito |
|----------|-----|-----------|
| **Aplicación Principal** | `https://tu-app.azurewebsites.net` | Dashboard web |
| **Health Check** | `https://tu-app.azurewebsites.net/health` | Monitoreo |
| **API Docs** | `https://tu-app.azurewebsites.net/api/docs` | Documentación API |
| **SignalR Hub** | `https://tu-app.azurewebsites.net/dashboardHub` | Tiempo real |

---

## 🛠️ Troubleshooting

### **Problema: "Web App no existe"**
```bash
# Solución: Re-ejecutar azure-setup.ps1
.\azure-setup.ps1 -subscriptionId "TU-SUBSCRIPTION-ID"
```

### **Problema: "Error de migración de BD"**
```bash
# Verificar connection string
az keyvault secret show --vault-name tu-keyvault --name "SqlConnectionString"

# Verificar conectividad SQL
sqlcmd -S tu-server.database.windows.net -d tu-database -U tu-usuario -P
```

### **Problema: "App no responde"**
```bash
# Ver logs en tiempo real
az webapp log tail --name tu-app-name --resource-group rg-capturer-dashboard

# Reiniciar aplicación
az webapp restart --name tu-app-name --resource-group rg-capturer-dashboard
```

### **Problema: "SignalR no conecta"**
1. Verifica que Azure SignalR Service esté en "Default mode"
2. Verifica connection string en Key Vault
3. Revisa configuración CORS en appsettings.Production.json

---

## 💰 Costos Estimados

### **Configuración Básica (B1)**
- **App Service Plan B1**: ~$13/mes
- **Azure SQL Basic**: ~$5/mes  
- **SignalR Standard S1**: ~$50/mes
- **Key Vault**: ~$1/mes
- **Application Insights**: Gratis hasta 1GB
- **Total**: ~$69/mes

### **Optimización de Costos**
```bash
# Escalar a plan gratuito (solo para desarrollo)
az appservice plan update --name tu-plan --resource-group rg-capturer-dashboard --sku F1

# Pausar SQL Database (desarrollo)
az sql db pause --name tu-database --server tu-server --resource-group rg-capturer-dashboard
```

---

## 🔄 CI/CD con GitHub Actions

Para automatizar deployments futuros, puedes configurar GitHub Actions:

```yaml
# .github/workflows/azure-deploy.yml
name: Deploy to Azure
on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Build and Deploy
        run: |
          dotnet restore
          dotnet build -c Release
          dotnet publish -c Release -o ./publish
          
      - name: Deploy to Azure
        uses: azure/webapps-deploy@v2
        with:
          app-name: ${{ secrets.AZURE_WEBAPP_NAME }}
          publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
          package: ./publish
```

---

## 📈 Monitoreo y Mantenimiento

### **Comandos Útiles**
```bash
# Ver métricas de la aplicación
az monitor metrics list --resource tu-app-resource-id --metric "Http5xx"

# Backup de base de datos
az sql db export --name tu-database --server tu-server --resource-group rg-capturer-dashboard

# Escalar aplicación
az appservice plan update --name tu-plan --resource-group rg-capturer-dashboard --sku S1
```

### **Logs importantes**
- **Application Insights**: Errores y performance
- **App Service Logs**: Startup y runtime issues
- **Azure SQL Logs**: Database performance y queries

---

## ✅ Checklist Final

- [ ] Azure CLI configurado y logueado
- [ ] Scripts ejecutados sin errores
- [ ] Aplicación responde en `/health`
- [ ] SignalR conecta correctamente
- [ ] Base de datos migrada y con datos
- [ ] Key Vault con todos los secrets
- [ ] Alertas de monitoreo configuradas
- [ ] Dominio personalizado (opcional)
- [ ] SSL certificate (opcional)
- [ ] CI/CD pipeline (opcional)

---

## 🆘 Soporte

Si tienes problemas:

1. **Revisa logs**: `az webapp log tail --name tu-app-name --resource-group rg-capturer-dashboard`
2. **Verifica recursos**: Azure Portal > Resource Groups
3. **Health checks**: Visita `/health` endpoint
4. **Connection strings**: Verifica Key Vault secrets
5. **Networking**: Verifica firewalls y CORS

---

🎉 **¡Felicitaciones!** Tu Capturer Dashboard está ahora funcionando profesionalmente en Azure con:
- ✅ Escalabilidad automática
- ✅ Base de datos en la nube
- ✅ Tiempo real con SignalR
- ✅ Monitoreo integrado
- ✅ Seguridad enterprise
- ✅ Backups automáticos

**¡Tu aplicación está lista para producción!** 🚀