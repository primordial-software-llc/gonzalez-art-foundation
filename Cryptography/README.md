# Cryptography

AES symmetric key cryptography file encryption client. Files are encrypted using a password and the file name. **The file name can't be changed after encryption or the file will not decrypt**.

## Usage
CAUTION: Encrypt only which you can suffer the loss of e.g. backups or transient communications. This software is licensed under Apache-2.0 and provides no warranty of any kind (unless required by law).

    CryptographyClientV1

    Usage: CryptographyClient.exe [FILE_NAME_FULL_PATH] [PASSWORD] [PASSWORD AGAIN]

    Only the following arguments may be provided and they are required: FILE_NAME_FULL_PATH, PASSWORD PASSWORD AGAIN

## Encrypt
    CryptographyClientV1 crypto-test.txt peacock peacock
    New encrypted file created crypto-test.txt.encrypted

**File Contents**
>/E6ERv2k45ZQ1HN4gWmGvA==

## Decrypt
    CryptographyClientV1 crypto-test.txt.encrypted peacock peacock
    New decrypted file created crypto-test.txt.decrypted-2017-12-19T20-16-55Z

**File Contets**
>Take care.