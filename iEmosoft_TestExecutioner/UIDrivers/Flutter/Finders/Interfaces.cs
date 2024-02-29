using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aUI.Automation.Flutter.Interfaces
{
    public interface IFindByAncestor
    {
        /// <summary>
        /// Find by Ancestor, "of" field is current element
        /// https://api.flutter.dev/flutter/flutter_driver/Ancestor-class.html
        /// </summary>
        /// <param name="matching"></param>
        /// <param name="matchRoot"></param>
        /// <param name="firstMatchOnly"></param>
        /// <returns></returns>
        FlutterElement FindElementByAncestor(FlutterBy matching, bool matchRoot = true, bool firstMatchOnly = false);
    }
    public interface IFindByDescendant
    {
        /// <summary>
        /// Find by Descendant, "of" field is current element
        /// https://api.flutter.dev/flutter/flutter_driver/Descendant-class.html
        /// </summary>
        /// <param name="matching"></param>
        /// <param name="matchRoot"></param>
        /// <param name="firstMatchOnly"></param>
        /// <returns></returns>
        FlutterElement FindElementByDescendant(FlutterBy matching, bool matchRoot = true, bool firstMatchOnly = false);
    }
    public interface IFindBySemanticsLabel
    {
        /// <summary>
        /// Find by semantic label.
        /// https://api.flutter.dev/flutter/flutter_driver/BySemanticsLabel-class.html
        /// </summary>
        /// <param name="label"></param>
        /// <returns></returns>
        FlutterElement FindElementBySemanticsLabel(string label);
    }
    public interface IFindByText
    {
        /// <summary>
        /// Find by text
        /// https://api.flutter.dev/flutter/flutter_driver/ByText-class.html
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        FlutterElement FindElementByText(string text);
    }
    public interface IFindByTooltipMessage
    {
        /// <summary>
        /// Find by tooltip message
        /// https://api.flutter.dev/flutter/flutter_driver/ByTooltipMessage-class.html
        /// </summary>
        /// <param name="tooltipMessage"></param>
        /// <returns></returns>
        FlutterElement FindElementByTooltipMessage(string tooltipMessage);
    }
    public interface IFindByValueKey
    {
        /// <summary>
        /// Find by value key
        /// https://api.flutter.dev/flutter/flutter_driver/ByValueKey-class.html
        /// </summary>
        /// <param name="valueKey"></param>
        /// <returns></returns>
        FlutterElement FindElementByValueKey(string valueKey);
    }
    public interface IFindByPageBack
    {
        /// <summary>
        /// Not sure what this actually does.
        /// Find by page back.
        /// https://api.flutter.dev/flutter/flutter_driver/PageBack-class.html
        /// </summary>
        /// <returns></returns>
        FlutterElement FindElementByPageBack();
    }
}
