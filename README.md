# XD-API

A .NET Framework console application demonstrating how to integrate with the **XDPeople / XD Software (Sage50c)** ERP libraries — document generation, entity/item management, and licensing setup.

---

## 🎯 Purpose

This project is a runnable reference for third-party integrators who need to call the `XDPeople.GC.Core`, `XDPeople.License`, and `XDPeople.NET` libraries directly from code, instead of through the standard XD Software application. It shows the correct initialization sequence (license, database, global config) and working examples of the most common operations: creating sales documents, signing documents externally, generating receipts, and managing customers/items.

## ✨ Features

- 🔑 License and INI-based configuration bootstrap (`Constant`, `IniFile`, `XDLicence`)
- 🗄️ Database connection and global data preloading (`Db`, `GlobalVars`)
- 📄 Sales document generation from a typed JSON order payload (`OrderData` → `SalesDocumentManager`)
- 🚚 Shipment costs (Portes) through a shipment item line (`ItemType 10`)
- ✍️ Externally-signed document creation (custom `SignatureHashPT`/`SignatureStampPT`)
- 🧾 Receipt document generation tied to the MSS Integration license module
- 👤 Customer (`Entity`) and 📦 item (`ItemBE`) creation
- 🖼️ User picture retrieval
- 🖥️ Interactive console menu that remembers the last-used document type, serie, and item references (`settings.json`)

## 🏗️ Architecture

| Project | Role |
|---|---|
| `XD-API.csproj` | Console app entry point (`Program.cs`), usage examples (`examples.cs`) and typed order payload models (`OrderModels.cs`) |

| Referenced Library | Purpose |
|---|---|
| `XDPeople.GC.Core` | Core business managers (documents, entities, items, receipts) |
| `XDPeople.License` | License loading and module activation checks |
| `XDPeople.NET` | Framework utilities (`Constant`, `IniFile`, `Localization`, `Log`) |
| `Newtonsoft.Json` | JSON (de)serialization of order payloads and local settings |

All three XDPeople libraries are external, prebuilt dependencies resolved from `c:\XDSoftware\bin\xdgc\` and are not part of this repository.

## 🛠️ Tech Stack

- **.NET Framework 4.8**
- **Newtonsoft.Json** 6.x (with binding redirect to the version shipped by XD Software)
- **BouncyCastle.Crypto**, **System.Net.Http** — via assembly binding redirects for XDPeople dependencies

## 🚀 Quick Start

### Prerequisites

- Windows with **.NET Framework 4.8** developer pack
- XD Software / XDGC installed locally, with libraries available at `c:\XDSoftware\bin\xdgc\`
- A valid XD Software license and configuration under `C:\XDSoftware\cfg\` (`xd.ini`, `xd.pem`, license file)
- Access to the XD Software database configured via `Db.Configure()`

### Setup

```bash
git clone <repository-url>
cd XD-API
```

Open `XD-API.sln` in Visual Studio and restore NuGet packages, or build from the CLI:

```bash
msbuild XD-API.sln /p:Configuration=Debug
```

Run the compiled `XD-API.exe` — it loads the license/config, connects to the database, and presents the interactive menu described below.

## 📋 Configuration

Startup paths are hardcoded in [`Program.cs`](Program.cs) and point to the local XD Software installation:

```csharp
string pathToLicense = @"C:\XDSoftware\cfg\";
Constant.PathConfig = Constant.PathLicense = pathToLicense;
Constant.IniFile = "xd.ini";
Constant.MyXDPublicKey = $@"{Constant.PathConfig}xd.pem";
```

Adjust these to match the target environment before running against a different XD Software instance. `App.config` contains the assembly binding redirects required for `Newtonsoft.Json`, `System.Net.Http`, `BouncyCastle.Crypto`, `System.Runtime`, and `System.Threading.Tasks`.

## 🔄 Workflows

The console menu in `RunMenu()` ([Program.cs](Program.cs)) exposes each example:

1. **GenerateDocument** — builds a sales document from the sample order JSON, prompting for document type, serie, and item references (with the last-used values remembered in `settings.json`)
2. **GenerateExternalySignedDocument** — creates a sales document with manually supplied signature/stamp fields, for externally-certified documents
3. **GenerateReceiptDocument** — generates a receipt for a given customer, gated behind the `MssIntegration` license module
4. **NewCustomer** — creates a sample `Entity`
5. **NewItem** — creates a sample `ItemBE`
6. **GetItem** — looks up an item by `KeyId` from the preloaded global item list
7. **GetUserPicture** — retrieves a user's stored picture
0. **Exit**

The sample order payload used by the document-generation examples is `SampleOrderJson` in [`Program.cs`](Program.cs) (see also [`json_example.txt`](json_example.txt)). It is deserialized into the typed models in [`OrderModels.cs`](OrderModels.cs) (`OrderData`, `OrderHeader`, `OrderLine`).

### 🚚 Shipment costs (Portes)

XD's `SalesDocumentManager.Calculate()` computes `ShipmentCosts` / `ShipmentNetCosts` **from the document lines whose item is `ItemType 10`** (e.g. `PORTES NAC`) and overwrites any value set directly on the header. To get shipment costs on a document, enter the shipment item as one of the item references in **GenerateDocument** — its line total becomes the document's shipment costs.

## ⚠️ XD Gotchas

- **Keep the save path statically typed.** `XDCrypt.GetSignatureHash` walks the stack trace and reads `DeclaringType.Name` of every frame. Calling a method with a `dynamic` argument (e.g. an order from `JsonConvert.DeserializeObject<dynamic>`) adds a runtime-binder frame without a declaring type, and saving a certified document fails with `NullReferenceException` in `SalesDocumentManager.SignCertifiedDocument`. That is why the order payload is deserialized into `OrderData`.
- **Debugging the document before saving.** `GenerateDocument` serializes `manager.CurrentDocument` to a `json` variable right before `Save()` (debug only, not used). Put a breakpoint on `manager.Save()` to inspect it.

## 📦 Distribution

This is a standalone example console app; there is no packaging pipeline. Build in `Release` configuration to publish an executable, or copy the relevant patterns from `examples.cs` into your own integration project.

## 📝 License & Support

Internal reference project for XD Software / XDPeople API integrations. Contact Smart Digit for support.
