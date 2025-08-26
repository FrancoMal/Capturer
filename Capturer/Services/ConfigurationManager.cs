using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using Capturer.Models;

namespace Capturer.Services;

public interface IConfigurationManager
{
    Task<CapturerConfiguration> LoadConfigurationAsync();
    Task SaveConfigurationAsync(CapturerConfiguration configuration);
    Task<bool> ConfigurationExistsAsync();
    string ConfigurationPath { get; }
}

public class ConfigurationManager : IConfigurationManager
{
    private const string ConfigFileName = "capturer-settings.json";
    private readonly string _configDirectory;
    private readonly string _configPath;

    public string ConfigurationPath => _configPath;

    public ConfigurationManager()
    {
        _configDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Capturer");
        _configPath = Path.Combine(_configDirectory, ConfigFileName);

        // Ensure config directory exists
        Directory.CreateDirectory(_configDirectory);
    }

    public async Task<CapturerConfiguration> LoadConfigurationAsync()
    {
        if (!File.Exists(_configPath))
        {
            // Create default configuration
            var defaultConfig = new CapturerConfiguration();
            await SaveConfigurationAsync(defaultConfig);
            return defaultConfig;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_configPath);
            var config = JsonConvert.DeserializeObject<CapturerConfiguration>(json) ?? new CapturerConfiguration();

            // Decrypt sensitive data
            if (!string.IsNullOrEmpty(config.Email.Password))
            {
                config.Email.Password = DecryptString(config.Email.Password);
            }

            return config;
        }
        catch (Exception ex)
        {
            // Log error and return default configuration
            Console.WriteLine($"Error loading configuration: {ex.Message}");
            return new CapturerConfiguration();
        }
    }

    public async Task SaveConfigurationAsync(CapturerConfiguration configuration)
    {
        try
        {
            // Create a copy for serialization
            var configToSave = JsonConvert.DeserializeObject<CapturerConfiguration>(
                JsonConvert.SerializeObject(configuration)) ?? new CapturerConfiguration();

            // Encrypt sensitive data
            if (!string.IsNullOrEmpty(configToSave.Email.Password))
            {
                configToSave.Email.Password = EncryptString(configToSave.Email.Password);
            }

            var json = JsonConvert.SerializeObject(configToSave, Formatting.Indented);
            await File.WriteAllTextAsync(_configPath, json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error saving configuration: {ex.Message}", ex);
        }
    }

    public async Task<bool> ConfigurationExistsAsync()
    {
        return await Task.FromResult(File.Exists(_configPath));
    }

    private string EncryptString(string plainText)
    {
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(plainText);
            byte[] encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encrypted);
        }
        catch
        {
            return plainText; // Fallback to plain text if encryption fails
        }
    }

    private string DecryptString(string encryptedText)
    {
        try
        {
            byte[] data = Convert.FromBase64String(encryptedText);
            byte[] decrypted = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decrypted);
        }
        catch
        {
            return encryptedText; // Fallback to encrypted text if decryption fails
        }
    }
}