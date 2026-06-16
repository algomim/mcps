using Autodesk.AutoCAD.Runtime;
using Algomim.AutoCad.Mcp.App;

[assembly: ExtensionApplication(typeof(AutoCadMcpApp))]
[assembly: CommandClass(typeof(AutoCadMcpCommands))]
