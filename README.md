To build the project, first update the "Pre-build event command line:" (Project Properties -> Build Events).
Most probably you will need to adjust the path for the "tlbimp.exe" to your current version of the .NET SDK, for example:
"$(FrameworkSDKDir)\bin\NETFX 4.6 Tools\tlbimp.exe" "$(ProjectDir)\..\DMSConnector.tlb" /out:"$(ProjectDir)DMSConnector.dll"
                              ^^^
To install the connector, copy the resulting "SampleNETConnector.dll" to Power PDF's "Connectors" folder. 
By default, here: "[Power PDF install folder]\bin\Connectors\"

To make the connector appear on the UI of the Power PDF (In the "File->Open", and on the "Connectors" tab of the Ribbon), you will also need to updatet the "Publish Mode.xml" file:
1. Copy the content of the "...\XML\PublishMode.xml.partial" to the section "-<toolbar name="connectors" shortKey="N">" of the "Publish Mode.xml", which is located here: "[Power PDF install folder]\resource\PowerPDF\UILayout\Publish Mode.xml"
2. Please also delete the "%appdata%\Kofax\PDF\PowerPDF\UILayout\Publish.xml" if already exists (this is required to take into effect the changes in the "Publish Mode.xml")