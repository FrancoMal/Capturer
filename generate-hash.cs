using System;

class Program {
    static void Main() {
        string password = "admin123";
        string hash = BCrypt.Net.BCrypt.HashPassword(password);
        Console.WriteLine($"BCrypt hash for '{password}': {hash}");
        
        // Verify it works
        bool isValid = BCrypt.Net.BCrypt.Verify(password, hash);
        Console.WriteLine($"Verification test: {isValid}");
    }
}