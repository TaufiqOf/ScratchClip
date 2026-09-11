using System;
using System.Security.Cryptography;
using KeySharp;

namespace ScratchClip.Helper;

public static class ApplicationKeyStore
{
    public static Action? OnPasswordChanged;
    private const string Application = "com.scratch.scratchclip";
    private const string Service = "ScratchClip";

    private const string EncryptionKey = "EncryptionKey";
    private const string PasswordHash = "PasswordHash";

    private static void Save(string key, string value)
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

    public static string GetOrCreateApplicationKey()
    {
        var existing = Load(EncryptionKey);

        if (!string.IsNullOrEmpty(existing))
            return existing;

        var key = Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(32));

        Save(EncryptionKey, key);

        return key;
    }

    public static bool HasPassword()
    {
        return !string.IsNullOrEmpty(Load(PasswordHash));
    }

    public static void SetPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            DeletePassword();
            return;
        }
        var salt = RandomNumberGenerator.GetBytes(16);

        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            600_000,
            HashAlgorithmName.SHA256,
            32);

        // Store:
        // version.salt.hash
        //
        // Everything is Base64 so it can safely be stored
        // as a keyring string.
        var storedValue =
            $"1.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";

        Save(PasswordHash, storedValue);
        OnPasswordChanged?.Invoke();
    }

    public static bool VerifyPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            return false;

        var storedValue = Load(PasswordHash);

        if (string.IsNullOrEmpty(storedValue))
            return false;

        var parts = storedValue.Split('.');

        if (parts.Length != 3)
            return false;

        if (parts[0] != "1")
            return false;

        try
        {
            var salt = Convert.FromBase64String(parts[1]);
            var expectedHash = Convert.FromBase64String(parts[2]);

            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                600_000,
                HashAlgorithmName.SHA256,
                expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(
                actualHash,
                expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static void DeletePassword()
    {
        Delete(PasswordHash);
        OnPasswordChanged?.Invoke();
    }
}