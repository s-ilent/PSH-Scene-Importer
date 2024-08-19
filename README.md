# PSH Scene Importer for Unity
This uses a ScriptedImporter to load the data from a PS Home .scene file. These are essentially XML documents that define a hierarchy of GameObjects and components. To aid in representing this data, an XML Data Component is used, which just shows whatever raw data is available. 

Not all data formats are loaded, and the scene file has to be in plain text. This is basically just a tool for letting you visually inspect the hierarchy and placement data for objects and things like audio source nodes. 

### Future plans
- Load whatever missing data formats there are
- MDL format loader (maybe)