# Cryptography

This cryptography client uses symmetric key encryption to encrypt local files. The local files original path is used as an initialization vector in order to introduce randomness so two files don't produce the same result making the cryptography resilient to brute forcing, because even if the original file name were to be unchanged and known it requires a direct attack per file or knowing the password. Hashes refer to this type of randomness as a `salt`.

## Justification
This project exists, because I attempted to use open source AES cryptography, but I was prompted for my root password. Security shouldn't be a compromise.

## Usage
    C:\Users\peon\Desktop\projects\SlideshowCreator\Cryptography\CryptographyClient\bin\Debug>CryptographyClient
    CAUTION: Encrypt only which you can suffer the loss of e.g. backups or transient communications. This software is licensed under Apache-2.0 and provides no warranty of any kind (unless required by law).

    Usage: CryptographyClient.exe [FILE_NAME_FULL_PATH] [PASSWORD] [PASSWORD AGAIN]

    Only the following arguments may be provided and they are required: FILE_NAME_FULL_PATH, PASSWORD PASSWORD AGAIN

## Encrypt
    C:\Users\peon\Desktop\projects\SlideshowCreator\Cryptography\CryptographyClient\bin\Debug>CryptographyClient C:\Users\peon\Desktop\crypto-test.txt peacock peacock
    New encrypted file created C:\Users\peon\Desktop\crypto-test.txt.encrypted

**File Contents**
>/E6ERv2k45ZQ1HN4gWmGvA==

## Decrypt
    C:\Users\peon\Desktop\projects\SlideshowCreator\Cryptography\CryptographyClient\bin\Debug>CryptographyClient C:\Users\peon\Desktop\crypto-test.txt.encrypted peacock peacock
    New decrypted file created C:\Users\peon\Desktop\crypto-test.txt.decrypted-2017-08-26

**File Contets**
>Take care.