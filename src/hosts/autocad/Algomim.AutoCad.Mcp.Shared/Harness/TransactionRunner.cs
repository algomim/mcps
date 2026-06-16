using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;

namespace Algomim.AutoCad.Mcp.Harness;

/// <summary>Small transaction helper for future AutoCAD read/write tools.</summary>
public static class TransactionRunner
{
    public static T Run<T>(Document document, Func<Database, Transaction, T> action)
    {
        var database = document.Database;
        using var transaction = database.TransactionManager.StartTransaction();
        var result = action(database, transaction);
        transaction.Commit();
        return result;
    }
}
