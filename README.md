# Dokobit Documents Gateway .NET example

## How to start?
Check more documentation at  [https://gateway-sandbox.dokobit.com/api/doc](https://gateway-sandbox.dokobit.com/api/doc).

Request developer access token [here](https://www.dokobit.com/developers/request-token).

Check Mobile ID and Smart-ID test data [here](https://www.dokobit.com/developers/testing).

## Example configuration
- Enter API access token (accessToken) to Program.cs:21
- Enter callback url (callbaclkUrl) to Program.cs:28

Enter your file name, url and SHA256 digest in `Main` and update `CreateSigning` method with apropiate content values (or use testing ones provided).

## Console command
To run this example, execute it like this: `dokobit_gateway_example {command} {token}` where `command` is:
* `upload_file` - Upload file and get `token` ([#api](https://gateway-sandbox.dokobit.com/api/doc#_api_upload))
* `check_file_status` - Check file status (`token` required) ([#api](https://gateway-sandbox.dokobit.com/api/doc#_api_upload_status))
* `create_signing` - Create new signing (`token` required) ([#api](https://gateway-sandbox.dokobit.com/api/doc#_api_signing_create))
* `demo` (default - all of above)

## Flow

### Upload file
- Upload file you want to sign* and get uploaded file token.
- Check file upload status. If status `uploaded`\*\*, continue.

\* You should provide file URL which would be accessible for Documents Gateway.
\*\* File status must be checked before creating signing.

- See `UploadFile` method for example or use console command `upload_file` for uploading file.
- See `CheckFileStatus` method for example or use console command `check_file_status` for checking file status **(use token as second parameter)**.

### Create signing
- Use file token provided with file upload response.
- Add as many signers as you need.

- See `CreateSigning` method for example or use console command `create_signing` for creating signing  **(use token as second parameter)**.

### Sign
Signing URL formation: https://gateway-sandbox.dokobit.com/signing/SIGNING_TOKEN?access_token=SIGNER_ACCESS_TOKEN.
URL is unique for each signer.
`SIGNING_TOKEN`: token received with `signing/create` API call response.
`SIGNER_ACCESS_TOKEN`: token received with `signing/create` API call response as parameter `signers`.
Signers represented as associative array where key is signer's unique identifier - personal code.

Navigate to signing URL, sign document.

### Retrieving signed document
After successful signing, you have two ways to get the signed file.
#### Via postback url
Postback calls are trigered, if `postback_url` was set while creating signing.

There are four types of postback calls:

1. `signer_signed` - after signer has signed document.
2. `signing_completed` - after signing has been completed (all signers successfully signed).
3. `signing_archived` - after document was archived (for signings with PADES-LTV and XADES-XL levels only).
4. `signing_archive_failed` - after document couldn't be archived (for signings with PADES-LTV and XADES-XL levels only).

After each signature, a request to the specified endpoint with signer information and signed document will be made.

#### Via JavaScript callback
If you want to have JavaScript events, add its support by following the instructions [here](https://gateway-sandbox.dokobit.com/api/iframe-integration).

After receiving "onSignSuccess" callback, you can request signing status from your backend by making GET request to [/api/signing/SIGNING_TOKEN/status.json](https://gateway-sandbox.dokobit.com/api/doc#_api_signing_status) and fetch signed document by using "file" parameter in the response.
