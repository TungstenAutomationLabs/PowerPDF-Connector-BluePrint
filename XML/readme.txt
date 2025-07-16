To make the connector appear in Connectors ribbon:

1. Copy the content from PublishMode.xml.partial to [Power PDF install path]\resource\PowerPDF\UILayout\Publish Mode.xml
   In target file search for	toolbar name="connectors"	and insert content from source after that line
2. If %appdata%\Kofax\PDF\KofaxPDF\UILayout\Publish.xml exists delete it