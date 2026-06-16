// Disambiguate types that collide between the Revit API and WinForms/WPF (pulled in by
// UseWindowsForms / UseWpf) so Shared code can use the short name unambiguously.
global using View = Autodesk.Revit.DB.View;
global using Color = Autodesk.Revit.DB.Color;
