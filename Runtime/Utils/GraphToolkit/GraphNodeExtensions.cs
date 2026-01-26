#if UNITY_GRAPHTOOLKIT
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Unity.GraphToolkit.Editor;

namespace BrewedCode.Utils.GraphToolkit
{
    public static class GraphNodeExtensions
    {
        public static INodeOption GetNodeOptionBy<TEnum>(
            this Node node, TEnum option) where TEnum : struct, Enum
        {
            if (node == null) throw new ArgumentNullException(nameof(node));

            var enumType = typeof(TEnum);
            if (!Enum.IsDefined(enumType, option))
                throw new InvalidEnumArgumentException(nameof(option), Convert.ToInt32(option), enumType);

            return node.GetNodeOption(Convert.ToInt32(option));
        }

        public static bool TryGetNodeOptionValue<TValue, TEnum>(
            this Node node, TEnum option, out TValue value) where TEnum : struct, Enum
            => node.GetNodeOptionBy(option).TryGetValue(out value);

        public static TValue GetNodeOptionValue<TValue, TEnum>(
            this Node node, TEnum option) where TEnum : struct, Enum
            => node.TryGetNodeOptionValue<TValue, TEnum>(option, out var v) ? v : default;

        public static bool TryGetNodeOptionValue<TValue>(
            this Node node, string optionName, out TValue value)
            => node.GetNodeOptionByName(optionName).TryGetValue(out value);

        public static TValue GetNodeOptionValue<TValue>(
            this Node node, string optionName)
            => node.TryGetNodeOptionValue<TValue>(optionName, out var v) ? v : default;

        //Assuming that port has only one connection
        public static INode GetNextNode(this INode node, IPort port)
        {
            var nextNodePort = port.firstConnectedPort;
            var nextNode = nextNodePort?.GetNode();
            return nextNode;
        }

        public static List<INode> GetNextNodes(this INode node, IPort port)
        {
            List<IPort> outConnectedPorts = new();
            port.GetConnectedPorts(outConnectedPorts);
            return outConnectedPorts.Select(currentPort => currentPort.GetNode()).ToList();
        }
    }
}
#endif
