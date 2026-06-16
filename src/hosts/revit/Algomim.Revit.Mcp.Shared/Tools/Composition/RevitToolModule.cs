using Algomim.Aec.Mcp.Tooling;

namespace Algomim.Revit.Mcp.Tools.Composition;

/// <summary>Small adapter that lets existing domain tool factories participate in the module catalog.</summary>
internal sealed class RevitToolModule : IRevitToolModule
{
    private readonly Func<RevitToolServices, IEnumerable<IMcpTool>> _factory;

    public RevitToolModule(string name, Func<RevitToolServices, IEnumerable<IMcpTool>> factory)
    {
        Name = name;
        _factory = factory;
    }

    public string Name { get; }

    public IEnumerable<IMcpTool> CreateTools(RevitToolServices services) => _factory(services);
}
