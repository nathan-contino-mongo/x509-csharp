using System.Collections.Generic;

using System.Linq;

using System.Text;

using System.IO;

using System.Net.Security;



using MongoDB.Bson;

using MongoDB.Driver;

using MongoDB.Driver.Core;

using System;

using System.Threading.Tasks;



using System.Security.Cryptography.X509Certificates;

using System.Security.Cryptography;

using System.Security.Permissions;



namespace WorkingWithMongoDB

{

    public class Entity

    {

        public ObjectId Id { get; set; }

        public string Name { get; set; }

    }

    class Program

    {

        static void Main(string[] args)

        {

            MainAsync().Wait();

            Console.WriteLine("done");

        }



        static async Task MainAsync()

        {

            // EXAMPLE PULLED FROM THE DOCS: https://mongodb.github.io/mongo-csharp-driver/2.8/reference/driver/authentication/

            // does not work

            var client_cert = new X509Certificate2("client-certificate.pfx", "password");



            var docsExampleVersionOfSettings = new MongoClientSettings

            {

                Credentials = new[] 

                { // i just pulled this DN (username for x509) from openssl using: openssl pkcs12 -info -in client-certificate.pfx 

                    MongoCredential.CreateMongoX509Credential("OU=TestClientCertificateOrgUnit, O=TestClientCertificateOrg, L=TestClientCertificateLocality, ST=TestClientCertificateState")

                }, // note that this DN also matches the DN/subject printed out using cert.ToString()

                SslSettings = new SslSettings

                {

                    ClientCertificates = new[] { client_cert },

                },

                UseSsl = true

            };







            // working example starting with a connection string, and avoiding the Credential part of the setting object entirely

            var connectionString = "mongodb://localmongo1/?authMechanism=MONGODB-X509&tls=true";

            var connectionStringPlusObjectSettings = MongoClientSettings.FromConnectionString(connectionString);

            var cert = new X509Certificate2("client-certificate.pfx", "password");

            Console.WriteLine(cert.ToString());

            Console.WriteLine(cert.PrivateKey!=null ? "contains private key" : "does not contain private key"); // verify that there's a private key in the cert, docs tell us we need this

            var sslSettings = new SslSettings {

                    ClientCertificates = new List<X509Certificate>()

                    {

                        new X509Certificate2("client-certificate.pfx", "password")

                    }

            };

            connectionStringPlusObjectSettings.AllowInsecureTls = true; // for testing using self-signed certs, use this option to skip validation. DO NOT USE THIS OPTION FOR PRODUCTION USES

            connectionStringPlusObjectSettings.SslSettings = sslSettings;







            // just to demonstrate the difference between what mongod uses (order + spacing) vs openssl/c# version of the string:

            var from_mongod_logs_str = "OU=TestClientCertificateOrgUnit,O=TestClientCertificateOrg,L=TestClientCertificateLocality,ST=TestClientCertificateState";

            var from_openssl_str = "ST = TestClientCertificateState, L = TestClientCertificateLocality, O = TestClientCertificateOrg, OU = TestClientCertificateOrgUnit";

            var from_csharp_str = "OU=TestClientCertificateOrgUnit, O=TestClientCertificateOrg, L=TestClientCertificateLocality, S=TestClientCertificateState";

            // note that C# adds extra spaces and uses S instead of ST; openssl adds even more extra spaces and puts values in a completely different order.



            // suggested example to replace the existing docs example. Should work as long as the certificate is valid!

            var settingObjectOnlySettings = new MongoClientSettings 

            {

                Credential =  MongoCredential.CreateMongoX509Credential(null),

                SslSettings = new SslSettings

                {

                    ClientCertificates = new List<X509Certificate>()

                    {

                        new X509Certificate2("client-certificate.pfx", "password")

                    },

                },

                UseTls = true,

                Server = new MongoServerAddress("localmongo1", 27017),

                AllowInsecureTls = true // for testing using self-signed certs, use this option to skip validation. DO NOT USE THIS OPTION FOR PRODUCTION USES

            };



            var client = new MongoClient(settingObjectOnlySettings); // suggested fixed example

            //var client = new MongoClient(connectionStringPlusObjectSettings); // x509 auth activated via connection string

            //var client = new MongoClient(docsExampleVersionOfSettings); // example from the docs, in case you want to play around with it

            

            // just doing a quick read + insert to verify the usability of this connection

            var database = client.GetDatabase("test");

            var collection = database.GetCollection<BsonDocument>("stuff");

            

            var allItemsInCollection = await collection.Find(new BsonDocument()).ToListAsync();

            Console.WriteLine(allItemsInCollection.Count);



            var entity = new BsonDocument {{ "count", allItemsInCollection.Count } };

            collection.InsertOne(entity);

            Console.WriteLine("wrote " + entity + " to DB");



            /*

             *  Driver suggestions:

             *  https://api.mongodb.com/csharp/2.2/html/T_MongoDB_Driver_MongoCredential.htm

             *     - the X509Credential arguments are missing for some reason from the API docs MongoCredential page (above)

             *  https://mongodb.github.io/mongo-csharp-driver/2.8/reference/driver/authentication/

             *     - the x.509 auth portion of this page has 3 problems:

             *           1) Credentials is deprecated, replaced by Credential

             *           2) useSsl is deprecated, should be  useTls

             *           3) the value of Credential (created by MongoCredential.CreateMongoX509Credential(connectionStr)) must

             *              match the DN of the certificate EXACTLY as interpreted by mongodb. Unfortunately this means it must

             *              match spacing that openSSL/C# do not necessarily include when one prints out a pfx file with commands

             *              like ``openssl pkcs12 -info -in client-certificate.pfx``.

             *

             *           Two solutions to #3 for our documentation:

             *           1) set connectionStr to null and this snippet will work just fine

             *           2) set the authentication mechanism to x509 in a connection string, then import

             *              that connection string into a settings object using MongoClientSettings.FromConnectionString,

             *              then add the certificate to the SslSettings. This lets you avoid setting a Credential entirely

             *

             *   Personally I would simply recommend removing/deprecating the argument

             *   from CreateMongoX509Credential entirely, as it was only a source of

             *   pain for me when trying to use X.509 with the drivers.

             *

             */

        }

    }

}


