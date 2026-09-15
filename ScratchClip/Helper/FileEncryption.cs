using System;
using System.Security.Cryptography;

namespace ScratchClip.Helper;

public static class FileEncryption
{
    private const byte Version = 1;

    private const int SaltSize = 16;
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public static byte[] Encrypt(
        byte[] plaintext,
        string? password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);

        var keyringSecret =
            ApplicationKeyStore.GetOrCreateEncryptionSecret();

        try
        {
            var key =
                ApplicationKeyStore.DeriveEncryptionKey(
                    password,
                    keyringSecret,
                    salt);

            var nonce = RandomNumberGenerator.GetBytes(NonceSize);
            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[TagSize];

            using var aes = new AesGcm(key, TagSize);

            aes.Encrypt(
                nonce,
                plaintext,
                ciphertext,
                tag);

            /*
                 * File format:
                 *
                 * [version: 1]
                 * [salt: 16]
                 * [nonce: 12]
                 * [tag: 16]
                 * [ciphertext: N]
                 */

            var result = new byte[
                1 +
                SaltSize +
                NonceSize +
                TagSize +
                ciphertext.Length];

            var offset = 0;

            result[offset++] = Version;

            Buffer.BlockCopy(
                salt,
                0,
                result,
                offset,
                SaltSize);

            offset += SaltSize;

            Buffer.BlockCopy(
                nonce,
                0,
                result,
                offset,
                NonceSize);

            offset += NonceSize;

            Buffer.BlockCopy(
                tag,
                0,
                result,
                offset,
                TagSize);

            offset += TagSize;

            Buffer.BlockCopy(
                ciphertext,
                0,
                result,
                offset,
                ciphertext.Length);

            CryptographicOperations.ZeroMemory(key);

            return result;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(keyringSecret);
            CryptographicOperations.ZeroMemory(salt);
        }
    }

    public static byte[] Decrypt(
        byte[] encrypted,
        string? password)
    {
        const int HeaderSize =
            1 +
            SaltSize +
            NonceSize +
            TagSize;

        if (encrypted.Length < HeaderSize)
        {
            throw new CryptographicException(
                "Invalid encrypted file.");
        }

        var offset = 0;

        var version = encrypted[offset++];

        if (version != Version)
        {
            throw new CryptographicException(
                "Unsupported encryption version.");
        }

        var salt = encrypted.AsSpan(
            offset,
            SaltSize);

        offset += SaltSize;

        var nonce = encrypted.AsSpan(
            offset,
            NonceSize);

        offset += NonceSize;

        var tag = encrypted.AsSpan(
            offset,
            TagSize);

        offset += TagSize;

        var ciphertext = encrypted.AsSpan(offset);

        var keyringSecret =
            ApplicationKeyStore.GetOrCreateEncryptionSecret();

        try
        {
            var key =
                ApplicationKeyStore.DeriveEncryptionKey(
                    password,
                    keyringSecret,
                    salt);

            try
            {
                var plaintext = new byte[ciphertext.Length];

                using var aes = new AesGcm(key, TagSize);

                aes.Decrypt(
                    nonce,
                    ciphertext,
                    tag,
                    plaintext);

                return plaintext;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(key);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(keyringSecret);
        }
    }
}