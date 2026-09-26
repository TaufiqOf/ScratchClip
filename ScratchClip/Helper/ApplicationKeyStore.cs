using System;
using System.Security.Cryptography;
using System.Text;
using KeySharp;

namespace ScratchClip.Helper;

public static class ApplicationKeyStore
{
    private const string Application = "com.scratch.scratchclip";
    private const string Service = "ScratchClip";

    private const string PasswordHashKey = "PasswordHash";
    private const string EncryptionSecretKey = "EncryptionSecret";

    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Pbkdf2Iterations = 600_000;

    // Stored as a zeroable UTF-8 byte array in memory after authentication.
    private static byte[]? _sessionPasswordBytes;

    public static Action? OnPasswordChanged;

    public static bool HasPassword()
    {
        return !string.IsNullOrEmpty(Load(PasswordHashKey));
    }

    /// <summary>
    ///     Returns the user password if authenticated.
    ///     Returns null when the application is in passwordless mode.
    /// </summary>
    public static string? GetSessionPassword()
    {
        if (_sessionPasswordBytes == null || _sessionPasswordBytes.Length == 0)
            return null;

        return Encoding.UTF8.GetString(_sessionPasswordBytes);
    }

    /// <summary>
    ///     Gets the application's random encryption secret.
    ///     This secret is generated once and stored in the OS keyring.
    ///     It is never stored in the encrypted file.
    /// </summary>
    public static byte[] GetOrCreateEncryptionSecret()
    {
        var existing = Load(EncryptionSecretKey);

        if (!string.IsNullOrEmpty(existing))
            try
            {
                var secret = Convert.FromBase64String(existing);

                if (secret.Length == KeySize)
                    return secret;
            }
            catch (FormatException)
            {
                NotificationHelper.Error("Error",$"Invalid encryption secret in keyring.");
                // Invalid keyring value.
                // Generate a new secret below.
            }

        var newSecret = RandomNumberGenerator.GetBytes(KeySize);

        try
        {
            Save(
                EncryptionSecretKey,
                Convert.ToBase64String(newSecret));

            return newSecret;
        }
        catch
        {
            CryptographicOperations.ZeroMemory(newSecret);
            throw;
        }
    }

    public static void SetSessionPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException(
                "Password cannot be empty.",
                nameof(password));

        ClearSessionPassword();
        _sessionPasswordBytes = Encoding.UTF8.GetBytes(password);
    }

    public static void ClearSessionPassword()
    {
        if (_sessionPasswordBytes != null)
        {
            CryptographicOperations.ZeroMemory(_sessionPasswordBytes);
            _sessionPasswordBytes = null;
        }
    }

    public static void SetPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            DeletePassword();
            return;
        }

        var salt = RandomNumberGenerator.GetBytes(SaltSize);

        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Pbkdf2Iterations,
            HashAlgorithmName.SHA256,
            KeySize);

        try
        {
            // version.salt.hash
            var storedValue =
                $"1.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";

            Save(
                PasswordHashKey,
                storedValue);

            // Authenticated for this session.
            SetSessionPassword(password);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(hash);
            CryptographicOperations.ZeroMemory(salt);
        }

        OnPasswordChanged?.Invoke();
    }

    public static bool VerifyPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            return false;

        var storedValue = Load(PasswordHashKey);

        if (string.IsNullOrEmpty(storedValue))
            return false;

        var parts = storedValue.Split('.');

        if (parts.Length != 3 || parts[0] != "1")
            return false;

        try
        {
            var salt = Convert.FromBase64String(parts[1]);
            var expectedHash = Convert.FromBase64String(parts[2]);

            if (salt.Length != SaltSize ||
                expectedHash.Length != KeySize)
                return false;

            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Pbkdf2Iterations,
                HashAlgorithmName.SHA256,
                KeySize);

            try
            {
                return CryptographicOperations.FixedTimeEquals(
                    actualHash,
                    expectedHash);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(actualHash);
                CryptographicOperations.ZeroMemory(salt);
                CryptographicOperations.ZeroMemory(expectedHash);
            }
        }
        catch (FormatException)
        {
            return false;
        }
    }

    /// <summary>
    ///     Derives the AES-256 encryption key.
    ///     The key is derived from:
    ///     user password (if configured)
    ///     +
    ///     random keyring secret
    ///     +
    ///     per-file salt
    ///     In passwordless mode the keyring secret alone is used.
    /// </summary>
    public static byte[] DeriveEncryptionKey(
        string? password,
        ReadOnlySpan<byte> keyringSecret,
        ReadOnlySpan<byte> salt)
    {
        if (keyringSecret.Length != KeySize)
            throw new ArgumentException(
                $"Keyring secret must be exactly {KeySize} bytes.",
                nameof(keyringSecret));

        if (salt.Length != SaltSize)
            throw new ArgumentException(
                $"Salt must be exactly {SaltSize} bytes.",
                nameof(salt));

        var passwordBytes = string.IsNullOrEmpty(password)
            ? Array.Empty<byte>()
            : Encoding.UTF8.GetBytes(password);

        try
        {
            /*
             * We combine the two independent secrets:
             *
             *   password
             *   +
             *   keyring secret
             *
             * and then run PBKDF2 using the per-file salt.
             *
             * Passwordless:
             *
             *   keyring secret
             *
             * Password:
             *
             *   password + keyring secret
             */

            var combined = new byte[
                passwordBytes.Length +
                keyringSecret.Length];

            try
            {
                passwordBytes.CopyTo(combined, 0);

                keyringSecret.CopyTo(
                    combined.AsSpan(passwordBytes.Length));

                return Rfc2898DeriveBytes.Pbkdf2(
                    combined,
                    salt,
                    Pbkdf2Iterations,
                    HashAlgorithmName.SHA256,
                    KeySize);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(combined);
            }
        }
        finally
        {
            if (passwordBytes.Length > 0) CryptographicOperations.ZeroMemory(passwordBytes);
        }
    }

    public static void DeletePassword()
    {
        Delete(PasswordHashKey);

        ClearSessionPassword();

        /*
         * Do NOT delete EncryptionSecretKey.
         *
         * The same keyring secret continues to protect
         * passwordless encryption.
         */

        OnPasswordChanged?.Invoke();
    }

    private static void Save(
        string key,
        string value)
    {
        Keyring.SetPassword(
            Application,
            Service,
            key,
            value);
    }

    private static string? Load(string key)
    {
        try
        {
            return Keyring.GetPassword(
                Application,
                Service,
                key);
        }
        catch (KeyringException)
        {
            return null;
        }
    }

    private static void Delete(string key)
    {
        try
        {
            Keyring.DeletePassword(
                Application,
                Service,
                key);
        }
        catch (KeyringException)
        {
            // Nothing to delete.
        }
    }
}