using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace iSignGatewayNetExample
{
    class Program
    {
        public class Api
        {
            //Enter Your iSign.io API access token
            public static string accessToken = "";
 
            // Api url
            public static string apiUrl = "https://gateway-sandbox.dokobit.com";

            // Callback urls
            public static string callbaclkUrl = "http://yourhost/gateway-php-example/signing-finished-postback.php";
        }


        [DataContract]
        public class Response
        {
            [DataMember(Name = "status")]
            public string Status { get; set; }
            [DataMember(IsRequired = false, Name = "message")]
            public string Message { get; set; }
            [DataMember(IsRequired = false, Name = "errors")]
            public IEnumerable<string> Errors { get; set; }
            [DataMember(IsRequired = false, Name = "code")]
            public string Code { get; set; }
        }

        [DataContract, KnownType(typeof(Response))]
        public class UploadResponse : Response
        {
            [DataMember(IsRequired = false, Name = "token")]
            public string Token { get; set; }
        }

        [DataContract, KnownType(typeof(UploadResponse))]
        public class CreateSigningResponse : UploadResponse
        {
            [DataMember(IsRequired = false, Name = "signers")]
            public Dictionary<string, string> Signers { get; set; }
        }

        /// <summary>
        /// Console write line helper
        /// </summary>
        public static void WriteLine(string tag, string text)
        {
            Console.WriteLine(String.Format("[{0}] {1}", tag, text));
        }

        /// <summary>
        /// Print response object
        /// </summary>
        /// <param name="response">Response object</param>
        public static void printResponse(Response response, string tag)
        {
            if (response != null)
            {
                if (response.Status != null && response.Status != "ok")
                {
                    WriteLine(tag, "Status: " + response.Status);
                }

                if (response.Message != null)
                {
                    WriteLine(tag, "Message: " + response.Message);
                }

                if (response.Errors != null && response.Errors.Count() > 0)
                {
                    WriteLine(tag, "Errors:");
                    foreach (var error in response.Errors)
                    {
                        WriteLine(tag, "\t" + error);
                    }
                }
            }
            else
            {
                WriteLine(tag, "Failed to receive response\n");
            }
        }

        /// <summary>
        /// Upload fileUrl with fileName and SHA1 fileDigest
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <param name="fileUrl">File remote url</param>
        /// <param name="fileDigest">File SHA1 digest</param>
        /// <returns>Upload response object</returns>
        public static UploadResponse UploadFile(string fileName, string fileUrl, string fileDigest)
        {
            using (var client = new HttpClient())
            {
                using (var content =
                    new MultipartFormDataContent("Upload----" + DateTime.Now))
                {
                    content.Add(new StringContent(fileName), "file[name]");
                    content.Add(new StringContent(fileUrl), "file[url]");
                    content.Add(new StringContent(fileDigest), "file[digest]");

                    using (
                        var message =
                            client.PostAsync(Api.apiUrl + "/api/upload.json?access_token=" + Api.accessToken,
                                content))
                    {
                        var input = message.Result;
                        var serializator = new DataContractJsonSerializer(typeof(UploadResponse));
                        return (UploadResponse)serializator.ReadObject(input.Content.ReadAsStreamAsync().Result);
                    }
                }
            }
        }

        /// <summary>
        /// Request to upload file. Wraps UploadFile method with some output
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <param name="fileUrl">File remote url</param>
        /// <param name="fileDigest">File SHA1 digest</param>
        /// <returns>Received token or empty string</returns>
        public static string RequestUploadFile(string fileName, string fileUrl, string fileDigest)
        {
            const string tag = "/api/file/upload.json";
            WriteLine(tag, String.Format("Uploading file: {0}\n\tUrl: {1}", fileName, fileUrl));

            // Upload request
            UploadResponse response = UploadFile(fileName, fileUrl, fileDigest);
            
            printResponse(response, tag);

            if (response != null && response.Status != "ok")
            {
                WriteLine(tag, "File could not be uploaded. Please ensure that file URL is accessible from the internet.\n");
            }
            else if (response != null)
            {
                WriteLine(tag, "Received token:");
                Console.WriteLine(response.Token + "\n");

                return response.Token;
            }

            return String.Empty;
        }

        /// <summary>
        /// Check uploaded file status by providing token 
        /// </summary>
        /// <param name="token">File token</param>
        /// <returns>Response object</returns>
        public static Response CheckFileStatus(string token)
        {
            using (var client = new HttpClient())
            {
                using (var message = client.GetAsync(Api.apiUrl + "/api/upload/status/" + token + ".json?access_token=" + Api.accessToken))
                {
                    var input = message.Result;
                    var serializator = new DataContractJsonSerializer(typeof(Response));
                    return (Response)serializator.ReadObject(input.Content.ReadAsStreamAsync().Result);
                }
            }
        }

        /// <summary>
        /// Request to check file status
        /// </summary>
        /// <param name="token">File token</param>
        /// <returns>True if file was successfully uploaded</returns>
        public static bool RequestCheckFileStatus(string token)
        {
            string tag = "/api/upload/{token}.json";
            WriteLine(tag, "Checking file status with token: ");
            Console.WriteLine(token);

            Response response = null;

            for (int i = 0; i < 30; i++)
            {
                WriteLine(tag, "Pending");

                // Check uploaded file
                response = CheckFileStatus(token);

                if (response.Status != "pending") break;
                Thread.Sleep(1000);
            }

            printResponse(response, tag);

            if (response.Status != "uploaded")
            {
                WriteLine(tag, "Gateway API could not download the file. Please ensure that file URL is accessible from the internet.\n");
            }
            else if (response != null)
            {
                WriteLine(tag, "File has been successfully uploaded.\n");
                return true;
            }

            return false;
        }

        public static CreateSigningResponse CreateSigning(string token, string signerUID, string postbackUrl)
        {
            using (var client = new HttpClient())
            {
                using (var content =
                    new MultipartFormDataContent("Create----" + DateTime.Now))
                {
                    // Signed document format. Check documentation for all available options.
                    content.Add(new StringContent("pdf"), "type");

                    // Signing name. Will be displayed as the main title.
                    content.Add(new StringContent("Agreement"), "name");

                    // Signer's unique identifier - personal code.
                    content.Add(new StringContent(signerUID), "signers[0][id]");

                    // Name
                    content.Add(new StringContent("Tester"), "signers[0][name]");       
                    // Surname
                    content.Add(new StringContent("Surname"), "signers[0][surname]");   
                    // Phone number. Optional.
                    content.Add(new StringContent("+37260000007"), "signers[0][phone]");
                    // Personal code. Optional.
                    content.Add(new StringContent("51001091072"), "signers[0][code]");
                    // Signing purpose. Availabe options listed in documentation.
                    content.Add(new StringContent("signature"), "signers[0][signing_purpose]");   
                    
                    content.Add(new StringContent(token), "files[0][token]"); // For 'pdf' type only one file is supported.
                    content.Add(new StringContent(postbackUrl), "postback_url"); 

                    using (
                        var message =
                            client.PostAsync(Api.apiUrl + "/api/signing/create.json?access_token=" + Api.accessToken,
                                content))
                    {
                        var input = message.Result;


                        var settings = new DataContractJsonSerializerSettings();
                        settings.UseSimpleDictionaryFormat = true;

                        var serializator = new DataContractJsonSerializer(typeof(CreateSigningResponse), settings);


                        return (CreateSigningResponse)serializator.ReadObject(input.Content.ReadAsStreamAsync().Result);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static bool RequestCreateSigning(string token, string signerUID)
        {
            string tag = " /api/signing/create.json";
            WriteLine(tag, "Creating new signing with token:");
            Console.WriteLine(token);

            CreateSigningResponse response = CreateSigning(token, signerUID, Api.callbaclkUrl); ;

            printResponse(response, tag);

            if (response != null && response.Status != "ok")
            {
                WriteLine(tag, "Signing could not be created.\n");
            }
            else if (response != null)
            {
                if (response.Signers.ContainsKey(signerUID))
                {
                    string signingUrl = Api.apiUrl.TrimEnd('/') + "/signing/" + response.Token + "?access_token=" + response.Signers[signerUID];

                    WriteLine(tag, "Signing successfully created:");
                    WriteLine(tag, signingUrl + "\n");

                    /*
                     * Signing url formation: <API_URL>/signing/<SIGNING_TOKEN>?access_token=<SIGNER_ACCESS_TOKEN>
                     * SIGNING_TOKEN: token received with 'signing/create' API call response.
                     * SIGNER_ACCESS_TOKEN: token received with 'signing/create' API call response as parameter 'signers'.
                     * Signers represented as associative array where key is signer's unique identifier - personal code.
                     */
                }
                else {
                    WriteLine(tag, "Signer not found: " + signerUID + "\n");
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Main
        /// </summary>
        /// <param name="args">Console arguments</param>
        static void Main(string[] args)
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            /**
             * File name of document you want to sign.
             */
            string fileName = "test.pdf";

            /**
             * HTTP URL where the file is stored.
             * Gateway API will download the file from given resource URL.
             * Ensure that file URL is accessible from internet.
             */
            string fileUrl = "https://developers.dokobit.com/sc/test.pdf";

            /**
             * SHA1 digest of file content.
             */
            string fileDigest = "a50edb61f4bbdce166b752dbd3d3c434fb2de1ab";

            /*
             * Signer UID
             */
            string signerUID = "51001091072";

            /*
             * Example application logic
             */
            if (Api.accessToken == String.Empty) { Console.WriteLine("Enter API access token to Program.cs:21"); return; }

            string command = args.Length > 0 ? args[0] : "demo";
            string token = args.Length > 1 ? args[1] : "some_token";
            Console.WriteLine("iSign.io API signing gateway example.\n");

            switch (command)
            {
                case "upload_file":
                    RequestUploadFile(fileName, fileUrl, fileDigest);
                    break;
                case "check_file_status":
                    RequestCheckFileStatus(token);
                    break;
                case "create_signing":
                    RequestCreateSigning(token, signerUID);
                    break;
                case "demo":
                    token = RequestUploadFile(fileName, fileUrl, fileDigest);

                    if (RequestCheckFileStatus(token))
                    {
                        RequestCreateSigning(token, signerUID);
                    }
                    break;
                default:
                    Console.WriteLine("Bad command argument");
                    break;
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}
