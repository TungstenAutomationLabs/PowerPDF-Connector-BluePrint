# Tungsten Automation Power PDF Connector – Blueprint Template

This repository provides a .NET Framework-based sample connector for integrating with **Tungsten Power PDF**, built using the `CurrentDocumentBluePrint` template. It allows developers to extend Power PDF functionality with custom open/save behaviours and toolbar actions.

## 📁 Folder Structure
PowerPDF-Connector-BluePrint/
├── Documentation/ # Developer documentation (Word format)
├── PowerPDF-Connector-CurrentDocumentBluePrint/ # Source project (.csproj)
├── References/ # External DLLs or TLBs if needed
├── PowerPDF-Connector-BluePrint.sln # Visual Studio solution file
├── README.md

## 🛠 Build Instructions
1. Open `PowerPDF-Connector-BluePrint.sln` in **Visual Studio 2019 or later**
2. In the project properties:
   - Set **Platform Target** to `x86` under **Build**
   - Ensure `AssemblyVersion` is defined in `AssemblyInfo.cs` if needed
3. (Optional) Update the `GuidAttribute` in `Connector.cs` using **Tools > Create GUID**
4. Build the solution in **Release** mode

The output DLL will be located at:
PowerPDF-Connector-CurrentDocumentBluePrint\bin\Release\SampleNETConnector.dll

## 🚀 Deployment Steps

### 1. Copy the Connector DLL
Copy `SampleNETConnector.dll` (and any required dependencies from `References/`) to:
[Power PDF install folder]\bin\Connectors\

### 2. Locate the section:
```xml
<toolbar name="connectors" shortKey="N">
```

### 3. Paste in the XML snippet found in:
References\XML\PublishMode.xml.partial

### 4. Delete the cached layout file if it exists:
%appdata%\Kofax\PDF\PowerPDF\UILayout\Publish.xml

3. Add a Connector Display Name
Edit:

mathematica
[Power PDF install folder]\resource\PowerPDF\ENU\NameAndTitle.xml
Under the <!--connectors toolbar--> section, add:

xml
<PFFGroup name="connector::CurrentDocumentBluePrint" title="Blueprint" />


4. Register the Connector DLL
From an elevated (Administrator) command prompt, run:
"C:\Windows\Microsoft.NET\Framework\v4.0.30319\regasm.exe" "C:\Program Files (x86)\Kofax\Power PDF 51\bin\Connectors\SampleNETConnector.dll" /codebase

To unregister:
cmd
"C:\Windows\Microsoft.NET\Framework\v4.0.30319\regasm.exe" /unregister "C:\Program Files (x86)\Kofax\Power PDF 51\bin\Connectors\SampleNETConnector.dll"

📄 Developer Documentation
See the full developer guide for detailed explanation of:

Connector lifecycle methods (DocAddNew, DocOpen, DocSave, MenuAction)
UI configuration via PropertySheet2.cs
Menu integration via Menu.cs and Resources.resx
XML and registry customisations for full integration

📄 Path:
Documentation/PPDF Connector – Developer’s Guide.docx

🧹 Troubleshooting
Ensure the DLL is registered using regasm

Delete any cached layout files after modifying XML:

objectivec
%appdata%\Kofax\PDF\PowerPDF\UILayout\Publish.xml

Restart Power PDF after any change

✅ Summary
| Task                              | Status |
| --------------------------------- | ------ |
| Build DLL in Visual Studio        | ✅      |
| Place in `bin\Connectors`         | ✅      |
| Update `Publish Mode.xml`         | ✅      |
| Register DLL via `regasm`         | ✅      |
| Clear cache and restart Power PDF | ✅      |

📬 Support
For questions or contributions, please raise an issue or submit a pull request to this repository.
