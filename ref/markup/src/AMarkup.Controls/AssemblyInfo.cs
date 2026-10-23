using System.Reflection.Metadata;
using Avalonia.Controls;
using Avalonia.Layout;

[assembly: AMarkup.GenerateMarkupForAssembly(typeof(Button))]
[assembly: AMarkup.GenerateMarkupForAssembly(typeof(Layoutable))]
[assembly: MetadataUpdateHandler(typeof(AMarkup.MarkupHotReload))]
