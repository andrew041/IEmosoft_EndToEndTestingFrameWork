using aUI.Automation.Elements;
using System;
using System.ComponentModel;

namespace aUI.Automation.Enums
{
    public static class EnumUtils
    {
        /*
                public static string defaultValue(Enum field, string defaultRtn = null)
                {
                    var fi = field.GetType().GetField(field.ToString());
                    var attributes = (DefaultValueAttribute[])fi.GetCustomAttributes(typeof(DefaultValueAttribute), false);

                    if(attributes.Length > 0)
                    {
                        return attributes[0].Value.ToString();
                    }
                    else
                    {
                        return defaultRtn == null ? field.ToString() : defaultRtn;
                    }
                }
        */
        public static string DefaultValue(this Enum field, string defaultRtn = null)
        {
            var fi = field.GetType().GetField(field.ToString());
            var attributes = (DefaultValueAttribute[])fi.GetCustomAttributes(typeof(DefaultValueAttribute), false);

            if (attributes.Length > 0)
            {
                return attributes[0].Value.ToString();
            }
            else
            {
                return defaultRtn ?? field.ToString();
            }
        }
        public static string AmbientValue(this Enum field, string defaultRtn = null)
        {
            var fi = field.GetType().GetField(field.ToString());
            var attributes = (AmbientValueAttribute[])fi.GetCustomAttributes(typeof(AmbientValueAttribute), false);

            if (attributes.Length > 0)
            {
                return attributes[0].Value.ToString();
            }
            else
            {
                return defaultRtn ?? field.ToString();
            }
        }

        public static int[] IntArr(this Enum field)
        {
            var fi = field.GetType().GetField(field.ToString());
            var attributes = (IntArrAttribute[])fi.GetCustomAttributes(typeof(IntArrAttribute), false);

            if (attributes.Length > 0)
            {
                return attributes[0].IntArr;
            }
            else
            {
                return new int[] { };
            }
        }

        public static ElementType Type(this Enum field)
        {
            var fi = field.GetType().GetField(field.ToString());
            var attributes = (ETypeAttribute[])fi.GetCustomAttributes(typeof(ETypeAttribute), false);

            if (attributes.Length > 0)
            {
                return attributes[0].EType;
            }
            else
            {
                return ElementType.Id;
            }
        }

        public static string Ref(this Enum field, string defaultRtn = null)
        {
            var fi = field.GetType().GetField(field.ToString());
            var attributes = (ERefAttribute[])fi.GetCustomAttributes(typeof(ERefAttribute), false);

            if (attributes.Length > 0)
            {
                return attributes[0].ERef.ToString();
            }
            else
            {
                return defaultRtn ?? field.ToString();
            }
        }

        public static string Api(this Enum field, string defaultRtn = null)
        {
            var fi = field.GetType().GetField(field.ToString());
            var attributes = (ApiAttribute[])fi.GetCustomAttributes(typeof(ApiAttribute), false);

            if (attributes.Length > 0)
            {
                return attributes[0].Api.ToString();
            }
            else
            {
                return defaultRtn ?? field.ToString();
            }
        }

        public static Enum RelatedEnum(this Enum field, Enum related = null)
        {
            var fi = field.GetType().GetField(field.ToString());
            var attributes = (RelatedEnumAttribute[])fi.GetCustomAttributes(typeof(RelatedEnumAttribute), false);

            if (attributes.Length > 0)
            {
                return attributes[0].RelatedEnum;
            }
            else
            {
                return related;
            }
        }
    }


    [AttributeUsage(AttributeTargets.Enum | AttributeTargets.Field)]
    public class IntArrAttribute : Attribute
    {
        public int[] IntArr { get; }
        public IntArrAttribute(int[] iArr)
        {
            IntArr = iArr;
        }
    }

    [AttributeUsage(AttributeTargets.Enum | AttributeTargets.Field)]
    public class ETypeAttribute : Attribute
    {
        public ElementType EType { get; }
        public ETypeAttribute(ElementType type)
        {
            EType = type;
        }
    }

    [AttributeUsage(AttributeTargets.Enum | AttributeTargets.Field)]
    public class ERefAttribute : Attribute
    {
        public string ERef { get; }
        public ERefAttribute(string eRef)
        {
            ERef = eRef;
        }
    }

    [AttributeUsage(AttributeTargets.Enum | AttributeTargets.Field)]
    public class ApiAttribute : Attribute
    {
        public string Api { get; }
        public ApiAttribute(string api)
        {
            Api = api;
        }
    }

    [AttributeUsage(AttributeTargets.Enum | AttributeTargets.Field)]
    public class RelatedEnumAttribute : Attribute
    {
        public Enum RelatedEnum { get; }
        public RelatedEnumAttribute(object related)
        {
            RelatedEnum = (Enum)related;
        }
    }
}
