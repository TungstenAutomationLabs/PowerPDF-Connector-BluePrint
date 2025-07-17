# Tungsten Power PDF – Connector Blueprint

This repository provides a .NET Framework-based sample connector for integrating with **Tungsten Power PDF**. It allows developers to extend Power PDF functionality with custom open/save behaviours and toolbar actions.

## 📁 Folder Structure

```
PowerPDF-Connector-BluePrint/
├── Documentation/                             		# Developer documentation (Word format)
├── PowerPDF-Connector-CurrentDocumentBluePrint/  	# Source project (.csproj)
├── References/                                		# External DLLs or TLBs if needed
├── PowerPDF-Connector-BluePrint.sln           		# Visual Studio solution file
├── README.md
```

## 🛠 Build Instructions

1. Open `PowerPDF-Connector-BluePrint.sln` in **Visual Studio 2019 or later**
2. In the project properties:
   - Set **Platform Target** to `x86` under **Build**
   - Ensure `AssemblyVersion` is defined in `AssemblyInfo.cs` if required
3. (Optional) Update the `GuidAttribute` in `Connector.cs` using **Tools > Create GUID**
4. Build the solution in **Release** mode

The output DLL will be located at:
```
PowerPDF-Connector-CurrentDocumentBluePrint\bin\Release\SampleNETConnector.dll
```

## 🚀 Deployment Steps

### 1. Copy the Connector DLL

Copy `SampleNETConnector.dll` (and any required dependencies from `References/`) to:
```
[Power PDF install folder]\bin\Connectors\
```

### 2. Update Publish Mode Configuration

1. Open:
   ```
   [Power PDF install folder]\resource\PowerPDF\UILayout\Publish Mode.xml
   ```
2. Locate the section:
   ```xml
   <toolbar name="connectors" shortKey="N">
   ```
3. Paste in the XML snippet found in:
   ```
   References\XML\PublishMode.xml.partial
   ```
4. Delete the cached layout file if it exists to refresh UI layout:
   ```
   %appdata%\Kofax\PDF\PowerPDF\UILayout\Publish.xml
   ```

### 3. Add a Connector Display Name

Edit:
```
[Power PDF install folder]\resource\PowerPDF\ENU\NameAndTitle.xml
```

Under the `<!--connectors toolbar-->` section, add:
```xml
<PFFGroup name="connector::CurrentDocumentBluePrint" title="Blueprint" />
```

### 4. Register the Connector DLL

Run the following from an **elevated (Administrator)** command prompt:

```cmd
"C:\Windows\Microsoft.NET\Framework\v4.0.30319\regasm.exe" "C:\Program Files (x86)\Kofax\Power PDF 51\bin\Connectors\SampleNETConnector.dll" /codebase
```

To unregister:
```cmd
"C:\Windows\Microsoft.NET\Framework\v4.0.30319\regasm.exe" /unregister "C:\Program Files (x86)\Kofax\Power PDF 51\bin\Connectors\SampleNETConnector.dll"
```

## 📄 Developer Documentation

Refer to the developer guide included in this repository for full details on:
- Connector lifecycle methods (`DocAddNew`, `DocOpen`, `DocSave`, `MenuAction`)
- Configuration UI via `PropertySheet2.cs`
- Toolbar/menu integration via `Menu.cs` and `Resources.resx`
- Required XML changes and registry registration

📄 **Path:**
```
Documentation/PPDF Connector – Developer’s Guide.docx
```

## 🧹 Troubleshooting

- Ensure the DLL is correctly registered using `regasm`
- Delete the cached UI file after editing `Publish Mode.xml`:
  ```
  %appdata%\Kofax\PDF\PowerPDF\UILayout\Publish.xml
  ```
- Restart Power PDF to apply changes

## ✅ Summary

| Task                              | Status |
|-----------------------------------|--------|
| Build DLL in Visual Studio        | ✅     |
| Place in `bin\Connectors`         | ✅     |
| Update `Publish Mode.xml`         | ✅     |
| Register DLL via `regasm`         | ✅     |
| Clear cache and restart Power PDF | ✅     |

## 📬 Support

For questions or contributions, please raise an issue or submit a pull request to this repository.