using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json;
using Algomim.Aec.Mcp.Tooling;
using Algomim.Rhino.Mcp.Tools.Common;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Display;
using Rhino.Geometry;

namespace Algomim.Rhino.Mcp.Tools.Grasshopper;

internal static class GrasshopperToolSet
{
    private const string MetaAlias = "algomim.alias";
    private const string MetaGraphId = "algomim.graph_id";
    private const string MetaRole = "algomim.role";

    private static readonly Dictionary<string, string> ComponentAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["slider"] = "Number Slider",
        ["number_slider"] = "Number Slider",
        ["num_slider"] = "Number Slider",
        ["toggle"] = "Boolean Toggle",
        ["boolean_toggle"] = "Boolean Toggle",
        ["panel"] = "Panel",
        ["value_list"] = "Value List",
        ["valuelist"] = "Value List",
        ["relay"] = "Relay",
        ["note"] = "Scribble",
        ["scribble"] = "Scribble",
        ["add"] = "Addition",
        ["plus"] = "Addition",
        ["subtract"] = "Subtraction",
        ["minus"] = "Subtraction",
        ["multiply"] = "Multiplication",
        ["divide"] = "Division",
        ["point"] = "Point",
        ["curve"] = "Curve",
        ["brep"] = "Brep",
        ["line"] = "Line",
        ["circle"] = "Circle",
        ["construct_point"] = "Construct Point",
        ["move"] = "Move",
        ["rotate"] = "Rotate",
        ["scale"] = "Scale",
        ["list_item"] = "List Item",
    };

    private static readonly MethodInfo? SetStringValueMethod = typeof(GH_DocumentObject).GetMethod(
        "SetValue",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
        null,
        [typeof(string), typeof(string)],
        null);

    private static readonly MethodInfo? GetStringValueMethod = typeof(GH_DocumentObject).GetMethod(
        "GetValue",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
        null,
        [typeof(string), typeof(string)],
        null);

    public static IEnumerable<IMcpTool> Create()
    {
        yield return Start();
        yield return CreateDocument();
        yield return GetDocumentInfo();
        yield return GetCanvasState();
        yield return ClearCanvas();
        yield return SearchComponents();
        yield return BatchSearchComponents();
        yield return ListAvailableComponents();
        yield return ListComponentCategories();
        yield return GetComponentTypeInfo();
        yield return BatchGetComponentTypeInfo();
        yield return ListComponents();
        yield return GetComponentInfo();
        yield return AddComponent();
        yield return UpdateComponent();
        yield return DeleteComponent();
        yield return LayoutComponents();
        yield return ConnectComponents();
        yield return DisconnectComponents();
        yield return SetParameterValue();
        yield return GetParameterValue();
        yield return RunSolution();
        yield return ExpireSolution();
        yield return BuildGraph();
        yield return MutateGraph();
        yield return GetGraph();
        yield return ClearGraph();
        yield return CapturePreview();
    }

    private static IMcpTool Start()
        => Tool(
            "grasshopper_start",
            "Opens Grasshopper and ensures there is an active Grasshopper document.",
            RhinoSchemas.Object(new Dictionary<string, object>
            {
                ["createIfMissing"] = RhinoSchemas.Boolean("Create a new Grasshopper document if none exists", defaultValue: true),
            }),
            (reader, _) =>
            {
                var doc = GetActiveDocument(createIfMissing: reader.GetBool("createIfMissing", fallback: true), openCanvas: true, makeActive: true);
                return ToolResponse.Success(DocumentSummary(doc), "Grasshopper is ready.");
            });

    private static IMcpTool CreateDocument()
        => Tool(
            "grasshopper_document_create",
            "Creates or activates a Grasshopper document and optionally clears the current canvas.",
            RhinoSchemas.Object(new Dictionary<string, object>
            {
                ["clear"] = RhinoSchemas.Boolean("Clear the active Grasshopper canvas after creating or activating it", defaultValue: false),
                ["openCanvas"] = RhinoSchemas.Boolean("Open the Grasshopper editor canvas", defaultValue: true),
            }),
            (reader, _) =>
            {
                var doc = GetActiveDocument(createIfMissing: true, openCanvas: reader.GetBool("openCanvas", fallback: true), makeActive: true);
                var cleared = 0;
                if (reader.GetBool("clear"))
                {
                    var objects = doc.Objects.ToList();
                    doc.RemoveObjects(objects, false);
                    cleared = objects.Count;
                    RedrawCanvas();
                }

                return ToolResponse.Success(new { document = DocumentSummary(doc), cleared }, "Grasshopper document is active.");
            });

    private static IMcpTool GetDocumentInfo()
        => Tool(
            "grasshopper_document_get_info",
            "Gets active Grasshopper document counts, canvas state, and preview visibility state.",
            RhinoSchemas.Empty,
            (_, _) =>
            {
                var doc = GetActiveDocument();
                return ToolResponse.Success(new
                {
                    document = DocumentSummary(doc),
                    canvas = CanvasState(doc),
                    visibility = VisibilityState(doc),
                }, "Read Grasshopper document info.");
            });

    private static IMcpTool GetCanvasState()
        => Tool(
            "grasshopper_canvas_get_state",
            "Gets active Grasshopper canvas state and object layout summary.",
            RhinoSchemas.Empty,
            (_, _) =>
            {
                var doc = GetActiveDocument();
                var objects = doc.Objects
                    .Where(obj => obj is not GH_Group)
                    .Select(ObjectSummary)
                    .ToArray();

                return ToolResponse.Success(new
                {
                    canvas = CanvasState(doc),
                    objectCount = objects.Length,
                    objects,
                }, "Read Grasshopper canvas state.");
            });

    private static IMcpTool ClearCanvas()
        => Tool(
            "grasshopper_canvas_clear",
            "Clears the active Grasshopper canvas.",
            RhinoSchemas.Object(new Dictionary<string, object>
            {
                ["includeGroups"] = RhinoSchemas.Boolean("Delete Grasshopper group annotations too", defaultValue: true),
                ["recompute"] = RhinoSchemas.Boolean("Run a solution after clearing", defaultValue: false),
            }),
            (reader, _) =>
            {
                var doc = GetActiveDocument();
                var includeGroups = reader.GetBool("includeGroups", fallback: true);
                var objects = doc.Objects.Where(obj => includeGroups || obj is not GH_Group).ToList();
                doc.RemoveObjects(objects, false);
                if (reader.GetBool("recompute"))
                    RunGrasshopperSolution(doc, expireAll: false);

                RedrawCanvas();
                return ToolResponse.Success(new { deleted = objects.Count, includeGroups }, $"Cleared {objects.Count} Grasshopper canvas object(s).");
            });

    private static IMcpTool SearchComponents()
        => Tool(
            "grasshopper_component_search",
            "Searches Grasshopper component library by name, nickname, category, subcategory, or description.",
            RhinoSchemas.Object(new Dictionary<string, object>(RhinoSchemas.Props(
                ("query", "string", "Search text"),
                ("category", "string", "Optional category filter"),
                ("subcategory", "string", "Optional subcategory filter")))
            {
                ["limit"] = RhinoSchemas.Integer("Maximum matches", 1, 500, 50),
            }),
            (reader, _) =>
            {
                var query = reader.GetString("query");
                var category = reader.GetString("category");
                var subcategory = reader.GetString("subcategory");
                var limit = Math.Clamp(reader.GetInt("limit", 50), 1, 500);
                var proxies = OrderProxiesForQuery(FilterProxies(query, category, subcategory), query)
                    .Take(limit)
                    .Select(proxy => ProxySummary(proxy, includeDescription: true))
                    .ToArray();

                return ToolResponse.Success(new { count = proxies.Length, query, category, subcategory, components = proxies }, $"Found {proxies.Length} Grasshopper component type(s).");
            });

    private static IMcpTool BatchSearchComponents()
        => Tool(
            "grasshopper_component_batch_search",
            "Searches several Grasshopper component queries in one call.",
            RhinoSchemas.Object(new Dictionary<string, object>
            {
                ["queries"] = RhinoSchemas.StringArray("Component search queries"),
                ["maxMatches"] = RhinoSchemas.Integer("Maximum matches per query", 1, 50, 5),
            }, ["queries"]),
            (reader, _) =>
            {
                var maxMatches = Math.Clamp(reader.GetInt("maxMatches", 5), 1, 50);
                var results = reader.RequireStringArray("queries")
                    .ToDictionary(
                        query => query,
                        query => OrderProxiesForQuery(FilterProxies(query, null, null), query)
                            .Take(maxMatches)
                            .Select(proxy => ProxySummary(proxy, includeDescription: false))
                            .ToArray(),
                        StringComparer.OrdinalIgnoreCase);

                return ToolResponse.Success(new { totalQueries = results.Count, maxMatches, results }, "Completed Grasshopper component batch search.");
            });

    private static IMcpTool ListAvailableComponents()
        => Tool(
            "grasshopper_component_list_available",
            "Lists available Grasshopper component types, optionally by category.",
            RhinoSchemas.Object(new Dictionary<string, object>(RhinoSchemas.Props(
                ("category", "string", "Optional category filter")))
            {
                ["includeDescription"] = RhinoSchemas.Boolean("Include descriptions", defaultValue: false),
                ["limit"] = RhinoSchemas.Integer("Maximum component types", 1, 5000, 500),
            }),
            (reader, _) =>
            {
                var category = reader.GetString("category");
                var limit = Math.Clamp(reader.GetInt("limit", 500), 1, 5000);
                var components = FilterProxies(null, category, null)
                    .OrderBy(proxy => proxy.Desc.Category)
                    .ThenBy(proxy => proxy.Desc.SubCategory)
                    .ThenBy(proxy => proxy.Desc.Name)
                    .Take(limit)
                    .Select(proxy => ProxySummary(proxy, reader.GetBool("includeDescription")))
                    .ToArray();

                return ToolResponse.Success(new
                {
                    count = components.Length,
                    totalAvailable = Instances.ComponentServer.ObjectProxies.Count(),
                    category,
                    components,
                }, $"Listed {components.Length} Grasshopper component type(s).");
            });

    private static IMcpTool ListComponentCategories()
        => Tool(
            "grasshopper_component_list_categories",
            "Lists Grasshopper component categories and subcategories.",
            RhinoSchemas.Empty,
            (_, _) =>
            {
                var categories = Instances.ComponentServer.ObjectProxies
                    .Where(proxy => !string.IsNullOrWhiteSpace(proxy.Desc.Category))
                    .GroupBy(proxy => proxy.Desc.Category)
                    .OrderBy(group => group.Key)
                    .Select(group => new
                    {
                        category = group.Key,
                        componentCount = group.Count(),
                        subcategories = group
                            .Select(proxy => proxy.Desc.SubCategory)
                            .Where(value => !string.IsNullOrWhiteSpace(value))
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .OrderBy(value => value)
                            .ToArray(),
                    })
                    .ToArray();

                return ToolResponse.Success(new { count = categories.Length, categories }, $"Listed {categories.Length} Grasshopper component categories.");
            });

    private static IMcpTool GetComponentTypeInfo()
        => Tool(
            "grasshopper_component_get_type_info",
            "Gets Grasshopper component type metadata and input/output parameter definitions by name or GUID.",
            RhinoSchemas.Object(new Dictionary<string, object>(RhinoSchemas.Props(
                ("componentName", "string", "Component name"),
                ("componentGuid", "string", "Component type GUID")))),
            (reader, _) =>
            {
                var result = ComponentTypeInfo(reader.GetString("componentName") ?? reader.GetString("name"), reader.GetString("componentGuid") ?? reader.GetString("guid"));
                return ToolResponse.Success(result, "Read Grasshopper component type info.");
            });

    private static IMcpTool BatchGetComponentTypeInfo()
        => Tool(
            "grasshopper_component_batch_get_type_info",
            "Gets type information for several Grasshopper component names or GUIDs.",
            RhinoSchemas.Object(new Dictionary<string, object>
            {
                ["components"] = RhinoSchemas.JsonObjectArray("Component selectors with componentName/name or componentGuid/guid"),
            }, ["components"]),
            (reader, _) =>
            {
                var selectors = RequireArray(reader, "components");
                var results = selectors
                    .Select(selector => ComponentTypeInfo(GetString(selector, "componentName", "name"), GetString(selector, "componentGuid", "guid")))
                    .ToArray();
                return ToolResponse.Success(new { count = results.Length, results }, $"Read {results.Length} Grasshopper component type definition(s).");
            });

    private static IMcpTool ListComponents()
        => Tool(
            "grasshopper_component_list",
            "Lists objects currently on the active Grasshopper canvas.",
            RhinoSchemas.Object(new Dictionary<string, object>(RhinoSchemas.Props(
                ("category", "string", "Optional component category filter"),
                ("name", "string", "Optional name/nickname filter")))
            {
                ["includeGroups"] = RhinoSchemas.Boolean("Include groups", defaultValue: false),
                ["limit"] = RhinoSchemas.Integer("Maximum objects", 1, 5000, 500),
            }),
            (reader, _) =>
            {
                var doc = GetActiveDocument();
                var category = reader.GetString("category");
                var name = reader.GetString("name");
                var includeGroups = reader.GetBool("includeGroups");
                var limit = Math.Clamp(reader.GetInt("limit", 500), 1, 5000);
                var objects = doc.Objects
                    .Where(obj => includeGroups || obj is not GH_Group)
                    .Where(obj => string.IsNullOrWhiteSpace(category) || string.Equals(obj.Category, category, StringComparison.OrdinalIgnoreCase))
                    .Where(obj => string.IsNullOrWhiteSpace(name) ||
                                  obj.Name.Contains(name, StringComparison.OrdinalIgnoreCase) ||
                                  obj.NickName.Contains(name, StringComparison.OrdinalIgnoreCase))
                    .Take(limit)
                    .Select(ObjectSummary)
                    .ToArray();

                return ToolResponse.Success(new { count = objects.Length, objects }, $"Listed {objects.Length} Grasshopper canvas object(s).");
            });

    private static IMcpTool GetComponentInfo()
        => Tool(
            "grasshopper_component_get_info",
            "Gets detailed information for a Grasshopper canvas object by id, alias, or nickname.",
            RhinoSchemas.Object(SelectorProperties(), ["id"]),
            (reader, _) =>
            {
                var doc = GetActiveDocument();
                var obj = FindObject(doc, reader);
                return ToolResponse.Success(ObjectDetails(obj), $"Read Grasshopper object '{obj.NickName}'.");
            });

    private static IMcpTool AddComponent()
        => Tool(
            "grasshopper_component_add",
            "Adds a Grasshopper component, parameter, slider, toggle, panel, value list, relay, or note to the canvas.",
            RhinoSchemas.Object(ComponentAddProperties(), ["componentName"]),
            (reader, _) =>
            {
                var doc = GetActiveDocument(createIfMissing: true, openCanvas: true, makeActive: true);
                var obj = CreateObject(reader.GetString("componentName") ?? reader.GetString("name"), reader.GetString("componentGuid") ?? reader.GetString("guid"), reader.GetElement("value"));
                PlaceAndConfigureObject(obj, reader, FindNextSlot(doc));
                doc.AddObject(obj, false);
                ApplyMetadata(obj, reader.GetString("alias"), reader.GetString("graphId") ?? reader.GetString("graph_id"), reader.GetString("role"));
                if (reader.GetBool("recompute", fallback: true))
                    RunGrasshopperSolution(doc, expireAll: false);

                RedrawCanvas(obj.Attributes?.Pivot);
                return ToolResponse.Success(ObjectDetails(obj), $"Added Grasshopper object '{obj.NickName}'.");
            });

    private static IMcpTool UpdateComponent()
        => Tool(
            "grasshopper_component_update",
            "Updates a Grasshopper canvas object's nickname, position, preview, enabled state, alias, graph id, or role.",
            RhinoSchemas.Object(UpdateProperties(), ["id"]),
            (reader, _) =>
            {
                var doc = GetActiveDocument();
                var obj = FindObject(doc, reader);
                ApplyObjectUpdate(obj, reader);
                ApplyMetadata(obj, reader.GetString("alias"), reader.GetString("graphId") ?? reader.GetString("graph_id"), reader.GetString("role"));
                if (reader.GetBool("recompute"))
                    RunGrasshopperSolution(doc, expireAll: false);

                RedrawCanvas(obj.Attributes?.Pivot);
                return ToolResponse.Success(ObjectDetails(obj), $"Updated Grasshopper object '{obj.NickName}'.");
            });

    private static IMcpTool DeleteComponent()
        => Tool(
            "grasshopper_component_delete",
            "Deletes a Grasshopper canvas object by id, alias, or nickname.",
            RhinoSchemas.Object(SelectorProperties(), ["id"]),
            (reader, _) =>
            {
                var doc = GetActiveDocument();
                var obj = FindObject(doc, reader);
                var summary = ObjectSummary(obj);
                doc.RemoveObject(obj, false);
                RunGrasshopperSolution(doc, expireAll: false);
                RedrawCanvas();
                return ToolResponse.Success(new { deleted = summary }, $"Deleted Grasshopper object '{obj.NickName}'.");
            });

    private static IMcpTool LayoutComponents()
        => Tool(
            "grasshopper_component_layout",
            "Lays out Grasshopper canvas objects in graph-depth or grid order.",
            RhinoSchemas.Object(new Dictionary<string, object>
            {
                ["ids"] = RhinoSchemas.StringArray("Optional object GUIDs to layout. Defaults to all non-group objects."),
                ["graphId"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Optional graph id to layout" },
                ["start"] = RhinoSchemas.Point("Top-left canvas position"),
                ["xSpacing"] = RhinoSchemas.Number("Horizontal spacing", minimum: 1, defaultValue: 220),
                ["ySpacing"] = RhinoSchemas.Number("Vertical spacing", minimum: 1, defaultValue: 90),
                ["maxColumns"] = RhinoSchemas.Integer("Wrap after this many columns; 0 disables wrapping", 0, 100, 0),
                ["recompute"] = RhinoSchemas.Boolean("Run a solution after layout", defaultValue: false),
            }),
            (reader, _) =>
            {
                var doc = GetActiveDocument();
                var objects = ResolveObjectList(doc, reader).Where(obj => obj is not GH_Group).ToList();
                var start = ReadPosition(reader, "start", new PointF(40, 40));
                var result = ApplyLayout(objects, start, (float)Math.Max(60, reader.GetDouble("xSpacing", 220)), (float)Math.Max(40, reader.GetDouble("ySpacing", 90)), reader.GetInt("maxColumns"));
                if (reader.GetBool("recompute"))
                    RunGrasshopperSolution(doc, expireAll: false);

                RedrawCanvas(start);
                return ToolResponse.Success(result, $"Laid out {objects.Count} Grasshopper object(s).");
            });

    private static IMcpTool ConnectComponents()
        => Tool(
            "grasshopper_component_connect",
            "Connects a source Grasshopper output parameter to a target input parameter.",
            RhinoSchemas.Object(ConnectionProperties(disconnect: false)),
            (reader, _) =>
            {
                var doc = GetActiveDocument();
                var sourceObj = FindObject(doc, reader, "source");
                var targetObj = FindObject(doc, reader, "target");
                var output = FindOutputParam(sourceObj, reader, "source");
                var input = FindInputParam(targetObj, reader, "target");
                if (!input.Sources.Contains(output))
                    input.AddSource(output);

                targetObj.ExpireSolution(true);
                if (reader.GetBool("recompute", fallback: true))
                    RunGrasshopperSolution(doc, expireAll: false);

                RedrawCanvas();
                return ToolResponse.Success(new
                {
                    source = ParamEndpoint(sourceObj, output),
                    target = ParamEndpoint(targetObj, input),
                }, $"Connected {sourceObj.NickName}.{output.Name} to {targetObj.NickName}.{input.Name}.");
            });

    private static IMcpTool DisconnectComponents()
        => Tool(
            "grasshopper_component_disconnect",
            "Disconnects a Grasshopper input parameter from one source or all sources.",
            RhinoSchemas.Object(ConnectionProperties(disconnect: true)),
            (reader, _) =>
            {
                var doc = GetActiveDocument();
                var targetObj = FindObject(doc, reader, "target");
                var input = FindInputParam(targetObj, reader, "target");
                var removed = 0;
                if (reader.GetBool("disconnectAll", fallback: false))
                {
                    removed = input.SourceCount;
                    input.RemoveAllSources();
                }
                else
                {
                    var sourceObj = FindObject(doc, reader, "source");
                    var output = FindOutputParam(sourceObj, reader, "source");
                    if (input.Sources.Contains(output))
                    {
                        input.RemoveSource(output);
                        removed = 1;
                    }
                }

                targetObj.ExpireSolution(true);
                if (reader.GetBool("recompute", fallback: true))
                    RunGrasshopperSolution(doc, expireAll: false);

                RedrawCanvas();
                return ToolResponse.Success(new { target = ParamEndpoint(targetObj, input), disconnected = removed }, $"Disconnected {removed} Grasshopper connection(s).");
            });

    private static IMcpTool SetParameterValue()
        => Tool(
            "grasshopper_parameter_set_value",
            "Sets a Grasshopper special object value or persistent input parameter value.",
            RhinoSchemas.Object(ParameterSetProperties(), ["id", "value"]),
            (reader, _) =>
            {
                var doc = GetActiveDocument();
                var value = reader.GetElement("value") ?? throw new ArgumentException("Argument 'value' is required.");
                var obj = FindObject(doc, reader);
                if (TrySetSpecialValue(obj, value, reader, out var specialResult))
                {
                    RunGrasshopperSolution(doc, expireAll: false);
                    return ToolResponse.Success(specialResult, $"Set Grasshopper value on '{obj.NickName}'.");
                }

                var input = FindInputParam(obj, reader, prefix: "");
                SetParamValue(input, value);
                obj.ExpireSolution(true);
                RunGrasshopperSolution(doc, expireAll: false);
                return ToolResponse.Success(new { target = ParamEndpoint(obj, input), value = ToPlainJson(value) }, $"Set Grasshopper parameter '{input.Name}'.");
            });

    private static IMcpTool GetParameterValue()
        => Tool(
            "grasshopper_parameter_get_value",
            "Reads Grasshopper output parameter volatile data.",
            RhinoSchemas.Object(new Dictionary<string, object>(SelectorProperties())
            {
                ["outputIndex"] = RhinoSchemas.Integer("Output parameter index", 0, 1000, 0),
                ["outputName"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Output parameter name or nickname" },
                ["maxItems"] = RhinoSchemas.Integer("Maximum data items to return", 0, 10000, 100),
            }, ["id"]),
            (reader, _) =>
            {
                var doc = GetActiveDocument();
                var obj = FindObject(doc, reader);
                var output = FindOutputParam(obj, reader, prefix: "");
                if (!ParamHasData(output))
                {
                    obj.ExpireSolution(true);
                    RunGrasshopperSolution(doc, expireAll: false);
                }

                return ToolResponse.Success(ParamValue(obj, output, Math.Clamp(reader.GetInt("maxItems", 100), 0, 10000)), $"Read Grasshopper parameter '{output.Name}'.");
            });

    private static IMcpTool RunSolution()
        => Tool(
            "grasshopper_solution_run",
            "Runs the active Grasshopper document solution and returns runtime messages.",
            RhinoSchemas.Object(new Dictionary<string, object>
            {
                ["expireAll"] = RhinoSchemas.Boolean("Expire all objects before solving", defaultValue: false),
            }),
            (reader, _) =>
            {
                var doc = GetActiveDocument();
                var start = Stopwatch.StartNew();
                RunGrasshopperSolution(doc, reader.GetBool("expireAll"));
                start.Stop();
                var runtime = RuntimeSummary(doc);
                return ToolResponse.Success(new { runtime, durationMs = start.ElapsedMilliseconds, solutionState = doc.SolutionState.ToString() }, SolutionMessage(runtime));
            });

    private static IMcpTool ExpireSolution()
        => Tool(
            "grasshopper_solution_expire",
            "Expires all, selected, or specific Grasshopper objects and optionally recomputes.",
            RhinoSchemas.Object(new Dictionary<string, object>(SelectorProperties())
            {
                ["ids"] = RhinoSchemas.StringArray("Object GUIDs to expire"),
                ["expireDownstream"] = RhinoSchemas.Boolean("Expire downstream recipients", defaultValue: true),
                ["recompute"] = RhinoSchemas.Boolean("Run solution after expiring", defaultValue: false),
            }),
            (reader, _) =>
            {
                var doc = GetActiveDocument();
                var targets = ResolveObjectList(doc, reader);
                if (targets.Count == 0 && HasSelector(reader))
                    targets.Add(FindObject(doc, reader));
                if (targets.Count == 0)
                    targets.AddRange(doc.Objects);

                foreach (var obj in targets)
                    obj.ExpireSolution(reader.GetBool("expireDownstream", fallback: true));
                if (reader.GetBool("recompute"))
                    RunGrasshopperSolution(doc, expireAll: false);

                return ToolResponse.Success(new { expired = targets.Count, recomputed = reader.GetBool("recompute") }, $"Expired {targets.Count} Grasshopper object(s).");
            });

    private static IMcpTool BuildGraph()
        => Tool(
            "grasshopper_graph_build",
            "Builds a Grasshopper graph from component specs, value assignments, connections, optional groups, and layout.",
            RhinoSchemas.Object(new Dictionary<string, object>
            {
                ["graphId"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Optional graph id" },
                ["components"] = RhinoSchemas.JsonObjectArray("Component specs with alias, componentName/componentGuid, position, value, nickname, role"),
                ["values"] = RhinoSchemas.JsonObjectArray("Value assignments with target and value"),
                ["connections"] = RhinoSchemas.JsonObjectArray("Connection specs with source/target aliases or ids"),
                ["groups"] = RhinoSchemas.JsonObjectArray("Optional group specs"),
                ["layout"] = RhinoSchemas.JsonObject("Layout options or { enabled: false }"),
                ["recompute"] = RhinoSchemas.Boolean("Run solution after graph build", defaultValue: true),
                ["openCanvas"] = RhinoSchemas.Boolean("Open Grasshopper canvas", defaultValue: true),
            }, ["components"]),
            (reader, _) =>
            {
                var stopwatch = Stopwatch.StartNew();
                var doc = GetActiveDocument(createIfMissing: true, openCanvas: reader.GetBool("openCanvas", fallback: true), makeActive: true);
                var graphId = NormalizeGraphId(reader.GetString("graphId") ?? reader.GetString("graph_id"));
                var aliases = new Dictionary<string, IGH_DocumentObject>(StringComparer.OrdinalIgnoreCase);
                var created = new List<IGH_DocumentObject>();
                var connectionCount = 0;
                var valueCount = 0;

                foreach (var spec in RequireArray(reader, "components"))
                {
                    var alias = GetRequiredString(spec, "alias");
                    if (aliases.ContainsKey(alias))
                        throw new ArgumentException($"Duplicate Grasshopper graph alias '{alias}'.");

                    var obj = CreateObject(GetString(spec, "componentName", "component_name", "name"), GetString(spec, "componentGuid", "component_guid", "guid"), GetElement(spec, "value"));
                    PlaceAndConfigureObject(obj, spec, FindNextSlot(doc));
                    doc.AddObject(obj, false);
                    ApplyMetadata(obj, alias, GetString(spec, "graphId", "graph_id") ?? graphId, GetString(spec, "role"));
                    aliases[alias] = obj;
                    created.Add(obj);
                }

                foreach (var valueSpec in GetArray(reader, "values"))
                {
                    ApplyGraphValue(doc, aliases, valueSpec);
                    valueCount++;
                }

                foreach (var connectionSpec in GetArray(reader, "connections"))
                {
                    ApplyGraphConnection(doc, aliases, connectionSpec);
                    connectionCount++;
                }

                var groups = CreateGroups(doc, GetArray(reader, "groups"), aliases, created, graphId);
                var layout = MaybeApplyLayout(doc, reader.GetElement("layout"), created.Where(obj => obj is not GH_Group).ToList());

                if (reader.GetBool("recompute", fallback: true))
                    RunGrasshopperSolution(doc, expireAll: false);

                RedrawCanvas();
                stopwatch.Stop();
                return ToolResponse.Success(new
                {
                    graphId,
                    componentCount = created.Count,
                    connectionCount,
                    valueCount,
                    groupCount = groups.Count,
                    aliases = aliases.ToDictionary(pair => pair.Key, pair => pair.Value.InstanceGuid.ToString("D"), StringComparer.OrdinalIgnoreCase),
                    components = created.Select(ObjectSummary).ToArray(),
                    groups,
                    layout,
                    runtime = RuntimeSummary(doc),
                    durationMs = stopwatch.ElapsedMilliseconds,
                }, $"Built Grasshopper graph '{graphId}' with {created.Count} object(s).");
            });

    private static IMcpTool MutateGraph()
        => Tool(
            "grasshopper_graph_mutate",
            "Applies create, connect, set, update, disconnect, delete, and recompute operations to a Grasshopper graph.",
            RhinoSchemas.Object(new Dictionary<string, object>
            {
                ["graphId"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Optional graph id for newly created objects" },
                ["operations"] = RhinoSchemas.JsonObjectArray("Mutation operations"),
                ["layout"] = RhinoSchemas.JsonObject("Optional layout options"),
                ["recompute"] = RhinoSchemas.Boolean("Run solution after mutations", defaultValue: true),
                ["rollbackOnError"] = RhinoSchemas.Boolean("Remove newly created objects if a mutation fails", defaultValue: true),
                ["openCanvas"] = RhinoSchemas.Boolean("Open Grasshopper canvas", defaultValue: true),
            }, ["operations"]),
            (reader, _) =>
            {
                var stopwatch = Stopwatch.StartNew();
                var doc = GetActiveDocument(createIfMissing: true, openCanvas: reader.GetBool("openCanvas", fallback: true), makeActive: true);
                var graphId = NormalizeGraphId(reader.GetString("graphId") ?? reader.GetString("graph_id"));
                var aliases = new Dictionary<string, IGH_DocumentObject>(StringComparer.OrdinalIgnoreCase);
                var created = new List<IGH_DocumentObject>();
                var touched = new List<IGH_DocumentObject>();
                var results = new List<object>();

                try
                {
                    foreach (var opSpec in RequireArray(reader, "operations"))
                    {
                        var op = GetRequiredString(opSpec, "op").Trim().ToLowerInvariant();
                        switch (op)
                        {
                            case "create":
                            {
                                var alias = GetRequiredString(opSpec, "alias");
                                var obj = CreateObject(GetString(opSpec, "componentName", "component_name", "name"), GetString(opSpec, "componentGuid", "component_guid", "guid"), GetElement(opSpec, "value"));
                                PlaceAndConfigureObject(obj, opSpec, FindNextSlot(doc));
                                doc.AddObject(obj, false);
                                ApplyMetadata(obj, alias, GetString(opSpec, "graphId", "graph_id") ?? graphId, GetString(opSpec, "role"));
                                aliases[alias] = obj;
                                created.Add(obj);
                                touched.Add(obj);
                                results.Add(new { op, alias, id = obj.InstanceGuid.ToString("D"), obj.Name, obj.NickName });
                                break;
                            }
                            case "connect":
                                ApplyGraphConnection(doc, aliases, opSpec);
                                results.Add(new { op });
                                break;
                            case "set":
                            {
                                var obj = ApplyGraphValue(doc, aliases, opSpec);
                                touched.Add(obj);
                                results.Add(new { op, id = obj.InstanceGuid.ToString("D"), obj.NickName });
                                break;
                            }
                            case "update":
                            {
                                var obj = ResolveGraphEndpoint(doc, aliases, opSpec, "target");
                                ApplyObjectUpdate(obj, opSpec);
                                ApplyMetadata(obj, GetString(opSpec, "alias"), GetString(opSpec, "graphId", "graph_id"), GetString(opSpec, "role"));
                                touched.Add(obj);
                                results.Add(new { op, id = obj.InstanceGuid.ToString("D"), obj.NickName });
                                break;
                            }
                            case "disconnect":
                            {
                                var target = ResolveGraphEndpoint(doc, aliases, opSpec, "target");
                                var input = FindInputParam(target, opSpec, "target");
                                if (GetBool(opSpec, "disconnectAll", "disconnect_all"))
                                {
                                    input.RemoveAllSources();
                                }
                                else
                                {
                                    var source = ResolveGraphEndpoint(doc, aliases, opSpec, "source");
                                    var output = FindOutputParam(source, opSpec, "source");
                                    input.RemoveSource(output);
                                }
                                target.ExpireSolution(true);
                                touched.Add(target);
                                results.Add(new { op, target = target.InstanceGuid.ToString("D") });
                                break;
                            }
                            case "delete":
                            {
                                var obj = ResolveGraphEndpoint(doc, aliases, opSpec, "target");
                                doc.RemoveObject(obj, false);
                                results.Add(new { op, deleted = obj.InstanceGuid.ToString("D"), obj.NickName });
                                break;
                            }
                            case "recompute":
                                RunGrasshopperSolution(doc, expireAll: false);
                                results.Add(new { op });
                                break;
                            default:
                                throw new ArgumentException($"Unsupported Grasshopper mutation op '{op}'.");
                        }
                    }

                    var layoutTargets = touched.Concat(created).Distinct().Where(obj => obj is not GH_Group).ToList();
                    var layout = MaybeApplyLayout(doc, reader.GetElement("layout"), layoutTargets);
                    if (reader.GetBool("recompute", fallback: true))
                        RunGrasshopperSolution(doc, expireAll: false);

                    RedrawCanvas();
                    stopwatch.Stop();
                    return ToolResponse.Success(new
                    {
                        graphId,
                        operationCount = results.Count,
                        createdCount = created.Count,
                        operations = results,
                        layout,
                        runtime = RuntimeSummary(doc),
                        durationMs = stopwatch.ElapsedMilliseconds,
                    }, $"Mutated Grasshopper graph with {results.Count} operation(s).");
                }
                catch
                {
                    if (reader.GetBool("rollbackOnError", fallback: true))
                    {
                        doc.RemoveObjects(created, false);
                        RedrawCanvas();
                    }

                    throw;
                }
            });

    private static IMcpTool GetGraph()
        => Tool(
            "grasshopper_graph_get",
            "Gets all Grasshopper objects carrying a graph id.",
            RhinoSchemas.Object(new Dictionary<string, object>
            {
                ["graphId"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Graph id" },
                ["includeValues"] = RhinoSchemas.Boolean("Include volatile data samples", defaultValue: false),
                ["maxItems"] = RhinoSchemas.Integer("Maximum data items per parameter", 0, 1000, 20),
            }, ["graphId"]),
            (reader, _) =>
            {
                var doc = GetActiveDocument();
                var graphId = reader.RequireString("graphId");
                var maxItems = Math.Clamp(reader.GetInt("maxItems", 20), 0, 1000);
                var includeValues = reader.GetBool("includeValues");
                var objects = GraphObjects(doc, graphId).ToArray();
                return ToolResponse.Success(new
                {
                    graphId,
                    count = objects.Length,
                    objects = objects.Select(obj => ObjectDetails(obj, includeValues, maxItems)).ToArray(),
                }, $"Read Grasshopper graph '{graphId}'.");
            });

    private static IMcpTool ClearGraph()
        => Tool(
            "grasshopper_graph_clear",
            "Deletes all Grasshopper objects carrying a graph id.",
            RhinoSchemas.Object(new Dictionary<string, object>
            {
                ["graphId"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Graph id" },
                ["includeGroups"] = RhinoSchemas.Boolean("Delete group annotations too", defaultValue: true),
                ["recompute"] = RhinoSchemas.Boolean("Run solution after clearing", defaultValue: false),
            }, ["graphId"]),
            (reader, _) =>
            {
                var doc = GetActiveDocument();
                var graphId = reader.RequireString("graphId");
                var includeGroups = reader.GetBool("includeGroups", fallback: true);
                var objects = GraphObjects(doc, graphId).Where(obj => includeGroups || obj is not GH_Group).ToList();
                doc.RemoveObjects(objects, false);
                if (reader.GetBool("recompute"))
                    RunGrasshopperSolution(doc, expireAll: false);

                RedrawCanvas();
                return ToolResponse.Success(new { graphId, deleted = objects.Count, includeGroups }, $"Cleared Grasshopper graph '{graphId}'.");
            });

    private static IMcpTool CapturePreview()
        => Tool(
            "grasshopper_preview_capture",
            "Captures visible Grasshopper preview geometry from the Rhino viewport as base64 PNG.",
            RhinoSchemas.Object(new Dictionary<string, object>
            {
                ["graphId"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Optional graph id filter" },
                ["targets"] = RhinoSchemas.StringArray("Optional Grasshopper object ids, aliases, or nicknames"),
                ["viewport"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "active, perspective, top, front, right, left, back, bottom" },
                ["width"] = RhinoSchemas.Integer("PNG width", 100, 4096, 800),
                ["height"] = RhinoSchemas.Integer("PNG height", 100, 4096, 600),
                ["includeHidden"] = RhinoSchemas.Boolean("Include hidden preview objects", defaultValue: false),
                ["recompute"] = RhinoSchemas.Boolean("Run solution before capture", defaultValue: true),
                ["paddingFactor"] = RhinoSchemas.Number("Bounds padding factor", minimum: 1, defaultValue: 1.15),
            }),
            (reader, rhinoDoc) =>
            {
                var doc = GetActiveDocument(openCanvas: true, makeActive: true);
                if (reader.GetBool("recompute", fallback: true))
                    RunGrasshopperSolution(doc, expireAll: false);

                var candidates = ResolvePreviewCandidates(doc, reader);
                var includeHidden = reader.GetBool("includeHidden");
                var bounds = BoundingBox.Empty;
                var previewObjects = new List<object>();
                var skipped = 0;
                foreach (var obj in candidates)
                {
                    if (obj is not IGH_PreviewObject preview || !preview.IsPreviewCapable || (!includeHidden && preview.Hidden))
                    {
                        skipped++;
                        continue;
                    }

                    var box = preview.ClippingBox;
                    if (!box.IsValid)
                    {
                        skipped++;
                        continue;
                    }

                    bounds.Union(box);
                    previewObjects.Add(new { id = obj.InstanceGuid.ToString("D"), obj.Name, obj.NickName, bounds = Box(box) });
                }

                if (!bounds.IsValid)
                    throw new InvalidOperationException("No visible Grasshopper preview bounds were found.");

                var view = ResolveView(rhinoDoc, reader.GetString("viewport", "active"));
                var padded = Pad(bounds, Math.Max(1, reader.GetDouble("paddingFactor", 1.15)));
                view.ActiveViewport.ZoomBoundingBox(padded);
                view.Redraw();

                var width = Math.Clamp(reader.GetInt("width", 800), 100, 4096);
                var height = Math.Clamp(reader.GetInt("height", 600), 100, 4096);
                using var bitmap = view.CaptureToBitmap(new Size(width, height));
                if (bitmap is null)
                    throw new InvalidOperationException("Rhino did not return a viewport bitmap.");

                using var stream = new MemoryStream();
                bitmap.Save(stream, ImageFormat.Png);
                return ToolResponse.Success(new
                {
                    imageData = Convert.ToBase64String(stream.ToArray()),
                    mimeType = "image/png",
                    width,
                    height,
                    viewport = view.ActiveViewport.Name,
                    capturedPreviewObjectCount = previewObjects.Count,
                    skippedCount = skipped,
                    bounds = Box(bounds),
                    paddedBounds = Box(padded),
                    previewObjects,
                    visibility = VisibilityState(doc),
                }, "Captured Grasshopper preview.");
            });

    private static IMcpTool Tool(string name, string description, JsonElement schema, Func<ArgumentReader, RhinoDoc, ToolResponse> execute)
        => new DelegateRhinoTool(
            name,
            description,
            schema,
            args => RhinoUiThread.InvokeWithActiveDocumentAsync(document => execute(new ArgumentReader(args), document)));

    private static GH_Document GetActiveDocument(bool required = true, bool createIfMissing = false, bool openCanvas = false, bool makeActive = false)
    {
        if (openCanvas)
            EnsureCanvasOpen();

        var canvas = Instances.ActiveCanvas;
        var server = Instances.DocumentServer;
        var doc = canvas?.Document;
        if (doc is null && server.DocumentCount > 0)
            doc = server.NextAvailableDocument();

        if (doc is null && createIfMissing)
        {
            doc = server.AddNewDocument();
            if (doc is not null)
                server.PromoteDocument(doc);
        }

        if (doc is not null && makeActive)
            server.PromoteDocument(doc);

        if (doc is not null && canvas is not null && canvas.Document != doc)
            canvas.Document = doc;

        if (doc is null && required)
            throw new InvalidOperationException("No active Grasshopper document is available.");

        return doc!;
    }

    private static void EnsureCanvasOpen()
    {
        if (Instances.ActiveCanvas is not null)
            return;

        RhinoApp.RunScript("_Grasshopper", false);
        RhinoApp.Wait();
    }

    private static void RedrawCanvas(PointF? focus = null)
    {
        var canvas = Instances.ActiveCanvas;
        if (canvas is not null && focus.HasValue)
            canvas.Viewport.Focus(focus.Value);

        Instances.InvalidateCanvas();
        Instances.RedrawCanvas();
    }

    private static void RunGrasshopperSolution(GH_Document doc, bool expireAll)
    {
        if (expireAll)
            doc.ExpireSolution();

        doc.NewSolution(expireAll, GH_SolutionMode.CommandLine);
    }

    private static object DocumentSummary(GH_Document doc)
        => new
        {
            objectCount = doc.Objects.Count,
            componentCount = doc.Objects.OfType<IGH_Component>().Count(),
            parameterCount = doc.Objects.OfType<IGH_Param>().Count(),
            groupCount = doc.Objects.OfType<GH_Group>().Count(),
            solutionState = doc.SolutionState.ToString(),
        };

    private static object CanvasState(GH_Document doc)
        => new
        {
            canvasOpen = Instances.ActiveCanvas is not null,
            activeCanvasDocument = Instances.ActiveCanvas?.Document == doc,
            objectCount = doc.Objects.Count,
        };

    private static object VisibilityState(GH_Document doc)
    {
        var previewCapable = doc.Objects.OfType<IGH_PreviewObject>().ToArray();
        var previewEnabled = previewCapable.Count(obj => !obj.Hidden);
        return new
        {
            previewCapableCount = previewCapable.Length,
            previewEnabledCount = previewEnabled,
            previewDisabledCount = previewCapable.Length - previewEnabled,
            hasPreviewEnabledObjects = previewEnabled > 0,
        };
    }

    private static IEnumerable<IGH_ObjectProxy> FilterProxies(string? query, string? category, string? subcategory)
    {
        IEnumerable<IGH_ObjectProxy> proxies = Instances.ComponentServer.ObjectProxies;
        if (!string.IsNullOrWhiteSpace(category))
            proxies = proxies.Where(proxy => proxy.Desc.Category?.Contains(category, StringComparison.OrdinalIgnoreCase) == true);
        if (!string.IsNullOrWhiteSpace(subcategory))
            proxies = proxies.Where(proxy => proxy.Desc.SubCategory?.Contains(subcategory, StringComparison.OrdinalIgnoreCase) == true);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var alias = ComponentAliases.TryGetValue(query, out var aliasValue) ? aliasValue : query;
            proxies = proxies.Where(proxy =>
                proxy.Desc.Name?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
                proxy.Desc.NickName?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
                proxy.Desc.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
                proxy.Desc.Name?.Contains(alias, StringComparison.OrdinalIgnoreCase) == true);
        }

        return proxies.GroupBy(proxy => proxy.Guid).Select(group => group.First());
    }

    private static IOrderedEnumerable<IGH_ObjectProxy> OrderProxiesForQuery(IEnumerable<IGH_ObjectProxy> proxies, string? query)
    {
        var normalized = !string.IsNullOrWhiteSpace(query) && ComponentAliases.TryGetValue(query, out var alias)
            ? alias
            : query;

        return proxies
            .OrderByDescending(proxy => IsExactProxyMatch(proxy, normalized))
            .ThenBy(ProxyKindRank)
            .ThenBy(proxy => proxy.Desc.Category)
            .ThenBy(proxy => proxy.Desc.SubCategory)
            .ThenBy(proxy => proxy.Desc.Name)
            .ThenBy(proxy => proxy.Desc.NickName);
    }

    private static bool IsExactProxyMatch(IGH_ObjectProxy proxy, string? query)
        => !string.IsNullOrWhiteSpace(query) && (
            string.Equals(proxy.Desc.Name, query, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(proxy.Desc.NickName, query, StringComparison.OrdinalIgnoreCase));

    private static int ProxyKindRank(IGH_ObjectProxy proxy)
    {
        if (proxy.Type is not null && typeof(IGH_Component).IsAssignableFrom(proxy.Type))
            return 0;
        if (proxy.Type is not null && typeof(IGH_Param).IsAssignableFrom(proxy.Type))
            return 1;

        return 2;
    }

    private static object ProxySummary(IGH_ObjectProxy proxy, bool includeDescription)
        => new
        {
            guid = proxy.Guid.ToString("D"),
            name = proxy.Desc.Name,
            nickname = proxy.Desc.NickName,
            category = proxy.Desc.Category,
            subcategory = proxy.Desc.SubCategory,
            kind = Classify(proxy.Type),
            description = includeDescription ? proxy.Desc.Description : null,
        };

    private static object ComponentTypeInfo(string? componentName, string? componentGuid)
    {
        var proxy = FindProxy(componentName, componentGuid);
        if (proxy is null)
            throw new ArgumentException($"Grasshopper component type '{componentName ?? componentGuid}' was not found.");

        var instance = proxy.CreateInstance();
        var inputs = Array.Empty<object>();
        var outputs = Array.Empty<object>();
        var isParameter = false;
        if (instance is IGH_Component component)
        {
            inputs = ParamsToObjects(component.Params.Input, includeSources: false, includeValues: false, maxItems: 0);
            outputs = ParamsToObjects(component.Params.Output, includeSources: false, includeValues: false, maxItems: 0);
        }
        else if (instance is IGH_Param param)
        {
            isParameter = true;
            outputs = [ParamToObject(param, 0, includeSources: false, includeValues: false, maxItems: 0)];
        }

        return new
        {
            proxy = ProxySummary(proxy, includeDescription: true),
            isParameter,
            inputCount = inputs.Length,
            outputCount = outputs.Length,
            inputs,
            outputs,
        };
    }

    private static object ObjectSummary(IGH_DocumentObject obj)
        => new
        {
            id = obj.InstanceGuid.ToString("D"),
            obj.Name,
            obj.NickName,
            obj.Category,
            obj.SubCategory,
            type = obj.GetType().Name,
            kind = Classify(obj.GetType()),
            position = obj.Attributes is null ? null : new[] { (double)obj.Attributes.Pivot.X, obj.Attributes.Pivot.Y },
            alias = MetadataValue(obj, MetaAlias),
            graphId = MetadataValue(obj, MetaGraphId),
            role = MetadataValue(obj, MetaRole),
        };

    private static object ObjectDetails(IGH_DocumentObject obj, bool includeValues = false, int maxItems = 20)
    {
        var summary = ObjectSummary(obj);
        var special = SpecialState(obj);
        if (obj is IGH_Component component)
        {
            return new
            {
                summary,
                component.Description,
                component.Obsolete,
                runtimeMessageLevel = component.RuntimeMessageLevel.ToString(),
                runtimeMessages = RuntimeMessages(component),
                inputs = ParamsToObjects(component.Params.Input, includeSources: true, includeValues, maxItems),
                outputs = ParamsToObjects(component.Params.Output, includeSources: true, includeValues, maxItems),
                special,
            };
        }

        if (obj is IGH_Param param)
        {
            return new
            {
                summary,
                param.TypeName,
                param.SourceCount,
                recipientCount = param.Recipients.Count,
                value = includeValues ? ParamVolatileData(param, maxItems) : null,
                special,
            };
        }

        return new { summary, special };
    }

    private static object? SpecialState(IGH_DocumentObject obj)
    {
        if (obj is GH_NumberSlider slider)
        {
            return new
            {
                specialType = "number_slider",
                value = (double)slider.CurrentValue,
                min = (double)slider.Slider.Minimum,
                max = (double)slider.Slider.Maximum,
                decimals = slider.Slider.DecimalPlaces,
                expression = slider.Expression,
            };
        }

        if (obj is GH_BooleanToggle toggle)
            return new { specialType = "boolean_toggle", value = toggle.Value };
        if (obj is GH_Panel panel)
            return new { specialType = "panel", content = panel.UserText };
        if (obj is GH_ValueList valueList)
        {
            return new
            {
                specialType = "value_list",
                itemCount = valueList.ListItems.Count,
                selectedIndex = valueList.ListItems.Cast<GH_ValueListItem>().ToList().FindIndex(item => item.Selected),
                items = valueList.ListItems.Cast<GH_ValueListItem>().Select((item, index) => new
                {
                    index,
                    item.Name,
                    item.Expression,
                    item.Selected,
                }).ToArray(),
            };
        }

        return null;
    }

    private static object[] ParamsToObjects(IList<IGH_Param> parameters, bool includeSources, bool includeValues, int maxItems)
        => parameters.Select((param, index) => ParamToObject(param, index, includeSources, includeValues, maxItems)).ToArray();

    private static object ParamToObject(IGH_Param param, int index, bool includeSources, bool includeValues, int maxItems)
        => new
        {
            index,
            param.Name,
            param.NickName,
            param.Description,
            type = param.TypeName,
            access = param.Access.ToString(),
            param.Optional,
            param.SourceCount,
            recipientCount = param.Recipients.Count,
            hasData = ParamHasData(param),
            dataCount = param.VolatileData.DataCount,
            branchCount = param.VolatileData.PathCount,
            sources = includeSources ? param.Sources.Select(source => ParamReference(source)).ToArray() : null,
            recipients = includeSources ? param.Recipients.Select(recipient => ParamReference(recipient)).ToArray() : null,
            value = includeValues ? ParamVolatileData(param, maxItems) : null,
        };

    private static object ParamReference(IGH_Param param)
        => new
        {
            componentId = param.Attributes?.GetTopLevel.DocObject.InstanceGuid.ToString("D"),
            param.Name,
            param.NickName,
        };

    private static object ParamEndpoint(IGH_DocumentObject obj, IGH_Param param)
        => new
        {
            objectId = obj.InstanceGuid.ToString("D"),
            obj.NickName,
            paramName = param.Name,
            paramNickname = param.NickName,
            paramType = param.TypeName,
        };

    private static object ParamValue(IGH_DocumentObject obj, IGH_Param param, int maxItems)
        => new
        {
            target = ParamEndpoint(obj, param),
            data = ParamVolatileData(param, maxItems),
        };

    private static object ParamVolatileData(IGH_Param param, int maxItems)
    {
        var branches = new List<object>();
        var itemCount = 0;
        foreach (GH_Path path in param.VolatileData.Paths)
        {
            var items = new List<object?>();
            foreach (var item in param.VolatileData.get_Branch(path))
            {
                if (itemCount >= maxItems)
                    break;

                items.Add(ExtractValue(item));
                itemCount++;
            }

            branches.Add(new { path = path.ToString(), items });
            if (itemCount >= maxItems)
                break;
        }

        return new
        {
            dataCount = param.VolatileData.DataCount,
            branchCount = param.VolatileData.PathCount,
            truncated = param.VolatileData.DataCount > maxItems,
            branches,
        };
    }

    private static object? ExtractValue(object? item)
    {
        if (item is null) return null;
        if (item is GH_Number number) return number.Value;
        if (item is GH_Integer integer) return integer.Value;
        if (item is GH_Boolean boolean) return boolean.Value;
        if (item is GH_String text) return text.Value;
        if (item is GH_Point point) return Point(point.Value);
        if (item is GH_Vector vector) return new { x = vector.Value.X, y = vector.Value.Y, z = vector.Value.Z };
        if (item is GH_Line line) return new { from = Point(line.Value.From), to = Point(line.Value.To) };
        if (item is GH_Circle circle) return new { center = Point(circle.Value.Center), circle.Value.Radius };
        if (item is GH_Curve curve) return new { type = "Curve", isClosed = curve.Value?.IsClosed, length = curve.Value?.GetLength() };
        if (item is GH_Brep brep) return new { type = "Brep", isSolid = brep.Value?.IsSolid, faceCount = brep.Value?.Faces.Count };
        if (item is GH_Mesh mesh) return new { type = "Mesh", vertexCount = mesh.Value?.Vertices.Count, faceCount = mesh.Value?.Faces.Count };
        if (item is IGH_Goo goo) return new { type = goo.TypeName, description = goo.ToString() };
        return item.ToString();
    }

    private static IGH_DocumentObject CreateObject(string? componentName, string? componentGuid, JsonElement? initialValue)
    {
        var special = TryCreateSpecialObject(componentName, initialValue);
        if (special is not null)
            return special;

        var proxy = FindProxy(componentName, componentGuid);
        if (proxy is null)
            throw new ArgumentException($"Grasshopper component type '{componentName ?? componentGuid}' was not found. Use grasshopper_component_search first.");

        var obj = proxy.CreateInstance();
        if (obj is null)
            throw new InvalidOperationException($"Grasshopper component type '{proxy.Desc.Name}' could not be instantiated.");

        if (initialValue.HasValue)
            InitializeSpecialObject(obj, initialValue.Value);

        return obj;
    }

    private static IGH_DocumentObject? TryCreateSpecialObject(string? componentName, JsonElement? initialValue)
    {
        if (string.IsNullOrWhiteSpace(componentName))
            return null;

        var name = ComponentAliases.TryGetValue(componentName, out var alias) ? alias : componentName;
        if (name.Equals("Number Slider", StringComparison.OrdinalIgnoreCase))
        {
            var slider = new GH_NumberSlider();
            InitializeSlider(slider, initialValue);
            return slider;
        }

        if (name.Equals("Boolean Toggle", StringComparison.OrdinalIgnoreCase))
            return new GH_BooleanToggle { Value = initialValue.HasValue && ToBoolean(initialValue.Value) };
        if (name.Equals("Panel", StringComparison.OrdinalIgnoreCase))
        {
            var panel = new GH_Panel();
            if (initialValue.HasValue)
                panel.SetUserText(initialValue.Value.ToString());
            return panel;
        }

        if (name.Equals("Value List", StringComparison.OrdinalIgnoreCase))
            return new GH_ValueList();
        if (name.Equals("Relay", StringComparison.OrdinalIgnoreCase))
            return new GH_Relay();
        if (name.Equals("Scribble", StringComparison.OrdinalIgnoreCase))
            return new GH_Scribble { Text = initialValue?.ToString() ?? "Note" };

        return null;
    }

    private static void InitializeSpecialObject(IGH_DocumentObject obj, JsonElement value)
    {
        if (obj is GH_NumberSlider slider)
            InitializeSlider(slider, value);
        else if (obj is GH_BooleanToggle toggle)
            toggle.Value = ToBoolean(value);
        else if (obj is GH_Panel panel)
            panel.SetUserText(value.ToString());
    }

    private static void InitializeSlider(GH_NumberSlider slider, JsonElement? initialValue)
    {
        var min = 0m;
        var max = 100m;
        var value = 50m;
        var decimals = 2;
        if (initialValue.HasValue)
        {
            if (initialValue.Value.ValueKind == JsonValueKind.Object)
            {
                min = GetDecimal(initialValue.Value, "min") ?? min;
                max = GetDecimal(initialValue.Value, "max") ?? max;
                value = GetDecimal(initialValue.Value, "value") ?? value;
                decimals = GetInt(initialValue.Value, "decimals") ?? decimals;
            }
            else
            {
                value = ToDecimal(initialValue.Value);
            }
        }

        if (min > max)
            throw new ArgumentException("Slider min must be less than or equal to max.");

        value = Math.Min(Math.Max(value, min), max);
        slider.Slider.Minimum = min;
        slider.Slider.Maximum = max;
        slider.Slider.DecimalPlaces = Math.Clamp(decimals, 0, 12);
        SetSliderValue(slider, value);
    }

    private static IGH_ObjectProxy? FindProxy(string? componentName, string? componentGuid)
    {
        var proxies = Instances.ComponentServer.ObjectProxies.ToList();
        if (!string.IsNullOrWhiteSpace(componentGuid))
        {
            if (!Guid.TryParse(componentGuid, out var guid))
                throw new ArgumentException($"Invalid Grasshopper component GUID: {componentGuid}");

            return proxies.FirstOrDefault(proxy => proxy.Guid == guid);
        }

        if (string.IsNullOrWhiteSpace(componentName))
            return null;

        var normalized = ComponentAliases.TryGetValue(componentName, out var alias) ? alias : componentName;
        return OrderProxiesForQuery(proxies.Where(proxy => string.Equals(proxy.Desc.Name, normalized, StringComparison.OrdinalIgnoreCase)), normalized).FirstOrDefault()
            ?? OrderProxiesForQuery(proxies.Where(proxy => string.Equals(proxy.Desc.NickName, normalized, StringComparison.OrdinalIgnoreCase)), normalized).FirstOrDefault()
            ?? OrderProxiesForQuery(proxies.Where(proxy => proxy.Desc.Name?.Contains(normalized, StringComparison.OrdinalIgnoreCase) == true), normalized).FirstOrDefault()
            ?? OrderProxiesForQuery(proxies.Where(proxy => proxy.Desc.NickName?.Contains(normalized, StringComparison.OrdinalIgnoreCase) == true), normalized).FirstOrDefault();
    }

    private static void PlaceAndConfigureObject(IGH_DocumentObject obj, ArgumentReader reader, PointF fallback)
    {
        if (obj.Attributes is null)
            obj.CreateAttributes();

        obj.Attributes!.Pivot = ReadPosition(reader, "position", fallback);
        var nickname = reader.GetString("nickname");
        if (!string.IsNullOrWhiteSpace(nickname))
            obj.NickName = nickname;

        if (reader.Has("preview") && obj is IGH_PreviewObject preview)
            preview.Hidden = !reader.GetBool("preview");
        if (reader.Has("enabled") && obj is IGH_ActiveObject active)
            active.Locked = !reader.GetBool("enabled");
    }

    private static void PlaceAndConfigureObject(IGH_DocumentObject obj, JsonElement spec, PointF fallback)
    {
        if (obj.Attributes is null)
            obj.CreateAttributes();

        obj.Attributes!.Pivot = ReadPosition(spec, "position", fallback);
        var nickname = GetString(spec, "nickname");
        if (!string.IsNullOrWhiteSpace(nickname))
            obj.NickName = nickname;

        if (TryGet(spec, "preview", out var previewValue) && obj is IGH_PreviewObject preview)
            preview.Hidden = !ToBoolean(previewValue);
        if (TryGet(spec, "enabled", out var enabledValue) && obj is IGH_ActiveObject active)
            active.Locked = !ToBoolean(enabledValue);
    }

    private static void ApplyObjectUpdate(IGH_DocumentObject obj, ArgumentReader reader)
    {
        var nickname = reader.GetString("newNickname") ?? reader.GetString("nickname");
        if (!string.IsNullOrWhiteSpace(nickname))
            obj.NickName = nickname;
        if (reader.Has("position"))
        {
            if (obj.Attributes is null)
                obj.CreateAttributes();
            obj.Attributes!.Pivot = ReadPosition(reader, "position", obj.Attributes.Pivot);
            obj.Attributes.ExpireLayout();
        }
        if (reader.Has("preview") && obj is IGH_PreviewObject preview)
            preview.Hidden = !reader.GetBool("preview");
        if (reader.Has("enabled") && obj is IGH_ActiveObject active)
            active.Locked = !reader.GetBool("enabled");

        obj.ExpireSolution(false);
    }

    private static void ApplyObjectUpdate(IGH_DocumentObject obj, JsonElement spec)
    {
        var nickname = GetString(spec, "newNickname", "new_nickname", "nickname");
        if (!string.IsNullOrWhiteSpace(nickname))
            obj.NickName = nickname;
        if (TryGet(spec, "position", out var _))
        {
            if (obj.Attributes is null)
                obj.CreateAttributes();
            obj.Attributes!.Pivot = ReadPosition(spec, "position", obj.Attributes.Pivot);
            obj.Attributes.ExpireLayout();
        }
        if (TryGet(spec, "preview", out var previewValue) && obj is IGH_PreviewObject preview)
            preview.Hidden = !ToBoolean(previewValue);
        if (TryGet(spec, "enabled", out var enabledValue) && obj is IGH_ActiveObject active)
            active.Locked = !ToBoolean(enabledValue);

        obj.ExpireSolution(false);
    }

    private static void ApplyMetadata(IGH_DocumentObject obj, string? alias, string? graphId, string? role)
    {
        if (obj is not GH_DocumentObject docObject)
            return;

        if (!string.IsNullOrWhiteSpace(alias))
            SetMetadata(docObject, MetaAlias, alias.Trim());
        if (!string.IsNullOrWhiteSpace(graphId))
            SetMetadata(docObject, MetaGraphId, graphId.Trim());
        if (!string.IsNullOrWhiteSpace(role))
            SetMetadata(docObject, MetaRole, role.Trim());
    }

    private static void SetMetadata(GH_DocumentObject obj, string key, string value)
        => SetStringValueMethod?.Invoke(obj, [key, value]);

    private static string MetadataValue(IGH_DocumentObject obj, string key)
        => obj is GH_DocumentObject docObject
            ? GetStringValueMethod?.Invoke(docObject, [key, string.Empty])?.ToString() ?? string.Empty
            : string.Empty;

    private static IGH_DocumentObject FindObject(GH_Document doc, ArgumentReader reader, string prefix = "")
    {
        var id = FirstNonEmpty(
            reader.GetString($"{prefix}Id"),
            reader.GetString($"{prefix}_id"),
            reader.GetString($"{prefix}InstanceId"),
            reader.GetString($"{prefix}_instance_id"),
            prefix.Length == 0 ? reader.GetString("componentId") : null,
            prefix.Length == 0 ? reader.GetString("component_id") : null);
        var alias = FirstNonEmpty(reader.GetString($"{prefix}Alias"), reader.GetString($"{prefix}_alias"), prefix.Length == 0 ? reader.GetString("alias") : null);
        var nickname = FirstNonEmpty(reader.GetString($"{prefix}Nickname"), reader.GetString($"{prefix}_nickname"), prefix.Length == 0 ? reader.GetString("nickname") : null);
        return FindObject(doc, id, alias, nickname);
    }

    private static IGH_DocumentObject FindObject(GH_Document doc, string? id, string? alias, string? nickname)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            if (!Guid.TryParse(id, out var guid))
            {
                if (string.IsNullOrWhiteSpace(alias) && string.IsNullOrWhiteSpace(nickname))
                    throw new ArgumentException($"Invalid Grasshopper object GUID: {id}");
            }
            else
            {
                var byId = doc.FindObject(guid, true);
                if (byId is not null)
                    return byId;
            }
        }

        if (!string.IsNullOrWhiteSpace(alias))
        {
            var matches = doc.Objects
                .Where(obj => MetadataValue(obj, MetaAlias).Equals(alias, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (matches.Length == 1)
                return matches[0];
            if (matches.Length > 1)
                throw new InvalidOperationException($"Grasshopper alias '{alias}' is ambiguous; use id.");
        }

        if (!string.IsNullOrWhiteSpace(nickname))
        {
            var matches = doc.Objects.Where(obj => obj.NickName.Equals(nickname, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length == 1)
                return matches[0];
            if (matches.Length > 1)
                throw new InvalidOperationException($"Grasshopper nickname '{nickname}' is ambiguous; use id.");
        }

        throw new InvalidOperationException($"Grasshopper object '{id ?? alias ?? nickname ?? "(missing selector)"}' was not found.");
    }

    private static IGH_DocumentObject ResolveGraphObject(GH_Document doc, Dictionary<string, IGH_DocumentObject> aliases, string selector)
    {
        selector = selector.Trim();
        if (aliases.TryGetValue(selector, out var aliased))
            return aliased;

        return FindObject(doc, selector, selector, selector);
    }

    private static IGH_DocumentObject ResolveGraphEndpoint(GH_Document doc, Dictionary<string, IGH_DocumentObject> aliases, JsonElement spec, string role)
    {
        var selector = GraphEndpointSelector(spec, role);
        if (string.IsNullOrWhiteSpace(selector))
            throw new ArgumentException($"Property '{role}' is required.");

        return ResolveGraphObject(doc, aliases, selector);
    }

    private static string? GraphEndpointSelector(JsonElement spec, string role)
    {
        var endpoint = GetEndpointElement(spec, role);
        var nestedSelector = endpoint.HasValue ? GraphEndpointSelector(endpoint.Value) : null;
        if (!string.IsNullOrWhiteSpace(nestedSelector))
            return nestedSelector;

        return role switch
        {
            "source" => FirstNonEmpty(
                GetString(spec, "sourceAlias", "source_alias"),
                GetString(spec, "sourceId", "source_id", "sourceInstanceId", "source_instance_id"),
                GetString(spec, "sourceNickname", "source_nickname"),
                GetString(spec, "fromAlias", "from_alias"),
                GetString(spec, "fromId", "from_id", "fromInstanceId", "from_instance_id"),
                GetString(spec, "fromNickname", "from_nickname")),
            "target" => FirstNonEmpty(
                GetString(spec, "targetAlias", "target_alias"),
                GetString(spec, "targetId", "target_id", "targetInstanceId", "target_instance_id"),
                GetString(spec, "targetNickname", "target_nickname"),
                GetString(spec, "toAlias", "to_alias"),
                GetString(spec, "toId", "to_id", "toInstanceId", "to_instance_id"),
                GetString(spec, "toNickname", "to_nickname")),
            _ => FirstNonEmpty(
                GetString(spec, $"{role}Alias", $"{role}_alias"),
                GetString(spec, $"{role}Id", $"{role}_id", $"{role}InstanceId", $"{role}_instance_id"),
                GetString(spec, $"{role}Nickname", $"{role}_nickname"))
        };
    }

    private static string? GraphEndpointSelector(JsonElement endpoint)
    {
        return endpoint.ValueKind switch
        {
            JsonValueKind.String => endpoint.GetString(),
            JsonValueKind.Object => FirstNonEmpty(
                GetString(endpoint, "alias", "objectAlias", "object_alias", "componentAlias", "component_alias"),
                GetString(endpoint, "id", "guid", "instanceId", "instance_id", "objectId", "object_id", "componentId", "component_id"),
                GetString(endpoint, "nickname", "nickName", "name")),
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            _ => endpoint.ToString()
        };
    }

    private static JsonElement? GetEndpointElement(JsonElement spec, string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return null;

        return role switch
        {
            "source" => GetElement(spec, "source", "from"),
            "target" => GetElement(spec, "target", "to", "destination"),
            _ => GetElement(spec, role)
        };
    }

    private static IGH_Param FindInputParam(IGH_DocumentObject obj, ArgumentReader reader, string prefix)
    {
        var index = FirstInt(reader.GetString($"{prefix}InputIndex"), reader.GetString($"{prefix}_input_index"), reader.GetString("inputIndex"), reader.GetString("input"));
        var name = FirstNonEmpty(reader.GetString($"{prefix}InputName"), reader.GetString($"{prefix}_input_name"), reader.GetString("inputName"), reader.GetString("paramName"));
        return FindInputParam(obj, index, name);
    }

    private static IGH_Param FindOutputParam(IGH_DocumentObject obj, ArgumentReader reader, string prefix)
    {
        var index = FirstInt(reader.GetString($"{prefix}OutputIndex"), reader.GetString($"{prefix}_output_index"), reader.GetString("outputIndex"), reader.GetString("output"));
        var name = FirstNonEmpty(reader.GetString($"{prefix}OutputName"), reader.GetString($"{prefix}_output_name"), reader.GetString("outputName"), reader.GetString("paramName"));
        return FindOutputParam(obj, index, name);
    }

    private static IGH_Param FindInputParam(IGH_DocumentObject obj, JsonElement spec, string prefix)
    {
        var endpoint = GetEndpointElement(spec, prefix);
        var index = endpoint.HasValue ? GetParamIndex(endpoint.Value, "input") : null;
        var name = endpoint.HasValue ? GetParamName(endpoint.Value, "input") : null;
        index ??= GetInt(spec, $"{prefix}InputIndex", $"{prefix}_input_index", "inputIndex", "input", "targetInputIndex", "target_input_index");
        name ??= GetString(spec, $"{prefix}InputName", $"{prefix}_input_name", "inputName", "paramName", "param_name", "targetInputName", "target_input_name", "targetParamName", "target_param_name");
        return FindInputParam(obj, index, name);
    }

    private static IGH_Param FindOutputParam(IGH_DocumentObject obj, JsonElement spec, string prefix)
    {
        var endpoint = GetEndpointElement(spec, prefix);
        var index = endpoint.HasValue ? GetParamIndex(endpoint.Value, "output") : null;
        var name = endpoint.HasValue ? GetParamName(endpoint.Value, "output") : null;
        index ??= GetInt(spec, $"{prefix}OutputIndex", $"{prefix}_output_index", "outputIndex", "output", "sourceOutputIndex", "source_output_index");
        name ??= GetString(spec, $"{prefix}OutputName", $"{prefix}_output_name", "outputName", "paramName", "param_name", "sourceOutputName", "source_output_name", "sourceParamName", "source_param_name");
        return FindOutputParam(obj, index, name);
    }

    private static int? GetParamIndex(JsonElement endpoint, string kind)
        => endpoint.ValueKind == JsonValueKind.Object
            ? GetInt(endpoint, $"{kind}Index", $"{kind}_index", kind, "paramIndex", "param_index", "parameterIndex", "parameter_index", "index")
            : null;

    private static string? GetParamName(JsonElement endpoint, string kind)
        => endpoint.ValueKind == JsonValueKind.Object
            ? GetString(endpoint, $"{kind}Name", $"{kind}_name", $"{kind}Nickname", $"{kind}_nickname", "paramName", "param_name", "parameterName", "parameter_name", "param", "parameter", "portName", "port_name", "socketName", "socket_name")
            : null;

    private static IGH_Param FindInputParam(IGH_DocumentObject obj, int? index, string? name)
    {
        if (obj is IGH_Component component)
            return PickParam(component.Params.Input, index, name, "input", obj.NickName);
        if (obj is IGH_Param param)
            return param;

        throw new InvalidOperationException($"Grasshopper object '{obj.NickName}' has no input parameters.");
    }

    private static IGH_Param FindOutputParam(IGH_DocumentObject obj, int? index, string? name)
    {
        if (obj is IGH_Component component)
            return PickParam(component.Params.Output, index, name, "output", obj.NickName);
        if (obj is IGH_Param param)
            return param;

        throw new InvalidOperationException($"Grasshopper object '{obj.NickName}' has no output parameters.");
    }

    private static IGH_Param PickParam(IList<IGH_Param> parameters, int? index, string? name, string kind, string owner)
    {
        if (index.HasValue)
        {
            if (index.Value < 0 || index.Value >= parameters.Count)
                throw new ArgumentException($"{owner} {kind} index {index.Value} is out of range.");

            return parameters[index.Value];
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            var match = parameters.FirstOrDefault(param =>
                param.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                param.NickName.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
                return match;
        }

        if (parameters.Count == 0)
            throw new InvalidOperationException($"Grasshopper object '{owner}' has no {kind} parameters.");

        return parameters[0];
    }

    private static bool TrySetSpecialValue(IGH_DocumentObject obj, JsonElement value, ArgumentReader reader, out object result)
    {
        if (obj is GH_NumberSlider slider)
        {
            if (reader.Has("min")) slider.Slider.Minimum = (decimal)reader.GetDouble("min");
            if (reader.Has("max")) slider.Slider.Maximum = (decimal)reader.GetDouble("max");
            if (reader.Has("decimals")) slider.Slider.DecimalPlaces = Math.Clamp(reader.GetInt("decimals"), 0, 12);
            var numeric = Math.Min(Math.Max(ToDecimal(value), slider.Slider.Minimum), slider.Slider.Maximum);
            SetSliderValue(slider, numeric);
            ExpireRecipients(slider);
            result = new { id = obj.InstanceGuid.ToString("D"), obj.NickName, value = (double)numeric, min = (double)slider.Slider.Minimum, max = (double)slider.Slider.Maximum };
            return true;
        }

        if (obj is GH_BooleanToggle toggle)
        {
            toggle.Value = ToBoolean(value);
            toggle.ExpireSolution(true);
            result = new { id = obj.InstanceGuid.ToString("D"), obj.NickName, toggle.Value };
            return true;
        }

        if (obj is GH_Panel panel)
        {
            panel.SetUserText(value.ToString());
            panel.ExpireSolution(true);
            result = new { id = obj.InstanceGuid.ToString("D"), obj.NickName, value = panel.UserText };
            return true;
        }

        if (obj is GH_ValueList valueList)
        {
            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var index))
            {
                if (index < 0 || index >= valueList.ListItems.Count)
                    throw new ArgumentOutOfRangeException(nameof(value), $"Value list index {index} is out of range.");

                valueList.SelectItem(index);
            }
            else
            {
                var requested = value.ToString();
                var item = valueList.ListItems.Cast<GH_ValueListItem>().FirstOrDefault(candidate =>
                    candidate.Name.Equals(requested, StringComparison.OrdinalIgnoreCase) ||
                    candidate.Expression.Equals(requested, StringComparison.OrdinalIgnoreCase));
                if (item is null)
                    throw new InvalidOperationException($"Value list item '{requested}' was not found.");

                valueList.SelectItem(valueList.ListItems.IndexOf(item));
            }

            valueList.ExpireSolution(true);
            result = new { id = obj.InstanceGuid.ToString("D"), obj.NickName, special = SpecialState(obj) };
            return true;
        }

        result = new { };
        return false;
    }

    private static void SetParamValue(IGH_Param param, JsonElement value)
    {
        param.RemoveAllSources();
        if (param is Param_Number numberParam)
        {
            numberParam.PersistentData.ClearData();
            foreach (var item in ScalarItems(value))
                numberParam.PersistentData.Append(new GH_Number(ToDouble(item)));
        }
        else if (param is Param_Integer intParam)
        {
            intParam.PersistentData.ClearData();
            foreach (var item in ScalarItems(value))
                intParam.PersistentData.Append(new GH_Integer(ToInt(item)));
        }
        else if (param is Param_Boolean boolParam)
        {
            boolParam.PersistentData.ClearData();
            boolParam.PersistentData.Append(new GH_Boolean(ToBoolean(value)));
        }
        else if (param is Param_String stringParam)
        {
            stringParam.PersistentData.ClearData();
            foreach (var item in ScalarItems(value))
                stringParam.PersistentData.Append(new GH_String(item.ToString()));
        }
        else if (param is Param_Point pointParam)
        {
            pointParam.PersistentData.ClearData();
            if (value.ValueKind == JsonValueKind.Array && value.GetArrayLength() > 0 && value[0].ValueKind == JsonValueKind.Array)
            {
                foreach (var item in value.EnumerateArray())
                    pointParam.PersistentData.Append(new GH_Point(ToPoint(item)));
            }
            else
            {
                pointParam.PersistentData.Append(new GH_Point(ToPoint(value)));
            }
        }
        else if (param is Param_Vector vectorParam)
        {
            vectorParam.PersistentData.ClearData();
            vectorParam.PersistentData.Append(new GH_Vector(ToVector(value)));
        }
        else if (param is Param_Plane planeParam)
        {
            planeParam.PersistentData.ClearData();
            planeParam.PersistentData.Append(new GH_Plane(Plane.WorldXY));
        }
        else
        {
            throw new InvalidOperationException($"Unsupported Grasshopper parameter type '{param.GetType().Name}'. Wire a source component instead.");
        }

        param.ExpireSolution(true);
    }

    private static void SetSliderValue(GH_NumberSlider slider, decimal value)
    {
        if (!slider.TrySetSliderValue(value))
            slider.SetSliderValue(value);

        slider.ClearData();
        slider.AddVolatileData(new GH_Path(0), 0, new GH_Number((double)value));
    }

    private static void ExpireRecipients(IGH_Param source)
    {
        foreach (var recipient in source.Recipients)
        {
            var owner = recipient.Attributes?.GetTopLevel.DocObject;
            if (owner is not null)
                owner.ExpireSolution(true);
            else
                recipient.ExpireSolution(true);
        }
    }

    private static bool ParamHasData(IGH_Param param)
        => param.VolatileDataCount > 0 || param.VolatileData.DataCount > 0;

    private static object RuntimeSummary(GH_Document doc)
    {
        var errors = new List<object>();
        var warnings = new List<object>();
        foreach (var obj in doc.Objects.OfType<IGH_ActiveObject>())
        {
            var errorMessages = obj.RuntimeMessages(GH_RuntimeMessageLevel.Error).ToArray();
            if (errorMessages.Length > 0)
                errors.Add(new { id = ((IGH_DocumentObject)obj).InstanceGuid.ToString("D"), obj.Name, messages = errorMessages });
            var warningMessages = obj.RuntimeMessages(GH_RuntimeMessageLevel.Warning).ToArray();
            if (warningMessages.Length > 0)
                warnings.Add(new { id = ((IGH_DocumentObject)obj).InstanceGuid.ToString("D"), obj.Name, messages = warningMessages });
        }

        return new { success = errors.Count == 0, errorCount = errors.Count, warningCount = warnings.Count, errors, warnings };
    }

    private static string SolutionMessage(object runtime)
    {
        var json = JsonSerializer.SerializeToElement(runtime, McpJson.Default);
        var errors = json.TryGetProperty("errorCount", out var errorElement) ? errorElement.GetInt32() : 0;
        var warnings = json.TryGetProperty("warningCount", out var warningElement) ? warningElement.GetInt32() : 0;
        if (errors > 0) return $"Grasshopper solution has {errors} error(s).";
        if (warnings > 0) return $"Grasshopper solution completed with {warnings} warning(s).";
        return "Grasshopper solution completed successfully.";
    }

    private static object[] RuntimeMessages(IGH_ActiveObject obj)
        => new[] { GH_RuntimeMessageLevel.Error, GH_RuntimeMessageLevel.Warning, GH_RuntimeMessageLevel.Remark }
            .SelectMany(level => obj.RuntimeMessages(level).Select(message => new { level = level.ToString(), message }))
            .Cast<object>()
            .ToArray();

    private static List<IGH_DocumentObject> ResolveObjectList(GH_Document doc, ArgumentReader reader)
    {
        var ids = reader.GetStringArray("ids");
        if (ids.Count > 0)
            return ids.Select(id => FindObject(doc, id, null, null)).ToList();

        var graphId = reader.GetString("graphId") ?? reader.GetString("graph_id");
        if (!string.IsNullOrWhiteSpace(graphId))
            return GraphObjects(doc, graphId).ToList();

        return doc.Objects.Where(obj => obj is not GH_Group).ToList();
    }

    private static IEnumerable<IGH_DocumentObject> GraphObjects(GH_Document doc, string graphId)
        => doc.Objects.Where(obj => MetadataValue(obj, MetaGraphId).Equals(graphId, StringComparison.OrdinalIgnoreCase));

    private static string NormalizeGraphId(string? graphId)
        => string.IsNullOrWhiteSpace(graphId) ? $"algomim_{DateTime.UtcNow:yyyyMMddHHmmssfff}" : graphId.Trim();

    private static IGH_DocumentObject ApplyGraphValue(GH_Document doc, Dictionary<string, IGH_DocumentObject> aliases, JsonElement spec)
    {
        var target = ResolveGraphEndpoint(doc, aliases, spec, "target");
        var value = GetElement(spec, "value") ?? throw new ArgumentException("Graph value update requires 'value'.");
        if (TrySetSpecialValue(target, value, new ArgumentReader(JsonSerializer.SerializeToElement(ToPlainJson(spec), McpJson.Default)), out _))
            return target;

        var input = FindInputParam(target, spec, prefix: "target");
        SetParamValue(input, value);
        target.ExpireSolution(true);
        return target;
    }

    private static void ApplyGraphConnection(GH_Document doc, Dictionary<string, IGH_DocumentObject> aliases, JsonElement spec)
    {
        var source = ResolveGraphEndpoint(doc, aliases, spec, "source");
        var target = ResolveGraphEndpoint(doc, aliases, spec, "target");
        var output = FindOutputParam(source, spec, "source");
        var input = FindInputParam(target, spec, "target");
        if (!input.Sources.Contains(output))
            input.AddSource(output);

        target.ExpireSolution(true);
    }

    private static List<object> CreateGroups(GH_Document doc, IReadOnlyList<JsonElement> specs, Dictionary<string, IGH_DocumentObject> aliases, IReadOnlyList<IGH_DocumentObject> defaults, string graphId)
    {
        var groups = new List<object>();
        foreach (var spec in specs)
        {
            var name = GetString(spec, "name", "label", "nickname") ?? "Group";
            var targetSelectors = GetStringArray(spec, "targets");
            var targets = targetSelectors.Count == 0
                ? defaults.Where(obj => obj is not GH_Group).ToList()
                : targetSelectors.Select(selector => ResolveGraphObject(doc, aliases, selector)).Where(obj => obj is not GH_Group).ToList();

            if (targets.Count == 0)
                continue;

            var group = new GH_Group { NickName = name };
            foreach (var target in targets)
                group.AddObject(target.InstanceGuid);

            ApplyMetadata(group, GetString(spec, "alias"), graphId, GetString(spec, "role") ?? "group");
            doc.AddObject(group, false);
            groups.Add(new { id = group.InstanceGuid.ToString("D"), group.NickName, objectCount = targets.Count });
        }

        return groups;
    }

    private static object? MaybeApplyLayout(GH_Document doc, JsonElement? layout, List<IGH_DocumentObject> defaults)
    {
        if (!layout.HasValue || layout.Value.ValueKind == JsonValueKind.Null)
            return null;
        if (layout.Value.ValueKind == JsonValueKind.False)
            return null;
        if (layout.Value.ValueKind == JsonValueKind.Object && TryGet(layout.Value, "enabled", out var enabled) && !ToBoolean(enabled))
            return null;

        var targets = defaults.Count > 0 ? defaults : doc.Objects.Where(obj => obj is not GH_Group).ToList();
        var start = layout.Value.ValueKind == JsonValueKind.Object ? ReadPosition(layout.Value, "start", new PointF(40, 40)) : new PointF(40, 40);
        var xSpacing = layout.Value.ValueKind == JsonValueKind.Object ? (float)Math.Max(60, GetDouble(layout.Value, "xSpacing", "x_spacing") ?? 220) : 220;
        var ySpacing = layout.Value.ValueKind == JsonValueKind.Object ? (float)Math.Max(40, GetDouble(layout.Value, "ySpacing", "y_spacing") ?? 90) : 90;
        var maxColumns = layout.Value.ValueKind == JsonValueKind.Object ? GetInt(layout.Value, "maxColumns", "max_columns") ?? 0 : 0;
        return ApplyLayout(targets, start, xSpacing, ySpacing, maxColumns);
    }

    private static object ApplyLayout(List<IGH_DocumentObject> objects, PointF start, float xSpacing, float ySpacing, int maxColumns)
    {
        var objectSet = objects.ToHashSet();
        var incoming = objects.ToDictionary(obj => obj, _ => new List<IGH_DocumentObject>());
        foreach (var target in objects)
        {
            foreach (var input in InputParams(target))
            {
                foreach (var source in input.Sources)
                {
                    var sourceObj = source.Attributes?.GetTopLevel.DocObject;
                    if (sourceObj is not null && sourceObj != target && objectSet.Contains(sourceObj) && !incoming[target].Contains(sourceObj))
                        incoming[target].Add(sourceObj);
                }
            }
        }

        var edgeCount = incoming.Values.Sum(list => list.Count);
        var ordered = objects
            .OrderBy(obj => obj.Attributes?.Pivot.X ?? 0)
            .ThenBy(obj => obj.Attributes?.Pivot.Y ?? 0)
            .ThenBy(obj => obj.NickName)
            .ToList();

        if (edgeCount == 0)
        {
            var rows = maxColumns > 0 ? Math.Max(1, (int)Math.Ceiling((double)ordered.Count / maxColumns)) : Math.Max(1, (int)Math.Ceiling(Math.Sqrt(ordered.Count)));
            for (var index = 0; index < ordered.Count; index++)
            {
                var column = maxColumns > 0 ? index % maxColumns : index / rows;
                var row = maxColumns > 0 ? index / maxColumns : index % rows;
                SetPosition(ordered[index], new PointF(start.X + column * xSpacing, start.Y + row * ySpacing));
            }
        }
        else
        {
            var memo = new Dictionary<IGH_DocumentObject, int>();
            var visiting = new HashSet<IGH_DocumentObject>();
            int Level(IGH_DocumentObject obj)
            {
                if (memo.TryGetValue(obj, out var level)) return level;
                if (!visiting.Add(obj)) return 0;
                var result = incoming[obj].Count == 0 ? 0 : incoming[obj].Max(source => Level(source) + 1);
                visiting.Remove(obj);
                memo[obj] = result;
                return result;
            }

            foreach (var group in ordered.GroupBy(Level).OrderBy(group => group.Key))
            {
                var row = 0;
                var column = maxColumns > 0 ? group.Key % maxColumns : group.Key;
                var band = maxColumns > 0 ? group.Key / maxColumns : 0;
                foreach (var obj in group)
                {
                    SetPosition(obj, new PointF(start.X + column * xSpacing, start.Y + (band * (group.Count() + 1) + row) * ySpacing));
                    row++;
                }
            }
        }

        return new
        {
            layoutCount = objects.Count,
            edgeCount,
            positions = objects.Select(obj => new
            {
                id = obj.InstanceGuid.ToString("D"),
                obj.NickName,
                position = obj.Attributes is null ? null : new[] { (double)obj.Attributes.Pivot.X, obj.Attributes.Pivot.Y },
            }).ToArray(),
        };
    }

    private static IEnumerable<IGH_Param> InputParams(IGH_DocumentObject obj)
    {
        if (obj is IGH_Component component)
            return component.Params.Input;
        if (obj is IGH_Param param)
            return [param];
        return [];
    }

    private static void SetPosition(IGH_DocumentObject obj, PointF position)
    {
        if (obj.Attributes is null)
            obj.CreateAttributes();
        obj.Attributes!.Pivot = position;
        obj.Attributes.ExpireLayout();
        obj.ExpireSolution(false);
    }

    private static PointF FindNextSlot(GH_Document doc)
    {
        var pivots = doc.Objects.Where(obj => obj.Attributes is not null).Select(obj => obj.Attributes!.Pivot).ToList();
        if (pivots.Count == 0)
            return new PointF(40, 40);

        return new PointF(pivots.Max(point => point.X) + 220, pivots.Min(point => point.Y));
    }

    private static List<IGH_DocumentObject> ResolvePreviewCandidates(GH_Document doc, ArgumentReader reader)
    {
        var targetSelectors = reader.GetStringArray("targets");
        if (targetSelectors.Count > 0)
            return targetSelectors.Select(selector => FindObject(doc, selector, selector, selector)).ToList();

        var graphId = reader.GetString("graphId") ?? reader.GetString("graph_id");
        if (!string.IsNullOrWhiteSpace(graphId))
            return GraphObjects(doc, graphId).Where(obj => obj is not GH_Group).ToList();

        return doc.Objects.Where(obj => obj is not GH_Group).ToList();
    }

    private static RhinoView ResolveView(RhinoDoc doc, string? viewport)
    {
        if (string.IsNullOrWhiteSpace(viewport) || viewport.Equals("active", StringComparison.OrdinalIgnoreCase))
            return doc.Views.ActiveView ?? throw new InvalidOperationException("No active Rhino viewport is available.");

        var view = doc.Views.GetViewList(true, true)
            .FirstOrDefault(candidate => candidate.ActiveViewport.Name?.Equals(viewport, StringComparison.OrdinalIgnoreCase) == true);
        if (view is not null)
            return view;

        var active = doc.Views.ActiveView ?? throw new InvalidOperationException("No active Rhino viewport is available.");
        var projection = viewport.Trim().ToLowerInvariant() switch
        {
            "top" => DefinedViewportProjection.Top,
            "bottom" => DefinedViewportProjection.Bottom,
            "left" => DefinedViewportProjection.Left,
            "right" => DefinedViewportProjection.Right,
            "front" => DefinedViewportProjection.Front,
            "back" => DefinedViewportProjection.Back,
            "perspective" => DefinedViewportProjection.Perspective,
            _ => DefinedViewportProjection.None,
        };
        if (projection == DefinedViewportProjection.None)
            throw new ArgumentException("Argument 'viewport' must be active, perspective, top, bottom, left, right, front, or back.");

        active.ActiveViewport.SetProjection(projection, null, true);
        return active;
    }

    private static BoundingBox Pad(BoundingBox box, double factor)
    {
        var center = box.Center;
        var diagonal = box.Diagonal;
        var padX = Math.Max(Math.Abs(diagonal.X) * (factor - 1) / 2, 0.5);
        var padY = Math.Max(Math.Abs(diagonal.Y) * (factor - 1) / 2, 0.5);
        var padZ = Math.Max(Math.Abs(diagonal.Z) * (factor - 1) / 2, 0.5);
        return new BoundingBox(
            new Point3d(center.X - Math.Abs(diagonal.X) / 2 - padX, center.Y - Math.Abs(diagonal.Y) / 2 - padY, center.Z - Math.Abs(diagonal.Z) / 2 - padZ),
            new Point3d(center.X + Math.Abs(diagonal.X) / 2 + padX, center.Y + Math.Abs(diagonal.Y) / 2 + padY, center.Z + Math.Abs(diagonal.Z) / 2 + padZ));
    }

    private static Dictionary<string, object> SelectorProperties()
        => new(StringComparer.Ordinal)
        {
            ["id"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Grasshopper object GUID" },
            ["alias"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Algomim graph alias" },
            ["nickname"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Grasshopper object nickname" },
        };

    private static Dictionary<string, object> ComponentAddProperties()
        => new(SelectorProperties(), StringComparer.Ordinal)
        {
            ["componentName"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Component type name or special object alias" },
            ["componentGuid"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Component type GUID" },
            ["position"] = RhinoSchemas.Point("Canvas position"),
            ["value"] = RhinoSchemas.JsonObject("Initial value for slider/toggle/panel or object-specific value"),
            ["nickname"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Object nickname" },
            ["graphId"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Graph id metadata" },
            ["role"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Graph role metadata" },
            ["preview"] = RhinoSchemas.Boolean("Enable preview", defaultValue: true),
            ["enabled"] = RhinoSchemas.Boolean("Enable component execution", defaultValue: true),
            ["recompute"] = RhinoSchemas.Boolean("Run a solution after adding", defaultValue: true),
        };

    private static Dictionary<string, object> UpdateProperties()
        => new(SelectorProperties(), StringComparer.Ordinal)
        {
            ["newNickname"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "New object nickname" },
            ["position"] = RhinoSchemas.Point("Canvas position"),
            ["preview"] = RhinoSchemas.Boolean("Enable preview", defaultValue: true),
            ["enabled"] = RhinoSchemas.Boolean("Enable component execution", defaultValue: true),
            ["graphId"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Graph id metadata" },
            ["role"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Graph role metadata" },
            ["recompute"] = RhinoSchemas.Boolean("Run a solution after updating", defaultValue: false),
        };

    private static Dictionary<string, object> ConnectionProperties(bool disconnect)
        => new(StringComparer.Ordinal)
        {
            ["sourceId"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Source object GUID" },
            ["sourceAlias"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Source alias" },
            ["sourceNickname"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Source nickname" },
            ["sourceOutputIndex"] = RhinoSchemas.Integer("Source output index", 0, 1000, 0),
            ["sourceOutputName"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Source output name or nickname" },
            ["targetId"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Target object GUID" },
            ["targetAlias"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Target alias" },
            ["targetNickname"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Target nickname" },
            ["targetInputIndex"] = RhinoSchemas.Integer("Target input index", 0, 1000, 0),
            ["targetInputName"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Target input name or nickname" },
            ["disconnectAll"] = RhinoSchemas.Boolean("Disconnect all sources from the target input", defaultValue: disconnect),
            ["recompute"] = RhinoSchemas.Boolean("Run a solution after editing connection", defaultValue: true),
        };

    private static Dictionary<string, object> ParameterSetProperties()
        => new(SelectorProperties(), StringComparer.Ordinal)
        {
            ["inputIndex"] = RhinoSchemas.Integer("Input parameter index", 0, 1000, 0),
            ["inputName"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Input parameter name or nickname" },
            ["value"] = RhinoSchemas.JsonObject("Value to set"),
            ["min"] = RhinoSchemas.Number("Optional slider minimum"),
            ["max"] = RhinoSchemas.Number("Optional slider maximum"),
            ["decimals"] = RhinoSchemas.Integer("Optional slider decimal places", 0, 12, 2),
        };

    private static PointF ReadPosition(ArgumentReader reader, string name, PointF fallback)
    {
        var value = reader.GetElement(name);
        return value.HasValue ? ReadPosition(value.Value, name, fallback) : fallback;
    }

    private static PointF ReadPosition(JsonElement source, string name, PointF fallback)
    {
        if (!TryGet(source, name, out var value))
            return fallback;
        if (value.ValueKind == JsonValueKind.Object)
            return new PointF(
                (float)(GetDouble(value, "x") ?? fallback.X),
                (float)(GetDouble(value, "y") ?? fallback.Y));

        if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() < 2)
            throw new ArgumentException($"Argument '{name}' must be [x, y], [x, y, z], or an object with x/y.");

        return new PointF((float)ToDouble(value[0]), (float)ToDouble(value[1]));
    }

    private static bool HasSelector(ArgumentReader reader)
        => reader.Has("id") || reader.Has("alias") || reader.Has("nickname");

    private static IReadOnlyList<JsonElement> RequireArray(ArgumentReader reader, string name)
    {
        var value = reader.GetElement(name);
        if (!value.HasValue || value.Value.ValueKind != JsonValueKind.Array)
            throw new ArgumentException($"Argument '{name}' must be an array.");

        return value.Value.EnumerateArray().ToArray();
    }

    private static IReadOnlyList<JsonElement> GetArray(ArgumentReader reader, string name)
    {
        var value = reader.GetElement(name);
        if (!value.HasValue || value.Value.ValueKind == JsonValueKind.Null)
            return Array.Empty<JsonElement>();
        if (value.Value.ValueKind != JsonValueKind.Array)
            throw new ArgumentException($"Argument '{name}' must be an array.");

        return value.Value.EnumerateArray().ToArray();
    }

    private static IReadOnlyList<string> GetStringArray(JsonElement source, string name)
    {
        if (!TryGet(source, name, out var value) || value.ValueKind == JsonValueKind.Null)
            return Array.Empty<string>();
        if (value.ValueKind != JsonValueKind.Array)
            throw new ArgumentException($"Property '{name}' must be an array.");

        return value.EnumerateArray().Select(item => item.ToString()).Where(item => !string.IsNullOrWhiteSpace(item)).ToArray();
    }

    private static string GetRequiredString(JsonElement source, string name)
    {
        var value = GetString(source, name);
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"Property '{name}' is required.");

        return value;
    }

    private static string? GetString(JsonElement source, params string[] names)
    {
        foreach (var name in names)
        {
            if (!TryGet(source, name, out var value) || value.ValueKind == JsonValueKind.Null)
                continue;

            return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
        }

        return null;
    }

    private static JsonElement? GetElement(JsonElement source, params string[] names)
    {
        foreach (var name in names)
        {
            if (TryGet(source, name, out var value))
                return value;
        }

        return null;
    }

    private static int? GetInt(JsonElement source, params string[] names)
    {
        foreach (var name in names)
        {
            if (!TryGet(source, name, out var value))
                continue;
            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
                return number;
            if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out number))
                return number;
        }

        return null;
    }

    private static double? GetDouble(JsonElement source, params string[] names)
    {
        foreach (var name in names)
        {
            if (!TryGet(source, name, out var value))
                continue;
            if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number))
                return number;
            if (value.ValueKind == JsonValueKind.String && double.TryParse(value.GetString(), out number))
                return number;
        }

        return null;
    }

    private static decimal? GetDecimal(JsonElement source, string name)
    {
        if (!TryGet(source, name, out var value))
            return null;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
            return number;
        if (value.ValueKind == JsonValueKind.String && decimal.TryParse(value.GetString(), out number))
            return number;
        return null;
    }

    private static bool GetBool(JsonElement source, params string[] names)
    {
        foreach (var name in names)
        {
            if (TryGet(source, name, out var value))
                return ToBoolean(value);
        }

        return false;
    }

    private static bool TryGet(JsonElement source, string name, out JsonElement value)
    {
        value = default;
        return source.ValueKind == JsonValueKind.Object && source.TryGetProperty(name, out value);
    }

    private static IEnumerable<JsonElement> ScalarItems(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array && (value.GetArrayLength() == 0 || value[0].ValueKind != JsonValueKind.Array))
        {
            foreach (var item in value.EnumerateArray())
                yield return item;
        }
        else
        {
            yield return value;
        }
    }

    private static object? ToPlainJson(JsonElement value)
        => value.ValueKind switch
        {
            JsonValueKind.Object => value.EnumerateObject().ToDictionary(property => property.Name, property => ToPlainJson(property.Value), StringComparer.Ordinal),
            JsonValueKind.Array => value.EnumerateArray().Select(ToPlainJson).ToArray(),
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number when value.TryGetInt64(out var integer) => integer,
            JsonValueKind.Number when value.TryGetDouble(out var number) => number,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => value.ToString(),
        };

    private static double ToDouble(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number)) return number;
        if (value.ValueKind == JsonValueKind.String && double.TryParse(value.GetString(), out number)) return number;
        throw new ArgumentException("Expected a numeric value.");
    }

    private static int ToInt(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number)) return number;
        if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out number)) return number;
        throw new ArgumentException("Expected an integer value.");
    }

    private static decimal ToDecimal(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number)) return number;
        if (value.ValueKind == JsonValueKind.String && decimal.TryParse(value.GetString(), out number)) return number;
        throw new ArgumentException("Expected a decimal value.");
    }

    private static bool ToBoolean(JsonElement value)
        => value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(value.GetString(), out var parsed) => parsed,
            JsonValueKind.Number when value.TryGetInt32(out var number) => number != 0,
            _ => throw new ArgumentException("Expected a boolean value."),
        };

    private static Point3d ToPoint(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array)
        {
            var values = value.EnumerateArray().Select(ToDouble).ToArray();
            if (values.Length is < 2 or > 3)
                throw new ArgumentException("Point values must have two or three coordinates.");

            return new Point3d(values[0], values[1], values.Length == 3 ? values[2] : 0);
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            return new Point3d(
                GetDouble(value, "x") ?? 0,
                GetDouble(value, "y") ?? 0,
                GetDouble(value, "z") ?? 0);
        }

        throw new ArgumentException("Point value must be an array or object.");
    }

    private static Vector3d ToVector(JsonElement value)
    {
        var point = ToPoint(value);
        return new Vector3d(point.X, point.Y, point.Z);
    }

    private static int? FirstInt(params string?[] values)
    {
        foreach (var value in values)
        {
            if (int.TryParse(value, out var parsed))
                return parsed;
        }

        return null;
    }

    private static string? FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static string Classify(Type? type)
    {
        if (type is null) return "Other";
        if (typeof(GH_NumberSlider).IsAssignableFrom(type)) return "Slider";
        if (typeof(IGH_Component).IsAssignableFrom(type)) return "Component";
        if (typeof(IGH_Param).IsAssignableFrom(type)) return "Parameter";
        if (typeof(GH_Group).IsAssignableFrom(type)) return "Group";
        return "Other";
    }

    private static object Point(Point3d point)
        => new { x = point.X, y = point.Y, z = point.Z };

    private static object Box(BoundingBox box)
        => new { min = Point(box.Min), max = Point(box.Max), center = Point(box.Center) };
}
