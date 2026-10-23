using AMarkup;
using Mav.Controls;

// Scan MavUI and generate fluent setters for its controls. Avalonia controls are already in AMarkup.Controls.
[assembly: GenerateMarkupForAssembly(typeof(MavButton))]
