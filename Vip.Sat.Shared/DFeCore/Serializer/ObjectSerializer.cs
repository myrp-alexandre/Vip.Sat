using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Vip.Sat.DFeCore.Attributes;
using Vip.Sat.Exceptions;
using Vip.Sat.Extensions;

namespace Vip.Sat.DFeCore.Serializer
{
    internal class ObjectSerializer
    {
        #region Serialize

        public static XElement Serialize(object value, Type tipo, string name, string nameSpace, SerializerOptions options)
        {
            try
            {
                XNamespace aw = nameSpace ?? string.Empty;
                var objectElement = new XElement(aw + name);

                var properties = tipo.GetProperties()
                    .Where(x => !x.ShouldIgnoreProperty() && x.ShouldSerializeProperty(value))
                    .OrderBy(x => x.GetAttribute<DFeBaseAttribute>()?.Ordem ?? 0).ToArray();

                foreach (var prop in properties)
                {
                    if (prop.ShouldIgnoreProperty() || !prop.ShouldSerializeProperty(value)) continue;

                    var elements = Serialize(prop, value, options);
                    if (elements == null) continue;

                    foreach (var element in elements)
                        if (element is XElement child)
                            objectElement.AddChild(child);
                        else
                            objectElement.AddAttribute((XAttribute) element);
                }

                return objectElement;
            }
            catch (Exception e)
            {
                var msg = $"Erro ao serializar o objeto:{Environment.NewLine}{value}";
                throw new VipException(msg, e);
            }
        }

        public static IEnumerable<XObject> Serialize(PropertyInfo prop, object parentObject, SerializerOptions options)
        {
            try
            {
                var objectType = ObjectType.From(prop.PropertyType);

                if (objectType == ObjectType.DictionaryType)
                    return DictionarySerializer.Serialize(prop, parentObject, options);

                if (objectType.IsIn(ObjectType.ListType, ObjectType.ArrayType, ObjectType.EnumerableType))
                    return CollectionSerializer.Serialize(prop, parentObject, options);

                var value = prop.GetValue(parentObject, null);

                if (objectType.IsIn(ObjectType.InterfaceType, ObjectType.AbstractType))
                    return value == null ? null : InterfaceSerializer.Serialize(prop, parentObject, options);

                if (objectType == ObjectType.ClassType)
                {
                    var attribute = prop.GetAttribute<DFeElementAttribute>();
                    if (attribute.Ocorrencia == Ocorrencia.NaoObrigatoria && value == null) return null;
                    return new XObject[] {Serialize(value, prop.PropertyType, attribute.Name, attribute.Namespace, options)};
                }

                if (objectType == ObjectType.RootType)
                {
                    if (prop.HasAttribute<DFeElementAttribute>())
                    {
                        var attribute = prop.GetAttribute<DFeElementAttribute>();
                        if (attribute.Ocorrencia == Ocorrencia.NaoObrigatoria && value == null) return null;
                        return new XObject[] {Serialize(value, prop.PropertyType, attribute.Name, attribute.Namespace, options)};
                    }

                    if (value == null) return null;
                    var rooTag = prop.PropertyType.GetAttribute<DFeRootAttribute>();
                    var rootName = rooTag.Name;

                    if (rootName.IsNullOrEmpty())
                    {
                        var root = prop.PropertyType.GetRootName(value);
                        rootName = root.IsNullOrEmpty() ? prop.PropertyType.Name : root;
                    }

                    var rootElement = Serialize(value, prop.PropertyType, rootName, rooTag.Namespace, options);
                    return new XObject[] {rootElement};
                }

                var tag = prop.GetTag();
                return new[] {PrimitiveSerializer.Serialize(tag, parentObject, prop, options)};
            }
            catch (Exception e)
            {
                var msg = $"Erro ao serializar a propriedade:{Environment.NewLine}{prop.DeclaringType?.Name ?? prop.PropertyType.Name} - {prop.Name}";
                throw new VipException(msg, e);
            }
        }

        #endregion Serialize

        #region Deserialize

        public static object Deserialize(Type type, XElement element, SerializerOptions options)
        {
            try
            {
                var ret = type.HasCreate() ? type.GetCreate().Invoke() : Activator.CreateInstance(type);

                if (element == null) return ret;

                var properties = type.GetProperties();
                foreach (var prop in properties)
                {
                    if (prop.ShouldIgnoreProperty()) continue;

                    var value = Deserialize(prop, element, ret, options);
                    prop.SetValue(ret, value, null);
                }

                return ret;
            }
            catch (Exception e)
            {
                var msg = $"Erro ao deserializar o objeto:{Environment.NewLine}{type.Name} - {element.Name}";
                throw new VipException(msg, e);
            }
        }

        public static object Deserialize(PropertyInfo prop, XElement parentElement, object item, SerializerOptions options)
        {
            try
            {
                var tag = prop.HasAttribute<DFeElementAttribute>()
                    ? (DFeBaseAttribute) prop.GetAttribute<DFeElementAttribute>()
                    : prop.GetAttribute<DFeAttributeAttribute>();

                var objectType = ObjectType.From(prop.PropertyType);
                if (objectType == ObjectType.DictionaryType)
                {
                    var dicTag = prop.GetAttribute<DFeDictionaryAttribute>();
                    var dictionaryElement = parentElement.ElementAnyNs(dicTag.Name);
                    return DictionarySerializer.Deserialize(prop, dictionaryElement, item, options);
                }

                if (objectType.IsIn(ObjectType.ArrayType, ObjectType.EnumerableType))
                {
                    var listElement = parentElement.ElementsAnyNs(tag.Name);
                    var list = (ArrayList) CollectionSerializer.Deserialize(typeof(ArrayList), listElement.ToArray(), prop, item, options);
                    var type = prop.PropertyType.IsArray ? prop.PropertyType.GetElementType() : prop.PropertyType.GetGenericArguments()[0];
                    return objectType == ObjectType.ArrayType ? list.ToArray(type) : list.Cast(type);
                }

                if (objectType == ObjectType.ListType)
                {
                    var listElement = parentElement.ElementsAnyNs(tag.Name);
                    return CollectionSerializer.Deserialize(prop.PropertyType, listElement.ToArray(), prop, item, options);
                }

                if (objectType.IsIn(ObjectType.InterfaceType, ObjectType.AbstractType))
                    return InterfaceSerializer.Deserialize(prop, parentElement, item, options);

                if (objectType == ObjectType.RootType)
                {
                    if (tag != null)
                    {
                        var xElement = parentElement.ElementsAnyNs(tag.Name).FirstOrDefault();
                        return Deserialize(prop.PropertyType, xElement, options);
                    }

                    var rootTag = prop.PropertyType.GetAttribute<DFeRootAttribute>();
                    var rootNames = new List<string>();
                    if (!rootTag.Name.IsNullOrEmpty())
                    {
                        rootNames.Add(rootTag.Name);
                        rootNames.Add(prop.PropertyType.Name);
                    }
                    else
                    {
                        rootNames.AddRange(prop.PropertyType.GetRootNames());
                        rootNames.Add(prop.PropertyType.Name);
                    }

                    var xmlNode = (from node in parentElement.Elements()
                        where node.Name.LocalName.IsIn(rootNames)
                        select node).FirstOrDefault();

                    return Deserialize(prop.PropertyType, xmlNode, options);
                }

                if (objectType == ObjectType.ClassType)
                {
                    var xElement = parentElement.ElementsAnyNs(tag.Name).FirstOrDefault();
                    return Deserialize(prop.PropertyType, xElement, options);
                }

                var element = parentElement.ElementsAnyNs(tag.Name).FirstOrDefault() ??
                              (XObject) parentElement.Attributes(tag.Name).FirstOrDefault();

                return PrimitiveSerializer.Deserialize(tag, element, item, prop, options);
            }
            catch (Exception e)
            {
                var msg = $"Erro ao deserializar a propriedade:{Environment.NewLine}{prop.DeclaringType?.Name ?? prop.PropertyType.Name} - {prop.Name}";
                throw new VipException(msg, e);
            }
        }

        #endregion Deserialize
    }
}