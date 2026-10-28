# Editable XAML reconstruction

These three files were extracted from the original 1.3.6 `resources.pri` and converted with XbfTools. The current WIP C# project still uses its recovered generated connectors and the original binary PRI. The editable XAML is not yet used to regenerate those connectors.

Corrections applied to the converter output: restore Grid attached-property names, restore ToolTipService.ToolTip, and recover event bindings from the original managed IComponentConnector methods.

This is not yet a visually verified replacement. The next stage is native Windows XAML compilation, regeneration of the connectors, and original-versus-rebuilt screenshots.

Converter sources: https://github.com/chausner/PriTools and https://github.com/chausner/XbfTools . The converters warn that XBF-to-XAML reconstruction can be incomplete.
