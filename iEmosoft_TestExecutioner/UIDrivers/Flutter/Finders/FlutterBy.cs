using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using OpenQA.Selenium;
namespace aUI.Automation.Flutter
{
    public static class FlutterFinderType
    {
        public const string Ancestor = "Ancestor";  // https://api.flutter.dev/flutter/flutter_driver/Ancestor/finderType.html
        public const string ByText = "ByText";  // https://api.flutter.dev/flutter/flutter_driver/ByText/finderType.html
        public const string Descendant = "Descendant"; // https://api.flutter.dev/flutter/flutter_driver/Descendant/finderType.html
        public const string BySemanticsLabel = "BySemanticsLabel"; // https://api.flutter.dev/flutter/flutter_driver/BySemanticsLabel/finderType.html
        public const string ByTooltipMessage = "ByTooltipMessage"; // https://api.flutter.dev/flutter/flutter_driver/ByTooltipMessage/finderType.html
        public const string ByType = "ByType"; // https://api.flutter.dev/flutter/flutter_driver/ByType/finderType.html
        public const string ByValueKey = "ByValueKey"; // https://api.flutter.dev/flutter/flutter_driver/ByValueKey/finderType.html
        public const string PageBack = "PageBack"; // https://api.flutter.dev/flutter/flutter_driver/PageBack/finderType.html
    }
    public abstract class FlutterBy : By
    {
        public FlutterBy() : base() { }
        public static FlutterBy Ancestor(FlutterBy of, FlutterBy matching, bool matchroot = true, bool firstMatchOnly = true)
        {
            return new ByAncestor(of, matching, matchroot, firstMatchOnly);
        }

        public static FlutterBy SemanticsLabel(string semanticsLabel)
        {
            return new BySemanticsLabel(semanticsLabel);
        }

        public static FlutterBy Text(string text)
        {
            return new ByText(text);
        }

        public static FlutterBy TooltipMessage(string tooltipMessage)
        {
            return new ByTooltipMessage(tooltipMessage);
        }

        public static FlutterBy Type(string type)
        {
            return new ByType(type);
        }

        public static FlutterBy ValueKey(string valueKey)
        {
            return new ByValueKey(valueKey);
        }

        public static FlutterBy Descendant(FlutterBy of, FlutterBy matching, bool matchRoot = true, bool firstMatchOnly = true)
        {
            return new ByDescendant(of, matching, matchRoot, firstMatchOnly);
        }

        public static FlutterBy PageBack()
        {
            return new ByPageBack();
        }

        public override FlutterElement FindElement(ISearchContext context)
        {
            if (context is FlutterDriver)
            {
                //return new FlutterElement()
                throw new NotImplementedException("Create the finder from flutter driver");
            }
            else if (context is FlutterElement)
            {
                //return 
            }
            throw new NotImplementedException();
        }

        public override ReadOnlyCollection<IWebElement> FindElements(ISearchContext context)
        {
            throw new NotSupportedException("appium-flutter-driver does not support returning multiple elements");
        }

        public abstract string ToSerializedJSON();

        public string Encoded()
        {
            var asBytes = System.Text.Encoding.UTF8.GetBytes(ToSerializedJSON());
            var base64 = Convert.ToBase64String(asBytes);
            return base64;
        }
    }

    public class ByAncestor : FlutterBy
    {
        private readonly FlutterBy _Of;
        private readonly FlutterBy _Matching;
        private readonly bool _MatchRoot;
        private readonly bool _FirstMatchOnly;
        public ByAncestor(FlutterBy of, FlutterBy matching, bool matchRoot = true, bool firstMatchOnly = true)
        {
            _Of = of;
            _Matching = matching;
            _MatchRoot = matchRoot;
            _FirstMatchOnly = firstMatchOnly;
        }
        public override string ToSerializedJSON()
        {
            return JsonSerializer.Serialize(new Dictionary<string, string>()
            {
                { "finderType", FlutterFinderType.Ancestor },
                { "of", _Of.ToSerializedJSON() },
                { "matching", _Matching.ToSerializedJSON() },
                { "matchRoot", $"{_MatchRoot}" },
                { "firstMatchOnly", $"{_FirstMatchOnly}" }
            });
        }
    }

    public class ByDescendant : FlutterBy
    {
        private readonly FlutterBy _Of;
        private readonly FlutterBy _Matching;
        private readonly bool _MatchRoot;
        private readonly bool _FirstMatchOnly;
        public ByDescendant(FlutterBy of, FlutterBy matching, bool matchRoot = true, bool firstMatchOnly = true)
        {
            _Of = of;
            _Matching = matching;
            _MatchRoot = matchRoot;
            _FirstMatchOnly = firstMatchOnly;
        }
        public override string ToSerializedJSON()
        {
            return JsonSerializer.Serialize(new Dictionary<string, string>()
            {
                { "finderType", FlutterFinderType.Descendant },
                { "of", _Of.ToSerializedJSON() },
                { "matching", _Matching.ToSerializedJSON() },
                { "matchRoot", $"{_MatchRoot}" },
                { "firstMatchOnly", $"{_FirstMatchOnly}" }
            });
        }
    }

    public class ByText : FlutterBy
    {
        private readonly string _Text;
        public ByText(string text)
        {
            _Text = text;
        }

        public override string ToSerializedJSON()
        {
            return JsonSerializer.Serialize(new Dictionary<string, string>()
            {
                { "finderType", FlutterFinderType.ByText },
                { "text", _Text }
            });
        }
    }
    
    public class ByValueKey : FlutterBy
    {
        private readonly string _ValueKey;
        public ByValueKey(string valueKey)
        {
            _ValueKey = valueKey;
        }

        public override string ToSerializedJSON()
        {
            return JsonSerializer.Serialize(new Dictionary<string, string>()
            {
                { "finderType", FlutterFinderType.ByValueKey },
                { "keyValueString", _ValueKey },
                { "keyValueType", "String" }        // TODO: Is this always true?
            });
        }
    }

    public class ByType : FlutterBy
    {
        private readonly string _Type;
        public ByType(string type)
        {
            _Type = type;
        }

        public override string ToSerializedJSON()
        {
            return JsonSerializer.Serialize(new Dictionary<string, string>()
            {
                { "finderType", FlutterFinderType.ByType },
                { "type", _Type },
            });
        }
    }

    public class BySemanticsLabel : FlutterBy
    {
        private readonly string _SemanticsLabel;
        public BySemanticsLabel(string semanticsLabel)
        {
            _SemanticsLabel = semanticsLabel;
        }

        public override string ToSerializedJSON()
        {
            return JsonSerializer.Serialize(new Dictionary<string, string>()
            {
                { "finderType", FlutterFinderType.BySemanticsLabel },
                { "type", _SemanticsLabel },
            });
        }
    }

    public class ByTooltipMessage : FlutterBy
    {
        private readonly string _TooltipMessage;
        public ByTooltipMessage(string tooltipMessage)
        {
            _TooltipMessage = tooltipMessage;
        }

        public override string ToSerializedJSON()
        {
            return JsonSerializer.Serialize(new Dictionary<string, string>()
            {
                { "finderType", FlutterFinderType.ByTooltipMessage },
                { "type", _TooltipMessage },
            });
        }
    }

    public class ByPageBack : FlutterBy
    {
        public ByPageBack()
        {
        }

        public override string ToSerializedJSON()
        {
            return JsonSerializer.Serialize(new Dictionary<string, string>()
            {
                { "finderType", FlutterFinderType.PageBack },
            });
        }
    }
}

