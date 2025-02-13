using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;

[GlobalClass, Tool, Icon("res://ui/icons/StyleBoxConditional.svg")]
public partial class StyleBoxConditional : StyleBox {

    private static readonly int TYPES_RELOAD_INTERVAL_SECS = 10;
    private static IEnumerable<Type> canvasTypes;
    private static DateTime canvasTypesLoadedAt;

    private string comparisonTargetClass;
    [Export] public string ComparisonTargetClass { get => comparisonTargetClass; set { comparisonTargetClass = value; NotifyPropertyListChanged(); } }
    private string comparisonTargetProperty;
    [Export] public string ComparisonTargetProperty { get => comparisonTargetProperty; set { comparisonTargetProperty = value; NotifyPropertyListChanged(); } }
    [Export] public Variant CompareToValue { get; set; } = false;
    
    private StyleBox renderIfTrue;
    [Export] public StyleBox RenderIfTrue { get => renderIfTrue; set { renderIfTrue = value; AutoSetContentMargins(); } }

    private StyleBox renderIfFalse;
    [Export] public StyleBox RenderIfFalse { get => renderIfFalse; set { renderIfFalse = value; AutoSetContentMargins(); } }

    private readonly System.Collections.Generic.Dictionary<string, PropertyInfo> cachedProperties = new System.Collections.Generic.Dictionary<string, PropertyInfo>();

    private StyleBox? GetStyleBoxToDraw() {
        CanvasItem canvasItem = GetCurrentItemDrawn();

        string classAndPropertyName = ComparisonTargetClass + " -> " + comparisonTargetProperty;
        if (!cachedProperties.ContainsKey(classAndPropertyName)) {
            Type? comparisonTargetClass = StringToType(ComparisonTargetClass);
            PropertyInfo? comparisonTargetProperty = ComparisonTargetProperty != null ? comparisonTargetClass?.GetProperty(ComparisonTargetProperty) : null;
            if (comparisonTargetProperty != null) cachedProperties.Add(classAndPropertyName, comparisonTargetProperty);
        }
        
        bool comparisonResult = false;
        if (cachedProperties.TryGetValue(classAndPropertyName, out PropertyInfo property)) {
            try {
                object propertyValue = property.GetValue(canvasItem);
                comparisonResult = CompareToValue.Obj?.Equals(propertyValue) ?? propertyValue == null;
            } catch (TargetException e) { } // if canvas item is not of type configured by this StyleBoxConditional, we treat it the same as if condition resolves to false
        }
        
        return comparisonResult ? RenderIfTrue : RenderIfFalse;
    }
    
    public override void _Draw(Rid toCanvasItem, Rect2 rect) {
        GetStyleBoxToDraw()?.Draw(toCanvasItem, rect);
    }

    private static string TypeToString(Type type) {
        return type.Name + "    (" + type.FullName + " in " + type.Assembly.GetName().Name + ")";
    }

    private static Type? StringToType(string typeString) {
        string[] classNameParts = typeString?.Split(new string[]{ "    (", " in ", ")" }, StringSplitOptions.RemoveEmptyEntries) ?? new string[0];
        return classNameParts.Length == 3 ? Type.GetType($"{classNameParts[1]}, {classNameParts[2]}") : null;
    }

    public override void _ValidateProperty(Dictionary property) {
        StringName stringName = property["name"].AsStringName();
        
        if (stringName == PropertyName.ComparisonTargetClass) { // switch case not possible because PropertyName contains static readonlys instead of consts
            IEnumerable<string> canvasClassNames = GetCanvasItemClasses().Select(TypeToString);
            property["hint"] = (int) PropertyHint.EnumSuggestion;
            property["hint_string"] = string.Join(',', canvasClassNames.OrderBy(name => name));
        } else if (stringName == PropertyName.ComparisonTargetProperty) {
            Type? comparisonTargetClass = StringToType(ComparisonTargetClass);
            IEnumerable<string>? comparisonTargetPropertyNames = comparisonTargetClass?.GetProperties().Select(prop => prop.Name);
            property["hint"] = (int) PropertyHint.EnumSuggestion;
            if (comparisonTargetPropertyNames != null) {
                property["hint_string"] = string.Join(',', comparisonTargetPropertyNames.OrderBy(name => name));
            } else {
                property["usage"] = (int) (property["usage"].As<PropertyUsageFlags>() | PropertyUsageFlags.ReadOnly);
                property["hint_string"] = "";
            }
        } else if (stringName == PropertyName.CompareToValue) {
            Type? comparisonTargetClass = StringToType(ComparisonTargetClass);
            PropertyInfo? comparisonTargetProperty = ComparisonTargetProperty != null ? comparisonTargetClass?.GetProperty(ComparisonTargetProperty) : null;
            if (comparisonTargetProperty != null) {
                property["type"] = (int) GetTypeFor(comparisonTargetProperty);
            } else {
                property["usage"] = (int) (property["usage"].As<PropertyUsageFlags>() | PropertyUsageFlags.ReadOnly);
                property["type"] = (int) Variant.Type.Nil;
            }
        } else if (stringName == StyleBox.PropertyName.ContentMarginLeft || stringName == StyleBox.PropertyName.ContentMarginRight || stringName == StyleBox.PropertyName.ContentMarginTop || stringName == StyleBox.PropertyName.ContentMarginBottom) {
            property["usage"] = (int) (property["usage"].As<PropertyUsageFlags>() | PropertyUsageFlags.ReadOnly);
        }
    }

    private static IEnumerable<Type> GetCanvasItemClasses() {
        DateTime now = DateTime.Now;
        if (canvasTypes == null || (now - canvasTypesLoadedAt).Seconds >= TYPES_RELOAD_INTERVAL_SECS) {
            canvasTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes()).Where(p => typeof(CanvasItem).IsAssignableFrom(p));
            canvasTypesLoadedAt = now;
        }
        return canvasTypes;
    }

    private void AutoSetContentMargins() {
        ContentMarginLeft = Math.Max(RenderIfTrue?.ContentMarginLeft ?? 0, RenderIfFalse?.ContentMarginLeft ?? 0);
        ContentMarginRight = Math.Max(RenderIfTrue?.ContentMarginRight ?? 0, RenderIfFalse?.ContentMarginRight ?? 0);
        ContentMarginTop = Math.Max(RenderIfTrue?.ContentMarginTop ?? 0, RenderIfFalse?.ContentMarginTop ?? 0);
        ContentMarginBottom = Math.Max(RenderIfTrue?.ContentMarginBottom ?? 0, RenderIfFalse?.ContentMarginBottom ?? 0);
        EmitChanged();
    }

    private static Variant.Type GetTypeFor(PropertyInfo? comparisonTargetProperty) {
        if (comparisonTargetProperty == null) return Variant.Type.Nil;

        switch (comparisonTargetProperty.PropertyType) {
            case Type t when t == typeof(bool):
                return Variant.Type.Bool;
            case Type t when t == typeof(int):
                return Variant.Type.Int;
                return 0;
            case Type t when t == typeof(float):
                return Variant.Type.Float;
            case Type t when t == typeof(string):
                return Variant.Type.String;
            case Type t when t == typeof(Vector2):
                return Variant.Type.Vector2;
            case Type t when t == typeof(Vector2I):
                return Variant.Type.Vector2I;
            case Type t when t == typeof(Rect2):
                return Variant.Type.Rect2;
            case Type t when t == typeof(Rect2I):
                return Variant.Type.Rect2I;
            case Type t when t == typeof(Vector3):
                return Variant.Type.Vector3;
            case Type t when t == typeof(Vector3I):
                return Variant.Type.Vector3I;
            case Type t when t == typeof(Transform2D):
                return Variant.Type.Transform2D;
            case Type t when t == typeof(Vector4):
                return Variant.Type.Vector4;
            case Type t when t == typeof(Vector4I):
                return Variant.Type.Vector4I;
            case Type t when t == typeof(Plane):
                return Variant.Type.Plane;
            case Type t when t == typeof(Quaternion):
                return Variant.Type.Quaternion;
            case Type t when t == typeof(Aabb):
                return Variant.Type.Aabb;
            case Type t when t == typeof(Basis):
                return Variant.Type.Basis;
            case Type t when t == typeof(Transform3D):
                return Variant.Type.Transform3D;
            case Type t when t == typeof(Projection):
                return Variant.Type.Projection;
            case Type t when t == typeof(Color):
                return Variant.Type.Color;
            case Type t when t == typeof(StringName):
                return Variant.Type.StringName;
            case Type t when t == typeof(NodePath):
                return Variant.Type.NodePath;
            case Type t when t == typeof(Rid):
                return Variant.Type.Rid;
            case Type t when t == typeof(GodotObject):
                return Variant.Type.Object;
            case Type t when t == typeof(Callable):
                return Variant.Type.Callable;
            case Type t when t == typeof(Signal):
                return Variant.Type.Signal;
            case Type t when t == typeof(Godot.Collections.Dictionary):
                return Variant.Type.Dictionary;
            case Type t when t == typeof(Array):
                return Variant.Type.Array;
            case Type t when t == typeof(byte[]):
                return Variant.Type.PackedByteArray;
            case Type t when t == typeof(int[]):
                return Variant.Type.PackedInt32Array;
            case Type t when t == typeof(long[]):
                return Variant.Type.PackedInt64Array;
            case Type t when t == typeof(float[]):
                return Variant.Type.PackedFloat32Array;
            case Type t when t == typeof(double[]):
                return Variant.Type.PackedFloat64Array;
            case Type t when t == typeof(string[]):
                return Variant.Type.PackedStringArray;
            case Type t when t == typeof(Vector2[]):
                return Variant.Type.PackedVector2Array;
            case Type t when t == typeof(Vector3[]):
                return Variant.Type.PackedVector3Array;
            case Type t when t == typeof(Color[]):
                return Variant.Type.PackedColorArray;
            case Type t when t == typeof(Vector4[]):
                return Variant.Type.PackedVector4Array;
            default:
                return Variant.Type.Nil;
        }
    }

    private static Variant GetDefaultVariantValue(Variant.Type forType) {
        switch (forType) {
            case Variant.Type.Nil:
                return Variant.From((GodotObject) null);
            case Variant.Type.Bool:
                return false;
            case Variant.Type.Int:
                return 0;
            case Variant.Type.Float:
                return 0f;
            case Variant.Type.String:
                return "";
            case Variant.Type.Vector2:
                return new Vector2();
            case Variant.Type.Vector2I:
                return new Vector2I();
            case Variant.Type.Rect2:
                return new Rect2();
            case Variant.Type.Rect2I:
                return new Rect2I();
            case Variant.Type.Vector3:
                return new Vector3();
            case Variant.Type.Vector3I:
                return new Vector3I();
            case Variant.Type.Transform2D:
                return new Transform2D();
            case Variant.Type.Vector4:
                return new Vector4();
            case Variant.Type.Vector4I:
                return new Vector4I();
            case Variant.Type.Plane:
                return new Plane();
            case Variant.Type.Quaternion:
                return new Quaternion();
            case Variant.Type.Aabb:
                return new Aabb();
            case Variant.Type.Basis:
                return new Basis();
            case Variant.Type.Transform3D:
                return new Transform3D();
            case Variant.Type.Projection:
                return new Projection();
            case Variant.Type.Color:
                return new Color();
            case Variant.Type.StringName:
                return new StringName();
            case Variant.Type.NodePath:
                return new NodePath();
            case Variant.Type.Rid:
                return new Rid();
            case Variant.Type.Object:
                return (GodotObject) null;
            case Variant.Type.Callable:
                return new Callable();
            case Variant.Type.Signal:
                return new Signal();
            case Variant.Type.Dictionary:
                return new Godot.Collections.Dictionary();
            case Variant.Type.Array:
                return new Array();
            case Variant.Type.PackedByteArray:
                return new Byte[0];
            case Variant.Type.PackedInt32Array:
                return new Int32[0];
            case Variant.Type.PackedInt64Array:
                return new Int32[0];
            case Variant.Type.PackedFloat32Array:
                return new Int64[0];
            case Variant.Type.PackedFloat64Array:
                return new Double[0];
            case Variant.Type.PackedStringArray:
                return new String[0];
            case Variant.Type.PackedVector2Array:
                return new Vector2[0];
            case Variant.Type.PackedVector3Array:
                return new Vector3[0];
            case Variant.Type.PackedColorArray:
                return new Color[0];
            case Variant.Type.PackedVector4Array:
                return new Vector4[0];
            case Variant.Type.Max:
            default:
                throw new ArgumentOutOfRangeException(nameof(forType), forType, null);
        }
    }
}