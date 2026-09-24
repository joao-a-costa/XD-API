using System;
using XDPeople.Data;
using XDPeople.Business;
using XDPeople.Entities;
using XDPeople.Utils;
using System.Drawing;
using System.Linq;
using XDPeople.Framework;
using Newtonsoft.Json;

namespace API_EXAMPLE
{
    class examples
    {
        private const string _DefaultPaymentMechanism = "ou";

        //copied from ItemTransactionController.LoadDefaultValues
        private static void LoadDefaultValues(ItemTransactionDocument document)
        {
            // TODO: Get from database
            if (document.CreateDate == DateTime.MinValue)
                document.CreateDate = DateTime.Now;

            if (document.PaymentType == null)
                document.PaymentType = GlobalVars.GlobalListPaymentType.Find(f =>
                f.PaymentMechanism.ToLower() == _DefaultPaymentMechanism.ToLower());

            if (document.PaymentMode == null)
                document.PaymentMode = GlobalVars.GlobalListPaymentMode.FirstOrDefault();

            if (string.IsNullOrEmpty(document.Currency?.KeyId))
                document.Currency = GlobalVars.GlobalCurrencyList.FirstOrDefault();

            if (document.SalesMan == null || string.IsNullOrEmpty(document.SalesMan.Name))
                document.SalesMan =
                    document.SalesMan?.Id == null || document.SalesMan?.Id == 1 ?
                    GlobalVars.GlobalListEmployee.FirstOrDefault(fod => !fod.Inactive)
                    : GlobalVars.GlobalListEmployee.FirstOrDefault(fod => fod.Id == document.SalesMan.Id)
                        ?? GlobalVars.GlobalListEmployee.FirstOrDefault(fod => !fod.Inactive);

            if (document.TaxRegion == null || (document.TaxRegion != null && document.TaxRegion.Id == 0))
                document.TaxRegion = GlobalVars.GlobalListRegions.FirstOrDefault(x => x.Id == GlobalVars.GlobalXConfigBE.RegionId);

            if (document.Address?.City == null && !string.IsNullOrEmpty(document.EntityId))
            {
                var entity = GlobalVars.GlobalDictionaryEntities[document.EntityId];

                if (entity != null)
                {
                    if (string.IsNullOrEmpty(document.Entity.Name))
                        document.Entity.Name = document.EntityName;
                    document.Address = new XDPeople.Entities.Address
                    {
                        Id = entity.Guid,
                        City = entity.City,
                        Country = entity.Country,
                        PostalCode = entity.PostalCode,
                        Coordinates = null,
                        Line1 = entity.Address,
                        Line2 = entity.AddressLine2,
                        StateOrProvince = string.Empty
                    };

                    if (document.LoadPlace?.City == null)
                        document.LoadPlace = document.Address;
                    if (document.UnloadPlace?.City == null)
                        document.UnloadPlace = document.Address;
                }
            }

            if (string.IsNullOrEmpty(document.Account.Name) && !string.IsNullOrEmpty(document.Entity.Name))
                document.Account.Name = document.Entity.Name;


            if (document.LoadPlace?.City == null && !string.IsNullOrEmpty(document.EntityId))
            {
                var entity = GlobalVars.GlobalDictionaryEntities[document.EntityId];

                if (entity != null)
                {
                    if (string.IsNullOrEmpty(document.Entity.Name))
                        document.Entity.Name = document.EntityName;
                    document.Address = new XDPeople.Entities.Address
                    {
                        Id = entity.Guid,
                        City = entity.City,
                        Country = entity.Country,
                        PostalCode = entity.PostalCode,
                        Coordinates = null,
                        Line1 = entity.Address,
                        Line2 = entity.AddressLine2,
                        StateOrProvince = string.Empty
                    };
                }
            }

            document.Details.ToList().ForEach(fe =>
            {
                if (fe.Guid == Guid.Empty)
                    fe.Guid = Guid.NewGuid();

                if (fe.OriginWarehouse == 0)
                {
                    var firstWarehosue = GlobalVars.GlobalListWarehouses.FirstOrDefault();
                    if (firstWarehosue != null)
                        fe.OriginWarehouse = firstWarehosue.Id;
                }

                if (fe.TaxValue?.Id == 0 && fe.DefaultTaxId == 0)
                {
                    var defaultTaxId = GlobalVars.GlobalListTaxes.FirstOrDefault();

                    if (defaultTaxId != null)
                        fe.DefaultTaxId = defaultTaxId.Id;
                }
                else
                {
                    fe.TaxValue.Tax = GlobalVars.GlobalListTaxes.FirstOrDefault(fod => fod.Id == fe.TaxValue.Id).Tax;
                }

                // If is tax included and unit price is 0, then set the unit price to the net price
                if (fe.IsTaxIncluded && fe.UnitPrice == 0 && fe.Price.NetPrice > 0)
                    fe.UnitPrice = fe.Price.NetPrice;

            });
        }

        public static void GenerateDocument(OrderData orderObjectData, string documentTypeId, int serieId, string[] itemReferences, decimal shippingTaxRate = 23m)
        {
            //validate document type, same as ItemTransactionController.PostItemTransaction
            XConfigDocumentsTypesBE docConfig = GlobalVars.GlobalListXConfigDocumentsTypes
                .Find(x => x.KeyId.Equals(documentTypeId, StringComparison.InvariantCultureIgnoreCase));

            if (docConfig == null)
                throw new Exception($"Document type not found: {documentTypeId}");

            EDocumentType documentType = (EDocumentType)docConfig.DocumentType;

            if (documentType != EDocumentType.Sales)
                throw new Exception($"Invalid document type: {documentTypeId}");

            //validate serie, same as GenerateExternalySignedDocument
            XConfigDocumentsSeriesBE serie = GlobalVars.GlobalListXConfigDocumentsSeries.FirstOrDefault(x => x.Id == serieId);

            if (serie == null)
                throw new Exception("Serie not found");

            SalesDocumentManager manager = new SalesDocumentManager(Db.CurrentDatabase, documentType);

            string idClient = orderObjectData.Order.IdUser;
            int rows = orderObjectData.NrOrderLines;

            manager.Init(documentTypeId, serieId);

            //resolve the customer from the cache, same as PostItemTransaction
            if (!GlobalVars.GlobalDictionaryEntities.ContainsKey(idClient))
                throw new Exception($"Entity not found: {idClient}");

            Entity entity = GlobalVars.GlobalDictionaryEntities[idClient];

            manager.SetEntity(entity);

            SalesDocumentDetailManager detailManager = manager.DetailManager;

            detailManager.Init(false, false);

            for (int i = 0; i < rows; i++)
            {
                if (i > 0)
                {
                    detailManager.Init();
                }

                string KeyId = itemReferences[i];
                decimal quantity = orderObjectData.OrderLines[i].Quantity;
                decimal price = orderObjectData.OrderLines[i].ProductValue;

                detailManager.SetItemID(KeyId);
                detailManager.SetQuantity(quantity);
                detailManager.SetPrice(price);

                if (i > 0)
                {
                    detailManager.Commit(false, false);
                }
                else
                {
                    detailManager.Commit(false, true);
                }
            }

            LoadDefaultValues(manager.CurrentDocument);

            //shipment costs: order shipping_value is tax included, the document needs both
            //the net (ShipmentNetCosts) and the tax included (ShipmentCosts) values,
            //otherwise documents configured to show net values display the shipment as 0
            decimal shippingValue = orderObjectData.Order.ShippingValue;

            if (shippingValue > 0)
            {
                ItemTransactionDocument document = manager.CurrentDocument;

                document.ShipmentCosts = shippingValue;
                document.ShipmentNetCosts = Math.Round(shippingValue / (1 + shippingTaxRate / 100), 2);
            }

            //recalculate totals after the shipment costs are set
            manager.Calculate();

            try
            {
                manager.Validate();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Document cannot be saved because:");
                Console.WriteLine(ex.Message);
                return;
            }

            try
            {
                ItemTransactionDocument document = manager.CurrentDocument;

                //debug only: serializes the document before saving so it can be inspected with a breakpoint,
                //json isn't used anywhere
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    Error = (sender, args) => { args.ErrorContext.Handled = true; }
                };

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(document, settings);

                if (manager.Save())
                {
                    Console.WriteLine("Sucess");
                }
                else
                {
                    Console.WriteLine("Failed to save document");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saving:");
                Console.WriteLine(ex.Message);
            }

        }

        public static void GenerateExternalySignedDocument(OrderData orderObjectData)
        {

            //initialize a new data context for transactions purposes
            DbUniversal dataContext = Db.CurrentDatabase.Clone();

            SalesDocumentManager manager = new SalesDocumentManager(dataContext, EDocumentType.Sales, true);
            SalesDocumentProvider documentProvider = new SalesDocumentProvider(dataContext);
            SalesDocumentDetailManager detailManager = manager.DetailManager;

            string documentTypeId = orderObjectData.DocumentType;
            int serieId = 1;

            //validate external serie usage
            XConfigDocumentsSeriesBE serie = GlobalVars.GlobalListXConfigDocumentsSeries.FirstOrDefault(x => x.Id == serieId);

            if (serie == null)
                throw new Exception("Serie not found");

            if (serie.SourceBilling != (int)SourceBilling.I)
                throw new Exception("Invalid Source Billing");


            string idClient = orderObjectData.Order.IdUser;
            int rows = orderObjectData.NrOrderLines;

            //Start a new document
            manager.Init(documentTypeId, serieId);

            EntityProvider p = new EntityProvider();
            Entity entity = p.Get(idClient);

            //Set the customer
            manager.SetEntity(entity);

            for (int i = 0; i < rows; i++)
            {
                //Start a new document line
                detailManager.Init(false, false);

                string KeyId = orderObjectData.OrderLines[i].Reference;
                decimal quantity = orderObjectData.OrderLines[i].Quantity;
                decimal price = orderObjectData.OrderLines[i].ProductValue;

                detailManager.SetItemID(KeyId);
                detailManager.SetQuantity(quantity);
                detailManager.SetPrice(price);

                detailManager.Commit(false, true);

            }

            ItemTransactionDocument document = manager.CurrentDocument;

            manager.Calculate();

            //totals
            document.TotalIncome = 0;
            document.TotalTaxes = 0;
            document.LineDiscountAmount = 0;
            document.TotalAmount = 0;
            document.TotalNetAmount = 0;
            document.TotalTaxAmount = document.TotalAmount - document.TotalNetAmount;
            document.HeaderDiscountAmount = 0;

            //certification fields: fill accordingly to original system
            document.CreateDate = DateTime.Now;
            document.OsDate = DateTime.Now;
            document.Number = 1;
            document.TotalAmount = 99999;
            document.SignatureVersionPT = 1;
            document.SignatureHashPT = "dljoqwue932184opn";
            document.SignatureStampPT = XDCrypt.GetInstance().GetSignatureStamp(document.SignatureHashPT);

            try
            {
                manager.Validate();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Document cannot be saved because:");
                Console.WriteLine(ex.Message);
                return;
            }

            try
            {

                if (documentProvider.Save(document, true))
                {
                    Console.WriteLine("Sucess");
                }
                else
                {
                    Console.WriteLine("Failed to save document");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saving:");
                Console.WriteLine(ex.Message);
            }

        }

        public static void GenerateReceiptDocument(string entityKeyId)
        {

            if (!XDLicence.IsModuleActive(XDPeople.License.LicenseModules.MssIntegration))
                return;


            //initialize a new data context for transactions purposes
            DbUniversal dataContext = Db.CurrentDatabase.Clone();
            ReceiptDocumentManager manager = new ReceiptDocumentManager(dataContext, "RE", true);

            manager.Init();
            //manager.SetSeries();
            //manager.SetType()

            if (GlobalVars.GlobalDictionaryEntities.ContainsKey(entityKeyId))
            {
                Entity customer = GlobalVars.GlobalDictionaryEntities[entityKeyId];

                manager.SetCustomer(customer);

                manager.CurrentDocument.CreateDate = DateTime.Now;

                foreach (ReceiptDocumentDetail detail in manager.CurrentDocument.Details)
                {
                    manager.SetCurrentDetail(detail);
                    manager.SetMarkedForPaymentFlag(true);
                    manager.SetDetailAmountToPay(detail.Total);
                }

                try
                {
                    PaymentTypeBE payment = GlobalVars.GlobalListPaymentType.FirstOrDefault(x => x.PaymentMechanism == PaymentMechanism.NU.ToString());

                    manager.SetPaymentType(payment);

                    if (manager.Validate(false))
                        manager.Save();
                }
                catch (Exception ex)
                {

                    Log.Write(ex);
                }
            }

        }

        public static void NewCustomer()
        {
            Entity customer = new Entity()
            {
                KeyId = "C00001",
                Name = "Nome do cliente",
                Vat = "123456789",
                Address = "Morada",
                PostalCode = "1100-001",
                City = "Lisboa"
            };

            EntityProvider entityProvider = new EntityProvider();

            entityProvider.Save(customer, true);


        }

        public static void NewItem()
        {
            ItemBE item = new ItemBE()
            {
                KeyId = Guid.NewGuid().ToString(),
                ItemType = (int)ItemType.Normal,
                Description = "ola",
                Barcode = "1234567890123",
                PurchaseNetPrice = 10m,
                PurchasePrice = 12.3m,
                NetPrice1 = 20m,
                RetailPrice1 = 24.6m
            };

            //itemProvider.Save(item, true);
            DataOperationsManager.Save(new ItemProvider(Db.CurrentDatabase), item, true);
        }

        public static ItemBE GetItem(string id)
        {
            return GlobalVars.GlobalListItemBE.FirstOrDefault(fod => fod.KeyId == id);
        }

        public Bitmap GetUserPicture(int userid)
        {
            UserProvider provider = new UserProvider();

            UserBE user = provider.Get(userid);

            return user.Picture;
        }

    }
}
