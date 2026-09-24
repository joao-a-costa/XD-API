using Newtonsoft.Json;
using System;
using System.IO;
using XDPeople.Data;
using XDPeople.Framework;
using XDPeople.License;
using XDPeople.Utils;

namespace API_EXAMPLE
{
    class Program
    {
        static void Main(string[] args)
        {
            string pathToLicense = @"C:\XDSoftware\cfg\";
            Constant.PathConfig = Constant.PathLicense = pathToLicense;
            Constant.IniFile = "xd.ini";
            Constant.MyXDPublicKey = $@"{Constant.PathConfig}xd.pem";
            IniFile.Load();
            XDPeople.Utils.Localization.Language = new Languages(IniFile.Lang);
            XDLicence.Load(Constant.PathLicense + Constant.LicFile, XDProg.XDGC);
            XDLicence.DefaultXDProg = XDProg.XDGC;
            Db.Configure();

            if (Db.Connect() == 0)
            {

                Db.InitializeDatabase(null, true, XDLicence.XDProgram, (CountryCode)XDLicence.ClientCountryCode,
                           XDLicence.ClientVat);
                GlobalVars.LoadTerminalConfig();
                GlobalVars.LoadCurrency();
                GlobalVars.LoadListEntities();
                GlobalVars.LoadInitialConfigurations(false, true);
                SystemSettings.CurrentUser = GlobalVars.GlobalListEmployee[0];

                RunMenu();
            }
        }

        private const string SampleOrderJson = @"{""documentType"":""FAC"",""order"":{""id"":26,""returned"":0,""idUser"":1,""idClient"":1,""idAddress"":0,""order_status"":""new"",""total"":""59.02"",""nrArticles"":2,""payment_date"":null,""notes"":null,""idShipping"":3,""shipping_value"":""8.00"",""sent_date"":null,""tracking_number"":null,""created_at"":""2019-04-16 14:18:19"",""updated_at"":""2019-04-16 14:18:19""},""order_info"":{""id"":26,""idOrder"":26,""address"":"""",""address_2"":"""",""village"":"""",""postalCode"":"""",""phone"":"""",""cellPhone"":"""",""fax"":"""",""client_id"":1,""client_name"":""Consumidor Final"",""client_surname"":"""",""client_email"":"""",""client_nif"":0,""resseller_id"":null,""resseller_name"":null,""resseller_surname"":null,""resseller_email"":null,""resseller_nif"":null,""idPaymentType"":1,""idPaymentConditions"":1,""dateTimeDelivery"":""2019-04-17 00:00:00"",""created_at"":""2019-04-16 14:18:19"",""updated_at"":""2019-04-16 14:18:19""},""nr_order_lines"":2,""order_lines"":[{""id"":51,""returned"":0,""idOrder"":26,""idProduct"":""208"",""idProductFather"":0,""reference"":""itemWithoutAttributos2"",""sub_reference"":"""",""quantity"":1,""product_value"":""18.79"",""product_vat_value"":""4.32"",""row_total_value"":""18.79"",""row_total_vat_value"":""4.32"",""comission"":""0.00"",""extra_comission"":""0.00"",""shipping_wheight"":null,""created_at"":""2019-04-16 14:18:19"",""updated_at"":""2019-04-16 14:18:19""},{""id"":52,""returned"":0,""idOrder"":26,""idProduct"":""795"",""idProductFather"":0,""reference"":""SA400S376/120G"",""sub_reference"":"""",""quantity"":1,""product_value"":""40.23"",""product_vat_value"":""9.25"",""row_total_value"":""40.23"",""row_total_vat_value"":""9.25"",""comission"":""0.00"",""extra_comission"":""0.00"",""shipping_wheight"":null,""created_at"":""2019-04-16 14:18:19"",""updated_at"":""2019-04-16 14:18:19""}],""order_payment"":{""id"":26,""idOrder"":26,""total"":""59.02"",""payment_status"":""pending"",""payment_method"":""D"",""payment_date"":""0000-00-00 00:00:00"",""notes"":null,""idPaypal_transaction"":null,""idPaypal_buyer"":null,""paypal_token"":null,""epEntity"":null,""epSubEntity"":null,""epReference"":null,""epLink"":"""",""created_at"":""2019-04-16 14:18:19"",""updated_at"":""2019-04-16 14:18:19""}}";

        private class AppSettings
        {
            public string LastDocumentType { get; set; }
            public int? LastSerieId { get; set; }
            public System.Collections.Generic.List<string> LastItemReferences { get; set; }
        }

        private static readonly string SettingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        private static AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                    return JsonConvert.DeserializeObject<AppSettings>(File.ReadAllText(SettingsFilePath)) ?? new AppSettings();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not load settings: {ex.Message}");
            }

            return new AppSettings();
        }

        private static void SaveSettings(AppSettings settings)
        {
            try
            {
                File.WriteAllText(SettingsFilePath, JsonConvert.SerializeObject(settings));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not save settings: {ex.Message}");
            }
        }

        private static void RunMenu()
        {
            bool exit = false;
            AppSettings settings = LoadSettings();

            while (!exit)
            {
                Console.WriteLine();
                Console.WriteLine("Select an example to run:");
                Console.WriteLine(" 1 - GenerateDocument");
                Console.WriteLine(" 2 - GenerateExternalySignedDocument");
                Console.WriteLine(" 3 - GenerateReceiptDocument");
                Console.WriteLine(" 4 - NewCustomer");
                Console.WriteLine(" 5 - NewItem");
                Console.WriteLine(" 6 - GetItem");
                Console.WriteLine(" 7 - GetUserPicture");
                Console.WriteLine(" 0 - Exit");
                Console.Write("Option: ");

                string option = Console.ReadLine();

                try
                {
                    switch (option)
                    {
                        case "1":
                            Console.Write($"Document Type{(string.IsNullOrEmpty(settings.LastDocumentType) ? "" : $" [{settings.LastDocumentType}]")}: ");
                            string docTypeInput = Console.ReadLine();
                            string docTypeId = string.IsNullOrWhiteSpace(docTypeInput) ? settings.LastDocumentType : docTypeInput;

                            Console.Write($"Serie Id{(settings.LastSerieId.HasValue ? $" [{settings.LastSerieId}]" : "")}: ");
                            string serieInput = Console.ReadLine();
                            int? docSerieId = string.IsNullOrWhiteSpace(serieInput)
                                ? settings.LastSerieId
                                : (int.TryParse(serieInput, out int parsedSerieId) ? (int?)parsedSerieId : null);

                            if (string.IsNullOrEmpty(docTypeId))
                            {
                                Console.WriteLine("Document type is required.");
                                break;
                            }

                            if (!docSerieId.HasValue)
                            {
                                Console.WriteLine("Invalid serie id.");
                                break;
                            }

                            OrderData order = JsonConvert.DeserializeObject<OrderData>(SampleOrderJson);
                            int nrOrderLines = order.NrOrderLines;
                            var itemReferences = new System.Collections.Generic.List<string>();

                            for (int i = 0; i < nrOrderLines; i++)
                            {
                                string lastReference = settings.LastItemReferences != null && i < settings.LastItemReferences.Count
                                    ? settings.LastItemReferences[i]
                                    : null;

                                Console.Write($"Item Reference (line {i + 1}){(string.IsNullOrEmpty(lastReference) ? "" : $" [{lastReference}]")}: ");
                                string referenceInput = Console.ReadLine();
                                itemReferences.Add(string.IsNullOrWhiteSpace(referenceInput) ? lastReference : referenceInput);
                            }

                            if (itemReferences.Exists(r => string.IsNullOrEmpty(r)))
                            {
                                Console.WriteLine("Item reference is required for every line.");
                                break;
                            }

                            examples.GenerateDocument(order, docTypeId, docSerieId.Value, itemReferences.ToArray());

                            settings.LastDocumentType = docTypeId;
                            settings.LastSerieId = docSerieId;
                            settings.LastItemReferences = itemReferences;
                            SaveSettings(settings);
                            break;
                        case "2":
                            examples.GenerateExternalySignedDocument(JsonConvert.DeserializeObject<OrderData>(SampleOrderJson));
                            break;
                        case "3":
                            Console.Write("Entity KeyId: ");
                            examples.GenerateReceiptDocument(Console.ReadLine());
                            break;
                        case "4":
                            examples.NewCustomer();
                            break;
                        case "5":
                            examples.NewItem();
                            break;
                        case "6":
                            Console.Write("Item KeyId: ");
                            var item = examples.GetItem(Console.ReadLine());
                            Console.WriteLine(item != null
                                ? $"Item: {item.Description}, Discontinued: {item.Discontinued}"
                                : "Item not found.");
                            break;
                        case "7":
                            Console.Write("User Id: ");
                            if (int.TryParse(Console.ReadLine(), out int userId))
                            {
                                var picture = new examples().GetUserPicture(userId);
                                Console.WriteLine(picture != null ? "Picture retrieved." : "No picture found.");
                            }
                            else
                            {
                                Console.WriteLine("Invalid user id.");
                            }
                            break;
                        case "0":
                            exit = true;
                            break;
                        default:
                            Console.WriteLine("Invalid option.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }
    }
}