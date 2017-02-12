# iSign.io Gateway API .NET example

## How to start? 

Check more documentation at  [https://gateway-sandbox.isign.io/api/doc](https://gateway-sandbox.isign.io/api/doc).

Request developer access token [here](https://www.isign.io/services/contacts#request-access).

## Example configuration
- Enter API access token (accessToken) to Program.cs:21
- Enter callback url (callbaclkUrl) to Program.cs:28

Enter your file name, url and SHA1 digest in `Main` and update `CreateSigning` method with apropiate content values (or use testing ones provided).

Build project & Run.

## Console command

To run this example, execute it like this: `isign_gateway_example {command} {token}` where `command` is:
* `upload_file` - Upload file and get `token` ([#api](https://gateway-sandbox.isign.io/api/doc#_api_upload))
* `check_file_status` - Check file status (`token` required) ([#api](https://gateway-sandbox.isign.io/api/doc#_api_upload_status)) 
* `create_signing` - Create new signing (`token` required) ([#api](https://gateway-sandbox.isign.io/api/doc#_api_signing_create))
* `demo` (default - all of above)

## Flow

### Upload file
- Upload file you want to sign* and get uploaded file token.
- Check file upload status. If status `uploaded`\*\*, continue.

\* You should provide file URL which would be accessible for Gateway API.  
\*\* File status must be checked before creating signing.

- See `UploadFile` method for example or use console command `upload_file` for uploading file. 
- See `CheckFileStatus` method for example or use console command `check_file_status` for checking file status **(use token as second parameter)**.

### Create signing
- Use file token provided with file upload response.
- Add as many signers as you need.

- See `CreateSigning` method for example or use console command `create_signing` for creating signing  **(use token as second parameter)**.

### Sign
Signing URL formation: https://gateway-sandbox.isign.io/signing/SIGNING_TOKEN?access_token=SIGNER_ACCESS_TOKEN.
URL is unique for each signer.  
`SIGNING_TOKEN`: token received with `signing/create` API call response.  
`SIGNER_ACCESS_TOKEN`: token received with `signing/create` API call response as parameter `signers`.  
Signers represented as associative array where key is signer's unique identifier - personal code.  

Navigate to signing URL, sign document.  


### Retrieving signed document
After document signing postback calls are trigered, if `callbaclkUrl` was set while creating signing.  
There are two types of postback calls:
1. After signer has signed document - `signer_signed`.
2. After signing has been completed (all signers successfully signed) - `signing_completed`.

Information about signed document will be sent to postback URL. See [signing-finished-postback.php](https://github.com/isign/gateway-php-example/blob/master/signing-finished-postback.php) for more information.
